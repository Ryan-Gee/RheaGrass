using UnityEditor;

using PlasticGui.WorkspaceWindow;

namespace Unity.PlasticSCM.Editor.Developer
{
    internal class IncomingChangesNotificationPanel :
        IIncomingChangesNotificationPanel,
        CheckIncomingChanges.IUpdateIncomingChanges
    {
        bool IIncomingChangesNotificationPanel.IsVisible
        {
            get { return mIsVisible; }
        }

        NotificationPanelData IIncomingChangesNotificationPanel.Data
        {
            get { return mPanelData; }
        }

        internal IncomingChangesNotificationPanel(
            EditorWindow window)
        {
            mWindow = window;
        }

        void CheckIncomingChanges.IUpdateIncomingChanges.Hide()
        {
            mPanelData.Clear();

            mIsVisible = false;

            mWindow.Repaint();
        }

        void CheckIncomingChanges.IUpdateIncomingChanges.Show(
            string infoText,
            string actionText,
            string tooltipText,
            CheckIncomingChanges.Severity severity,
            CheckIncomingChanges.Action action)
        {
            UpdateData(
                mPanelData, infoText, actionText,
                tooltipText, severity, action);

            mIsVisible = true;

            mWindow.Repaint();
        }

        static void UpdateData(
            NotificationPanelData data,
            string infoText,
            string actionText,
            string tooltipText,
            CheckIncomingChanges.Severity severity,
            CheckIncomingChanges.Action action)
        {
            data.HasUpdateAction =
                action == CheckIncomingChanges.Action.Update;
            data.InfoText = infoText;
            data.ActionText = actionText;
            data.TooltipText = tooltipText;
            data.NotificationStyle =
                severity == CheckIncomingChanges.Severity.Info ?
                NotificationPanelData.StyleType.Green :
                NotificationPanelData.StyleType.Red;
        }

        bool mIsVisible;

        NotificationPanelData mPanelData = new NotificationPanelData();

        EditorWindow mWindow;
    }
}
