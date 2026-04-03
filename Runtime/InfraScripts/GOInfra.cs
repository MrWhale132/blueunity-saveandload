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
using Theblueway.Core.Runtime.Debugging.Logging;
using Theblueway.Core.Runtime.Packages.com.blueutils.core.Runtime.Misc;
using Theblueway.Core.Runtime.DataStructures;
using Theblueway.Core;










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


        //not finished feature
        [HideInInspector]
        [Tooltip("With this toggle this instance will act like an inactive component AND will be ignored by external systems like " + nameof(SceneInfra) +
                 ".\n The primary purpose of this toggle is to test out what would happen if this component wasnt there.")]
        public bool TurnedOff;


        [ReadOnly]
        public bool _IsRootObject;

        [ReadOnly]
        public bool _IsScenePlaced;


#if UNITY_EDITOR
        public bool _IsPrefabasset;
        [ReadOnly]
        public string _PrefabAssetPath;
#endif
        public RandomId InlinedPrefabAssetId;

        [Tooltip("Use this to assign a prefab assetid manually if for some reason "+nameof(InlinedPrefabAssetId)+ " can not be assigned automaticly. " + nameof(InlinedPrefabAssetId) +" takes precedence.")]
        public RandomIdReference ReferencedPrefabAssetId; //note: the referenced id can still be default


        //design decision: why two seperate descriptors for prefab and scene-placed instances?
        //Why not just one descriptor if these descriptors are not tied to specific workflows?
        //Answer: because when a prefab is dragged into a scene, you may not want to save it the same way you would a prefab instance, vica-versa.
        public InlinedObjectDescription InlinedPrefabDescription;
        public InlinedObjectDescription InlinedScenePlacedDescription;


        [NonSerialized]
        public List<RandomId> _registeredInstanceIdsOfAuthoredObjects;


#if UNITY_EDITOR
        [Tooltip("Setting this will mark all components' all properties as mutable if they have an automatic shared/non-shared semantics.")]
        public bool _markAssetsMutable;
#endif

        [Tooltip("Consider this list as read-only. Any modifications to it will be lost on next refresh")]
        public InheritableList<AssetReferencingDisplayInfo> _assetReferences = new();


        //todo: fucks up ui
        //[ReadOnly]
        [Tooltip("This list should not be editited manually. Changes to it will be lost on next refresh. This is a list of child scopes that are directly under this scope in the hierarchy. This is used to walk the hierarchy of scopes when describing or saving objects under this scope.")]
        public InheritableList<ChildInfraScope> _immediateChildScopes;


        //todo: conditional hide
        [AutoGenerate]
        [Tooltip("This field is used only if the object is a root object.")]
        public RandomId _scopeId;


        //todo: Tooltip
        [ReadOnly]
        public GOInfra _rootInfraCandidate;


        [Header("Debug")]
        [ReadOnly]
        public GOInfra _parentScope;




        public bool IsPrefabRoot => PrefabAssetId.IsNotDefault;
        public bool HasInlinedPrefabParts => InlinedPrefabDescription != null;
        public bool HasInlinedSceneParts => InlinedScenePlacedDescription != null;
        public RandomId PrefabAssetId => InlinedPrefabAssetId.IsNotDefault ? InlinedPrefabAssetId
                                            : ReferencedPrefabAssetId != null ? ReferencedPrefabAssetId.Id : RandomId.Default;

        public bool ShouldRegisterPrefabDescription => IsPrefabRoot && _IsRootObject && !_IsScenePlaced;



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
                    DescribeAndRegisterPrefab();
                }

                bool isManaged = HasInlinedPrefabParts || HasInlinedSceneParts;

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
            var goId = Infra.Singleton.Register(gameObject, rootObject: true, createSaveHandler: true);

            _registeredInstanceIdsOfAuthoredObjects.Add(goId);


            List<Component> components = new List<Component>();

            gameObject.GetComponents(typeof(Component), components);


            for (int i = 0; i < components.Count; i++)
            {
                var comp = components[i];

                var registered = Infra.Singleton.IsRegistered(comp);

                if (!registered)
                {
                    var id = Infra.Singleton.Register(comp, rootObject: true, createSaveHandler: true);

                    _registeredInstanceIdsOfAuthoredObjects.Add(id);
                }
            }
        }









        public void OnDestroy()
        {
            if (ReturnEarly) return;
            RemoveFromSceneInfraIfNeeded();
            Unregister();
        }


        [HideInInspector]
        public bool _isUnregistered = false;


        public void Unregister()
        {
            if (_isUnregistered) return;
            _isUnregistered = true;


            if (_registeredInstanceIdsOfAuthoredObjects.IsNotNullAndNotEmpty())
            {
                foreach (var id in _registeredInstanceIdsOfAuthoredObjects)
                {
                    Infra.S.Unregister(id);
                }
            }

            _allInfras.Remove(this);
        }



