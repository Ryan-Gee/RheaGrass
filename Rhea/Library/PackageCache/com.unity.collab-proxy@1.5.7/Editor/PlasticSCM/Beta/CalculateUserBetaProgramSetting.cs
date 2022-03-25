using System;

using UnityEditor;

using Codice.Client.Common.Threading;
using Codice.Client.Common.WebApi;
using Codice.LogWrapper;
using Unity.PlasticSCM.Editor.WebApi;

namespace Unity.PlasticSCM.Editor.Beta
{
    [InitializeOnLoad]
    internal static class CalculateUserBetaProgramSetting
    {
        static CalculateUserBetaProgramSetting()
        {
            EditorApplication.update += RunOnceWhenAccessTokenIsInitialized;
        }

        static void RunOnceWhenAccessTokenIsInitialized()
        {
            if (string.IsNullOrEmpty(CloudProjectSettings.accessToken))
                return;

            EditorApplication.update -= RunOnceWhenAccessTokenIsInitialized;

            if (CollabPlugin.IsEnabled())
                return;

            Execute(CloudProjectSettings.accessToken);
        }

        static void Execute(string unityAccessToken)
        {
            if (SessionState.GetBool(
                    IS_USER_BETA_PROGRAM_ALREADY_CALCULATED_KEY, false))
            {
                return;
            }

            SessionState.SetBool(
                IS_USER_BETA_PROGRAM_ALREADY_CALCULATED_KEY, true);

            PlasticApp.InitializeIfNeeded();

            EnableUserBetaProgramIfNeeded(unityAccessToken);
        }

        static void EnableUserBetaProgramIfNeeded(string unityAccessToken)
        {
            int ini = Environment.TickCount;

            UnityPackageBetaEnrollResponse response = null;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter(10);
            waiter.Execute(
            /*threadOperationDelegate*/ delegate
            {
                response = PlasticScmRestApiClient.IsBetaEnabled(unityAccessToken);
            },
            /*afterOperationDelegate*/ delegate
            {
                mLog.DebugFormat(
                    "IsBetaEnabled time {0} ms",
                    Environment.TickCount - ini);

                if (waiter.Exception != null)
                {
                    ExceptionsHandler.LogException(
                        "CalculateUserBetaProgramSetting",
                        waiter.Exception);
                    return;
                }

                if (response == null)
                    return;

                if (response.Error != null)
                {
                    mLog.ErrorFormat(
                        "Unable to retrieve is beta enabled: {0} [code {1}]",
                        response.Error.Message, response.Error.ErrorCode);
                    return;
                }

                if (!response.IsBetaEnabled)
                {
                    mLog.InfoFormat(
                        "Beta is disabled for accessToken: {0}",
                        unityAccessToken);
                    return;
                }

                PlasticMenuItem.Add();
            });
    }

        const string IS_USER_BETA_PROGRAM_ALREADY_CALCULATED_KEY =
            "PlasticSCM.UserBetaProgram.IsAlreadyCalculated";

        static readonly ILog mLog = LogManager.GetLogger("CalculateUserBetaProgramSetting");
    }
}
