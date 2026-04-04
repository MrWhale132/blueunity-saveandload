
using System;
using System.Collections;
using Theblueway.Core.Runtime.UI.PageNavigation;
using Theblueway.SaveAndLoad.Samples.Samples.Scripts.UI.Pages;
using Theblueway.SaveAndLoad.Samples.Scripts.UI;
using Theblueway.SaveAndLoad.Samples.Scripts.UI.Pages;
using UnityEngine;

namespace Theblueway.SaveAndLoad.Samples.Samples.Scripts.UI
{
    public class MainMenuUIPageNavigationStateMachine : MonoBehaviour
    {
        public UIPageNavigationStack<MainMenuUIPagNavigationParams> _pageNavigationStack = new();
        public MainMenuUIPagNavigationParams _cachedDefault = default;


        public MainMenuUIPageManager _mainMenuPage;
        public NewWorldPageManager _newWorldPage;
        public LoadWorldSaveUI _loadWorldPage;


        private void Awake()
        {
            var enterNewWorldPageCommand = new UIPageNavigationCommand<MainMenuUIPagNavigationParams>(EnterNewWorldPage);
            var enterLoadWorldPageCommand = new UIPageNavigationCommand<MainMenuUIPagNavigationParams>(EnterLoadWorldPage);

            _mainMenuPage.EnterNewWorldPageCommand = enterNewWorldPageCommand;
            _mainMenuPage.EnterLoadWorldPageCommand = enterLoadWorldPageCommand;

            _pageNavigationStack.Push(_mainMenuPage, _cachedDefault);
        }



        public void EnterNewWorldPage(MainMenuUIPagNavigationParams navigationParams)
        {
            _pageNavigationStack.Push(_newWorldPage, navigationParams);
        }

        public void EnterLoadWorldPage(MainMenuUIPagNavigationParams navigationParams)
        {
            _pageNavigationStack.Push(_loadWorldPage, navigationParams);
        }


        private void OnValidate()
        {
            ValidatePage(_mainMenuPage,typeof(MainMenuUIPageManager));
            ValidatePage(_loadWorldPage,typeof(LoadWorldSaveUI));
        }

        public void ValidatePage(IMainMenuUIPageNavigator navigator, Type expectedType)
        {
            if(navigator == null)
            {
                Debug.LogError($"{nameof(MainMenuUIPageNavigationStateMachine)}: Page navigator {expectedType.Name} is not assigned", this);
            }
        }
    }

    public class MainMenuUIPagNavigationParams
    {

    }
}
