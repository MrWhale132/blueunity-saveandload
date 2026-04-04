using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.Infrastructure.AddressableInfra;
using Assets._Project.Scripts.SaveAndLoad;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using Assets._Project.Scripts.UtilScripts;
using System.Collections.Generic;
using System.Linq;
using Theblueway.Core;
using Theblueway.Core.Runtime.Packages.com.blueutils.core.Runtime.ScriptResources;
using UnityEditor;
using UnityEngine;
using static PlasticGui.WorkspaceWindow.Merge.MergeInProgress;

namespace Theblueway.SaveAndLoad.Packages.com.theblueway.saveandload.Runtime.InfraScripts
{
    [DefaultExecutionOrder(-100000)]
    //[ExecuteInEditMode]
    public class SceneInfra : MonoBehaviour
    {
#if UNITY_EDITOR
        [Tooltip(StringResources.ActsLikeAButton)]
        public bool _collectScenePlacedGOInfras;

        [Tooltip("Does not modify the state of the object. Only logs the erros so you can inspect them. " + StringResources.ActsLikeAButton)]
        public bool _triggerValidate;

        [Tooltip("Check this in to automaticly fix (that can be fixed) things while validating. " + StringResources.ActsLikeAButton)]
        public bool _applyFixesWhileValidating;
#endif


        [AutoGenerate]
        public RandomId _scopeId;


        public List<ChildInfraScope> _scenePlacedRootInfraScopes = new();

        //public List<GOInfra> scenePlacedGOInfrasEditorView = new();
        //public HashSet<GOInfra> ScenePlacedGOInfras { get; set; } = new();


        public List<AssetEntryReference> assetEntryReferences = new();

        public bool IsObjectLoading => SaveAndLoadManager.IsObjectLoading(this);



        private void OnValidate()
        {
#if UNITY_EDITOR
            CollectInfras();
            ValidateState();
#endif
        }



#if UNITY_EDITOR

        public void CollectInfras()
        {
            if (_collectScenePlacedGOInfras)
            {
                _collectScenePlacedGOInfras = false;

                HashSet<GOInfra> existingChildScopes = _scenePlacedRootInfraScopes
                    .Where(childScope => childScope != null && childScope.childInfra)
                    .Select(scope => scope.childInfra)
                    .ToHashSet();

                HashSet<GOInfra> foundRootInfras = gameObject.scene.GetRootGameObjects()
                    .Select(go => go.GetComponent<GOInfra>())
                    .Where(x => x != null)
                    .ToHashSet();


                foreach (var goInfra in foundRootInfras)
                {
                    if (!existingChildScopes.Contains(goInfra))
                    {
                        var scope = new ChildInfraScope { childInfra = goInfra, scopeId = RandomId.New };
                        _scenePlacedRootInfraScopes.Add(scope);
                    }

                    SetUpGOInfrasInHierarchy(goInfra.gameObject);
                }

                Debug.Log("Successfully collected scene placed GOInfras.");
            }
        }
#endif



#if UNITY_EDITOR

        public void ValidateState()
        {
            if (_triggerValidate)
            {
                _triggerValidate = false;

                Debug.Log("SceneInfra validation started",gameObject);


                if (_scopeId.IsDefault) _scopeId = RandomId.New;


                for (int i = assetEntryReferences.Count - 1; i >= 0; i--)
                {
                    var assetRef = assetEntryReferences[i];

                    assetRef.UpdateReference();

                    if (_applyFixesWhileValidating && !assetRef.isValid)
                    {
                        assetEntryReferences.RemoveAt(i);
                    }
                }


                for (int i = _scenePlacedRootInfraScopes.Count - 1; i >= 0; i--)
                {
                    bool isValid = true;

                    var scope = _scenePlacedRootInfraScopes[i];

                    if (scope == null) isValid = false;

                    if (scope.childInfra == null)
                    {
                        isValid = false;

                        _LogNullGOInfraInScope(scope);
                    }

                    if (scope.childInfra.transform.parent != null)
                    {
                        isValid = false;

                        _LogNonRootGOInfra(scope);
                    }

                    if (_applyFixesWhileValidating && !isValid)
                    {
                        string scopeId = scope != null ? scope.scopeId.ToString() : "null";
                        Debug.Log($"Removing child scope with id={scopeId} at list index={i} from SceneInfra because it is not valid.");

                        _scenePlacedRootInfraScopes.RemoveAt(i);
                    }
                }

                Debug.Log("SceneInfra validation finished", gameObject);
            }
        } 
#endif



        public static void _LogNullGOInfraInScope(ChildInfraScope scope)
        {
            Debug.LogError($"Null GOInfra reference in childscope with id={scope.scopeId}.");
        }

