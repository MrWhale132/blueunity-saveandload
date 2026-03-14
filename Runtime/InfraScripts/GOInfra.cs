using Assets._Project.Scripts.SaveAndLoad;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using Assets._Project.Scripts.UtilScripts;
using Assets._Project.Scripts.UtilScripts.Extensions;
using Packages.com.blueutils.core.Runtime.Misc;
using System.Collections.Generic;
using System.Linq;
using Theblueway.SaveAndLoad;
using Theblueway.SaveAndLoad.Packages.com.theblueway.saveandload.Runtime.InfraScripts;
using Theblueway.SaveAndLoad.Packages.com.theblueway.saveandload.Runtime.UtilsScripts.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using Assets._Project.Scripts.Infrastructure.AddressableInfra;
using System;
using Object = UnityEngine.Object;
using UnityEngine.UI;
using Theblueway.Core.Runtime.Packages.com.blueutils.core.Runtime.Debugging.Logging;
using Theblueway.Core.Runtime.Packages.com.blueutils.core.Runtime.Misc;


#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Assets._Project.Scripts.Infrastructure
{
    [DisallowMultipleComponent]
    public class GOInfra : MonoBehaviour, IEditorValidatable
    {
        //not used currently
        public static HashSet<GOInfra> _allInfras = new HashSet<GOInfra>();


        [Tooltip("With this toggle this instance will act like an inactive component AND will be ignored by external systems like " + nameof(SceneInfra) +
                 ".\n The primary purpose of this toggle is to test out what would happen if this component wasnt there.")]
        public bool TurnedOff;


#if UNITY_EDITOR
        public bool _IsPrefabasset;
        [ReadOnly]
        public string _PrefabAssetPath;
#endif
        public RandomIdReference ReferencedPrefabAssetId; //note: the referenced id can still be default
        public RandomId InlinedPrefabAssetId;
        public RandomId PrefabAssetId => InlinedPrefabAssetId.IsNotDefault ? InlinedPrefabAssetId
                                            : ReferencedPrefabAssetId != null ? ReferencedPrefabAssetId.Id : RandomId.Default;

        //design decision: why two seperate descriptors for prefab and scene-placed instances?
        //Why not just one descriptor if these descriptors are not tied to specific workflows?
        //Answer: because when a prefab is dragged into a scene, you may not want to save it the same way you would a prefab instance, vica-versa.
        public ObjectDescriptor PrefabDescriptor;
        public ObjectDescriptor ScenePlacedDescriptor;

        public InlinedObjectDescription InlinedPrefabDescription;
        public InlinedObjectDescription InlinedScenePlacedDescription;


        public bool IsPrefabRoot => PrefabAssetId.IsNotDefault;
        public bool HasPrefabParts => PrefabDescriptor != null;
        public bool HasSceneParts => ScenePlacedDescriptor != null;
        public bool HasInlinedPrefabParts => InlinedPrefabDescription != null;
        public bool HasInlinedSceneParts => InlinedScenePlacedDescription != null;
        public bool HasAnySceneParts => HasSceneParts || HasInlinedSceneParts;
        public bool HasAnyPrefabParts => HasPrefabParts || HasInlinedPrefabParts;


        public bool IsObjectLoading => SaveAndLoadManager.IsObjectLoading(this);
        public bool InfraIsNotPresent => Infra.Singleton == null;
        public bool ReturnEarly => TurnedOff || InfraIsNotPresent || IsObjectLoading;




        /// note: Awake may not be called if the gameobject is deactivated either as part of a prefab or scene placed.
        /// everything that should run anyway, place it in a method and let <see cref="SceneInfra"/> call it if it is sceneplaced
        /// root of the prefabs are never inactive when instantiated so they are fine.
        public void Awake()
        {
            if (ReturnEarly) return;

            _allInfras.Add(this);

            bool registered = Infra.Singleton.IsRegistered(gameObject);

            if (!registered)
            {
                if (IsPrefabRoot)
                {
                    DescribePrefab();
                }

                bool isManaged = HasAnyPrefabParts || HasAnySceneParts;

                if (!isManaged)
                {
                    SetupUnmanagedInstance();
                }
            }
        }


        //note: on sceneplaced or prefab part gameobject that are disabled from the start, Awake and Start wont be called
        //and thus, the actions inside them may not be executed
        private void Start()
        {
            if (ReturnEarly) return;
        }




        public void SetupUnmanagedInstance()
        {
            Infra.Singleton.Register(gameObject, rootObject: true, createSaveHandler: true);

            List<Component> components = new List<Component>();

            gameObject.GetComponents(typeof(Component), components);


            for (int i = 0; i < components.Count; i++)
            {
                var comp = components[i];

                var registered = Infra.Singleton.IsRegistered(comp);

                if (!registered)
                    Infra.Singleton.Register(comp, rootObject: true, createSaveHandler: true);
            }
        }









        public void OnDestroy()
        {
            if (ReturnEarly) return;
            Unregister();
        }

        [HideInInspector]
        public bool _isUnregistered = false;


        public void Unregister()
        {
            if (_isUnregistered) return;


            List<Component> components = new List<Component>();

            gameObject.GetComponents(typeof(Component), components);

            for (int i = 0; i < components.Count; i++)
            {
                var component = components[i];

                Infra.Singleton.Unregister(component);
            }
            //SaveAndLoadManager.Singleton.RemoveSaveHandler(__goSaveHandler);

            Infra.Singleton.Unregister(gameObject);

            _allInfras.Remove(this);
            _isUnregistered = true;
        }



        public static void AddToAllInfras(GOInfra infra)
        {
            if (_allInfras.Contains(infra)) return;

            _allInfras.Add(infra);
        }


        public static HashSet<GOInfra> GetInfrasByScene(Scene scene)
        {
            var result = new HashSet<GOInfra>();

            foreach (var infra in _allInfras)
            {
                if (infra == null)
                {
                    continue;
                }

                if (infra.gameObject.scene == scene)
                {
                    result.Add(infra);
                }
            }

            return result;
        }








        public bool DescribeSceneObject(out GraphWalkingResult result)
        {
            bool createdDescription = false;
            result = null;

            var saveHandlerInitContext = new InitContext { isScenePlaced = true };


            if (HasSceneParts)
            {
                createdDescription = true;

                var walker = new ObjectMemberGraphWalker(ScenePlacedDescriptor);

                result = walker.Walk(gameObject, saveHandlerInitContext);
            }

            if (HasInlinedSceneParts)
            {
                createdDescription = true;

                if (result is null) result = new(init: true);

                foreach (var memberDesc in InlinedScenePlacedDescription.members)
                {
                    if (memberDesc.member == null)
                    {
                        Debug.LogError($"A null list element found in the inlined scene member element list of gameobject: {gameObject.HierarchyPath()}.\n" +
                            $"This is invalid and it should be removed. You can remove it manually or by triggering a recollection of members.\n" +
                            $"Going to ignore it and continue on.");
                        continue;
                    }


                    if (Infra.S.IsNotRegistered(memberDesc.member))
                    {
                        var id = Infra.Singleton.Register(memberDesc.member, context: saveHandlerInitContext, rootObject: false, createSaveHandler: true);

                        result.memberIds.Add(memberDesc.memberId);
                        result.generatedIds.Add(id);
                    }
                }
            }

            _CheckIfSelfIsRegistered(scenePlaced: true);

            return createdDescription;
        }



        public void DescribePrefab()
        {
            //if (PrefabDescriptor == null)
            //{
            //    Debug.LogError($"GOInfra on GameObject '{gameObject.HierarchyPath()}' has a PrefabAssetId assigned but no PrefabDescriptor. A PrefabDescriptor is required if a PrefabAssetId is assigned.", gameObject);
            //}

            RegisterAssetRefernces();

            var childrenAndSelf = GetChildrenInfra(includeTurnedOff: false, includeSelf: true);

            var results = new List<GraphWalkingResult>();

            foreach (var child in childrenAndSelf)
            {
                if (child.DescribePrefab(out var result))
                {
                    results.Add(result);
                }
            }

            _CheckIfSelfIsRegistered(prefabInstance: true);

            SaveAndLoadManager.PrefabDescriptionRegistry.Register(this, results);
        }


        public bool DescribePrefab(out GraphWalkingResult result)
        {
            bool createdDescription = false;
            result = null;

            var saveHandlerInitContext = new InitContext { isPrefabPart = true };


            if (HasPrefabParts)
            {
                createdDescription = true;

                var walker = new ObjectMemberGraphWalker(PrefabDescriptor);

                result = walker.Walk(gameObject, saveHandlerInitContext);
            }

            if (HasInlinedPrefabParts)
            {
                createdDescription = true;

                result ??= new(init: true);

                foreach (var memberDesc in InlinedPrefabDescription.members)
                {
                    if (memberDesc.member == null)
                    {
                        Debug.LogError($"A null list element found in the inlined prefab member element list of gameobject: {gameObject.HierarchyPath()}.\n" +
                            $"This is invalid and it should be removed. This most often happens when you remove a component or a child and you dont refresh " +
                            $"the prefab member list. ({nameof(InlinedPrefabDescription)}). " +
                            $"You can remove it manually or by triggering a recollection of members.\n" +
                            $"Going to ignore it and continue on.");
                        continue;
                    }


                    if (Infra.S.IsNotRegistered(memberDesc.member))
                    {
                        var id = Infra.Singleton.Register(memberDesc.member, context: saveHandlerInitContext, rootObject: false, createSaveHandler: true);

                        result.memberIds.Add(memberDesc.memberId);
                        result.generatedIds.Add(id);
                    }
                }
            }

            return createdDescription;
        }



        public void _CheckIfSelfIsRegistered(bool prefabInstance = false, bool scenePlaced = false)
        {
            if (Infra.S.IsNotRegistered(this))
            {
                string baseMessage;

                if (prefabInstance)
                {
                    baseMessage = $"This instance is marked as an instance of a prefab asset";
                }
                else if (scenePlaced)
                {
                    ///doc: an infra instance can become sceneplaced too if it is in <see cref="SceneInfra"/>'s list
                    baseMessage = $"This instance is marked as a scene placed object";
                }
                else
                {
                    baseMessage = $"This instance is marked as either a scene placed object or as an instance of a prefab asset";
                }

                Debug.LogError($"GOInfra: {baseMessage}, but at the end of collecting objects that belong under it, " +
                    $"component {nameof(GOInfra)} itself is not present, which is invalid.\n" +
                    $"Make sure to add this component to the prefab or scene parts in the editor.\n" +
                    $"Hierarhcy path: {gameObject.HierarchyPath()}", this);
            }
        }




        public List<ObjectMemberGraphWalker.MemberCollectionResult> CollectPrefabParts()
        {
            var results = new List<ObjectMemberGraphWalker.MemberCollectionResult>();

            var childrenAndSelf = GetChildrenInfra(includeTurnedOff: false, includeSelf: true);

            var walker = new ObjectMemberGraphWalker();

            //if (HasPrefabParts)
            //var result = walker.CollectMembersV2(gameObject, PrefabDescriptor);
            //results.Add(result);


            HashSet<RandomId> added = new();

            foreach (var child in childrenAndSelf)
            {
                if (child.HasPrefabParts)
                {
                    var result = walker.CollectMembersV2(child.gameObject, child.PrefabDescriptor);

                    results.Add(result);
                }

                if (child.HasInlinedPrefabParts)
                {
                    var result = new ObjectMemberGraphWalker.MemberCollectionResult(init: true);

                    foreach (var memberDesc in InlinedPrefabDescription.members)
                    {
                        if (added.Contains(memberDesc.memberId)) Debug.LogError("ERROR: Multiple members of a prefabdescription shares the same memberid." +
                            $"MemberId: {memberDesc.memberId}, member name: {memberDesc.member.name}, GameObject name: {gameObject.name}", gameObject);

                        //this may add the same member multiple times but with different ids, which, theoretically, should not be a problem
                        //because it doesnt matter if a member has multiple ids as long as those ids point back to the same member
                        result.membersById.Add(memberDesc.memberId, memberDesc.member);

                    }

                    results.Add(result);
                }
            }


            return results;
        }



        public bool CollectSceneParts(out ObjectMemberGraphWalker.MemberCollectionResult result)
        {
            result = null;

            if (HasSceneParts)
            {
                var walker = new ObjectMemberGraphWalker();

                result = walker.CollectMembersV2(gameObject, ScenePlacedDescriptor);

                ///here we dont need to iterate children because <see cref="SceneInfra"/> calls each inidividual GOInfra
            }

            if (HasInlinedSceneParts)
            {
                result ??= new(init: true);

                foreach (var memberDesc in InlinedScenePlacedDescription.members)
                {
                    if (result.membersById.ContainsKey(memberDesc.memberId)) Debug.LogError("ERROR: Multiple members of a prefabdescription shares the same memberid." +
                        $"MemberId: {memberDesc.memberId}, member name: {memberDesc.member.name}, GameObject name: {gameObject.name}", gameObject);

                    //this may add the same member multiple times but with different ids, which, theoretically, should not be a problem
                    //because it doesnt matter if a member has multiple ids as long as those ids point back to the same member
                    result.membersById.Add(memberDesc.memberId, memberDesc.member);
                }
            }

            return result != null;
        }





        public void RegisterAssetRefernces()
        {
            if (_assetReferences.IsNotNullAndNotEmpty())
            {
                foreach (var referenceInfo in _assetReferences)
                {
                    foreach (var assetReference in referenceInfo.references)
                    {
                        var assetRegistrations = _GetAssetFromAssetReference(assetReference, referenceInfo.assetHolder);

                        foreach (var assetReg in assetRegistrations)
                            if (Infra.S.IsNotRegistered(assetReg.asset))
                            {
                                var initContext = new AssetInitContext()
                                {
                                    instantiatedFromAssetId = assetReference.assetEntryInfo.assetId,
                                    mutable = assetReg.mutable,
                                };

                                Infra.S.Register(assetReg.asset, context: initContext, rootObject: false, createSaveHandler: true);
                            }
                    }
                }
            }
        }



        public struct AssetRegistration
        {
            public Object asset;
            public bool mutable;

            public static implicit operator AssetRegistration(Object asset)
            {
                var reg = new AssetRegistration()
                {
                    asset = asset,
                    mutable = false,
                };
                return reg;
            }
        }


        public IEnumerable<AssetRegistration> _GetAssetFromAssetReference(AssetReferenceInfo assetReference, Component comp)
        {
            List<AssetRegistration> assetRegistrations = new();

            var member = assetReference.memberPath.memberEnum;
            int index = assetReference.memberPath.index;

            if (comp is MeshFilter meshFilter)
            {
                if (member == TypeMemberEnum.Mesh)
                {
                    if (assetReference.useNonSharedCounterpartToo)
                        assetRegistrations.Add(new() { asset = meshFilter.mesh, mutable = true });
                    assetRegistrations.Add(meshFilter.sharedMesh);
                }
            }
            else if (comp is MeshRenderer meshRenderer)
            {
                if (member == TypeMemberEnum.Materials)
                {
                    if (assetReference.useNonSharedCounterpartToo)
                    {
                        assetRegistrations.Add(new() { asset = meshRenderer.materials[index], mutable = true });
                    }
                    assetRegistrations.Add(meshRenderer.sharedMaterials[index]);
                }
                if (member == TypeMemberEnum.Shader)
                {
                    //if (assetReference.useNonSharedCounterpartToo)
                    //{
                    //    assetRegistrations.Add(meshRenderer.materials[index].shader);
                    //}
                    assetRegistrations.Add(meshRenderer.sharedMaterials[index].shader);
                }
            }
            else if (comp is Image image)
            {
                if (member == TypeMemberEnum.Sprite) assetRegistrations.Add(image.sprite);
                if (member == TypeMemberEnum.Material) assetRegistrations.Add(image.material);
                if (member == TypeMemberEnum.Shader) assetRegistrations.Add(image.material.shader);
            }
            else if (comp is AudioSource audioSource)
            {
                if (member == TypeMemberEnum.OutputAudioMixerGroup) assetRegistrations.Add(audioSource.outputAudioMixerGroup);
                if (member == TypeMemberEnum.Resource) assetRegistrations.Add(audioSource.resource);
            }
            else
            {
                Debug.LogError("You forget to add the new comp type here too.");
            }

            return assetRegistrations;
        }




        [Serializable]
        public class TypeMemberEnum
        {
            [SerializeField]
            internal int _memberId;

            private TypeMemberEnum(int memberId)
            {
                _memberId = memberId;
            }

            //never change existing values, they were serialized with that value, if you change them, equality comparsions will break
            public static TypeMemberEnum Material = new(0);
            public static TypeMemberEnum SharedMaterial = new(1);
            public static TypeMemberEnum Materials = new(2);
            public static TypeMemberEnum SharedMaterials = new(3);
            public static TypeMemberEnum Mesh = new(4);
            public static TypeMemberEnum SharedMesh = new(5);
            public static TypeMemberEnum Sprite = new(6);
            public static TypeMemberEnum OutputAudioMixerGroup = new(7);
            public static TypeMemberEnum Resource = new(8);
            public static TypeMemberEnum Shader = new(9);


            public override bool Equals(object obj) => obj is TypeMemberEnum memberEnum && memberEnum._memberId == _memberId;
            public override int GetHashCode() => _memberId;
            public override string ToString() => _memberId.ToString();

            public static bool operator !=(TypeMemberEnum left, TypeMemberEnum right) => !(left == right);
            public static bool operator ==(TypeMemberEnum left, TypeMemberEnum right)
            {
                if (left is null && right is null) return true;
                if (left is null || right is null) return false;
                if (left._memberId == right._memberId) return true;
                else return false;
            }

            public static implicit operator TypeMemberPath(TypeMemberEnum memberEnum)
            {
                return new TypeMemberPath(memberEnum, index: 0);
            }
        }



        [Serializable]
        public class AssetReferencingDisplayInfo
        {
            public Component assetHolder;

            public List<AssetReferenceInfo> references;
        }


        [Serializable]
        public class AssetReferenceInfo
        {
#if UNITY_EDITOR
            [ReadOnly]
            [Tooltip("This reference to the asset is for information purposes only. This is not the reference that will be used later.")]
            public Object asset;
#endif
            [Tooltip("Set this to True if you will access the non-shared property of this component.\n" +
                "For example if anypoint you will access the .mesh property of a MeshFilter.\n" +
                "This only has effect on properties where just by accessing the non-shared counterpart creates a copy. Default is False")]
            public bool useNonSharedCounterpartToo;
            [HideInInspector]
            public TypeMemberPath memberPath;
            public AssetEntryInfo assetEntryInfo;
        }


        [Serializable]
        public class TypeMemberPath
        {
            public TypeMemberEnum memberEnum;
            public int index; //in case of arrays

            public TypeMemberPath(TypeMemberEnum memberEnum, int index)
            {
                this.memberEnum = memberEnum;
                this.index = index;
            }
        }


#if UNITY_EDITOR
        [HideInInspector]
        public bool _lastKnownMarkAssetMutableValue;

        [Tooltip("Setting this will mark all components all properties as mutable where they have a automatic shared/non-shared semantics.")]
        public bool _markAssetsMutable;
#endif

        [Tooltip("Consider this list as read-only. Any modifications to it will be lost on next refresh")]
        public List<AssetReferencingDisplayInfo> _assetReferences = new();



#if UNITY_EDITOR
        public void RefreshReferencedAssets()
        {
            _assetReferences.Clear();

            List<Component> helper = new();
            List<Component> componentsToCheck = new();


            void Traverse(Transform parent)
            {
                parent.GetComponents(helper);
                componentsToCheck.AddRange(helper);

                for (int i = 0; i < parent.childCount; i++)
                {
                    var child = parent.GetChild(i);
                    Traverse(child);
                }
            }

            Traverse(gameObject.transform);



            List<(Object member, TypeMemberPath memberPath)> assetMembers = new();


            //note: take into account that for a lot of type of assets, even if there is nothing assigned to the field,
            //unity will still assign an internal "dummy" instance to that field, which undermines null checks. For example PhysicsMaterial of a collider

            //todo: sooner or later this has to be split up and scattered, because we can not refernce every single unity assembly there are to inspect their types
            foreach (var comp in componentsToCheck)
            {
                assetMembers.Clear();

                if (comp is MeshFilter meshFilter)
                {
                    assetMembers.Add((meshFilter.sharedMesh, TypeMemberEnum.Mesh));
                }
                else if (comp is MeshRenderer meshRenderer)
                {
                    int i = 0;
                    foreach (var mat in meshRenderer.sharedMaterials)
                    {
                        assetMembers.Add((mat, new(TypeMemberEnum.Materials, i)));
                        assetMembers.Add((mat.shader, new(TypeMemberEnum.Shader, i)));

                        i++;
                    }
                }
                else if (comp is Image image)
                {
                    assetMembers.Add((image.sprite, TypeMemberEnum.Sprite));
                    assetMembers.Add((image.material, TypeMemberEnum.Material));
                    assetMembers.Add((image.material.shader, TypeMemberEnum.Shader));
                }
                else if (comp is AudioSource audioSource)
                {
                    assetMembers.Add((audioSource.outputAudioMixerGroup, TypeMemberEnum.OutputAudioMixerGroup));
                    assetMembers.Add((audioSource.resource, TypeMemberEnum.Resource));
                }



                //todo: remove the default ui material filter
                //todo: configurable and extendable for users
                assetMembers.RemoveAll(asset => asset.member == null || asset.member.name == "" || asset.member.name == "Default UI Material");

                if (assetMembers.Count > 0)
                {
                    List<AssetReferenceInfo> referenceInfos = new(assetMembers.Count);

                    foreach (var (member, memberPath) in assetMembers)
                    {
                        var asset = member;

                        var assetEntryInfo = AddressableDb.Singleton.GetAssetEntryInfo(asset);

                        if (assetEntryInfo == null)
                        {
                            Debug.LogError($"Can not set asset reference for asset {asset.name} because the asset db could not find an entry for it.\n" +
                                $"asset type: {asset.GetType().Name}", comp);
                            continue;
                        }

                        referenceInfos.Add(new AssetReferenceInfo
                        {
                            asset = asset,
                            memberPath = memberPath,
                            assetEntryInfo = assetEntryInfo,
                        });
                    }

                    if (referenceInfos.Count > 0)
                        _assetReferences.Add(new AssetReferencingDisplayInfo
                        {
                            assetHolder = comp,
                            references = referenceInfos,
                        });
                }
            }


            CheckAssetReferenceSettings();
        }
#endif









        private void OnValidate()
        {
            EditorValidationService.RequestValidation(this);
        }


        public void EditorValidate()
        {
#if UNITY_EDITOR
            if (ReferencedPrefabAssetId != null && InlinedPrefabAssetId.IsNotDefault)
            {
                BlueDebug.Error($"You cant have both a {nameof(ReferencedPrefabAssetId)} and a {InlinedPrefabAssetId} at the same time.\n" +
                    $"{nameof(InlinedPrefabAssetId)} will be set to default.");
            }
            else if (_IsPrefabasset && ReferencedPrefabAssetId == null && InlinedPrefabAssetId.IsDefault)
            {
                if (!gameObject.scene.isLoaded)
                {
                    var assetEntry = AddressableDb.Singleton.GetAssetEntryInfo(this.gameObject);

                    if (assetEntry != null)
                    {
                        InlinedPrefabAssetId = assetEntry.assetId;
                        _PrefabAssetPath = assetEntry.assetPath;
                    }
                    else
                    {
                        BlueDebug.Warn($"GOInfra: Could not find asset id for prefab {gameObject.name}. " +
                            $"You may see this message because you are in the prefab editor view.\n(click to follow)", this);
                    }
                }
                //even in this delayed call back, serialization of the object is still not completed, values are not fully set
                //(perhaps because it sends multiple OnValidate accross multiple frames?)
                ///here the problem is that <see cref="_IsPrefabasset"/> comes with true value when it is really not
                //else
                //{
                //    Debug.Log($"GOInfra: Prefab editor scene instances of assets can not be looked up by AssetDb, thus, " +
                //        $"can not assign prefab assetid automaticly. You can still do it manually or by leaving the prefab editor scene " +
                //        $"and triggering OnValidate again");
                //}
            }

            CheckAssetReferenceSettings();

            CheckIfStillPrefabRoot();
#endif
        }


        private void Reset()
        {
            if (gameObject.scene.IsValid())
            {
                var sceneInfraGO = gameObject.scene.GetRootGameObjects().FirstOrDefault(go => go.GetComponent<SceneInfra>() != null);

                if (sceneInfraGO != null)
                {
                    var sceneInfra = sceneInfraGO.GetComponent<SceneInfra>();

                    sceneInfra.ScenePlacedGOInfras.Add(this);
                }
            }
        }




#if UNITY_EDITOR

        public void CheckIfStillPrefabRoot()
        {
            ///doc:
            ///an instance of a prefab can beacome a child of an other prefab asset when dragged under its hierarchy in editor
            ///If this is the case, this instance is no longer a prefab root

            //todo: if prefabassetid just assigned, clear child prefabassetids
            bool prefabRootCandidate = PrefabAssetId.IsNotDefault;

            if (prefabRootCandidate)
            {
                var parent = gameObject.transform.parent;

                while (parent != null)
                {
                    if (parent.TryGetComponent<GOInfra>(out var parentInfra))
                    {
                        if (parentInfra.PrefabAssetId.IsNotDefault)
                        {
                            ReferencedPrefabAssetId = null;
                            InlinedPrefabAssetId = RandomId.Default;
                            break;
                        }
                    }

                    parent = parent.parent;
                }
            }
        }




        public void CheckAssetReferenceSettings()
        {
            //todo: check if the referenced comps became null


            if (_lastKnownMarkAssetMutableValue != _markAssetsMutable)
            {
                _lastKnownMarkAssetMutableValue = _markAssetsMutable;

                List<AssetReferenceInfo> eligibleReferences = new();

                foreach (var displayInfo in _assetReferences)
                {
                    if (displayInfo.assetHolder.GetType() == typeof(MeshFilter))
                    {
                        foreach (var reference in displayInfo.references)
                        {
                            if (reference.memberPath.memberEnum == TypeMemberEnum.Mesh)
                                eligibleReferences.Add(reference);
                        }
                    }
                    else if (displayInfo.assetHolder.GetType() == typeof(MeshRenderer))
                    {
                        foreach (var reference in displayInfo.references)
                        {
                            if (reference.memberPath.memberEnum == TypeMemberEnum.Materials)
                                eligibleReferences.Add(reference);
                        }
                    }
                }

                foreach (var reference in eligibleReferences)
                {
                    reference.useNonSharedCounterpartToo = _markAssetsMutable;
                }
            }
        }
#endif








        [HideInInspector]
        public List<GOInfra> _children = new();

        public List<GOInfra> GetChildrenInfra(bool includeTurnedOff, bool includeSelf = false)
        {
            var children = new List<GOInfra>();

            if (includeSelf)
            {
                _children.Clear();
                gameObject.GetComponentsInChildren<GOInfra>(includeInactive: true, _children);
                children = _children;
            }
            else
            {
                children = gameObject.GetComponentsInChildrenExcludeSelf<GOInfra>(includeInactive: true).ToList();
            }

            if (!includeTurnedOff)
            {
                children.RemoveAll(child => child.TurnedOff);
            }

            return children;
        }



#if UNITY_EDITOR
        //editor only
        public void AddInfraToAllChildren()
        {
            foreach (Transform child in transform)
            {
                var childInfra = child.GetComponent<GOInfra>();
                if (childInfra == null)
                {
                    childInfra = child.gameObject.AddComponent<GOInfra>();
                }

                childInfra.AddInfraToAllChildren();
            }

            OnValidate();
        }


        public void RemoveInfraFromAllChildren()
        {
            var children = gameObject.GetComponentsInChildrenExcludeSelf<GOInfra>(includeInactive: true);

            foreach (GOInfra child in children)
            {
                DestroyImmediate(child);
            }
        }
#endif

        [HideInInspector]
        [ReadOnly]
        public List<Component> _cachedComponents = new();

#if UNITY_EDITOR


        public void CacheComponentsInChildrenAndSelf()
        {
            GetComponents(_cachedComponents);

            foreach (Transform child in transform)
            {
                if (child.TryGetComponent<GOInfra>(out var childInfra))
                {
                    childInfra.CacheComponentsInChildrenAndSelf();
                }
            }
        }
#endif

#if UNITY_EDITOR
        //tells if we have already hooked-up to the SceneView delegate, once, some time ago
        [System.NonSerialized]
        bool linked_SV = false;

        public void OnDrawGizmos()
        {
            if (linked_SV == false)
            {
                linked_SV = true;
                SceneView.duringSceneGui -= OnSceneDraw;
                SceneView.duringSceneGui += OnSceneDraw;
            }
        }


        void OnSceneDraw(SceneView sceneView)
        {

            try
            {
                if (gameObject) { };
            }
            catch
            {
                SceneView.duringSceneGui -= OnSceneDraw;
                //Debug.Log("on destroy");
                //custom logic here, but no unity api calls on 'this'

                return;
            }
        }

#endif




        [SaveHandler(id: 67685676547, dataGroupName: nameof(GOInfra), typeof(GOInfra))]
        public class GOInfraSaveHandler : MonoSaveHandler<GOInfra, GOInfraSaveData>
        {

        }
    }


    public class GOInfraSaveData : MonoSaveDataBase
    {
        public bool IsNetworked;
        public bool IsUIElement;
    }
}