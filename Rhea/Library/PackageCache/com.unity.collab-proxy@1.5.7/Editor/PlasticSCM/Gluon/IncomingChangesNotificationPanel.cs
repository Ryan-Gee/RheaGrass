using UnityEditor;

using PlasticGui.Gluon.WorkspaceWindow;

namespace Unity.PlasticSCM.Editor.Gluon
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
            CheckIncomingChanges.Severity severity)
        {
            UpdateData(
                mPanelData, infoText, actionText,
                tooltipText, severity);

            mIsVisible = true;

            mWindow.Repaint();
        }

        static void UpdateData(
            NotificationPanelData data,
            string infoText,
            string actionText,
            string tooltipText,
            CheckIncomingChanges.Severity severity)
        {
            data.HasUpdateAction = false;
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
