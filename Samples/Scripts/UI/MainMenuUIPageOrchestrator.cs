
using UnityEngine;

namespace Theblueway.SaveAndLoad.Samples.Scripts.UI
{
    public class MainMenuUIPageOrchestrator:MonoBehaviour
    {
        public GameObject _mainMenuPageContainer;

        private void Awake()
        {
            for(int i =0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }
                _mainMenuPageContainer.SetActive(true);
        }
    }
}
