using System;
using System.Collections.Generic;

using Codice.Client.BaseCommands;
using Codice.Client.Common;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using GluonGui;
using GluonGui.WorkspaceWindow.Views;
using GluonGui.WorkspaceWindow.Views.Checkin.Operations;
using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer;
using PlasticGui;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.Gluon.UpdateReport;
using Unity.PlasticSCM.Editor.Views.PendingChanges.Dialogs;

using IGluonUpdateReport = PlasticGui.Gluon.IUpdateReport;

namespace Unity.PlasticSCM.Editor
{
    internal partial class PlasticGUIClient :
        CheckinUIOperation.ICheckinView, IGluonUpdateReport
    {
        internal void PartialCheckin(bool keepItemsLocked)
        {
            List<ChangeInfo> changesToCheckin;
            List<ChangeInfo> dependenciesCandidates;

            mChangesTreeView.GetCheckedChanges(
                false, out changesToCheckin, out dependenciesCandidates);

            if (CheckEmptyOperation(changesToCheckin))
            {
                mProgressControls.ShowWarning(
                    PlasticLocalization.GetString(PlasticLocalization.Name.NoItemsAreSelected));
                return;
            }

            bool isCancelled;
            SaveAssets.ForChangesWithConfirmation(changesToCheckin, out isCancelled);

            if (isCancelled)
                return;

            CheckinUIOperation ciOperation = new CheckinUIOperation(
                mWkInfo, mViewHost, mProgressControls, mGuiMessage,
                new LaunchCheckinConflictsDialog(mPlasticWindow),
                new LaunchDependenciesDialog(
                    PlasticLocalization.GetString(PlasticLocalization.Name.CheckinButton),
                    mPlasticWindow),
                this, mGluonProgressOperationHandler);

            ciOperation.Checkin(
                changesToCheckin,
                dependenciesCandidates,
                CommentText,
                keepItemsLocked,
                RefreshAsset.UnityAssetDatabase);
        }

        internal void PartialUndo()
        {
            List<ChangeInfo> changesToUndo;
            List<ChangeInfo> dependenciesCandidates;

            mChangesTreeView.GetCheckedChanges(
                true, out changesToUndo, out dependenciesCandidates);

            PartialUndoChanges(changesToUndo, dependenciesCandidates);
        }

        internal void PartialUndoChanges(
            List<ChangeInfo> changesToUndo,
            List<ChangeInfo> dependenciesCandidates)
        {
            if (CheckEmptyOperation(changesToUndo))
            {
                mProgressControls.ShowWarning(
                    PlasticLocalization.GetString(PlasticLocalization.Name.NoItemsToUndo));
                return;
            }

            SaveAssets.ForChangesWithoutConfirmation(changesToUndo);

            UndoUIOperation undoOperation = new UndoUIOperation(
                mWkInfo, mViewHost,
                new LaunchDependenciesDialog(
                    PlasticLocalization.GetString(PlasticLocalization.Name.UndoButton),
                    mPlasticWindow),
                mProgressControls, mGuiMessage);

            undoOperation.Undo(
                changesToUndo,
                dependenciesCandidates,
                RefreshAsset.UnityAssetDatabase);
        }

        internal void PartialUpdateWorkspace()
        {
            mProgressControls.ShowProgress(PlasticLocalization.GetString(
                PlasticLocalization.Name.UpdatingWorkspace));

            ((IUpdateProgress)mGluonProgressOperationHandler).ShowCancelableProgress();

            OutOfDateUpdater outOfDateUpdater = new OutOfDateUpdater(mWkInfo);

            BuildProgressSpeedAndRemainingTime.ProgressData progressData =
                new BuildProgressSpeedAndRemainingTime.ProgressData(DateTime.Now);

            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    outOfDateUpdater.Execute();
                },
                /*afterOperationDelegate*/ delegate
                {
                    mProgressControls.HideProgress();

                    ((IUpdateProgress)mGluonProgressOperationHandler).EndProgress();

                    mViewHost.RefreshView(ViewType.CheckinView);
                    mViewHost.RefreshView(ViewType.IncomingChangesView);

                    RefreshAsset.UnityAssetDatabase();

                    if (waiter.Exception != null)
                    {
                        ExceptionsHandler.DisplayException(waiter.Exception);
                        return;
                    }

                    ShowUpdateReportDialog(
                        mWkInfo, mViewHost, outOfDateUpdater.Progress, mProgressControls,
                        mGuiMessage, mGluonProgressOperationHandler, this);
                },
                /*timerTickDelegate*/ delegate
                {
                    UpdateProgress progress = outOfDateUpdater.Progress;

                    if (progress == null)
                        return;

                    if (progress.IsCanceled)
                    {
                        mProgressControls.ShowNotification(
                            PlasticLocalization.GetString(PlasticLocalization.Name.Canceling));
                    }

                    ((IUpdateProgress)mGluonProgressOperationHandler).RefreshProgress(
                        progress,
                        UpdateProgressDataCalculator.CalculateProgressForWorkspaceUpdate(
                            mWkInfo.ClientPath, progress, progressData));
                });
        }

        static void ShowUpdateReportDialog(
            WorkspaceInfo wkInfo,
            ViewHost viewHost,
            UpdateProgress progress,
            IProgressControls progressControls,
            GuiMessage.IGuiMessage guiMessage,
            IUpdateProgress updateProgress,
            IGluonUpdateReport updateReport)
        {
            if (progress.ErrorMessages.Count == 0)
                return;

            UpdateReportResult updateReportResult =
                updateReport.ShowUpdateReport(wkInfo, progress.ErrorMessages);

            if (!updateReportResult.IsUpdateForcedRequested())
                return;

            UpdateForcedOperation updateForced = new UpdateForcedOperation(
                wkInfo, viewHost, progress, progressControls,
                guiMessage, updateProgress, updateReport);

            updateForced.UpdateForced(
                updateReportResult.UpdateForcedPaths,
                updateReportResult.UnaffectedErrors);
        }

        void CheckinUIOperation.ICheckinView.ClearComments()
        {
            ClearComments();
        }

        void CheckinUIOperation.ICheckinView.CollapseWarningMessagePanel()
        {
            GluonWarningMessage = string.Empty;
            RequestRepaint();
        }

        void CheckinUIOperation.ICheckinView.ExpandWarningMessagePanel(string text)
        {
            GluonWarningMessage = text;
            RequestRepaint();
        }

        static bool CheckEmptyOperation(List<ChangeInfo> elements)
        {
            return elements == null || elements.Count == 0;
        }

        UpdateReportResult IGluonUpdateReport.ShowUpdateReport(
            WorkspaceInfo wkInfo, List<ErrorMessage> errors)
        {
            return UpdateReportDialog.ShowUpdateReport(
                wkInfo, errors, mPlasticWindow);
        }

        void IGluonUpdateReport.AppendReport(string updateReport)
        {
            throw new NotImplementedException();
        }
    }
}
