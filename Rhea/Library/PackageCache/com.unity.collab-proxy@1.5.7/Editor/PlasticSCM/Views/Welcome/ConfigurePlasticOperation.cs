using System.Diagnostics;
using Codice.Client.BaseCommands;
using Codice.Client.Common;
using Codice.Client.Common.Threading;
using PlasticGui;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI.Progress;

namespace Unity.PlasticSCM.Editor.Views.Welcome
{
    static class ConfigurePlasticOperation
    {
        internal static void Run(
            bool isGluonMode,
            ProgressControlsForViews progressControls)
        {
            ((IProgressControls)progressControls).ShowProgress(string.Empty);

            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    Process plasticProcess = LaunchTool.OpenConfigurationForMode(isGluonMode);

                    if (plasticProcess != null)
                        plasticProcess.WaitForExit();
                },
                /*afterOperationDelegate*/ delegate
                {
                    ((IProgressControls)progressControls).HideProgress();

                    ClientConfig.Reset();
                    CmConnection.Reset();
                    ClientHandlers.Register();

                    if (waiter.Exception == null)
                        return;

                    ((IProgressControls)progressControls).ShowError(
                        waiter.Exception.Message);
                });
        }
    }
}