        public static void _LogNonRootGOInfra(ChildInfraScope scope)
        {
            Debug.LogError($"GOInfra with name={scope.childInfra.name} and id={scope.scopeId} is not a root object in the scene. Please make sure that all GOInfras in the _scenePlacedRootInfraScopes list are root objects in the scene. ");
        }


        private void Awake()
        {
            if (Infra.Singleton == null) return;


            //this is done by SceneInfra because not every gameobject is active at start, so GOInfra Awake may not be called
            //foreach (var infra in ScenePlacedGOInfras)
            foreach (var scope in _scenePlacedRootInfraScopes)
            {
                var infra = scope.childInfra;
                GOInfra.AddToAllInfras(infra);
            }


            foreach (var assetRef in assetEntryReferences)
            {
                if (!assetRef.isValid) continue;

                var initContext = new AssetInitContext { instantiatedFromAssetId = assetRef.assetId };

                Infra.S.Register(assetRef.asset, ifHasntAlready: true, context: initContext, rootObject: false, createSaveHandler: true);
            }
        }


        private void Start()
        {
            //if (!Application.isPlaying) return;
            if (Infra.Singleton == null) return;



            if (IsObjectLoading)
            {
                Infra.SceneManagement.SceneInfrasBySceneHandle.Add(gameObject.scene.handle, this);
                return;
            }

            foreach (var scope in _scenePlacedRootInfraScopes)
            {
                var infra = scope.childInfra;

                if (infra == null)
                {
                    _LogNullGOInfraInScope(scope);
                    continue;
                }
                else if(infra.transform.parent != null)
                {
                    _LogNonRootGOInfra(scope);
                }

                infra.RegisterAssetRefernces();
            }


            var descriptions = new List<ObjectDescription>();

            CollectScenePlacedObjectDescriptions(descriptions);

            RandomId sceneId = Infra.SceneManagement.SceneIdByHandle(gameObject.scene.handle);

            SaveAndLoadManager.ScenePlacedObjectRegistry.Register(sceneId, descriptions);
        }


        private void OnDestroy()
        {
            //if (!Application.isPlaying) return;

            if (Infra.Singleton == null) return;

            Infra.SceneManagement.SceneInfrasBySceneHandle.Remove(gameObject.scene.handle);
        }



        public void CollectScenePlacedObjectDescriptions(List<ObjectDescription> objectDescriptions)
        {
            var rootObjectDescription = new ObjectDescription
            {
                parentDescriptionId = default,
                descriptionId = RandomId.New,
                scopeId = _scopeId,
                members = null,
            };

            objectDescriptions.Add(rootObjectDescription);

            foreach (var childScope in _scenePlacedRootInfraScopes)
            {
                var infra = childScope.childInfra;

                if (infra == null)
                {
                    Debug.LogError("Null GOInfra found in SceneInfra's _scenePlacedGOInfras list. Please fix the references. " +
                        "You might forgot to refresh the list after you removed GOInfra components from the scene.");
                    continue;
                }

                infra.DescribeScenePlacedObject(rootObjectDescription, childScope.scopeId, objectDescriptions);
            }
        }




        public static bool HasSceneInfra(GameObject gameObject, out SceneInfra sceneInfra)
        {
            if (gameObject.scene.IsValid() && gameObject.scene.isLoaded)
            {
                var sceneInfraGO = gameObject.scene.GetRootGameObjects().FirstOrDefault(go => go.GetComponent<SceneInfra>() != null);

                if (sceneInfraGO != null)
                {
                    sceneInfra = sceneInfraGO.GetComponent<SceneInfra>();
                    return true;
                }
                else
                {
                    sceneInfra = null;
                    return false;
                }
            }
            else
            {
                sceneInfra = null;
                return false;
            }
        }




#if UNITY_EDITOR

        internal void SetUpGOInfrasInHierarchy(GameObject root)
        {
            var selfAndChildren = root.GetComponentsInChildren<GOInfra>(includeInactive: true);

            foreach (var goInfra in selfAndChildren)
            {
                goInfra._IsScenePlaced = true;
                EditorUtility.SetDirty(goInfra);
            }
        }



        public void AddRootInfra_Editor(GOInfra infra)
        {
            if (!_scenePlacedRootInfraScopes.Any(scope => scope != null && scope.childInfra == infra))
            {
                var scope = new ChildInfraScope { childInfra = infra, scopeId = RandomId.New };
                _scenePlacedRootInfraScopes.Add(scope);
            }
        }

        public void RemoveRootInfra_Editor(GOInfra infra)
        {
            var scope = _scenePlacedRootInfraScopes.FirstOrDefault(scope => scope != null && scope.childInfra == infra);

            if (scope != null)
            {
                _scenePlacedRootInfraScopes.Remove(scope);
            }
        }
#endif
    }
}
