using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using PlasticGui;
using PlasticGui.WebApi;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.UIElements;

namespace Unity.PlasticSCM.Editor.Configuration.CloudEdition.Welcome
{
    internal class SSOSignUpPanel : VisualElement
    {
        internal SSOSignUpPanel(
            CloudEditionWelcomeWindow parentWindow,
            IPlasticWebRestApi restApi)
        {
            mParentWindow = parentWindow;
            mRestApi = restApi;

            InitializeLayoutAndStyles();

            BuildComponents();
        }

        internal void Dispose()
        {
            mSignUpButton.clicked -= SignUpButton_Clicked;
            mTermsOfServiceButton.clicked -= TermsOfServiceButton_Clicked;
            mPrivacyPolicyButton.clicked -= PrivacyPolicyButton_Clicked;
            mPrivacyPolicyStatementButton.clicked -= PrivacyPolicyStatementButton_Clicked;

            mUserNameTextField.UnregisterValueChangedCallback(
                OnUserNameTextFieldChanged);
            mPasswordTextField.UnregisterValueChangedCallback(
                OnPasswordTextFieldChanged);
            mConfirmPasswordTextField.UnregisterValueChangedCallback(
                OnPasswordTextFieldChanged);
        }

        void OnUserNameTextFieldChanged(ChangeEvent<string> evt)
        {
            if (!evt.newValue.Contains('@'))
            {
                mUserNotificationLabel.RemoveFromClassList("hidden");
                mValidEmail = false;
            }
            else
            {
                mUserNotificationLabel.AddToClassList("hidden");
                mValidEmail = true;
            }

            SetSignUpButtonEnablement();
        }

        void OnPasswordTextFieldChanged(ChangeEvent<string> evt)
        {
            if (mPasswordTextField.value != mConfirmPasswordTextField.value)
            {
                mPasswordNotificationLabel.RemoveFromClassList("hidden");
                mMatchingPasswords = false;
            }
            else
            {
                mPasswordNotificationLabel.AddToClassList("hidden");
                mMatchingPasswords = true;
            }

            SetSignUpButtonEnablement();
        }

        void SignUpButton_Clicked()
        {
            OrganizationPanel organizationPanel = new OrganizationPanel(
                    new List<string>() { "codice@codice", "skullcito@codice" });
            //OrganizationPanel organizationPanel = new OrganizationPanel(new List<string>() { "codice@codice" });
            //OrganizationPanel organizationPanel = new OrganizationPanel(new List<string>() { });
            mParentWindow.mTabView.SwitchContent(organizationPanel);
        }

        void TermsOfServiceButton_Clicked()
        {
            Application.OpenURL("https://www.plasticscm.com/releases/eula/licenseagreement.pdf");
        }

        void PrivacyPolicyButton_Clicked()
        {
            Application.OpenURL("https://unity3d.com/legal/privacy-policy");
        }

        void PrivacyPolicyStatementButton_Clicked()
        {
            // TODO: update when dll is avaiable PlasticGui.Configuration.CloudEdition.Welcome
            //       SignUp.PRIVACY_POLICY_URL
            Application.OpenURL("https://unity3d.com/legal/privacy-policy");
        }

        void SetSignUpButtonEnablement()
        {
            if (!mValidEmail || !mMatchingPasswords || String.IsNullOrEmpty(mPasswordTextField.value))
                mSignUpButton.SetEnabled(false);
            else
                mSignUpButton.SetEnabled(true);
        }

