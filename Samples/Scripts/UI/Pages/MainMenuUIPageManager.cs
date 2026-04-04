using Eflatun.SceneReference;
using System;
using Theblueway.Core.Runtime.UI.PageNavigation;
using Theblueway.SaveAndLoad.Samples.Scripts.UI.PageNavitgationSpecifications;
using UnityEngine;
using UnityEngine.UI;

namespace Theblueway.SaveAndLoad.Samples.Samples.Scripts.UI.Pages
{
    public class MainMenuUIPageManager : MonoBehaviour,IMainMenuPageNavigator
    {
        [NonSerialized]
        public MainMenuUIPagNavigationParams _defaultNavParams;

        public Button _newWorldButton;
        public Button _loadWorldButton;


        public UIPageNavigationCommand<MainMenuUIPagNavigationParams> EnterNewWorldPageCommand { get; set; }

        public UIPageNavigationCommand<MainMenuUIPagNavigationParams> EnterLoadWorldPageCommand { get; set; }


        private void Awake()
        {
            _newWorldButton.onClick.AddListener(OnNewWorldButtonPress);
            _loadWorldButton.onClick.AddListener(OnLoadWorldButtonPress);
        }



        public void OnPageEnter(MainMenuUIPagNavigationParams transitioneStateParams)
        {
            gameObject.SetActive(true);
        }

        public void OnPageExit(MainMenuUIPagNavigationParams transitioneStateParams)
        {
            gameObject.SetActive(false);
        }

        public void OnNewWorldButtonPress()
        {
            EnterNewWorldPageCommand.Execute(_defaultNavParams);
        }

        public void OnLoadWorldButtonPress()
        {
            EnterLoadWorldPageCommand.Execute(_defaultNavParams);
        }






        public void YieldControl(MainMenuUIPagNavigationParams navigationParams)
        {
            OnPageExit(navigationParams);
        }

        public void GainControl(MainMenuUIPagNavigationParams navigationParams)
        {
            OnPageEnter(navigationParams);
        }

        public void TakeBackControl(MainMenuUIPagNavigationParams navigationParams)
        {
            OnPageEnter(navigationParams);
        }

        public void ReturnControl(MainMenuUIPagNavigationParams navigationParams)
        {
            OnPageExit(navigationParams);
        }
    }
}