#if UNITY_EDITOR

        public void RemoveFromSceneInfraIfNeeded()
        {
            if (!Application.isPlaying && gameObject.transform.parent == null && SceneInfra.HasSceneInfra(gameObject, out var sceneInfra))
            {
                sceneInfra.RemoveRootInfra_Editor(this);
            }
        }

#endif


        public static void AddToAllInfras(GOInfra infra)
        {
            if (_allInfras.Contains(infra)) return;
            //todo: add child scopes too
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








        public void DescribeAndRegisterPrefab()
        {
            RegisterAssetRefernces();


            if (_scopeId.IsDefault)
            {
                //Debug.LogError($"{nameof(_scopeId)} is not set. Root {nameof(GOInfra)}s of prefabs have to have a {nameof(_scopeId)} set manually.", this);
                Debug.LogError($"{nameof(_scopeId)} is not set. It must be set by collecting child scopes.", this); ///<see cref="CollectImmediateChildScopes()"/>
            }


            List<ObjectDescription> objectDescriptions = new();

            DescribePrefab(objectDescriptions);

            SaveAndLoadManager.PrefabDescriptionRegistry.Register(PrefabAssetId, objectDescriptions, out _registeredInstanceIdsOfAuthoredObjects);

            _CheckIfSelfIsRegistered(prefabInstance: true);
        }


        public void DescribePrefab(List<ObjectDescription> objectDescriptions)
        {
            var jumpStartDescription = new ObjectDescription
            {
                parentDescriptionId = default,
                scopeId = default,
                descriptionId = default,
            };


            DescribeObject(jumpStartDescription, _scopeId, objectDescriptions, InlinedPrefabDescription);
        }


        public void DescribeScenePlacedObject(ObjectDescription parentDescription, RandomId scopeId, List<ObjectDescription> objectDescriptions)
        {
            DescribeObject(parentDescription, scopeId, objectDescriptions, InlinedScenePlacedDescription);
        }




        public void DescribeObject(ObjectDescription parentDescription, RandomId scopeId, List<ObjectDescription> objectDescriptions, InlinedObjectDescription objectDescriptor)
        {
            var description = new ObjectDescription
            {
                parentDescriptionId = parentDescription.descriptionId,
                scopeId = scopeId,
                descriptionId = RandomId.New,
            };


            if (objectDescriptor != null)
            {
                foreach (var member in objectDescriptor.members.CombinedItems)
                {
                    if (member.member == null)
                    {
                        Debug.LogError($"A null list element found in the inlined member element list of gameobject: {gameObject.HierarchyPath()}.\n" +
                            $"This is invalid and it should be removed. This most often happens when you remove a component or a child and you dont refresh " +
                            $"the member list. " +
                            $"You can remove it manually or by triggering a recollection of members.\n" +
                            $"Going to ignore it and continue on.", this);
                        continue;
                    }

                    var objectMember = new ObjectDescription.ObjectMember
                    {
                        instance = member.member,
                        memberId = member.memberId,
                    };

                    description.members.Add(objectMember);
                }
            }


            objectDescriptions.Add(description);


            foreach (var childScope in _immediateChildScopes.CombinedItems)
            {
                if (childScope == null || childScope.childInfra == null) continue;

                childScope.childInfra.DescribeObject(description, childScope.scopeId, objectDescriptions, objectDescriptor);
            }
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








        public void RegisterAssetRefernces()
        {
            //if (_assetReferences.IsNotNullAndNotEmpty())
            {
                foreach (var referenceInfo in _assetReferences.CombinedItems)
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
#endif




#if UNITY_EDITOR
        public void RefreshReferencedAssets()
        {
            _assetReferences.PersonalItems.Clear();

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
                        _assetReferences.PersonalItems.Add(new AssetReferencingDisplayInfo
                        {
                            assetHolder = comp,
                            references = referenceInfos,
                        });
                }
            }


            CheckAssetReferenceSettings();
        }
#endif




        [NonSerialized]
        public bool? _isRootInfraCached;
        public bool IsRootInfraCached {
            get
            {
                if (_isRootInfraCached == null)
                {
                    _isRootInfraCached = IsRootInfra;
                }
                return _isRootInfraCached.Value;
            }
        }


        /// <summary>
        /// The GameObject this GOInfra is attached to is considered root object if there is no other GOInfra upward in the parent hierarchy chain.
        /// </summary>
        public bool IsRootInfra {
            get
            {
                var parentInfra = this.GetComponentInParentExcludeSelf<GOInfra>(includeInactive: true);

                bool isRootInfra = parentInfra == null;

                _IsRootObject = isRootInfra;
                _isRootInfraCached = isRootInfra;
                

                return _isRootInfraCached.Value;
            }
        }



        public bool IsScenePlaced {
            get
            {
                if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return false;

                var roots = gameObject.scene.GetRootGameObjects();

                if (roots.IsNotNullAndNotEmpty())
                {
                    if (roots.Any(go => go.GetComponent<SceneInfra>() != null))
                    {
                        return true;
                    }
                    else
                        return false;
                }
                else return false;
            }
        }




        private void OnValidate()
        {
            EditorValidationService.RequestValidation(this);

            _IsRootObject = IsRootInfraCached;
            _IsScenePlaced = IsScenePlaced;
        }


        public void EditorValidate()
        {
#if UNITY_EDITOR
            if (ReferencedPrefabAssetId != null && InlinedPrefabAssetId.IsNotDefault)
            {
                BlueDebug.Error($"You cant have both a {nameof(ReferencedPrefabAssetId)} and a {InlinedPrefabAssetId} at the same time.\n" +
                    $"{nameof(InlinedPrefabAssetId)} will be set to default.");
            }
            else if (IsRootInfraCached && _IsPrefabasset && ReferencedPrefabAssetId == null && InlinedPrefabAssetId.IsDefault)
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
            if (gameObject.transform.parent == null && SceneInfra.HasSceneInfra(gameObject, out var sceneInfra))
            {
                sceneInfra.AddRootInfra_Editor(this);
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
                            _PrefabAssetPath = "";
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

                foreach (var displayInfo in _assetReferences.PersonalItems)
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




        public GOInfra RootInfraCandidate {
            get
            {
                if (_rootInfraCandidate == null)
                {
                    _rootInfraCandidate = this;

                    Transform parent = transform.parent;

                    while (parent != null)
                    {
                        if (parent.TryGetComponent<GOInfra>(out var parentInfra))
                        {
                            _rootInfraCandidate = parentInfra;
                        }

                        parent = parent.parent;
                    }
                }

                return _rootInfraCandidate;
            }
        }

        /// <summary>
        /// This property can be used to determine if a prefab root candidate is still a root or not, because prefab roots can become non-roots if they are nested under another prefab.
        /// </summary>
        public bool IsRootInfraCandidateStillRoot {
            get
            {
                return RootInfraCandidate.IsRootInfra;
            }
        }


        public void CollectImmediateChildScopes()
        {
            if (_immediateChildScopes.PersonalItems == null)
            {
                Debug.LogError($"There isn't any list in the {nameof(_immediateChildScopes)} list chain. Add one.");
                return;
            }

            CollectImmediateChildScopes(_immediateChildScopes);

            Debug.Log("Successfully collected child scopes.");
        }


        public void CollectImmediateChildScopes(InheritableList<ChildInfraScope> childInfraScopes)
        {
            if (_scopeId.IsDefault) _scopeId = RandomId.New;


            _ValidateAndFilterChildScopes(childInfraScopes);

            var existingChildInstances = childInfraScopes.CombinedItems
                .Where(_ChildInfraScopeFilter)
                .Select(_ChildInfraScopeInfraInstanceSelector)
                .ToHashSet();

            var foundChildrenInfra = _FindImmediateChildInfraInstancesInHierarhcy();

            CollectImmediateChildScopes(childInfraScopes.PersonalItems, existingChildInstances, foundChildrenInfra);
        }

        public void CollectImmediateChildScopes(List<ChildInfraScope> childInfraScopes,
                                    HashSet<GOInfra> existingChildInstances,
                                    HashSet<GOInfra> foundChildrenInfra)
        {
            foreach (var child in foundChildrenInfra)
            {
                if (existingChildInstances.Contains(child)) continue;

                var scope = new ChildInfraScope
                {
                    childInfra = child,
                    scopeId = RandomId.New,
                };

                childInfraScopes.Add(scope);

                child._parentScope = this;
            }
        }


        public HashSet<GOInfra> _FindImmediateChildInfraInstancesInHierarhcy()
        {
            HashSet<GOInfra> foundChildrenInfra = new();


            void Traverse(Transform parent)
            {
                for (int i = 0; i < parent.childCount; i++)
                {
                    var child = parent.GetChild(i);

                    //doc:
                    ///every goinfra is responsible only for their "direct" children. The found child here will take care of other child infras
                    ///deeper in the hierarchy tree (if there is any) and so on. So we break here, otherwise keep looking for direct children.
                    if (child.TryGetComponent<GOInfra>(out var childInfra))
                    {
                        foundChildrenInfra.Add(childInfra);
                    }
                    else
                    {
                        Traverse(child);
                    }
                }
            }

            Traverse(transform);

            return foundChildrenInfra;
        }



        public static bool _ChildInfraScopeFilter(ChildInfraScope childInfraScope)
        {
            return childInfraScope != null && childInfraScope.childInfra != null;
        }

        public static GOInfra _ChildInfraScopeInfraInstanceSelector(ChildInfraScope childInfraScope) => childInfraScope.childInfra;


        public void _ValidateAndFilterChildScopes(InheritableList<ChildInfraScope> childInfraScopes)
        {
            if (childInfraScopes.PersonalItems.IsNotNullAndNotEmpty())
            {
                HashSet<GOInfra> handledByOthers = childInfraScopes.InheritedItems
                    .Where(_ChildInfraScopeFilter)
                    .Select(_ChildInfraScopeInfraInstanceSelector)
                    .ToHashSet();

                //Debug.Log(string.Join("\n", handledByOthers.Select(infra => infra.gameObject.HierarchyPath())));
                for (int i = childInfraScopes.PersonalItems.Count - 1; i >= 0; i--)
                {
                    var childScope = childInfraScopes.PersonalItems[i];

                    var childInfra = childScope.childInfra;

                    if (childInfra == null)
                    {
                        Debug.LogWarning($"Removing null reference child scope with id={childScope.scopeId}");
                        childInfraScopes.PersonalItems.RemoveAt(i);
                        continue;
                    }


                    if (handledByOthers.Contains(childInfra))
                    {
                        BlueDebug.Debug($"Removing child scope with id={childScope.scopeId}, because it is already handled by an inherited list.", this);
                        childInfraScopes.PersonalItems.RemoveAt(i);
                        continue;
                    }


                    GOInfra parentInfra = childInfra.GetComponentInParentExcludeSelf<GOInfra>(includeInactive: true);

                    if (parentInfra != this)
                    {
                        BlueDebug.Debug($"Part1: Removing child scope with id={childScope.scopeId}, because its parent is no longer this object.", this);
                        BlueDebug.Debug($"Part2: Child: {childInfra.gameObject.HierarchyPath()}", childInfra);
                        if (parentInfra == null)
                        {
                            Debug.Log($"Part3: New Parent: null");
                            break;
                        }
                        else
                            BlueDebug.Debug($"Part3: New Parent: {parentInfra.gameObject.HierarchyPath()}", parentInfra);

                        childInfraScopes.PersonalItems.RemoveAt(i);
                        continue;
                    }
                }
            }
        }
#endif



        //unused feature yet
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


    [Serializable]
    public class ChildInfraScope
    {
        public GOInfra childInfra;
        public RandomId scopeId;
    }




    public class GOInfraSaveData : MonoSaveDataBase
    {
        public bool IsNetworked;
        public bool IsUIElement;
    }
}