        void BuildComponents()
        {
            this.SetControlImage("buho",
                PlasticGui.Help.HelpImage.GenericBuho);

            this.SetControlText<Label>("signUpLabel",
                PlasticLocalization.Name.SignUp);

            mUserNameTextField = this.Q<TextField>("emailField");
            mUserNameTextField.label = PlasticLocalization.GetString(
                PlasticLocalization.Name.Email);
            mUserNameTextField.RegisterValueChangedCallback(
                OnUserNameTextFieldChanged);

            mUserNotificationLabel = this.Q<Label>("emailNotification");
            mUserNotificationLabel.text = PlasticLocalization.GetString(
                PlasticLocalization.Name.EnterValidEmailAddress);

            mPasswordTextField = this.Q<TextField>("passwordField");
            mPasswordTextField.label = PlasticLocalization.GetString(
                PlasticLocalization.Name.Password);
            mPasswordTextField.RegisterValueChangedCallback(
                OnPasswordTextFieldChanged);

            mConfirmPasswordTextField = this.Q<TextField>("confirmPasswordField");
            mConfirmPasswordTextField.label = PlasticLocalization.GetString(
                PlasticLocalization.Name.ConfirmPassword);
            mConfirmPasswordTextField.RegisterValueChangedCallback(
                OnPasswordTextFieldChanged);

            mPasswordNotificationLabel = this.Q<Label>("passwordNotificationLabel");
            mPasswordNotificationLabel.text = PlasticLocalization.GetString(
                PlasticLocalization.Name.PasswordDoesntMatch);

            mSignUpButton = this.Q<Button>("signUp");
            mSignUpButton.text = PlasticLocalization.GetString(PlasticLocalization.Name.SignUp);
            mSignUpButton.clicked += SignUpButton_Clicked;

            string[] signUpText = PlasticLocalization.GetString(
                PlasticLocalization.Name.SignUpAgreeToShort).Split('{', '}');
            Label signUpAgreePt1 = this.Q<Label>("signUpAgreePt1");
            signUpAgreePt1.text = signUpText[0];

            mTermsOfServiceButton = this.Q<Button>("termsOfService");
            mTermsOfServiceButton.text = PlasticLocalization.GetString(PlasticLocalization.Name.TermsOfService);
            mTermsOfServiceButton.clicked += TermsOfServiceButton_Clicked;

            Label signUpAgreePt2 = this.Q<Label>("signUpAgreePt2");
            signUpAgreePt2.text = signUpText[2];

            mPrivacyPolicyButton = this.Q<Button>("privacyPolicy");
            mPrivacyPolicyButton.text = PlasticLocalization.GetString(PlasticLocalization.Name.PrivacyPolicy);
            mPrivacyPolicyButton.clicked += PrivacyPolicyButton_Clicked;

            List<VisualElement> dashes = this.Query<VisualElement>("dash").ToList();
            if (EditorGUIUtility.isProSkin)
                dashes.ForEach(x => x.style.borderTopColor = new StyleColor(new UnityEngine.Color(0.769f, 0.769f, 0.769f)));
            else
                dashes.ForEach(x => x.style.borderTopColor = new StyleColor(new UnityEngine.Color(0.008f, 0.008f, 0.008f)));

            this.SetControlText<Label>("or",
                PlasticLocalization.Name.Or);

            this.SetControlImage("unityIcon",
                Images.Name.ButtonSsoSignInUnity);

            Button unityIDButton = this.Q<Button>("unityIDButton");
            unityIDButton.text = PlasticLocalization.GetString(PlasticLocalization.Name.SignUpUnityID);

            this.SetControlImage("googleIcon",
                Images.Name.ButtonSsoSignInGoogle);

            Button googleButton = this.Q<Button>("googleButton");
            googleButton.text = PlasticLocalization.GetString(PlasticLocalization.Name.SignUpGoogle);

            this.SetControlText<Label>("privacyStatementText",
                PlasticLocalization.Name.PrivacyStatementText,
                PlasticLocalization.GetString(PlasticLocalization.Name.PrivacyStatement));

            mPrivacyPolicyStatementButton = this.Query<Button>("privacyStatement").First();
            mPrivacyPolicyStatementButton.text = PlasticLocalization.GetString(
                PlasticLocalization.Name.PrivacyStatement);
            mPrivacyPolicyStatementButton.clicked += PrivacyPolicyStatementButton_Clicked;

            SetSignUpButtonEnablement();
        }

        void InitializeLayoutAndStyles()
        {
            this.LoadLayout(typeof(SSOSignUpPanel).Name);

            this.LoadStyle("SignInSignUp");
            this.LoadStyle(typeof(SSOSignUpPanel).Name);
        }

        Button mTermsOfServiceButton;
        Button mPrivacyPolicyButton;
        Button mPrivacyPolicyStatementButton;
        TextField mUserNameTextField;
        TextField mPasswordTextField;
        TextField mConfirmPasswordTextField;
        Label mUserNotificationLabel;
        Label mPasswordNotificationLabel;
        Button mSignUpButton;

        bool mValidEmail;
        bool mMatchingPasswords;

        readonly CloudEditionWelcomeWindow mParentWindow;
        readonly IPlasticWebRestApi mRestApi;
    }
}
