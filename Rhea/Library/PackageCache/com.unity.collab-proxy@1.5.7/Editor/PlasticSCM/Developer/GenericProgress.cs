using PlasticGui.WorkspaceWindow;

namespace Unity.PlasticSCM.Editor.Developer
{
    internal class GenericProgress
    {
        internal GenericProgress(PlasticGUIClient guiClient)
        {
            mGuiClient = guiClient;
            mGuiClient.Progress.CanCancelProgress = false;
        }

        internal void RefreshProgress(ProgressData progressData)
        {
            var progress = mGuiClient.Progress;

            progress.ProgressHeader = progressData.Status;
            progress.TotalProgressMessage = progressData.Details;
            progress.TotalProgressPercent = progressData.ProgressValue / 100f;
        }

        PlasticGUIClient mGuiClient;
    }
}
