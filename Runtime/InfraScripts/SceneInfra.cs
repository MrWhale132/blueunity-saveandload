using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.Infrastructure.AddressableInfra;
using Assets._Project.Scripts.SaveAndLoad;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using System.Collections.Generic;
using System.Linq;
using Theblueway.Core.Runtime;
using Theblueway.Core.Runtime.Packages.com.blueutils.core.Runtime.ScriptResources;
using UnityEngine;

namespace Theblueway.SaveAndLoad.Packages.com.theblueway.saveandload.Runtime.InfraScripts
{
    [DefaultExecutionOrder(-100000)]
    [ExecuteInEditMode]
    public class SceneInfra : MonoBehaviour
    {
#if UNITY_EDITOR
        [Tooltip(StringResources.ActsLikeAButton)]
        public bool _collectScenePlacedGOInfras;
#endif


        public List<GOInfra> scenePlacedGOInfrasEditorView = new();
        public HashSet<GOInfra> ScenePlacedGOInfras { get; set; } = new();


        public List<AssetEntryReference> assetEntryReferences = new();

        public bool IsObjectLoading => SaveAndLoadManager.IsObjectLoading(this);



#if UNITY_EDITOR

        public void CollectInfras()
        {
            if (_collectScenePlacedGOInfras)
            {
                _collectScenePlacedGOInfras = false;
                ScenePlacedGOInfras.Clear();
                var allInfras = FindObjectsByType<GOInfra>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var infra in allInfras)
                {
                    ScenePlacedGOInfras.Add(infra);
                }
            }
        }
#endif



        private void OnValidate()
        {
#if UNITY_EDITOR
            CollectInfras();

            foreach (var assetRef in assetEntryReferences)
            {
                assetRef.UpdateReferenceIfNeeded();
            }
#endif
        }


        private void Awake()
        {
            if (!Application.isPlaying) return;

            if (Infra.Singleton == null) return;

            ScenePlacedGOInfras = scenePlacedGOInfrasEditorView.ToHashSet();

            ScenePlacedGOInfras.RemoveWhere(x => x.TurnedOff);


            //this is done by SceneInfra because not every gameobject is active at start, so GOInfra Awake may not be called
            foreach (var infra in ScenePlacedGOInfras)
            {
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
            if (!Application.isPlaying) return;
            if (Infra.Singleton == null) return;



            if (IsObjectLoading)
            {
                Infra.SceneManagement.SceneInfrasBySceneHandle.Add(gameObject.scene.handle, this);
                return;
            }


            var results = new List<GraphWalkingResult>();

            foreach (var infra in ScenePlacedGOInfras)
            {
                if (infra == null)
                {
                    Debug.LogError("Null GOInfra found in SceneInfra's _scenePlacedGOInfras list. Please fix the references. " +
                        "You might forgot to refresh the list after you removed GOInfra components from the scene.");
                    continue;
                }


                infra.RegisterAssetRefernces();


                if (infra.DescribeSceneObject(out var result))
                {
                    results.Add(result);
                }
            }

            SaveAndLoadManager.ScenePlacedObjectRegistry.Register(this, results);
        }


        private void OnDestroy()
        {
            if (!Application.isPlaying) return;

            if (Infra.Singleton == null) return;

            Infra.SceneManagement.SceneInfrasBySceneHandle.Remove(gameObject.scene.handle);
        }


        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                var didDomainReload = ScenePlacedGOInfras is null or { Count: 0 };

                if (didDomainReload)
                {
                    ScenePlacedGOInfras = scenePlacedGOInfrasEditorView.ToHashSet();
                }

                return;
            }
        }


        private void Update()
        {
            if (!Application.isPlaying)
            {
                ScenePlacedGOInfras.RemoveWhere(infra => infra == null);

                scenePlacedGOInfrasEditorView.Clear();
                scenePlacedGOInfrasEditorView.AddRange(ScenePlacedGOInfras);
                return;
            }
        }


        public List<ObjectMemberGraphWalker.MemberCollectionResult> CollectScenePlacedObjects()
        {
            var results = new List<ObjectMemberGraphWalker.MemberCollectionResult>();

            foreach (var infra in ScenePlacedGOInfras)
            {
                if (infra == null)
                {
                    Debug.LogError("Null GOInfra found in SceneInfra's _scenePlacedGOInfras list. Please fix the references. " +
                        "You might forgot to refresh the list after you removed GOInfra components from the scene.");
                    continue;
                }

                if (infra.HasAnySceneParts)
                {
                    infra.CollectSceneParts(out var result);
                    results.Add(result);
                }
            }

            return results;
        }
    }
}
