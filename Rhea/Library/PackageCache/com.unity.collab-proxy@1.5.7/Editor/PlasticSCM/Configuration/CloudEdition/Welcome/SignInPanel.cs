using UnityEngine;
using UnityEngine.UIElements;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.UIElements;

namespace Unity.PlasticSCM.Editor.Configuration.CloudEdition.Welcome
{
    internal class SignInPanel : VisualElement
    {
        internal SignInPanel(
            CloudEditionWelcomeWindow parentWindow)
        {
            mParentWindow = parentWindow;

            InitializeLayoutAndStyles();

            BuildComponents();
        }

        internal void Dispose()
        {
            mSignInWithUnityIdButton.clicked -= SignInWithUnityIdButton_Clicked;
            mSignInWithEmailButton.clicked -= SignInWithEmailButton_Clicked;
            mPrivacyPolicyStatementButton.clicked -= PrivacyPolicyStatementButton_Clicked;

            if (mSignInWithEmailPanel != null)
                mSignInWithEmailPanel.Dispose();

            if (mWaitingSignInPanel != null)
                mWaitingSignInPanel.Dispose();
        }

        void SignInWithEmailButton_Clicked()
        {
            mSignInWithEmailPanel = new SignInWithEmailPanel(mParentWindow);

            mParentWindow.rootVisualElement.Clear();
            mParentWindow.rootVisualElement.Add(mSignInWithEmailPanel);
        }

        void SignInWithUnityIdButton_Clicked()
        {
            mWaitingSignInPanel = new WaitingSignInPanel(mParentWindow);

            mParentWindow.rootVisualElement.Clear();
            mParentWindow.rootVisualElement.Add(mWaitingSignInPanel);
        }

        void PrivacyPolicyStatementButton_Clicked()
        {
            // TODO: update when dll is avaiable PlasticGui.Configuration.CloudEdition.Welcome
            //       SignUp.PRIVACY_POLICY_URL
            Application.OpenURL("https://unity3d.com/legal/privacy-policy");
        }

        void BuildComponents()
        {
            this.SetControlImage("buho",
                PlasticGui.Help.HelpImage.CloudBuho);

            this.SetControlText<Label>("signInToPlasticSCM",
                PlasticLocalization.Name.SignInToPlasticSCM);

            this.SetControlImage("iconUnity",
                Images.Name.ButtonSsoSignInUnity);

            mSignInWithUnityIdButton = this.Query<Button>("signInWithUnityId").First();
            mSignInWithUnityIdButton.text = PlasticLocalization.GetString(
                PlasticLocalization.Name.SignInWithUnityID);
            mSignInWithUnityIdButton.clicked += SignInWithUnityIdButton_Clicked;

            this.SetControlImage("iconEmail",
                Images.Name.ButtonSsoSignInEmail);

            mSignInWithEmailButton = this.Query<Button>("signInWithEmail").First();
            mSignInWithEmailButton.clicked += SignInWithEmailButton_Clicked;

            this.SetControlText<Label>("privacyStatementText",
                PlasticLocalization.Name.PrivacyStatementText,
                PlasticLocalization.GetString(PlasticLocalization.Name.PrivacyStatement));

            mPrivacyPolicyStatementButton = this.Query<Button>("privacyStatement").First();
            mPrivacyPolicyStatementButton.text = PlasticLocalization.GetString(
                PlasticLocalization.Name.PrivacyStatement);
            mPrivacyPolicyStatementButton.clicked += PrivacyPolicyStatementButton_Clicked;
        }

        void InitializeLayoutAndStyles()
        {
            this.LoadLayout(typeof(SignInPanel).Name);

            this.LoadStyle("SignInSignUp");
            this.LoadStyle(typeof(SignInPanel).Name);
        }

        SignInWithEmailPanel mSignInWithEmailPanel;
        WaitingSignInPanel mWaitingSignInPanel;
        CloudEditionWelcomeWindow mParentWindow;
        Button mSignInWithUnityIdButton;
        Button mSignInWithEmailButton;
        Button mPrivacyPolicyStatementButton;
    }
}