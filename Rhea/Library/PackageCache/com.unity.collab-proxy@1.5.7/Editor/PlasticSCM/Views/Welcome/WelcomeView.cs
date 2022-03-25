using UnityEditor;
using UnityEngine;

using Codice.Client.Common;
using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WebApi;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views.CreateWorkspace;
using Unity.PlasticSCM.Editor.UI.Progress;

namespace Unity.PlasticSCM.Editor.Views.Welcome
{
    internal class WelcomeView
    {
        internal WelcomeView(
            EditorWindow parentWindow,
            CreateWorkspaceView.ICreateWorkspaceListener createWorkspaceListener,
            PlasticAPI plasticApi,
            IPlasticWebRestApi plasticWebRestApi)
        {
            mParentWindow = parentWindow;
            mCreateWorkspaceListener = createWorkspaceListener;
            mPlasticApi = plasticApi;
            mPlasticWebRestApi = plasticWebRestApi;

            mGuiMessage = new UnityPlasticGuiMessage(parentWindow);
            mDownloadProgress = new ProgressControlsForViews();
            mConfigureProgress = new ProgressControlsForViews();

            mInstallerFile = GetInstallerTmpFileName.ForPlatform();
        }


        internal void Update()
        {
            if (mCreateWorkspaceView != null)
                mCreateWorkspaceView.Update();

            mDownloadProgress.UpdateDeterminateProgress(mParentWindow);
            mConfigureProgress.UpdateDeterminateProgress(mParentWindow);
        }

        internal void OnGUI(
            bool isPlasticExeAvailable,
            bool clientNeedsConfiguration,
            bool isGluonMode)
        {
            GUILayout.BeginHorizontal();

            GUILayout.Space(LEFT_MARGIN);

            DoContentViewArea(
                isPlasticExeAvailable,
                clientNeedsConfiguration,
                isGluonMode,
                mIsCreateWorkspaceButtonClicked,
                mInstallerFile,
                mGuiMessage,
                mDownloadProgress,
                mConfigureProgress);

            GUILayout.EndHorizontal();
        }

        void DoContentViewArea(
            bool isPlasticExeAvailable,
            bool clientNeedsConfiguration,
            bool isGluonMode,
            bool isCreateWorkspaceButtonClicked,
            string installerFile,
            GuiMessage.IGuiMessage guiMessage,
            ProgressControlsForViews downloadProgress,
            ProgressControlsForViews configureProgress)
        {
            GUILayout.BeginVertical();

            GUILayout.Space(TOP_MARGIN);

            if (isCreateWorkspaceButtonClicked)
                GetCreateWorkspaceView().OnGUI();
            else
                DoSetupViewArea(
                    isPlasticExeAvailable,
                    clientNeedsConfiguration,
                    isGluonMode,
                    mInstallerFile,
                    mGuiMessage,
                    mDownloadProgress,
                    mConfigureProgress);

            GUILayout.EndVertical();
        }

        void DoSetupViewArea(
            bool isPlasticExeAvailable,
            bool clientNeedsConfiguration,
            bool isGluonMode,
            string installerFile,
            GuiMessage.IGuiMessage guiMessage,
            ProgressControlsForViews downloadProgress,
            ProgressControlsForViews configureProgress)
        {
            DoTitleLabel();

            GUILayout.Space(STEPS_TOP_MARGIN);

            bool isStep1Completed =
                isPlasticExeAvailable &
                !downloadProgress.ProgressData.IsOperationRunning;

            bool isStep2Completed =
                isStep1Completed &
                !clientNeedsConfiguration &
                !configureProgress.ProgressData.IsOperationRunning;

            DoStepsArea(
                isStep1Completed,
                isStep2Completed,
                downloadProgress.ProgressData,
                configureProgress.ProgressData);

            GUILayout.Space(BUTTON_MARGIN);

            DoActionButtonsArea(
                isStep1Completed,
                isStep2Completed,
                isGluonMode,
                installerFile,
                guiMessage,
                downloadProgress,
                configureProgress);

            DoNotificationArea(
                downloadProgress.ProgressData,
                configureProgress.ProgressData);
        }

        void DoActionButtonsArea(
            bool isStep1Completed,
            bool isStep2Completed,
            bool isGluonMode,
            string installerFile,
            GuiMessage.IGuiMessage guiMessage,
            ProgressControlsForViews downloadProgress,
            ProgressControlsForViews configureProgress)
        {
            GUILayout.BeginHorizontal();

            DoActionButton(
                isStep1Completed,
                isStep2Completed,
                isGluonMode,
                installerFile,
                guiMessage,
                downloadProgress,
                configureProgress);

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }

