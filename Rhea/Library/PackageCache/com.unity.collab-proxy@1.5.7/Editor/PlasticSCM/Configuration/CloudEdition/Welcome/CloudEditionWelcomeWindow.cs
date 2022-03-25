using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using PlasticGui;
using PlasticGui.WebApi;
using Unity.PlasticSCM.Editor.UI.UIElements;

namespace Unity.PlasticSCM.Editor.Configuration.CloudEdition.Welcome
{
    internal class CloudEditionWelcomeWindow : EditorWindow
    {
        internal static void ShowWindow(IPlasticWebRestApi restApi)
        {
            CloudEditionWelcomeWindow window = GetWindow<CloudEditionWelcomeWindow>();
            window.mRestApi = restApi;
            window.titleContent = new GUIContent(
                PlasticLocalization.GetString(PlasticLocalization.Name.SignInToPlasticSCM));
            window.minSize = new Vector2(800, 460);
            window.Show();
        }

        void OnEnable()
        {
            BuildComponents();
        }

        void OnDestroy()
        {
            Dispose();
        }

        void Dispose()
        {
            mSignInPanel.Dispose();
            mSSOSignUpPanel.Dispose();
        }

        internal void BuildComponents()
        {
            VisualElement root = rootVisualElement;

            root.Clear();
            mTabView = new TabView();

            mSignInPanel = new SignInPanel(this);
            mSSOSignUpPanel = new SSOSignUpPanel(this, mRestApi);

            mTabView.AddTab(
                PlasticLocalization.GetString(PlasticLocalization.Name.Login),
                mSignInPanel);
            mTabView.AddTab(
                PlasticLocalization.GetString(PlasticLocalization.Name.SignUp),
                mSSOSignUpPanel).clicked += () =>
                {
                    titleContent = new GUIContent(
                        PlasticLocalization.GetString(PlasticLocalization.Name.SignUp));
                };

            root.Add(mTabView);
        }

        internal TabView mTabView;

        SignInPanel mSignInPanel;
        SSOSignUpPanel mSSOSignUpPanel;

        IPlasticWebRestApi mRestApi;
    }
}