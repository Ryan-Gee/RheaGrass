using System.Collections.Generic;
using System.Linq;

using UnityEditor.UIElements;
using UnityEngine.UIElements;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI.UIElements;

namespace Unity.PlasticSCM.Editor.Configuration.CloudEdition.Welcome
{
    internal class OrganizationPanel : VisualElement
    {
        internal OrganizationPanel(List<string> organizations)
        {
            mOrganizations = organizations;

            InitializeLayoutAndStyles();

            BuildComponents();
        }

        void BuildComponents()
        {
            this.SetControlImage("buho",
                PlasticGui.Help.HelpImage.CloudBuho);

            this.SetControlText<Label>("confirmationMessage",
                PlasticLocalization.Name.SignedUpTitle);

            if (mOrganizations.Count == 1)
                BuildSingleOrganizationSection("Codice");
            else if (mOrganizations.Count > 1)
                BuildMultipleOrganizationsSection(mOrganizations);

            BuildCreateOrganizationSection(!mOrganizations.Any());
        }

        void BuildSingleOrganizationSection(string organizationName)
        {
            mOrganizationToJoin = organizationName;

            this.Query<VisualElement>("joinSingleOrganization").First().RemoveFromClassList("display-none");

            this.SetControlText<Label>("joinSingleOrganizationLabel",
                PlasticLocalization.Name.YouBelongToOrganization, organizationName);

            this.SetControlText<Button>("joinSingleOrganizationButton",
                PlasticLocalization.Name.JoinButton);
        }

        void BuildMultipleOrganizationsSection(List<string> organizationNames)
        {
            this.Query<VisualElement>("joinMultipleOrganizations").First().RemoveFromClassList("display-none");

            this.SetControlText<Label>("joinMultipleOrganizationsLabel",
                PlasticLocalization.Name.YouBelongToSeveralOrganizations);

            VisualElement organizationDropdown = this.Query<VisualElement>("organizationDropdown").First();
            ToolbarMenu toolbarMenu = new ToolbarMenu
            {
                text = organizationNames.FirstOrDefault()
            };
            foreach (string name in organizationNames)
            {
                toolbarMenu.menu.AppendAction(name, x => 
                {
                    toolbarMenu.text = name;
                    mOrganizationToJoin = name;
                }, DropdownMenuAction.AlwaysEnabled);
                organizationDropdown.Add(toolbarMenu);
            }

            this.SetControlText<Button>("joinMultipleOrganizationsButton",
                PlasticLocalization.Name.JoinButton);
        }

        void BuildCreateOrganizationSection(bool firstOrganization)
        {
            PlasticLocalization.Name createOrganizationLabelName = firstOrganization ?
                PlasticLocalization.Name.CreateFirstOrganizationLabel :
                PlasticLocalization.Name.CreateOtherOrganizationLabel;

            this.SetControlText<Label>("createOrganizationLabel",
                createOrganizationLabelName);

            this.SetControlText<Button>("createOrganizationButton",
                PlasticLocalization.Name.CreateButton);
        }

        void InitializeLayoutAndStyles()
        {
            this.LoadLayout(typeof(OrganizationPanel).Name);

            this.LoadStyle("SignInSignUp");
            this.LoadStyle(typeof(OrganizationPanel).Name);
        }

        List<string> mOrganizations;
        public string mOrganizationToJoin = "";
    }
}
