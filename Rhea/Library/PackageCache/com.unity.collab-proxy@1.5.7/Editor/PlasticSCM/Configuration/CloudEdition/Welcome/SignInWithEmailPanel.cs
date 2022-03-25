using UnityEditor;
using UnityEngine.UIElements;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI.UIElements;

namespace Unity.PlasticSCM.Editor.Configuration.CloudEdition.Welcome
{
    internal class SignInWithEmailPanel : VisualElement
    {
        internal SignInWithEmailPanel(CloudEditionWelcomeWindow parentWindow)
        {
            mParentWindow = parentWindow;

            InitializeLayoutAndStyles();

            BuildComponents();
        }

        internal void Dispose()
        {
            mSignInButton.clicked -= SignInButton_Clicked;
            mBackButton.clicked -= BackButton_Clicked;
        }

        void SignInButton_Clicked()
        {
            // TODO: Replace this with validation and sign in logic
            string message = string.Format("Signing in as '{0}' with password '{1}'",
                mEmailField.text,
                mPasswordField.text);

            EditorUtility.DisplayDialog("Test", message, "Ok");
        }

        void BackButton_Clicked()
        {
            mParentWindow.BuildComponents();
        }

        void BuildComponents()
        {
            mEmailField = this.Query<TextField>("email").First();
            mPasswordField = this.Query<TextField>("password").First();
            mEmailNotificationLabel = this.Query<Label>("emailNotification").First();
            mPasswordNotificationLabel = this.Query<Label>("passwordNotification").First();
            mSignInButton = this.Query<Button>("signIn").First();
            mBackButton = this.Query<Button>("back").First();

            mSignInButton.clicked += SignInButton_Clicked;
            mBackButton.clicked += BackButton_Clicked;
            mEmailField.FocusOnceLoaded();

            this.SetControlImage("buho",
                PlasticGui.Help.HelpImage.CloudBuho);
            this.SetControlText<Label>("signInLabel",
                PlasticLocalization.Name.SignInWithEmail);
            this.SetControlLabel<TextField>("email",
                PlasticLocalization.Name.Email);
            this.SetControlLabel<TextField>("password",
                PlasticLocalization.Name.Password);
            this.SetControlText<Button>("signIn",
                PlasticLocalization.Name.SignInButton);
            this.SetControlText<Button>("back",
                PlasticLocalization.Name.BackButton);
        }

        void InitializeLayoutAndStyles()
        {
            this.LoadLayout(typeof(SignInWithEmailPanel).Name);

            this.LoadStyle(typeof(SignInWithEmailPanel).Name);
        }

        TextField mEmailField;
        TextField mPasswordField;

        Label mEmailNotificationLabel;
        Label mPasswordNotificationLabel;

        Button mSignInButton;
        Button mBackButton;

        CloudEditionWelcomeWindow mParentWindow;
    }
}