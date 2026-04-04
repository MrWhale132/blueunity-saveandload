using UnityEngine;
using UnityEngine.Events;

namespace Theblueway.SaveAndLoad.Samples.Samples.Scripts.UI
{

    public class UIContentSetup : MonoBehaviour
    {
        public Transform _bootstrapObject;
        public UnityEvent _entryPoint;

        //the awake and start methods set the child active and then deactive to make sure their singletons are loaded in awake
        //this manager's start must be called first, see execution order
        public void Awake()
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(true);
            }
        }


        private void Start()
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }

            _bootstrapObject.gameObject.SetActive(true);
            _entryPoint.Invoke();
        }
    }

}