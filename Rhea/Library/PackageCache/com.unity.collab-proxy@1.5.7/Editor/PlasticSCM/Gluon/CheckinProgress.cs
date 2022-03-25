using GluonGui.WorkspaceWindow.Views.Checkin.Operations;

namespace Unity.PlasticSCM.Editor.Gluon
{
    internal class CheckinProgress
    {
        internal CheckinProgress(PlasticGUIClient guiClient)
        {
            mGuiClient = guiClient;
        }

        internal void Refresh(CheckinProgressData progress)
        {
            mGuiClient.Progress.ProgressHeader = progress.ProgressText;

            mGuiClient.Progress.TotalProgressMessage = progress.TotalProgressText;
            mGuiClient.Progress.TotalProgressPercent = ((double)progress.TotalProgressValue) / 100;

            mGuiClient.Progress.ShowCurrentBlock = progress.bShowCurrentBlock;
            mGuiClient.Progress.CurrentBlockProgressMessage = progress.CurrentBlockText;
            mGuiClient.Progress.CurrentBlockProgressPercent = ((double)progress.CurrentBlockProgressValue) / 100;
        }

        PlasticGUIClient mGuiClient;
    }
}
