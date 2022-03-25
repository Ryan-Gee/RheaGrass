using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer;

namespace Unity.PlasticSCM.Editor.Gluon
{
    internal class UpdateProgress
    {
        internal UpdateProgress(PlasticGUIClient guiClient)
        {
            mGuiClient = guiClient;
        }

        internal void Cancel()
        {
            if (mUpdateProgress == null)
                return;

            mUpdateProgress.Cancel();
        }

        internal void SetCancellable(bool bCancelable)
        {
            mGuiClient.Progress.CanCancelProgress = bCancelable;
        }

        internal void RefreshProgress(
            Codice.Client.BaseCommands.UpdateProgress progress,
            UpdateProgressData updateProgressData)
        {
            mUpdateProgress = progress;

            mGuiClient.Progress.ProgressHeader = updateProgressData.Details;

            mGuiClient.Progress.TotalProgressMessage = updateProgressData.Status;
            mGuiClient.Progress.TotalProgressPercent = updateProgressData.ProgressValue / 100;
        }

        Codice.Client.BaseCommands.UpdateProgress mUpdateProgress;

        PlasticGUIClient mGuiClient;
    }
}
