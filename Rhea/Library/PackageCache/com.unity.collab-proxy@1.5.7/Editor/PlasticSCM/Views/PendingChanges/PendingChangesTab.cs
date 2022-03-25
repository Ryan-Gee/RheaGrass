using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.Client.BaseCommands;
using Codice.Client.Common.FsNodeReaders;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using Codice.LogWrapper;

using PlasticGui;
using PlasticGui.Help.Actions;
using PlasticGui.Help.Conditions;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.Diff;
using PlasticGui.WorkspaceWindow.Items;
using PlasticGui.WorkspaceWindow.Open;
using PlasticGui.WorkspaceWindow.PendingChanges;

using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.Help;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.UI.Tree;
using Unity.PlasticSCM.Editor.Views.PendingChanges.Dialogs;
using Unity.PlasticSCM.Editor.Views.PendingChanges.PendingMergeLinks;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetsOverlays;

namespace Unity.PlasticSCM.Editor.Views.PendingChanges
{
    internal class PendingChangesTab :
        IRefreshableView,
        PendingChangesViewMenu.IMetaMenuOperations,
        IPendingChangesMenuOperations,
        IOpenMenuOperations,
        IFilesFilterPatternsMenuOperations,
        PendingChangesOptionsDialog.IAutorefreshView
    {
        internal IProgressControls ProgressControlsForTesting { get { return mProgressControls; } }
        internal HelpPanel HelpPanelForTesting { get { return mHelpPanel; } }

        internal PendingChangesTab(
            WorkspaceInfo wkInfo,
            PlasticGUIClient plasticClient,
            bool isGluonMode,
            PlasticGui.WorkspaceWindow.PendingChanges.PendingChanges pendingChanges,
            NewIncomingChangesUpdater developerNewIncomingChangesUpdater,
            IHistoryViewLauncher historyViewLauncher,
            IAssetStatusCache assetStatusCache,
            EditorWindow parentWindow)
        {
            mWkInfo = wkInfo;
            mPlasticClient = plasticClient;
            mIsGluonMode = isGluonMode;
            mDeveloperNewIncomingChangesUpdater = developerNewIncomingChangesUpdater;
            mPendingChanges = pendingChanges;
            mHistoryViewLauncher = historyViewLauncher;
            mAssetStatusCache = assetStatusCache;
            mParentWindow = parentWindow;

            mNewChangesInWk = NewChangesInWk.Build(
                mWkInfo, new BuildWorkspacekIsRelevantNewChange());

            BuildComponents(plasticClient, isGluonMode, parentWindow);

            mProgressControls = new ProgressControlsForViews();

            plasticClient.RegisterPendingChangesGuiControls(
                mProgressControls,
                mPendingChangesTreeView,
                mMergeLinksListView);

            InitIgnoreRulesAndRefreshView(mWkInfo.ClientPath, this);
        }

        internal void AutoRefresh()
        {
            if (mIsAutoRefreshDisabled)
                return;

            if (!PlasticGuiConfig.Get().Configuration.CommitAutoRefresh)
                return;

            if (mPlasticClient.IsRefreshing() || mPlasticClient.IsOperationInProgress())
                return;

            if (mNewChangesInWk != null && !mNewChangesInWk.Detected())
                return;

            ((IRefreshableView)this).Refresh();
        }

        internal void OnDisable()
        {
            mSearchField.downOrUpArrowKeyPressed -=
                SearchField_OnDownOrUpArrowKeyPressed;

            TreeHeaderSettings.Save(
                mPendingChangesTreeView.multiColumnHeader.state,
                UnityConstants.PENDING_CHANGES_TABLE_SETTINGS_NAME);
        }

        internal void Update()
        {
            mProgressControls.UpdateProgress(mParentWindow);
        }

        internal void OnGUI()
        {
            DrawCommentTextArea.For(
                mPlasticClient,
                mParentWindow.position.width,
                mProgressControls.IsOperationRunning());

            DoOperationsToolbar(
                mPlasticClient,
                mWkInfo,
                mIsGluonMode,
                mAdvancedDropdownMenu,
                mProgressControls.IsOperationRunning());

            if (!string.IsNullOrEmpty(mPlasticClient.GluonWarningMessage))
                DoWarningMessage(mPlasticClient.GluonWarningMessage);

            DoActionsToolbar(
                mWkInfo,
                mIsGluonMode,
                this,
                mProgressControls,
                mParentWindow);

            DoChangesArea(
                mPendingChangesTreeView,
                mProgressControls.IsOperationRunning());

            if (mPlasticClient.HasPendingMergeLinks())
                DoMergeLinksArea(mMergeLinksListView, mParentWindow.position.width);

            if (mProgressControls.HasNotification())
            {
                DrawProgressForViews.ForNotificationArea(
                    mProgressControls.ProgressData);
            }

            DrawHelpPanel.For(mHelpPanel);
        }

        void IRefreshableView.Refresh()
        {
            if (!mAreIgnoreRulesInitialized)
                return;

            DrawAssetOverlay.ClearCache();

            mPlasticClient.GetPendingChanges(mNewChangesInWk);
        }

        void PendingChangesOptionsDialog.IAutorefreshView.DisableAutorefresh()
        {
            mIsAutoRefreshDisabled = true;
        }

        void PendingChangesOptionsDialog.IAutorefreshView.EnableAutorefresh()
        {
            mIsAutoRefreshDisabled = false;
        }

        bool PendingChangesViewMenu.IMetaMenuOperations.SelectionHasMeta()
        {
            return mPendingChangesTreeView.SelectionHasMeta();
        }

        void PendingChangesViewMenu.IMetaMenuOperations.DiffMeta()
        {
            ChangeInfo selectedChange = PendingChangesSelection
                .GetSelectedChange(mPendingChangesTreeView);
            ChangeInfo selectedChangeMeta = mPendingChangesTreeView.GetMetaChange(
                selectedChange);

            ChangeInfo changedForMoved = mPendingChanges.GetChangedForMoved(selectedChange);
            ChangeInfo changedForMovedMeta = (changedForMoved == null) ?
                null : mPendingChangesTreeView.GetMetaChange(changedForMoved);

            DiffOperation.DiffWorkspaceContent(
                mWkInfo,
                selectedChangeMeta,
                changedForMovedMeta,
                mProgressControls,
                null, null);
        }

        void PendingChangesViewMenu.IMetaMenuOperations.HistoryMeta()
        {
            ChangeInfo selectedChange = PendingChangesSelection
                .GetSelectedChange(mPendingChangesTreeView);
            ChangeInfo selectedChangeMeta = mPendingChangesTreeView.GetMetaChange(
                selectedChange);

            mHistoryViewLauncher.ShowHistoryView(
                selectedChangeMeta.RepositorySpec,
                selectedChangeMeta.RevInfo.ItemId,
                selectedChangeMeta.Path,
                selectedChangeMeta.IsDirectory);
        }

        void PendingChangesViewMenu.IMetaMenuOperations.OpenMeta()
        {
            List<string> selectedPaths = PendingChangesSelection
                .GetSelectedMetaPaths(mPendingChangesTreeView);

            FileSystemOperation.Open(selectedPaths);
        }

        void PendingChangesViewMenu.IMetaMenuOperations.OpenMetaWith()
        {
            List<string> selectedPaths = PendingChangesSelection
                .GetSelectedMetaPaths(mPendingChangesTreeView);

            OpenOperation.OpenWith(
                FileSystemOperation.GetExePath(),
                selectedPaths);
        }

        void PendingChangesViewMenu.IMetaMenuOperations.OpenMetaInExplorer()
        {
            List<string> selectedPaths = PendingChangesSelection
                .GetSelectedMetaPaths(mPendingChangesTreeView);

            if (selectedPaths.Count < 1)
                return;

            FileSystemOperation.OpenInExplorer(selectedPaths[0]);
        }

        SelectedChangesGroupInfo IPendingChangesMenuOperations.GetSelectedChangesGroupInfo()
        {
            return PendingChangesSelection.GetSelectedChangesGroupInfo(
                mPendingChangesTreeView);
        }

        void IPendingChangesMenuOperations.Diff()
        {
            ChangeInfo selectedChange = PendingChangesSelection
                .GetSelectedChange(mPendingChangesTreeView);

            DiffOperation.DiffWorkspaceContent(
                mWkInfo,
                selectedChange,
                mPendingChanges.GetChangedForMoved(selectedChange),
                null,
                null,
                null);
        }

        void IPendingChangesMenuOperations.UndoChanges()
        {
            List<ChangeInfo> changesToUndo = PendingChangesSelection
                .GetSelectedChanges(mPendingChangesTreeView);

            List<ChangeInfo> dependenciesCandidates =
                mPendingChangesTreeView.GetDependenciesCandidates(changesToUndo, true);

            LaunchOperation.UndoChangesForMode(
                mIsGluonMode, mPlasticClient, changesToUndo, dependenciesCandidates);
        }

        void IPendingChangesMenuOperations.SearchMatches()
        {
            ChangeInfo selectedChange = PendingChangesSelection
                .GetSelectedChange(mPendingChangesTreeView);

            if (selectedChange == null)
                return;

            SearchMatchesOperation operation = new SearchMatchesOperation(
                mWkInfo, mPlasticClient, mPlasticClient,
                mProgressControls, mDeveloperNewIncomingChangesUpdater);

            operation.SearchMatches(
                selectedChange,
                PendingChangesSelection.GetAllChanges(mPendingChangesTreeView));
        }

        void IPendingChangesMenuOperations.ApplyLocalChanges()
        {
            List<ChangeInfo> selectedChanges = PendingChangesSelection
                .GetSelectedChanges(mPendingChangesTreeView);

            if (selectedChanges.Count == 0)
                return;

            ApplyLocalChangesOperation operation = new ApplyLocalChangesOperation(
                mWkInfo, mPlasticClient, mPlasticClient,
                mProgressControls, mDeveloperNewIncomingChangesUpdater);

            operation.ApplyLocalChanges(
                selectedChanges,
                PendingChangesSelection.GetAllChanges(mPendingChangesTreeView));
        }

        void IPendingChangesMenuOperations.Delete()
        {
            List<string> privateDirectoriesToDelete;
            List<string> privateFilesToDelete;

            if (!mPendingChangesTreeView.GetSelectedPathsToDelete(
                    out privateDirectoriesToDelete,
                    out privateFilesToDelete))
                return;

            DeleteOperation.Delete(
                mPlasticClient, mProgressControls,
                privateDirectoriesToDelete, privateFilesToDelete,
                mDeveloperNewIncomingChangesUpdater,
                RefreshAsset.UnityAssetDatabase);
        }

        void IPendingChangesMenuOperations.Annotate()
        {
            throw new NotImplementedException();
        }

        void IPendingChangesMenuOperations.History()
        {
            ChangeInfo selectedChange = PendingChangesSelection.
                GetSelectedChange(mPendingChangesTreeView);

            mHistoryViewLauncher.ShowHistoryView(
                selectedChange.RepositorySpec,
                selectedChange.RevInfo.ItemId,
                selectedChange.Path,
                selectedChange.IsDirectory);
        }

        void IOpenMenuOperations.Open()
        {
            List<string> selectedPaths = PendingChangesSelection.
                GetSelectedPathsWithoutMeta(mPendingChangesTreeView);

            FileSystemOperation.Open(selectedPaths);
        }

        void IOpenMenuOperations.OpenWith()
        {
            List<string> selectedPaths = PendingChangesSelection.
                GetSelectedPathsWithoutMeta(mPendingChangesTreeView);

            OpenOperation.OpenWith(
                FileSystemOperation.GetExePath(),
                selectedPaths);
        }

        void IOpenMenuOperations.OpenWithCustom(string exePath, string args)
        {
            List<string> selectedPaths = PendingChangesSelection.
                GetSelectedPathsWithoutMeta(mPendingChangesTreeView);

            OpenOperation.OpenWith(exePath, selectedPaths);
        }

        void IOpenMenuOperations.OpenInExplorer()
        {
            List<string> selectedPaths = PendingChangesSelection
                .GetSelectedPathsWithoutMeta(mPendingChangesTreeView);

            if (selectedPaths.Count < 1)
                return;

            FileSystemOperation.OpenInExplorer(selectedPaths[0]);
        }

        void IFilesFilterPatternsMenuOperations.AddFilesFilterPatterns(
            FilterTypes type, FilterActions action, FilterOperationType operation)
        {
            List<string> selectedPaths = PendingChangesSelection.GetSelectedPaths(
                mPendingChangesTreeView);

            string[] rules = FilterRulesGenerator.GenerateRules(
                selectedPaths, mWkInfo.ClientPath, action, operation);

            bool isApplicableToAllWorkspaces = !mIsGluonMode;
            bool isAddOperation = operation == FilterOperationType.Add;

            FilterRulesConfirmationData filterRulesConfirmationData = 
                FilterRulesConfirmationDialog.AskForConfirmation(
                    rules, isAddOperation, isApplicableToAllWorkspaces, mParentWindow);

            AddFilesFilterPatternsOperation.Run(
                mWkInfo, mPlasticClient, type, operation, filterRulesConfirmationData);
        }

        void SearchField_OnDownOrUpArrowKeyPressed()
        {
            mPendingChangesTreeView.SetFocusAndEnsureSelectedItem();
        }

        void InitIgnoreRulesAndRefreshView(
            string wkPath, IRefreshableView view)
        {
            IThreadWaiter waiter = ThreadWaiter.GetWaiter(10);
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    if (IsIgnoreConfigDefined.For(wkPath))
                        return;

                    AddIgnoreRules.WriteRules(
                        wkPath, UnityConditions.GetMissingIgnoredRules(wkPath));
                },
                /*afterOperationDelegate*/ delegate
                {
                    mAreIgnoreRulesInitialized = true;

                    view.Refresh();

                    if (waiter.Exception == null)
                        return;

                    mLog.ErrorFormat(
                        "Error adding ignore rules for Unity: {0}",
                        waiter.Exception);

                    mLog.DebugFormat(
                        "Stack trace: {0}",
                        waiter.Exception.StackTrace);
                });
        }

        static void DoOperationsToolbar(
            PlasticGUIClient plasticClient,
            WorkspaceInfo wkInfo,
            bool isGluonMode,
            GenericMenu advancedDropdownMenu,
            bool isOperationRunning)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUISpace.ForToolbar();

            using (new GuiEnabled(!isOperationRunning))
            {
                using (new GuiEnabled(!plasticClient.IsCommentWarningNeeded))
                {
                    var checkinchanges = PlasticLocalization.GetString(PlasticLocalization.Name.CheckinChanges);
                    if (DrawActionButton.For(checkinchanges))
                    {
                        LaunchOperation.CheckinForMode(
                            wkInfo, isGluonMode, plasticClient.KeepItemsLocked,
                            plasticClient);
                    }
                }

                var undochanges = PlasticLocalization.GetString(PlasticLocalization.Name.UndoChanges);
                if (DrawActionButton.For(undochanges))
                {
                    LaunchOperation.UndoForMode(isGluonMode, plasticClient);
                }

                if (isGluonMode)
                {
                    plasticClient.KeepItemsLocked = EditorGUILayout.ToggleLeft(
                        PlasticLocalization.GetString(PlasticLocalization.Name.KeepLocked),
                        plasticClient.KeepItemsLocked,
                        GUILayout.Width(UnityConstants.EXTRA_LARGE_BUTTON_WIDTH));
                }
                //TODO: Codice - beta: hide the advanced menu until the behavior is implemented
                /*else
                {
                    var dropDownContent = new GUIContent(string.Empty);
                    var dropDownRect = GUILayoutUtility.GetRect(
                        dropDownContent, EditorStyles.toolbarDropDown);

                    if (EditorGUI.DropdownButton(dropDownRect, dropDownContent,
                            FocusType.Passive, EditorStyles.toolbarDropDown))
                        advancedDropdownMenu.DropDown(dropDownRect);
                }*/
            }

            if (plasticClient.IsCommentWarningNeeded)
            {
                GUILayout.Space(5);
                DoCheckinWarningMessage();
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        static void DoWarningMessage(string message)
        {
            GUILayout.Label(message, UnityStyles.WarningMessage);
        }

        static void DoCheckinWarningMessage()
        {
            string label = PlasticLocalization.GetString(PlasticLocalization.Name.PleaseComment);

            GUILayout.Label(
                new GUIContent(label, Images.GetWarnIcon()),
                UnityStyles.PendingChangesTab.CommentWarningIcon);
        }

        void DoActionsToolbar(
            WorkspaceInfo workspaceInfo,
            bool isGluonMode,
            IRefreshableView refreshableView,
            ProgressControlsForViews progressControls,
            EditorWindow editorWindow)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            string checkinHeader = string.Format(
                PlasticLocalization.GetString(PlasticLocalization.Name.CheckinHeader),
                mPendingChangesTreeView.GetSelectedItemCount(),
                mPendingChangesTreeView.GetTotalItemCount());

            GUILayout.Label(checkinHeader, UnityStyles.PendingChangesTab.HeaderLabel);

            if (progressControls.IsOperationRunning())
            {
                DrawProgressForViews.ForIndeterminateProgress(
                    progressControls.ProgressData);
            }

            GUILayout.FlexibleSpace();

            DrawSearchField.For(
                mSearchField,
                mPendingChangesTreeView,
                UnityConstants.SEARCH_FIELD_WIDTH);

            if (GUILayout.Button(PlasticLocalization.GetString(
                    PlasticLocalization.Name.Options),
                    EditorStyles.toolbarButton,
                    GUILayout.Width(UnityConstants.REGULAR_BUTTON_WIDTH)))
            {
                PendingChangesOptionsDialog.ChangeOptions(
                    workspaceInfo, refreshableView, this, editorWindow);
                GUIUtility.ExitGUI();
            }

            DoRefreshButton(
                refreshableView,
                progressControls.IsOperationRunning());

            EditorGUILayout.EndHorizontal();
        }

        static void DoChangesArea(
            PendingChangesTreeView changesTreeView,
            bool isOperationRunning)
        {
            GUI.enabled = !isOperationRunning;

            Rect rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);

            changesTreeView.OnGUI(rect);

            GUI.enabled = true;
        }

        static void DoMergeLinksArea(
            MergeLinksListView mergeLinksListView, float width)
        {
            GUILayout.Label(
                PlasticLocalization.GetString(
                    PlasticLocalization.Name.MergeLinkDescriptionColumn),
                EditorStyles.boldLabel);

            float desiredTreeHeight = mergeLinksListView.DesiredHeight;

            Rect treeRect = GUILayoutUtility.GetRect(
                0, width, desiredTreeHeight, desiredTreeHeight);

            mergeLinksListView.OnGUI(treeRect);
        }

        static void DoRefreshButton(
            IRefreshableView refreshableView,
            bool isOperationRunning)
        {
            EditorGUI.BeginDisabledGroup(isOperationRunning);

            if (GUILayout.Button(new GUIContent(
                    Images.GetRefreshIcon()), EditorStyles.toolbarButton))
                refreshableView.Refresh();

            EditorGUI.EndDisabledGroup();
        }

        void BuildComponents(
            PlasticGUIClient plasticClient,
            bool isGluonMode,
            EditorWindow plasticWindow)
        {
            mHelpPanel = new HelpPanel(plasticWindow);

            mAdvancedDropdownMenu = new GenericMenu();
            mAdvancedDropdownMenu.AddItem(new GUIContent(
                PlasticLocalization.GetString(PlasticLocalization.Name.UndoUnchangedButton)),
                false, () => { });

            mSearchField = new SearchField();
            mSearchField.downOrUpArrowKeyPressed += SearchField_OnDownOrUpArrowKeyPressed;

            PendingChangesTreeHeaderState headerState =
                PendingChangesTreeHeaderState.GetDefault(isGluonMode);
            TreeHeaderSettings.Load(headerState,
                UnityConstants.PENDING_CHANGES_TABLE_SETTINGS_NAME,
                (int)PendingChangesTreeColumn.Item, true);

            mPendingChangesTreeView = new PendingChangesTreeView(
                mWkInfo, mIsGluonMode, headerState,
                PendingChangesTreeHeaderState.GetColumnNames(),
                new PendingChangesViewMenu(this, this, this, this),
                mAssetStatusCache);
            mPendingChangesTreeView.Reload();

            mMergeLinksListView = new MergeLinksListView();
            mMergeLinksListView.Reload();
        }

        SearchField mSearchField;
        INewChangesInWk mNewChangesInWk;
        GenericMenu mAdvancedDropdownMenu;

        PendingChangesTreeView mPendingChangesTreeView;
        MergeLinksListView mMergeLinksListView;
        HelpPanel mHelpPanel;

        bool mIsAutoRefreshDisabled;

        volatile bool mAreIgnoreRulesInitialized = false;

        readonly ProgressControlsForViews mProgressControls;
        readonly EditorWindow mParentWindow;
        readonly IAssetStatusCache mAssetStatusCache;
        readonly IHistoryViewLauncher mHistoryViewLauncher;
        readonly PlasticGui.WorkspaceWindow.PendingChanges.PendingChanges mPendingChanges;
        readonly NewIncomingChangesUpdater mDeveloperNewIncomingChangesUpdater;
        readonly bool mIsGluonMode;
        readonly PlasticGUIClient mPlasticClient;
        readonly WorkspaceInfo mWkInfo;

        static readonly ILog mLog = LogManager.GetLogger("PendingChangesTab");
    }
}
