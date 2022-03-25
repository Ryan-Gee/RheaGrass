using System;

using Codice.Client.BaseCommands;
using Codice.Client.Commands;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.Update;

namespace Unity.PlasticSCM.Editor.Developer
{
    internal class UpdateProgress
    {
        internal UpdateProgress(
            UpdateNotifier notifier, string wkPath, string title,
            PlasticGUIClient guiClient)
        {
            mNotifier = notifier;
            mWkPath = wkPath;
            mGuiClient = guiClient;

            mProgressData = new BuildProgressSpeedAndRemainingTime.ProgressData(DateTime.Now);

            mGuiClient.Progress.ProgressHeader = title;
            mGuiClient.Progress.CanCancelProgress = false;
        }

        internal void OnUpdateProgress()
        {
            var progress = mGuiClient.Progress;

            progress.ProgressHeader = UpdateProgressRender.FixNotificationPath(
                mWkPath, mNotifier.GetNotificationMessage());

            UpdateOperationStatus status = mNotifier.GetUpdateStatus();

            progress.TotalProgressMessage = UpdateProgressRender.GetProgressString(
                status, mProgressData);

            progress.TotalProgressPercent = GetProgressBarPercent.ForTransfer(
                status.UpdatedSize, status.TotalSize) / 100f;
        }

        readonly BuildProgressSpeedAndRemainingTime.ProgressData mProgressData;
        readonly PlasticGUIClient mGuiClient;
        readonly string mWkPath;
        readonly UpdateNotifier mNotifier;
    }
}
