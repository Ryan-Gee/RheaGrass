using System.Collections.Generic;

using Codice.Client.BaseCommands;
using Codice.Client.BaseCommands.EventTracking;
using Codice.CM.Common;
using PlasticGui;

namespace Unity.PlasticSCM.Editor.Views
{
    internal static class LaunchOperation
    {
        internal static void CheckinForMode(
            WorkspaceInfo wkInfo,
            bool isGluonMode,
            bool keepItemsLocked,
            PlasticGUIClient plasticClient)
        {
            TrackFeatureUseEvent.For(
                Plastic.API.GetRepositorySpec(wkInfo),
                isGluonMode ?
                    TrackFeatureUseEvent.Features.PartialCheckin :
                    TrackFeatureUseEvent.Features.Checkin);

            if (isGluonMode)
            {
                plasticClient.PartialCheckin(keepItemsLocked);
                return;
            }

            plasticClient.Checkin();
        }

        internal static void UndoForMode(
            bool isGluonMode,
            PlasticGUIClient plasticClient)
        {
            if (isGluonMode)
            {
                plasticClient.PartialUndo();
                return;
            }

            plasticClient.Undo();
        }

        internal static void UndoChangesForMode(
            bool isGluonMode,
            PlasticGUIClient plasticClient,
            List<ChangeInfo> changesToUndo,
            List<ChangeInfo> dependenciesCandidates)
        {
            if (isGluonMode)
            {
                plasticClient.PartialUndoChanges(
                    changesToUndo, dependenciesCandidates);
                return;
            }

            plasticClient.UndoChanges(
                changesToUndo, dependenciesCandidates);
        }

        internal static void UpdateWorkspaceForMode(
            bool isGluonMode,
            PlasticGUIClient plasticClient)
        {
            if (isGluonMode)
            {
                plasticClient.PartialUpdateWorkspace();
                return;
            }

            plasticClient.UpdateWorkspace();
        }
    }
}
