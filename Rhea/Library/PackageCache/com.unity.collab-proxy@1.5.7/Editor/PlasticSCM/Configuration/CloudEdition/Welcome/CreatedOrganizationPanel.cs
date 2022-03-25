using UnityEngine.UIElements;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI.UIElements;

namespace Unity.PlasticSCM.Editor.Configuration.CloudEdition.Welcome
{
    internal class CreatedOrganizationPanel : VisualElement
    {
        internal CreatedOrganizationPanel(string organizationName)
        {
            mOrganizationName = organizationName;

            InitializeLayoutAndStyles();

            BuildComponents();
        }

        void BuildComponents()
        {
            this.SetControlImage("buho",
                PlasticGui.Help.HelpImage.CloudBuho);

            this.SetControlText<Label>("createdTitle",
                PlasticLocalization.Name.CreatedOrganizationTitle);
            this.SetControlText<Label>("createdExplanation",
                PlasticLocalization.Name.CreatedOrganizationExplanation, mOrganizationName);
            this.SetControlText<Button>("continue",
                PlasticLocalization.Name.ContinueButton);
        }

        void InitializeLayoutAndStyles()
        {
            this.LoadLayout(typeof(CreatedOrganizationPanel).Name);

            this.LoadStyle("SignInSignUp");
            this.LoadStyle(typeof(CreatedOrganizationPanel).Name);
        }

        string mOrganizationName;
    } 
}