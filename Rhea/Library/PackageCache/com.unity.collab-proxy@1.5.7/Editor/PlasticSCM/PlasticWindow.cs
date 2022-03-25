using System;

using UnityEditor;
using UnityEngine;

using Codice.Client.BaseCommands.EventTracking;
using Codice.Client.Common;
using Codice.Client.Common.Encryption;
using Codice.Client.Common.EventTracking;
using Codice.Client.Common.FsNodeReaders;
using Codice.Client.Common.FsNodeReaders.Watcher;
using Codice.Client.Common.Threading;
using Codice.Client.Common.WebApi;
using Codice.CM.Common;
using Codice.LogWrapper;
using CodiceApp.EventTracking;
using GluonGui;
using PlasticGui;
using PlasticGui.Gluon;
using PlasticGui.WebApi;
using Unity.PlasticSCM.Editor.AssetMenu;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetsOverlays;
using Unity.PlasticSCM.Editor.AssetUtils.Processor;
using Unity.PlasticSCM.Editor.Configuration;
using Unity.PlasticSCM.Editor.Inspector;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Avatar;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.Views.CreateWorkspace;
using Unity.PlasticSCM.Editor.Views.Welcome;

using GluonCheckIncomingChanges = PlasticGui.Gluon.WorkspaceWindow.CheckIncomingChanges;
using GluonNewIncomingChangesUpdater = PlasticGui.Gluon.WorkspaceWindow.NewIncomingChangesUpdater;
using EventTracking = PlasticGui.EventTracking.EventTracking;
using Codice.Client.Common.Connection;

