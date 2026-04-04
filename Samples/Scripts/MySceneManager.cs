
using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.UtilScripts;
using Assets._Project.Scripts.UtilScripts.Extensions;
using Eflatun.SceneReference;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Theblueway.SaveAndLoad.Samples.Samples.Scripts
{
    public class MySceneManager : MonoBehaviour
    {
        public static MySceneManager Singleton { get; private set; }

        [NonSerialized] public SceneLoadingResult _cachedResult = new();


        public SceneReference _transition;
        public SceneReference _mainMenu;
        //public SceneReference _worldScene;
        public SceneReference _bootstrapScene;


        private void Awake()
        {
            if (Singleton != null && Singleton != this)
            {
                Debug.LogError("Multiple instances of MySceneManager detected. Destroying duplicate instance.");
                Destroy(gameObject);
                return;
            }

            Singleton = this;
            DontDestroyOnLoad(gameObject);
        }






        public IEnumerator StartNewWorldRoutine(int index)
        {
            yield return Transition(_mainMenu.LoadedScene, index);
        }


        public void ExitWorld(Scene scene)
        {
            StartCoroutine(ExitWorldRoutine(scene));
        }

        public IEnumerator ExitWorldRoutine(Scene scene)
        {
            yield return Transition(scene, _mainMenu.BuildIndex);
        }


        public void OnBootstrapCompleted()
        {
            StartCoroutine(OnBootstrapCompletedRoutine());
        }

        public IEnumerator OnBootstrapCompletedRoutine()
        {
            yield return Transition(_bootstrapScene, _mainMenu);
        }


        public IEnumerator PrepareLoadSavedWorldRoutine()
        {
            yield return StartCoroutine(LoadScene(_transition.BuildIndex));

            yield return StartCoroutine(UnloadScene(_mainMenu.LoadedScene));
        }




        public IEnumerator OnLoadSavedWorldCompletedRoutine()
        {
            var rootObjects = _transition.LoadedScene.GetRootGameObjects();

            var unhandledObjects = rootObjects.Where(x => !Infra.S.IsScheduledOrDestroyed(x));

            if (unhandledObjects.Count() > 0)
            {
                Debug.LogError($"Some objects are still present in the temporary scene used for loading the saved world. " +
                    $"This is most probably due to an error in the loading process. " +
                    $"Leaving the scene loaded for debugging.");

                List<string> reportList = new();

                foreach (var obj in unhandledObjects)
                {
                    var id = Infra.S.IsRegistered(obj) ? Infra.S.GetObjectId(obj, Infra.GlobalReferencing) : RandomId.Default;
                    var path = obj.HierarchyPath();
                    reportList.Add(id + " | " + path + " | " + (obj == null).ToString());
                }

                string report = string.Join("\n", reportList);

                Debug.LogError($"The list of root objects that are still present in the temporary scene:\n{report}");

#if UNITY_EDITOR
                Debug.Log("Going to pause the editor to let inspect the stall gameobject.");
                Debug.Break();
#endif
            }
            else
            {
                yield return StartCoroutine(UnloadScene(_transition.LoadedScene));
            }
        }





        public IEnumerator Transition(SceneReference from, SceneReference to)
        {
            yield return Transition(from.LoadedScene, to.BuildIndex);
        }

        public IEnumerator Transition(Scene from, int to)
        {
            yield return Transition(from, to, _transition.BuildIndex);
        }

        public IEnumerator Transition(Scene from, int to, int transition)
        {
            var result = new SceneLoadingResult();

            yield return StartCoroutine(LoadScene(transition, result));

            Scene transitionScene = result.LoadedScene;

            yield return StartCoroutine(UnloadScene(from));

            SceneManager.SetActiveScene(transitionScene);

            yield return StartCoroutine(LoadScene(to, result));

            SceneManager.SetActiveScene(result.LoadedScene);

            foreach (var go in transitionScene.GetRootGameObjects())
            {
                SceneManager.MoveGameObjectToScene(go, result.LoadedScene);
            }

            yield return StartCoroutine(UnloadScene(transitionScene));
        }



        public IEnumerator LoadScene(int index)
        {
            yield return LoadScene(index, _cachedResult);
        }

        public IEnumerator LoadScene(int index, SceneLoadingResult result)
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(index, LoadSceneMode.Additive);
            
            while (!loadOperation.isDone)
            {
                yield return null;
            }
            
            result.LoadedScene = SceneManager.GetSceneByBuildIndex(index);
        }

        public IEnumerator UnloadScene(Scene scene)
        {
            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(scene);

            while (!unloadOperation.isDone)
            {
                yield return null;
            }
        }


        public class SceneLoadingResult
        {
            public Scene LoadedScene { get; set; }
        }
    }
}