        void DoActionButton(
            bool isStep1Completed,
            bool isStep2Completed,
            bool isGluonMode,
            string installerFile,
            GuiMessage.IGuiMessage guiMessage,
            ProgressControlsForViews downloadProgress,
            ProgressControlsForViews configureProgress)
        {
            if (!isStep1Completed)
            {
                DoInstallButton(
                    downloadProgress, guiMessage, installerFile);
                return;
            }

            if (!isStep2Completed)
            {
                DoConfigureButton(
                    isGluonMode, configureProgress);
                return;
            }

            if (GUILayout.Button(PlasticLocalization.GetString(PlasticLocalization.Name.CreateWorkspace),
                    GUILayout.Width(BUTTON_WIDTH)))
                mIsCreateWorkspaceButtonClicked = true;
        }

        static void DoInstallButton(
            ProgressControlsForViews downloadProgress,
            GuiMessage.IGuiMessage guiMessage,
            string installerFile)
        {
            GUI.enabled = !downloadProgress.ProgressData.IsOperationRunning;

            if (GUILayout.Button(PlasticLocalization.GetString(PlasticLocalization.Name.InstallPlasticSCM),
                    GUILayout.Width(BUTTON_WIDTH)))
            {
                Edition plasticEdition;
                if (TryGetPlasticEditionToDownload(
                        guiMessage, out plasticEdition))
                {
                    DownloadAndInstallOperation.Run(
                        plasticEdition, installerFile,
                        downloadProgress);

                    GUIUtility.ExitGUI();
                }
            }

            GUI.enabled = true;
        }

        static void DoConfigureButton(
            bool isGluonMode,
            ProgressControlsForViews configureProgress)
        {
            GUI.enabled = !configureProgress.ProgressData.IsOperationRunning;

            if (GUILayout.Button(PlasticLocalization.GetString(PlasticLocalization.Name.LoginOrSignUp),
                    GUILayout.Width(BUTTON_WIDTH)))
            {
                ConfigurePlasticOperation.Run(
                    isGluonMode,
                    configureProgress);

                GUIUtility.ExitGUI();
            }

            GUI.enabled = true;
        }

        static void DoStepsArea(
            bool isStep1Completed,
            bool isStep2Completed,
            ProgressControlsForViews.Data downloadProgressData,
            ProgressControlsForViews.Data configureProgressData)
        {
            DoDownloadAndInstallStep(
                isStep1Completed,
                downloadProgressData);

            DoLoginOrSignUpStep(
                isStep2Completed,
                configureProgressData);

            DoCreatePlasticWorkspaceStep();
        }

        static void DoDownloadAndInstallStep(
            bool isStep1Completed,
            ProgressControlsForViews.Data progressData)
        {
            Images.Name stepImage = (isStep1Completed) ?
                Images.Name.StepOk :
                Images.Name.Step1;

            string stepText = GetDownloadStepText(
                progressData,
                isStep1Completed);

            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.richText = true;

            GUILayout.BeginHorizontal();

            DoStepLabel(
                stepText,
                stepImage,
                style);

            GUILayout.EndHorizontal();
        }

        static void DoLoginOrSignUpStep(
            bool isStep2Completed,
            ProgressControlsForViews.Data progressData)
        {
            Images.Name stepImage = (isStep2Completed) ?
                Images.Name.StepOk :
                Images.Name.Step2;

            string stepText = GetConfigurationStepText(
                progressData,
                isStep2Completed);

            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.richText = true;

            GUILayout.BeginHorizontal();

            DoStepLabel(
                stepText,
                stepImage,
                style);

            GUILayout.EndHorizontal();
        }

        static void DoCreatePlasticWorkspaceStep()
        {
            GUILayout.BeginHorizontal();

            DoStepLabel(
                PlasticLocalization.GetString(PlasticLocalization.Name.CreateAPlasticWorkspace),
                Images.Name.Step3,
                EditorStyles.label);

            GUILayout.EndHorizontal();
        }

        static void DoStepLabel(
            string text,
            Images.Name imageName,
            GUIStyle style)
        {
            GUILayout.Space(STEPS_LEFT_MARGIN);

            GUIContent stepLabelContent = new GUIContent(
                string.Format(" {0}", text),
                Images.GetImage(imageName));

            GUILayout.Label(
                stepLabelContent,
                style,
                GUILayout.Height(STEP_LABEL_HEIGHT));
        }