namespace Unity.PlasticSCM.Editor
{
    internal class PlasticWindow : EditorWindow,
        PlasticGui.WorkspaceWindow.CheckIncomingChanges.IAutoRefreshIncomingChangesView,
        GluonCheckIncomingChanges.IAutoRefreshIncomingChangesView,
        CreateWorkspaceView.ICreateWorkspaceListener
    {
        internal PlasticGUIClient PlasticClientForTesting { get { return mPlasticClient; } }
        internal ViewSwitcher ViewSwitcherForTesting { get { return mViewSwitcher; } }
        internal IPlasticAPI PlasticApiForTesting { get { return mPlasticAPI; } }
        internal IPlasticWebRestApi PlasticWebRestApiForTesting { get { return mPlasticWebRestApi; } }

        internal void SetupWindowTitle()
        {
            titleContent = new GUIContent(
                UnityConstants.PLASTIC_WINDOW_TITLE,
                Images.GetImage(Images.Name.IconPlasticView));
        }

        internal void DisableCollabIfEnabledWhenLoaded()
        {
            mDisableCollabIfEnabledWhenLoaded = true;
        }

        void PlasticGui.WorkspaceWindow.CheckIncomingChanges.IAutoRefreshIncomingChangesView.IfVisible()
        {
            mViewSwitcher.AutoRefreshIncomingChangesView();
        }

        void GluonCheckIncomingChanges.IAutoRefreshIncomingChangesView.IfVisible()
        {
            mViewSwitcher.AutoRefreshIncomingChangesView();
        }

        void CreateWorkspaceView.ICreateWorkspaceListener.OnWorkspaceCreated(
            WorkspaceInfo wkInfo, bool isGluonMode)
        {
            mWkInfo = wkInfo;
            mIsGluonMode = isGluonMode;
            mWelcomeView = null;

            if (mIsGluonMode)
                ConfigurePartialWorkspace.AsFullyChecked(mWkInfo);

            InitializePlastic();
            Repaint();
        }

        void OnEnable()
        {
            wantsMouseMove = true;

            if (mException != null)
                return;

            SetupWindowTitle();

            GuiMessage.Initialize(new UnityPlasticGuiMessage(this));

            PlasticApp.InitializeIfNeeded();

            RegisterApplicationFocusHandlers(this);

            PlasticMethodExceptionHandling.InitializeAskCredentialsUi(
                new CredentialsUiImpl(this));
            ClientEncryptionServiceProvider.SetEncryptionPasswordProvider(
                new MissingEncryptionPasswordPromptHandler(this));

            mPlasticAPI = new PlasticAPI();
            mPlasticWebRestApi = new PlasticWebRestApi();

            mEventSenderScheduler = EventTracking.Configure(
                mPlasticWebRestApi,
                ApplicationIdentifier.UnityPackage,
                IdentifyEventPlatform.Get());

            if (mEventSenderScheduler != null)
            {
                mPingEventLoop = new PingEventLoop();
                mPingEventLoop.Start();
            }

            InitializePlastic();
        }

        void OnDisable()
        {
            AssetsProcessors.Disable();

            if (mWkInfo != null)
            {
                MonoFileSystemWatcher.IsEnabled = false;
                WorkspaceFsNodeReaderCachesCleaner.CleanWorkspaceFsNodeReader(mWkInfo);
            }

            if (mException != null)
                return;

            if (mWkInfo == null)
            {
                ClosePlasticWindow(this);
                return;
            }

            mViewSwitcher.OnDisable();

            ClosePlasticWindow(this);
        }

        void OnDestroy()
        {
            if (mException != null)
                return;

            if (mWkInfo == null)
                return;

            if (!mPlasticClient.IsOperationInProgress())
                return;

            bool bCloseWindow = GuiMessage.ShowQuestion(
                PlasticLocalization.GetString(PlasticLocalization.Name.OperationRunning),
                PlasticLocalization.GetString(PlasticLocalization.Name.ConfirmClosingRunningOperation),
                PlasticLocalization.GetString(PlasticLocalization.Name.YesButton));

            if (bCloseWindow)
                return;

            mForceToOpen = true;
            ShowPlasticWindow(this);
        }

        void OnFocus()
        {
            if (mException != null)
                return;

            if (mWkInfo == null)
                return;

            mViewSwitcher.AutoRefreshPendingChangesView();
            mViewSwitcher.AutoRefreshIncomingChangesView();
        }

        void OnGUI()
        {
            if (mException != null)
            {
                DoExceptionErrorArea();
                return;
            }

            try
            {
                // IMPORTANT: disable collab (if needed)
                // must be executed before the next if statement
                // where we check if collab is enabled
                if (mDisableCollabIfEnabledWhenLoaded)
                {
                    mDisableCollabIfEnabledWhenLoaded = false;
                    DisableCollabIfEnabled(ProjectPath.FromApplicationDataPath(
                        Application.dataPath));
                }

                if (CollabPlugin.IsEnabled())
                {
                    // execute Close() once after all inspectors update
                    // to avoid our window to be drawn in back color
                    EditorApplication.delayCall = Close;
                    return;
                }

                bool isPlasticExeAvailable = IsExeAvailable.ForMode(mIsGluonMode);
                bool clientNeedsConfiguration = UnityConfigurationChecker.NeedsConfiguration();

                if (NeedsToDisplayWelcomeView(
                        isPlasticExeAvailable,
                        clientNeedsConfiguration,
                        mWkInfo))
                {
                    GetWelcomeView().OnGUI(
                        isPlasticExeAvailable,
                        clientNeedsConfiguration,
                        mIsGluonMode);
                    return;
                }

                DoHeader(
                    mWkInfo,
                    mPlasticClient,
                    mViewSwitcher,
                    mViewSwitcher,
                    mIsGluonMode,
                    mIncomingChangesNotificationPanel);

                DoTabToolbar(
                    mWkInfo,
                    mPlasticClient,
                    mViewSwitcher,
                    mIsGluonMode);

                mViewSwitcher.TabViewGUI();

                if (mPlasticClient.IsOperationInProgress())
                    DrawProgressForOperations.For(
                        mPlasticClient, mPlasticClient.Progress,
                        position.width);
            }
            catch (Exception ex)
            {
                if (IsExitGUIException(ex))
                    throw;

                GUI.enabled = true;

                if (IsIMGUIPaintException(ex))
                {
                    ExceptionsHandler.LogException("PlasticWindow", ex);
                    return;
                }

                mException = ex;

                DoExceptionErrorArea();

                ExceptionsHandler.HandleException("OnGUI", ex);
            }
        }

        void Update()
        {
            if (mException != null)
                return;

            if (mWkInfo == null)
                return;

            try
            {
                double currentUpdateTime = EditorApplication.timeSinceStartup;
                double elapsedSeconds = currentUpdateTime - mLastUpdateTime;

                mViewSwitcher.Update();
                mPlasticClient.OnParentUpdated(elapsedSeconds);

                if (mWelcomeView != null)
                    mWelcomeView.Update();

                mLastUpdateTime = currentUpdateTime;
            }
            catch (Exception ex)
            {
                mException = ex;

                ExceptionsHandler.HandleException("Update", ex);
            }
        }

        void InitializePlastic()
        {
            if (mForceToOpen)
            {
                mForceToOpen = false;
                return;
            }

            try
            {
                if (UnityConfigurationChecker.NeedsConfiguration())
                    return;

                mWkInfo = FindWorkspace.InfoForApplicationPath(
                    Application.dataPath, mPlasticAPI);

                if (mWkInfo == null)
                {
                    AssetMenuItems.Disable();
                    return;
                }

                MonoFileSystemWatcher.IsEnabled = true;

                SetupCloudProjectIdIfNeeded(mWkInfo, mPlasticAPI);

                DisableVCSIfEnabled(mWkInfo.ClientPath);

                mIsGluonMode = mPlasticAPI.IsGluonWorkspace(mWkInfo);

                IAssetStatusCache assetStatusCache =
                    new AssetStatusCache(
                        mWkInfo,
                        mIsGluonMode,
                        RepaintProjectWindow);

                AssetsProcessors.Enable(
                    mPlasticAPI,
                    assetStatusCache);

                if (mEventSenderScheduler != null)
                {
                    mPingEventLoop.SetWorkspace(mWkInfo);
                    ((IPlasticWebRestApi)mPlasticWebRestApi).SetToken(
                        CmConnection.Get().BuildWebApiTokenForCloudEditionDefaultUser());
                }

                InitializeNewIncomingChanges(mWkInfo, mIsGluonMode);

                ViewHost viewHost = new ViewHost();

                PlasticGui.WorkspaceWindow.PendingChanges.PendingChanges pendingChanges =
                    new PlasticGui.WorkspaceWindow.PendingChanges.PendingChanges(mWkInfo);

                mViewSwitcher = new ViewSwitcher(
                    mWkInfo,
                    viewHost,
                    mIsGluonMode,
                    pendingChanges,
                    mDeveloperNewIncomingChangesUpdater,
                    mGluonNewIncomingChangesUpdater,
                    mIncomingChangesNotificationPanel,
                    assetStatusCache,
                    this);

                mCooldownAutoRefreshPendingChangesAction = new CooldownWindowDelayer(
                    mViewSwitcher.AutoRefreshPendingChangesView,
                    UnityConstants.AUTO_REFRESH_PENDING_CHANGES_DELAYED_INTERVAL);

                mPlasticClient = new PlasticGUIClient(
                    mWkInfo,
                    mViewSwitcher,
                    mViewSwitcher,
                    viewHost,
                    pendingChanges,
                    mDeveloperNewIncomingChangesUpdater,
                    mGluonNewIncomingChangesUpdater,
                    this,
                    new UnityPlasticGuiMessage(this));

                mViewSwitcher.SetPlasticGUIClient(mPlasticClient);
                mViewSwitcher.ShowInitialView();

                UnityStyles.Initialize(Repaint);

                AssetOperations.IAssetSelection inspectorAssetSelection =
                    new InspectorAssetSelection();

                AssetOperations.IAssetSelection projectViewAssetSelection =
                    new ProjectViewAssetSelection();

                AssetOperations inspectorAssetOperations =
                    new AssetOperations(
                        mWkInfo,
                        mPlasticClient,
                        mViewSwitcher,
                        mViewSwitcher,
                        viewHost,
                        mDeveloperNewIncomingChangesUpdater,
                        assetStatusCache,
                        mViewSwitcher,
                        mViewSwitcher,
                        this,
                        inspectorAssetSelection,
                        mIsGluonMode);

                AssetOperations projectViewAssetOperations =
                    new AssetOperations(
                        mWkInfo,
                        mPlasticClient,
                        mViewSwitcher,
                        mViewSwitcher,
                        viewHost,
                        mDeveloperNewIncomingChangesUpdater,
                        assetStatusCache,
                        mViewSwitcher,
                        mViewSwitcher,
                        this,
                        projectViewAssetSelection,
                        mIsGluonMode);

                AssetMenuItems.Enable(
                    projectViewAssetOperations,
                    assetStatusCache,
                    projectViewAssetSelection);

                DrawInspectorOperations.Enable(
                    inspectorAssetOperations,
                    assetStatusCache,
                    inspectorAssetSelection);

                DrawAssetOverlay.Initialize(
                    assetStatusCache,
                    RepaintProjectWindow);

                mLastUpdateTime = EditorApplication.timeSinceStartup;
            }
            catch (Exception ex)
            {
                mException = ex;

                ExceptionsHandler.HandleException("InitializePlastic", ex);
            }
        }

        void InitializeNewIncomingChanges(
            WorkspaceInfo wkInfo,
            bool bIsGluonMode)
        {
            if (bIsGluonMode)
            {
                Gluon.IncomingChangesNotificationPanel gluonPanel =
                    new Gluon.IncomingChangesNotificationPanel(this);
                mGluonNewIncomingChangesUpdater =
                    NewIncomingChanges.BuildUpdaterForGluon(
                        wkInfo,
                        this,
                        gluonPanel,
                        new GluonCheckIncomingChanges.CalculateIncomingChanges());
                mIncomingChangesNotificationPanel = gluonPanel;
                return;
            }

            Developer.IncomingChangesNotificationPanel developerPanel =
                new Developer.IncomingChangesNotificationPanel(this);
            mDeveloperNewIncomingChangesUpdater =
                NewIncomingChanges.BuildUpdaterForDeveloper(
                    wkInfo, this, developerPanel);
            mIncomingChangesNotificationPanel = developerPanel;
        }

        void OnApplicationActivated()
        {
            if (mException != null)
                return;

            Reload.IfWorkspaceConfigChanged(
                mPlasticAPI, mWkInfo, mIsGluonMode,
                ExecuteFullReload);

            if (mWkInfo == null)
                return;

            ((IWorkspaceWindow)mPlasticClient).UpdateTitle();

            NewIncomingChanges.LaunchUpdater(
                mDeveloperNewIncomingChangesUpdater,
                mGluonNewIncomingChangesUpdater);

            // When Unity Editor window is activated it writes some files to its Temp folder.
            // This causes the fswatcher to process those events. 
            // We need to wait until the fswatcher finishes processing the events,
            // otherwise the NewChangesInWk method will return TRUE, causing
            // the pending changes view to unwanted auto-refresh.
            // So, we need to delay the auto-refresh call in order
            // to give the fswatcher enough time to process the events.
            // Note that the OnFocus event is not affected by this issue.
            mCooldownAutoRefreshPendingChangesAction.Ping();

            mViewSwitcher.AutoRefreshIncomingChangesView();
        }

        void OnApplicationDeactivated()
        {
            if (mException != null)
                return;

            if (mWkInfo == null)
                return;

            NewIncomingChanges.StopUpdater(
                mDeveloperNewIncomingChangesUpdater,
                mGluonNewIncomingChangesUpdater);
        }

        void ExecuteFullReload()
        {
            mException = null;

            DisposeNewIncomingChanges(this);

            InitializePlastic();
        }

        void DoExceptionErrorArea()
        {
            string labelText = PlasticLocalization.GetString(
                PlasticLocalization.Name.UnexpectedError);

            string buttonText = PlasticLocalization.GetString(
                PlasticLocalization.Name.ReloadButton);

            DrawActionHelpBox.For(
                Images.GetErrorDialogIcon(), labelText, buttonText,
                ExecuteFullReload);
        }

        WelcomeView GetWelcomeView()
        {
            if (mWelcomeView != null)
                return mWelcomeView;

            mWelcomeView = new WelcomeView(
                this,
                this,
                mPlasticAPI,
                mPlasticWebRestApi);

            return mWelcomeView;
        }

        static void DoHeader(
            WorkspaceInfo workspaceInfo,
            PlasticGUIClient plasticClient,
            IMergeViewLauncher mergeViewLauncher,
            IGluonViewSwitcher gluonSwitcher,
            bool isGluonMode,
            IIncomingChangesNotificationPanel incomingChangesNotificationPanel)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label(
                plasticClient.HeaderTitle,
                UnityStyles.PlasticWindow.HeaderTitleLabel);

            GUILayout.FlexibleSpace();

            DrawIncomingChangesNotificationPanel.ForMode(
                workspaceInfo, plasticClient,
                mergeViewLauncher, gluonSwitcher, isGluonMode,
                incomingChangesNotificationPanel.IsVisible,
                incomingChangesNotificationPanel.Data);

            //TODO: Codice - beta: hide the switcher until the update dialog is implemented
            //DrawGuiModeSwitcher.ForMode(
            //    isGluonMode, plasticClient, changesTreeView, editorWindow);

            EditorGUILayout.EndHorizontal();
        }

