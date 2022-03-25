using UnityEngine.UIElements;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI.UIElements;

namespace Unity.PlasticSCM.Editor.Configuration.CloudEdition.Welcome
{
    internal class WaitingSignInPanel : VisualElement
    {
        internal WaitingSignInPanel(CloudEditionWelcomeWindow parentWindow)
        {
            mParentWindow = parentWindow;

            InitializeLayoutAndStyles();

            BuildComponents();
        }

        internal void Dispose()
        {
            mCancelButton.clicked -= CancelButton_Clicked;
        }

        void CancelButton_Clicked()
        {
            mParentWindow.BuildComponents();
        }

        void BuildComponents()
        {
            this.SetControlImage("buho",
                PlasticGui.Help.HelpImage.GenericBuho);

            this.SetControlText<Label>("signInToPlasticSCM",
                PlasticLocalization.Name.SignInToPlasticSCM);

            this.SetControlText<Label>("completeSignInOnBrowser",
                PlasticLocalization.Name.CompleteSignInOnBrowser);

            this.SetControlText<Label>("oAuthSignInCheckMessage",
                PlasticLocalization.Name.OAuthSignInCheckMessage);

            mCancelButton = this.Query<Button>("cancelButton").First();
            mCancelButton.text = PlasticLocalization.GetString(
                PlasticLocalization.Name.CancelButton);
            mCancelButton.clicked += CancelButton_Clicked;
        }

        void InitializeLayoutAndStyles()
        {
            this.LoadLayout(typeof(WaitingSignInPanel).Name);

            this.LoadStyle("SignInSignUp");
            this.LoadStyle(typeof(WaitingSignInPanel).Name);
        }

        Button mCancelButton;

        readonly CloudEditionWelcomeWindow mParentWindow;
    }
}