        static void DoTitleLabel()
        {
            GUIContent labelContent = new GUIContent(
                PlasticLocalization.GetString(PlasticLocalization.Name.NextStepsToSetup),
                Images.GetInfoIcon());

            GUILayout.Label(labelContent, EditorStyles.boldLabel);
        }

        static void DoNotificationArea(
            ProgressControlsForViews.Data downloadProgressData,
            ProgressControlsForViews.Data configureProgressData)
        {
            if (!string.IsNullOrEmpty(downloadProgressData.NotificationMessage))
                DrawProgressForViews.ForNotificationArea(downloadProgressData);

            if (!string.IsNullOrEmpty(configureProgressData.NotificationMessage))
                DrawProgressForViews.ForNotificationArea(configureProgressData);
        }

        static string GetDownloadStepText(
            ProgressControlsForViews.Data progressData,
            bool isStep1Completed)
        {
            string result = PlasticLocalization.GetString(PlasticLocalization.Name.DownloadAndInstall);

            if (isStep1Completed)
                return result;

            if (progressData.IsOperationRunning)
                result = string.Format("<b>{0}</b>  -  ", result);

            result += progressData.ProgressMessage;

            if (progressData.IsOperationRunning && progressData.ProgressPercent >= 0)
            {
                result += string.Format(
                    " {0}%", progressData.ProgressPercent * 100);
            }

            return result;
        }

        static string GetConfigurationStepText(
            ProgressControlsForViews.Data progressData,
            bool isStep2Completed)
        {
            string result = PlasticLocalization.GetString(PlasticLocalization.Name.LoginOrSignUpPlastic);

            if (isStep2Completed)
                return result;

            if (!progressData.IsOperationRunning)
                return result;

            return string.Format("<b>{0}</b>", result);
        }

        static bool TryGetPlasticEditionToDownload(
            GuiMessage.IGuiMessage guiMessage,
            out Edition plasticEdition)
        {
            plasticEdition = Edition.Cloud;

            if (EditionToken.IsCloudEdition())
                return true;

            GuiMessage.GuiMessageResponseButton result = guiMessage.ShowQuestion(
                    PlasticLocalization.GetString(PlasticLocalization.Name.PlasticSCM),
                    PlasticLocalization.GetString(PlasticLocalization.Name.WhichVersionInstall),
                    PlasticLocalization.GetString(PlasticLocalization.Name.DownloadCloudEdition),
                    PlasticLocalization.GetString(PlasticLocalization.Name.DownloadEnterpriseEdition),
                    PlasticLocalization.GetString(PlasticLocalization.Name.CancelButton),
                    true);

            if (result == GuiMessage.GuiMessageResponseButton.Third)
                return false;

            if (result == GuiMessage.GuiMessageResponseButton.First)
                return true;

            plasticEdition = Edition.Enterprise;
            return true;
        }

        CreateWorkspaceView GetCreateWorkspaceView()
        {
            if (mCreateWorkspaceView != null)
                return mCreateWorkspaceView;

            string workspacePath = ProjectPath.FromApplicationDataPath(
                Application.dataPath);

            mCreateWorkspaceView = new CreateWorkspaceView(
                mParentWindow,
                mCreateWorkspaceListener,
                mPlasticApi,
                mPlasticWebRestApi,
                workspacePath);

            return mCreateWorkspaceView;
        }

        string mInstallerFile;
        bool mIsCreateWorkspaceButtonClicked = false;

        CreateWorkspaceView mCreateWorkspaceView;

        readonly ProgressControlsForViews mDownloadProgress;
        readonly ProgressControlsForViews mConfigureProgress;
        readonly GuiMessage.IGuiMessage mGuiMessage;
        readonly PlasticAPI mPlasticApi;
        readonly IPlasticWebRestApi mPlasticWebRestApi;
        readonly CreateWorkspaceView.ICreateWorkspaceListener mCreateWorkspaceListener;
        readonly EditorWindow mParentWindow;

        const int LEFT_MARGIN = 30;
        const int TOP_MARGIN = 20;
        const int STEPS_TOP_MARGIN = 5;
        const int STEPS_LEFT_MARGIN = 12;
        const int BUTTON_MARGIN = 10;
        const int STEP_LABEL_HEIGHT = 20;
        const int BUTTON_WIDTH = 170;

        const string DOWNLOAD_URL = @"https://www.plasticscm.com/download";
    }
}