        static void DoTabToolbar(
            WorkspaceInfo workspaceInfo,
            PlasticGUIClient plasticClient,
            ViewSwitcher viewSwitcher,
            bool isGluonMode)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            viewSwitcher.TabButtonsGUI();

            GUILayout.FlexibleSpace();

            DoLaunchButtons(workspaceInfo, isGluonMode);

            EditorGUILayout.EndHorizontal();
        }

        static void DoLaunchButtons(
            WorkspaceInfo wkInfo,
            bool isGluonMode)
        {
            //TODO: Codice - beta: hide the diff button until the behavior is implemented
            /*GUILayout.Button(PlasticLocalization.GetString(
                PlasticLocalization.Name.DiffWindowMenuItemDiff),
                EditorStyles.toolbarButton,
                GUILayout.Width(UnityConstants.REGULAR_BUTTON_WIDTH));*/

            if (isGluonMode)
            {
                var label = PlasticLocalization.GetString(PlasticLocalization.Name.ConfigureGluon);
                if (DrawActionButton.For(label))
                    LaunchTool.OpenWorkspaceConfiguration(wkInfo);
            }
            else
            {
                var label = PlasticLocalization.GetString(PlasticLocalization.Name.LaunchBranchExplorer);
                if (DrawActionButton.For(label))
                    LaunchTool.OpenBranchExplorer(wkInfo);
            }

            string openToolText = isGluonMode ?
                PlasticLocalization.GetString(PlasticLocalization.Name.LaunchGluonButton) :
                PlasticLocalization.GetString(PlasticLocalization.Name.LaunchPlasticButton);

            if (DrawActionButton.For(openToolText))
                LaunchTool.OpenGUIForMode(wkInfo, isGluonMode);
        }

        static void SetupCloudProjectIdIfNeeded(
            WorkspaceInfo wkInfo,
            IPlasticAPI plasticApi)
        {
            if (SetupCloudProjectId.HasCloudProjectId())
                return;

            SetupCloudProjectId.ForWorkspace(wkInfo, plasticApi);

            mLog.DebugFormat("Setup CloudProjectId on Project: {0}",
                wkInfo.ClientPath);
        }

        static void DisableVCSIfEnabled(string projectPath)
        {
            if (!VCSPlugin.IsEnabled())
                return;

            VCSPlugin.Disable();

            mLog.DebugFormat("Disabled VCS Plugin on Project: {0}",
                projectPath);
        }

        static void DisposeNewIncomingChanges(PlasticWindow window)
        {
            NewIncomingChanges.DisposeUpdater(
                window.mDeveloperNewIncomingChangesUpdater,
                window.mGluonNewIncomingChangesUpdater);

            window.mDeveloperNewIncomingChangesUpdater = null;
            window.mGluonNewIncomingChangesUpdater = null;
        }

        static void RegisterApplicationFocusHandlers(PlasticWindow window)
        {
            EditorWindowFocus.OnApplicationActivated += window.OnApplicationActivated;
            EditorWindowFocus.OnApplicationDeactivated += window.OnApplicationDeactivated;
        }

        static void UnRegisterApplicationFocusHandlers(PlasticWindow window)
        {
            EditorWindowFocus.OnApplicationActivated -= window.OnApplicationActivated;
            EditorWindowFocus.OnApplicationDeactivated -= window.OnApplicationDeactivated;
        }

        static bool IsExitGUIException(Exception ex)
        {
            return ex is ExitGUIException;
        }

        static bool IsIMGUIPaintException(Exception ex)
        {
            if (!(ex is ArgumentException))
                return false;

            return ex.Message.StartsWith("Getting control") &&
                   ex.Message.Contains("controls when doing repaint");
        }

        static void ClosePlasticWindow(PlasticWindow window)
        {
            UnRegisterApplicationFocusHandlers(window);

            PlasticApp.Dispose();

            AssetMenuItems.Disable();

            DrawInspectorOperations.Disable();

            DrawAssetOverlay.Dispose();

            if (window.mEventSenderScheduler != null)
            {
                window.mPingEventLoop.Stop();
                window.mEventSenderScheduler.End();
            }

            DisposeNewIncomingChanges(window);

            AvatarImages.Dispose();
        }

        static void ShowPlasticWindow(PlasticWindow window)
        {
            EditorWindow dockWindow = FindEditorWindow.ToDock<PlasticWindow>();

            PlasticWindow newPlasticWindow = InstantiateFrom(window);

            if (DockEditorWindow.IsAvailable())
                DockEditorWindow.To(dockWindow, newPlasticWindow);

            newPlasticWindow.Show();

            newPlasticWindow.Focus();
        }

        static bool NeedsToDisplayWelcomeView(
            bool isPlasticExeAvailable,
            bool clientNeedsConfiguration,
            WorkspaceInfo wkInfo)
        {
            if (!isPlasticExeAvailable)
                return true;

            if (clientNeedsConfiguration)
                return true;

            if (wkInfo == null)
                return true;

            return false;
        }

        static void RepaintProjectWindow()
        {
            EditorWindow projectWindow = FindEditorWindow.ProjectWindow();

            if (projectWindow == null)
                return;

            projectWindow.Repaint();
        }

        static void DisableCollabIfEnabled(string projectPath)
        {
            if (!CollabPlugin.IsEnabled())
                return;

            CollabPlugin.Disable();

            mLog.DebugFormat("Disabled Collab Plugin on Project: {0}",
                projectPath);
        }

        static PlasticWindow InstantiateFrom(PlasticWindow window)
        {
            PlasticWindow result = Instantiate(window);
            result.mWkInfo = window.mWkInfo;
            result.mPlasticClient = window.mPlasticClient;
            result.mViewSwitcher = window.mViewSwitcher;
            result.mCooldownAutoRefreshPendingChangesAction = window.mCooldownAutoRefreshPendingChangesAction;
            result.mDeveloperNewIncomingChangesUpdater = window.mDeveloperNewIncomingChangesUpdater;
            result.mGluonNewIncomingChangesUpdater = window.mGluonNewIncomingChangesUpdater;
            result.mException = window.mException;
            result.mLastUpdateTime = window.mLastUpdateTime;
            result.mIsGluonMode = window.mIsGluonMode;
            result.mIncomingChangesNotificationPanel = window.mIncomingChangesNotificationPanel;
            result.mWelcomeView = window.mWelcomeView;
            result.mPlasticAPI = window.mPlasticAPI;
            result.mEventSenderScheduler = window.mEventSenderScheduler;
            result.mPingEventLoop = window.mPingEventLoop;
            return result;
        }

        static class Reload
        {
            internal static void IfWorkspaceConfigChanged(
                IPlasticAPI plasticApi,
                WorkspaceInfo lastWkInfo,
                bool lastIsGluonMode,
                Action reloadAction)
            {
                string applicationPath = Application.dataPath;

                bool isGluonMode = false;
                WorkspaceInfo wkInfo = null;

                IThreadWaiter waiter = ThreadWaiter.GetWaiter(10);
                waiter.Execute(
                    /*threadOperationDelegate*/ delegate
                    {
                        wkInfo = FindWorkspace.
                            InfoForApplicationPath(applicationPath, plasticApi);

                        if (wkInfo != null)
                            isGluonMode = plasticApi.IsGluonWorkspace(wkInfo);
                    },
                    /*afterOperationDelegate*/ delegate
                    {
                        if (waiter.Exception != null)
                            return;

                        if (!IsWorkspaceConfigChanged(
                                lastWkInfo, wkInfo,
                                lastIsGluonMode, isGluonMode))
                            return;

                        reloadAction();
                });
            }

            static bool IsWorkspaceConfigChanged(
                WorkspaceInfo lastWkInfo,
                WorkspaceInfo currentWkInfo,
                bool lastIsGluonMode,
                bool currentIsGluonMode)
            {
                if (lastIsGluonMode != currentIsGluonMode)
                    return true;

                if (lastWkInfo == null || currentWkInfo == null)
                    return true;

                return !lastWkInfo.Equals(currentWkInfo);
            }
        }

        [SerializeField]
        bool mForceToOpen;

        [NonSerialized]
        WorkspaceInfo mWkInfo;

        Exception mException;

        IIncomingChangesNotificationPanel mIncomingChangesNotificationPanel;

        double mLastUpdateTime = 0f;

        CooldownWindowDelayer mCooldownAutoRefreshPendingChangesAction;
        ViewSwitcher mViewSwitcher;
        WelcomeView mWelcomeView;

        PlasticGui.WorkspaceWindow.NewIncomingChangesUpdater mDeveloperNewIncomingChangesUpdater;
        GluonNewIncomingChangesUpdater mGluonNewIncomingChangesUpdater;

        PlasticGUIClient mPlasticClient;

        bool mIsGluonMode;
        bool mDisableCollabIfEnabledWhenLoaded;

        PlasticAPI mPlasticAPI;
        static PlasticWebRestApi mPlasticWebRestApi;
        EventSenderScheduler mEventSenderScheduler;
        PingEventLoop mPingEventLoop;

        static readonly ILog mLog = LogManager.GetLogger("PlasticWindow");
    }
}
 