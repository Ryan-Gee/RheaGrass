using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using Codice.Client.BaseCommands;
using Codice.Client.Commands;
using Codice.Client.Commands.CheckIn;
using Codice.Client.Common;
using Codice.Client.Common.FsNodeReaders;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using Codice.CM.Common.Merge;
using Codice.CM.Common.Replication;
using GluonGui;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.PendingChanges;
using PlasticGui.WorkspaceWindow.Replication;
using PlasticGui.WorkspaceWindow.Update;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.Configuration;
using Unity.PlasticSCM.Editor.Developer.UpdateReport;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.Views.PendingChanges;
using Unity.PlasticSCM.Editor.Views.PendingChanges.Dialogs;
using Unity.PlasticSCM.Editor.Views.PendingChanges.PendingMergeLinks;

using GluonNewIncomingChangesUpdater = PlasticGui.Gluon.WorkspaceWindow.NewIncomingChangesUpdater;

namespace Unity.PlasticSCM.Editor
{
    internal partial class PlasticGUIClient :
        IPendingChangesView, IWorkspaceWindow, IUpdateReport
    {
        internal void SetUpdateNotifierForTesting(UpdateNotifier updateNotifier)
        {
            mUpdateNotifierForTesting = updateNotifier;
        }

        internal void SetMergeLinksForTesting(
            IDictionary<MountPoint, IList<PendingMergeLink>> mergeLinks)
        {
            mPendingMergeLinks = mergeLinks;

            UpdateMergeLinksList();
        }
        internal OperationProgressData Progress { get { return mOperationProgressData; } }
        internal string HeaderTitle { get; private set; }
        internal string CommentText { get; set; }
        internal bool KeepItemsLocked { get; set; }
        internal bool ForceToShowComment { get; set; }
        internal bool IsCommentWarningNeeded { get; set; }
        internal string GluonWarningMessage { get; private set; }

        internal Gluon.ProgressOperationHandler GluonProgressOperationHandler
        {
            get { return mGluonProgressOperationHandler; }
        }

        internal PlasticGUIClient(
            WorkspaceInfo wkInfo,
            ViewSwitcher switcher,
            IMergeViewLauncher mergeViewLauncher,
            ViewHost viewHost,
            PlasticGui.WorkspaceWindow.PendingChanges.PendingChanges pendingChanges,
            NewIncomingChangesUpdater developerNewIncomingChangesUpdater,
            GluonNewIncomingChangesUpdater gluonNewIncomingChangesUpdater,
            EditorWindow parentWindow,
            GuiMessage.IGuiMessage guiMessage)
        {
            mWkInfo = wkInfo;
            mSwitcher = switcher;
            mMergeViewLauncher = mergeViewLauncher;
            mViewHost = viewHost;
            mPendingChanges = pendingChanges;
            mDeveloperNewIncomingChangesUpdater = developerNewIncomingChangesUpdater;
            mGluonNewIncomingChangesUpdater = gluonNewIncomingChangesUpdater;
            mPlasticWindow = parentWindow;
            mGuiMessage = guiMessage;

            ((IWorkspaceWindow)this).UpdateTitle();

            mCheckedStateManager = new CheckedStateManager();

            mDeveloperProgressOperationHandler = new Developer.ProgressOperationHandler(mWkInfo, this);
            mGluonProgressOperationHandler = new Gluon.ProgressOperationHandler(this);
        }

        internal void RegisterPendingChangesGuiControls(
            ProgressControlsForViews progressControls,
            PendingChangesTreeView changesTreeView,
            MergeLinksListView mergeLinksListView)
        {
            mProgressControls = progressControls;
            mChangesTreeView = changesTreeView;
            mMergeLinksListView = mergeLinksListView;

            mPendingChangesOperations = new PendingChangesOperations(
                mWkInfo, this, mSwitcher, mMergeViewLauncher,
                this, mProgressControls, this, null, null, null);
        }

        internal bool HasPendingMergeLinks()
        {
            if (mPendingMergeLinks == null)
                return false;

            return mPendingMergeLinks.Count > 0;
        }

        internal void UpdateIsCommentWarningNeeded(string comment)
        {
            IsCommentWarningNeeded = string.IsNullOrEmpty(comment)
                && mPendingChanges.HasPendingChanges();
        }

        internal bool IsOperationInProgress()
        {
            return mDeveloperProgressOperationHandler.IsOperationInProgress()
                || mGluonProgressOperationHandler.IsOperationInProgress();
        }

        internal bool IsRefreshing()
        {
            return mIsRefreshing;
        }

        internal void CancelCurrentOperation()
        {
            if (mDeveloperProgressOperationHandler.IsOperationInProgress())
            {
                mDeveloperProgressOperationHandler.CancelCheckinProgress();
                return;
            }

            if (mGluonProgressOperationHandler.IsOperationInProgress())
            {
                mGluonProgressOperationHandler.CancelUpdateProgress();
                return;
            }
        }

        internal void Checkin()
        {
            List<ChangeInfo> changesToCheckin;
            List<ChangeInfo> dependenciesCandidates;

            mChangesTreeView.GetCheckedChanges(
                false, out changesToCheckin, out dependenciesCandidates);

            if (CheckEmptyOperation(changesToCheckin, HasPendingMergeLinks()))
            {
                mProgressControls.ShowWarning(
                    PlasticLocalization.GetString(PlasticLocalization.Name.NoItemsAreSelected));
                return;
            }

            bool isCancelled;
            SaveAssets.ForChangesWithConfirmation(changesToCheckin, out isCancelled);

            if (isCancelled)
                return;

            mPendingChangesOperations.Checkin(
                changesToCheckin,
                dependenciesCandidates,
                CommentText,
                null,
                RefreshAsset.UnityAssetDatabase);
        }

        internal void Undo()
        {
            List<ChangeInfo> changesToUndo;
            List<ChangeInfo> dependenciesCandidates;

            mChangesTreeView.GetCheckedChanges(
                true, out changesToUndo, out dependenciesCandidates);

            UndoChanges(changesToUndo, dependenciesCandidates);
        }

        internal void UndoChanges(
            List<ChangeInfo> changesToUndo,
            List<ChangeInfo> dependenciesCandidates)
        {
            if (CheckEmptyOperation(changesToUndo, HasPendingMergeLinks()))
            {
                mProgressControls.ShowWarning(
                    PlasticLocalization.GetString(PlasticLocalization.Name.NoItemsToUndo));
                return;
            }

            SaveAssets.ForChangesWithoutConfirmation(changesToUndo);

            mPendingChangesOperations.Undo(
                changesToUndo,
                dependenciesCandidates,
                mPendingMergeLinks.Count,
                RefreshAsset.UnityAssetDatabase);
        }

        internal void UpdateWorkspace()
        {
            UpdateWorkspaceOperation update = new UpdateWorkspaceOperation(
                mWkInfo, this, mSwitcher, mMergeViewLauncher, this,
                mDeveloperNewIncomingChangesUpdater,
                null);

            update.Run(
                UpdateWorkspaceOperation.UpdateType.UpdateToLatest,
                RefreshAsset.UnityAssetDatabase);
        }

        internal void GetPendingChanges(INewChangesInWk newChangesInWk)
        {
            if (mDeveloperNewIncomingChangesUpdater != null)
                mDeveloperNewIncomingChangesUpdater.Update();

            if (mGluonNewIncomingChangesUpdater != null)
                mGluonNewIncomingChangesUpdater.Update(DateTime.Now);

            FillPendingChanges(newChangesInWk);
        }

        internal void OnParentUpdated(double elapsedSeconds)
        {
            if (IsOperationInProgress() || mRequestedRepaint)
            {
                if (mDeveloperProgressOperationHandler.IsOperationInProgress())
                    mDeveloperProgressOperationHandler.Update(elapsedSeconds);

                mPlasticWindow.Repaint();

                mRequestedRepaint = false;
            }
        }

        void FillPendingChanges(INewChangesInWk newChangesInWk)
        {
            if (mIsRefreshing)
                return;

            mIsRefreshing = true;

            List<ChangeInfo> changesToSelect =
                PendingChangesSelection.GetChangesToFocus(mChangesTreeView);

            mProgressControls.ShowProgress(PlasticLocalization.GetString(
                PlasticLocalization.Name.LoadingPendingChanges));

            IDictionary<MountPoint, IList<PendingMergeLink>> mergeLinks = null;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    FilterManager.Get().Reload();

                    WorkspaceStatusOptions options = WorkspaceStatusOptions.None;
                    options |= WorkspaceStatusOptions.FindAdded;
                    options |= WorkspaceStatusOptions.FindDeleted;
                    options |= WorkspaceStatusOptions.FindMoved;
                    options |= WorkspaceStatusOptions.SplitModifiedMoved;
                    options |= PendingChangesOptions.GetWorkspaceStatusOptions();

                    if (newChangesInWk != null)
                        newChangesInWk.Detected();

                    mPendingChanges.Calculate(
                        options, PendingChangesOptions.GetMovedMatchingOptions());

                    mergeLinks = Plastic.API.GetPendingMergeLinks(mWkInfo);
                },
                /*afterOperationDelegate*/ delegate
                {
                    mPendingMergeLinks = mergeLinks;

                    try
                    {
                        if (waiter.Exception != null)
                        {
                            ExceptionsHandler.DisplayException(waiter.Exception);
                            return;
                        }

                        UpdateChangesTree();

                        UpdateMergeLinksList();

                        PendingChangesSelection.SelectChanges(
                            mChangesTreeView, changesToSelect);
                    }
                    finally
                    {
                        mProgressControls.HideProgress();

                        UpdateIsCommentWarningNeeded(CommentText);

                        UpdateNotificationPanel();

                        mIsRefreshing = false;
                    }
                });
        }

        void UpdateChangesTree()
        {
            mChangesTreeView.BuildModel(mPendingChanges, mCheckedStateManager);

            mChangesTreeView.Refilter();

            mChangesTreeView.Sort();

            mChangesTreeView.Reload();
        }

        void UpdateMergeLinksList()
        {
            mMergeLinksListView.BuildModel(mPendingMergeLinks);

            mMergeLinksListView.Reload();
        }

        void UpdateNotificationPanel()
        {
            if (Plastic.API.IsFsReaderWatchLimitReached(mWkInfo))
            {
                mProgressControls.ShowWarning(PlasticLocalization.GetString(
                    PlasticLocalization.Name.NotifyLinuxWatchLimitWarning));
                return;
            }
        }

        void IUpdateReport.Show(WorkspaceInfo wkInfo, IList reportLines)
        {
            UpdateReportDialog.ShowReportDialog(
                wkInfo,
                reportLines,
                mPlasticWindow);
        }

        void IWorkspaceWindow.RefreshView(ViewType viewType)
        {
            mSwitcher.RefreshView(viewType);
        }

        void IWorkspaceWindow.UpdateTitle()
        {
            string title = string.Empty;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    title = GetTitle(mWkInfo);
                },
                /*afterOperationDelegate*/ delegate
                {
                    if (waiter.Exception != null)
                        return;

                    HeaderTitle = title;

                    RequestRepaint();
                });
        }

        bool IWorkspaceWindow.CheckOperationInProgress()
        {
            return mDeveloperProgressOperationHandler.CheckOperationInProgress();
        }

        void IWorkspaceWindow.ShowUpdateProgress(string title, UpdateNotifier notifier)
        {
            mDeveloperProgressOperationHandler.ShowUpdateProgress(title, mUpdateNotifierForTesting ?? notifier);
        }

        void IWorkspaceWindow.EndUpdateProgress()
        {
            mDeveloperProgressOperationHandler.EndUpdateProgress();
        }

        void IWorkspaceWindow.ShowCheckinProgress()
        {
            mDeveloperProgressOperationHandler.ShowCheckinProgress();
        }

        void IWorkspaceWindow.EndCheckinProgress()
        {
            mDeveloperProgressOperationHandler.EndCheckinProgress();
        }

        void IWorkspaceWindow.RefreshCheckinProgress(
            CheckinStatus checkinStatus,
            BuildProgressSpeedAndRemainingTime.ProgressData progressData)
        {
            mDeveloperProgressOperationHandler.
                RefreshCheckinProgress(checkinStatus, progressData);
        }

        bool IWorkspaceWindow.HasCheckinCancelled()
        {
            return mDeveloperProgressOperationHandler.HasCheckinCancelled();
        }

        void IWorkspaceWindow.ShowReplicationProgress(IReplicationOperation replicationOperation)
        {
            throw new NotImplementedException();
        }

        void IWorkspaceWindow.RefreshReplicationProgress(BranchReplicationData replicationData, ReplicationStatus replicationStatus, int current, int total)
        {
            throw new NotImplementedException();
        }

        void IWorkspaceWindow.EndReplicationProgress(ReplicationStatus replicationStatus)
        {
            throw new NotImplementedException();
        }

        void IWorkspaceWindow.ShowProgress()
        {
            mDeveloperProgressOperationHandler.ShowProgress();
        }

        void IWorkspaceWindow.ShowProgress(IProgressOperation progressOperation)
        {
            throw new NotImplementedException();
        }

        void IWorkspaceWindow.RefreshProgress(ProgressData progressData)
        {
            mDeveloperProgressOperationHandler.RefreshProgress(progressData);
        }

        void IWorkspaceWindow.EndProgress()
        {
            mDeveloperProgressOperationHandler.EndProgress();
        }

        EncryptionConfigurationDialogData IWorkspaceWindow.RequestEncryptionPassword(string server)
        {
            return EncryptionConfigurationDialog.RequestEncryptionPassword(server, mPlasticWindow);
        }

        void IPendingChangesView.ClearChangesToCheck(List<string> changes)
        {
            mCheckedStateManager.ClearChangesToCheck(changes);
            RequestRepaint();
        }

        void IPendingChangesView.CleanCheckedElements(List<ChangeInfo> checkedChanges)
        {
            mCheckedStateManager.Clean(checkedChanges);
            RequestRepaint();
        }

        void IPendingChangesView.CheckChanges(List<string> changesToCheck)
        {
            mCheckedStateManager.SetChangesToCheck(changesToCheck);
            RequestRepaint();
        }

        bool IPendingChangesView.IncludeDependencies(
            IList<ChangeDependencies<ChangeInfo>> changesDependencies,
            string operation)
        {
            return DependenciesDialog.IncludeDependencies(
                mWkInfo, changesDependencies, operation, mPlasticWindow);
        }

        CheckinMergeNeededData IPendingChangesView.CheckinMergeNeeded()
        {
            return CheckinMergeNeededDialog.Merge(mWkInfo, mPlasticWindow);
        }

        void IPendingChangesView.ClearComments()
        {
            ClearComments();
        }

        SearchMatchesData IPendingChangesView.AskForMatches(string changePath)
        {
            throw new NotImplementedException();
        }

        void IPendingChangesView.CleanLinkedTasks()
        {
        }

        internal void RequestRepaint()
        {
            mRequestedRepaint = true;
        }

        void ClearComments()
        {
            CommentText = string.Empty;
            ForceToShowComment = true;
            RequestRepaint();
        }

        static string GetTitle(WorkspaceInfo wkInfo)
        {
            WorkspaceStatusString.Data wkStatusData =
                WorkspaceStatusString.GetSelectorData(wkInfo);

            return string.Format("{0} {1} @ {2} @ {3}",
                wkStatusData.ObjectName,
                wkStatusData.ObjectSpec,
                wkStatusData.RepositoryName,
                wkStatusData.Server);
        }

        static bool CheckEmptyOperation(List<ChangeInfo> elements, bool bHasPendingMergeLinks)
        {
            if (bHasPendingMergeLinks)
                return false;

            if (elements != null && elements.Count > 0)
                return false;

            return true;
        }

        bool mIsRefreshing;
        bool mRequestedRepaint;

        UpdateNotifier mUpdateNotifierForTesting;

        OperationProgressData mOperationProgressData = new OperationProgressData();
        IProgressControls mProgressControls;
        IDictionary<MountPoint, IList<PendingMergeLink>> mPendingMergeLinks;
        PendingChangesTreeView mChangesTreeView;
        MergeLinksListView mMergeLinksListView;
        PendingChangesOperations mPendingChangesOperations;

        readonly GluonNewIncomingChangesUpdater mGluonNewIncomingChangesUpdater;
        readonly NewIncomingChangesUpdater mDeveloperNewIncomingChangesUpdater;
        readonly PlasticGui.WorkspaceWindow.PendingChanges.PendingChanges mPendingChanges;
        readonly Developer.ProgressOperationHandler mDeveloperProgressOperationHandler;
        readonly CheckedStateManager mCheckedStateManager;
        readonly Gluon.ProgressOperationHandler mGluonProgressOperationHandler;
        readonly GuiMessage.IGuiMessage mGuiMessage;
        readonly EditorWindow mPlasticWindow;
        readonly ViewSwitcher mSwitcher;
        readonly IMergeViewLauncher mMergeViewLauncher;
        readonly ViewHost mViewHost;
        readonly WorkspaceInfo mWkInfo;
    }
}
