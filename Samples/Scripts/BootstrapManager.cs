
using UnityEngine;

namespace Theblueway.SaveAndLoad.Samples.Samples.Scripts
{
    [DefaultExecutionOrder(100)]
    public class BootstrapManager : MonoBehaviour
    {
        public void Start()
        {
            MyGameManager.Singleton.OnBootstrapCompleted();
        }
    }
}
