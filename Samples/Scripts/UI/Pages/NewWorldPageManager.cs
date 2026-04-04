using Eflatun.SceneReference;
using Theblueway.SaveAndLoad.Samples.Samples.Scripts;
using Theblueway.SaveAndLoad.Samples.Samples.Scripts.UI;
using Theblueway.SaveAndLoad.Samples.Scripts.UI.PageNavitgationSpecifications;
using UnityEngine;
using UnityEngine.UI;

namespace Theblueway.SaveAndLoad.Samples.Scripts.UI.Pages
{
    public class NewWorldPageManager : MonoBehaviour, INewWorldPageNavigator
    {
        public SceneReference _jungleWorld;
        public SceneReference _moonWorld;
        public SceneReference _desertWorld;

        public Button _jungleWorldButton;
        public Button _moonWorldButton;
        public Button _desertWorldButton;


        private void Awake()
        {
            _jungleWorldButton.onClick.AddListener(() => LoadWorld(_jungleWorld));
            _moonWorldButton.onClick.AddListener(() => LoadWorld(_moonWorld));
            _desertWorldButton.onClick.AddListener(() => LoadWorld(_desertWorld));
        }


        public void LoadWorld(SceneReference sceneReference)
        {
            MyGameManager.Singleton.StartNewWorld(sceneReference.BuildIndex);
        }


        public void OnPageEnter(MainMenuUIPagNavigationParams transitioneStateParams)
        {
            gameObject.SetActive(true);
        }

        public void OnPageExit(MainMenuUIPagNavigationParams transitioneStateParams)
        {
            gameObject.SetActive(false);
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
