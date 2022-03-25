using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Unity.PlasticSCM.Editor.Views;
using Unity.PlasticSCM.Editor.Views.PendingChanges;

namespace Unity.PlasticSCM.Editor
{
    internal static class DrawGuiModeSwitcher
    {
        internal static void ForMode(
            bool isGluonMode,
            PlasticGUIClient plasticClient,
            TreeView changesTreeView,
            EditorWindow editorWindow)
        {
            GUI.enabled = !plasticClient.IsOperationInProgress();

            EditorGUI.BeginChangeCheck();

            GuiMode currentMode = isGluonMode ?
                GuiMode.GluonMode : GuiMode.DeveloperMode;

            GuiMode selectedMode = (GuiMode)EditorGUILayout.EnumPopup(
                currentMode,
                EditorStyles.toolbarDropDown,
                GUILayout.Width(100));

            if (EditorGUI.EndChangeCheck())
            {
                SwitchGuiModeIfUserWants(
                    plasticClient, currentMode, selectedMode,
                    changesTreeView, editorWindow);
            }

            GUI.enabled = true;
        }

        static void SwitchGuiModeIfUserWants(
            PlasticGUIClient plasticClient,
            GuiMode currentMode, GuiMode selectedMode,
            TreeView changesTreeView,
            EditorWindow editorWindow)
        {
            if (currentMode == selectedMode)
                return;

            bool userConfirmed = SwitchModeConfirmationDialog.SwitchMode(
                currentMode == GuiMode.GluonMode, editorWindow);

            if (!userConfirmed)
                return;

            bool isGluonMode = selectedMode == GuiMode.GluonMode;

            LaunchOperation.UpdateWorkspaceForMode(
                isGluonMode, plasticClient);

            PendingChangesTreeHeaderState.SetMode(
                changesTreeView.multiColumnHeader.state,
                isGluonMode);
        }

        enum GuiMode
        {
            DeveloperMode,
            GluonMode
        }
    }
}
