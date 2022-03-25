using GluonGui.WorkspaceWindow.Views.Checkin.Operations;
using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer;

namespace Unity.PlasticSCM.Editor.Gluon
{
    internal class ProgressOperationHandler : IUpdateProgress, ICheckinProgress
    {
        internal ProgressOperationHandler(PlasticGUIClient guiClient)
        {
            mGuiClient = guiClient;
        }

        internal bool IsOperationInProgress()
        {
            return mUpdateProgress != null
                || mCheckinProgress != null;
        }

        internal void CancelUpdateProgress()
        {
            mUpdateProgress.Cancel();
        }

        void ICheckinProgress.ShowProgress()
        {
            mCheckinProgress = new CheckinProgress(mGuiClient);
        }

        void ICheckinProgress.RefreshProgress(CheckinProgressData progress)
        {
            mCheckinProgress.Refresh(progress);
        }

        void ICheckinProgress.EndProgress()
        {
            mCheckinProgress = null;
            mGuiClient.Progress.ResetProgress();
            mGuiClient.RequestRepaint();
        }

        void IUpdateProgress.ShowNoCancelableProgress()
        {
            mUpdateProgress = new UpdateProgress(mGuiClient);
            mUpdateProgress.SetCancellable(false);
        }

        void IUpdateProgress.ShowCancelableProgress()
        {
            mUpdateProgress = new UpdateProgress(mGuiClient);
            mUpdateProgress.SetCancellable(true);
        }

        void IUpdateProgress.RefreshProgress(
            Codice.Client.BaseCommands.UpdateProgress updateProgress,
            UpdateProgressData updateProgressData)
        {
            mUpdateProgress.RefreshProgress(updateProgress, updateProgressData);
        }

        void IUpdateProgress.EndProgress()
        {
            mUpdateProgress = null;
            mGuiClient.Progress.ResetProgress();
            mGuiClient.RequestRepaint();
        }

        UpdateProgress mUpdateProgress;
        CheckinProgress mCheckinProgress;

        PlasticGUIClient mGuiClient;
    }
}
