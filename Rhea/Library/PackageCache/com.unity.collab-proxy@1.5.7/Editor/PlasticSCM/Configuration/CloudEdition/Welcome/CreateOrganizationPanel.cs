using System.Collections.Generic;
using System.Linq;

using UnityEditor.UIElements;
using UnityEngine.UIElements;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI.UIElements;

namespace Unity.PlasticSCM.Editor.Configuration.CloudEdition.Welcome
{
    internal class CreateOrganizationPanel : VisualElement
    {
        internal CreateOrganizationPanel(VisualElement rootPanel)
        {
            mRootPanel = rootPanel;

            InitializeLayoutAndStyles();

            BuildComponents();
        }

        internal void Dispose()
        {
            mLoadingSpinner.Dispose();

            mCreateButton.clicked -= CreateButton_Clicked;

            mOrganizationNameTextField.UnregisterValueChangedCallback(
                OnOrganizationNameTextFieldChanged);
        }

        void StartProgress()
        {
            mGettingDatacenters.RemoveFromClassList("hidden");
            mLoadingSpinner.Start();
        }

        void StopProgress()
        {
            mGettingDatacenters.AddToClassList("hidden");
            mLoadingSpinner.Stop();
        }

        void OnOrganizationNameTextFieldChanged(ChangeEvent<string> evt)
        {
            mOrganizationNameNotification.text = "";
        }

        void DataCenterClicked(DropdownMenuAction action)
        {
            mSelectedDatacenter = action.name;
            mDatacenter.text = action.name;
        }

        void CreateButton_Clicked()
        {
            //TODO: Launch organization creation task

            mRootPanel.Clear();
            mRootPanel.Add(new CreatedOrganizationPanel(mOrganizationNameTextField.text));
        }

        DropdownMenuAction.Status DataCenterActive(DropdownMenuAction action)
        {
            if (action.name == mSelectedDatacenter)
                return DropdownMenuAction.Status.Checked;

            return DropdownMenuAction.Status.Normal;
        }

        IEnumerable<string> GetDatacenters()
        {
            // TODO: Replace with call
            return new string[]
            {
                "Test Server 1",
                "Test Server 2",
                "Test Server 3",
                "Test Server 4",
                "Test Server 5"
            };
        }

        static void SetGettingDatacentersVisibility(
            VisualElement gettingDatacenters,
            bool visible)
        {
            if (visible)
            {
                gettingDatacenters.AddToClassList("hidden");
                return;
            }

            gettingDatacenters.RemoveFromClassList("hidden");
        }

        void BuildComponents()
        {
            this.SetControlImage("buho",
                PlasticGui.Help.HelpImage.ColorBuho);

            VisualElement spinnerControl = this.Query<VisualElement>("gdSpinner").First();
            mLoadingSpinner = new LoadingSpinner();
            spinnerControl.Add(mLoadingSpinner);

            IEnumerable<string> datacenters = GetDatacenters();
            mSelectedDatacenter = datacenters.FirstOrDefault();
            mDatacenter = new ToolbarMenu { text = mSelectedDatacenter };
            foreach (string datacenter in GetDatacenters())
                mDatacenter.menu.AppendAction(datacenter, DataCenterClicked, DataCenterActive);
            VisualElement datacenterContainer = this.Query<VisualElement>("datacenter").First();
            datacenterContainer.Add(mDatacenter);

            mOrganizationNameTextField = this.Query<TextField>("orgName").First();
            mOrganizationNameNotification = this.Query<Label>("orgNameNotification").First();
            mBackButton = this.Query<Button>("back").First();
            mCreateButton = this.Query<Button>("create").First();
            mEncryptLearnMoreButton = this.Query<Button>("encryptLearnMore").First();
            mGettingDatacenters = this.Query<VisualElement>("gettingDatacenters").First();

            mCreateButton.clicked += CreateButton_Clicked;

            mOrganizationNameTextField.RegisterValueChangedCallback(
                OnOrganizationNameTextFieldChanged);
            mOrganizationNameTextField.FocusOnceLoaded();

            this.SetControlText<Label>("createLabel",
                PlasticLocalization.Name.CreateOrganizationTitle);
            this.SetControlLabel<TextField>("orgName",
                PlasticLocalization.Name.OrganizationName);
            this.SetControlText<Label>("datacenterLabel",
                PlasticLocalization.Name.Datacenter);
            this.SetControlText<Toggle>("encryptData",
                PlasticLocalization.Name.EncryptionCheckButton);
            this.SetControlText<Label>("encryptExplanation",
                PlasticLocalization.Name.EncryptionCheckButtonExplanation, "");
            this.SetControlText<Button>("encryptLearnMore",
                PlasticLocalization.Name.LearnMore);
            this.SetControlText<Label>("gdLabel",
                PlasticLocalization.Name.GettingDatacenters);
            this.SetControlText<Button>("back",
                PlasticLocalization.Name.BackButton);
            this.SetControlText<Button>("create",
                PlasticLocalization.Name.CreateButton);
        }

        void InitializeLayoutAndStyles()
        {
            this.LoadLayout(typeof(CreateOrganizationPanel).Name);

            this.LoadStyle("SignInSignUp");
            this.LoadStyle(typeof(CreateOrganizationPanel).Name);
        }

        VisualElement mRootPanel;
        TextField mOrganizationNameTextField;
        Label mOrganizationNameNotification;
        ToolbarMenu mDatacenter;
        Button mBackButton;
        Button mCreateButton;
        Button mEncryptLearnMoreButton;
        VisualElement mGettingDatacenters;
        LoadingSpinner mLoadingSpinner;
        string mSelectedDatacenter;
    }
}