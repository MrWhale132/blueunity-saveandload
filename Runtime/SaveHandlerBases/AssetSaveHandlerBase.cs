
using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.UtilScripts;
using Assets._Project.Scripts.UtilScripts.CodeGen;
using Assets._Project.Scripts.UtilScripts.Misc;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases
{
    public class AssetSaveHandlerBase<TAsset, TSaveData> : SaveHandlerGenericBase<TAsset, TSaveData>
        where TAsset : UnityEngine.Object
        where TSaveData : AssetSaveData, new()
    {
        public override bool IsValid => __instance != null;

        public RandomId AssetId => __saveData._AssetId_;
        public bool Mutable => __saveData._mutable_;


        //todo: tmp hack
        AssetInitContext _context;
        public override void Init(object instance, InitContext context)
        {
            //if(context is AssetInitContext assetContext)
            //{
            //    __saveData._AssetId_ = assetContext.instantiatedFromAssetId;
            //}
            _context = context as AssetInitContext;

            base.Init(instance, context);
        }


        public override void Init(object instance)
        {
            base.Init(instance);
            //todo: remove this lone after we no longer need backward compatibility
            if (_context != null)
            {
                __saveData._AssetId_ = _context.instantiatedFromAssetId;
                __saveData._mutable_ = _context.mutable;

                AssetIdMap.mutable.Add(HandledObjectId, _context.mutable);

                AssetIdMap.ObjectIdToAssetId.Add(HandledObjectId, __saveData._AssetId_);
                return;
            }
            

            if (!SupportsInstanceCreation)
                Debug.LogError((HandledType?.Name, __instance?.name, HandledObjectId, _context == null,
                    "\nThis asset type does not support instance creation, meaning all of its instances is expected to be preregistered with an assetid. " +
                    "All you have to do is to preresgister this somewhere in your code. " +
                    "If it is a non-shared private copy of a shared property then also set the option that indicates this. " +
                    "(hint: something like 'expectedToBeModified' or similar meaning)"), __instance);
        }


        public override void WriteSaveData()
        {
            base.WriteSaveData();
            __saveData._name_ = __instance.name;
        }

        public override void LoadPhase1()
        {
            base.LoadPhase1();
            __instance.name = __saveData._name_;
        }


        public override void CreateObject()
        {
            base.CreateObject();

            HandledObjectId = __saveData._ObjectId_;

                _AssignInstance();

                Infra.Singleton.RegisterReference(__instance, HandledObjectId, rootObject: __saveData._isRootObject_);

                if (__saveData._AssetId_.IsNotDefault)
                    AssetIdMap.ObjectIdToAssetId.Add(HandledObjectId, __saveData._AssetId_);
        }


        public override void _AssignInstance()
        {
            if (AssetIdMap.HasInstance<TAsset>(__saveData._ObjectId_, out var instance))
            {
                __instance = instance;
                return;
            }

            if(__saveData._AssetId_.IsNotDefault)
            {
                var assetId = __saveData._AssetId_;

                var orig = GetAssetById2<TAsset>(assetId, null);

                if (orig != null)
                {
                    var copy = Object.Instantiate(orig);

                    __instance = copy;
                }
                else
                {
                    Debug.LogError($"Object: {__saveData._ObjectId_} had an assetid: {assetId} but no asset was found with such id.");
                }
            }
            else if (_assetTypesThatSupportInstanceCreation.Contains(typeof(TAsset)))
            {
                if (typeof(TAsset) == typeof(PhysicsMaterial))
                {
                    __instance = (TAsset)(object)new PhysicsMaterial();
                }
            }
            ///in case of <see cref="SupportsModificationsToTheInstance"/> true, that handler should also override <see cref="_AssignInstance"/>
        }



        public override void ReleaseObject()
        {
            if (AssetIdMap.ObjectIdToAssetId.ContainsKey(HandledObjectId))
                AssetIdMap.ObjectIdToAssetId.Remove(HandledObjectId);
            AssetIdMap.mutable.Remove(HandledObjectId);

            base.ReleaseObject();
        }



        ///todo: this is temp solution because I dont want to mark every tpye of asset savehandler just to override their <see cref="_AssignInstance"/>
        ///and we can not (yet) configure the generation of these methods
        public static HashSet<Type> _assetTypesThatSupportInstanceCreation = new()
        {
            typeof(PhysicsMaterial),
            typeof(Material),
            typeof(Shader),
            typeof(Mesh),
        };

        public bool SupportsInstanceCreation {
            get
            {
                return _assetTypesThatSupportInstanceCreation.Contains(typeof(TAsset));
            }
        }
    }


    public class AssetSaveData : SaveDataBase
    {
        public RandomId _AssetId_;
        public bool _mutable_;
        public string _name_; //_ is for avoiding shadowing by derived types
    }












    /// <summary>
    /// Helper class to help decouple dependency between savehandlers when they need to share data with eachother
    /// </summary>
    public class AssetIdMap
    {
        public static Dictionary<RandomId, RandomId> ObjectIdToAssetId = new();
        //note: instance can be null
        public static Dictionary<RandomId, Object> ObjectIdToAssetInstance = new();

        public static Dictionary<RandomId, bool> mutable = new();

        public static bool IsMutable(RandomId id)
        {
            if(mutable.TryGetValue(id, out var isMutable)) return isMutable;
            else return false;
        }


        //lol, I missunderstood what I wanted to do and later realized this is not needed... yet. Keeping it anyway in case for future usecase.
        public static void AddObjectAndAssetId(RandomId objectId, RandomId assetId)
        {
            if (ObjectIdToAssetId.TryGetValue(objectId, out var alreadyRegisteredAssetId))
            {
                if (alreadyRegisteredAssetId != assetId)
                {
                    Debug.LogError($"ERROR {nameof(AssetIdMap)}: objectid: {objectId} is already registered with a different assetid.\n" +
                        $"existing assetid: {alreadyRegisteredAssetId}\n" +
                        $"requested assetid: {assetId}");
                }
            }
            else
            {
                ObjectIdToAssetId.Add(objectId, assetId);
            }
        }


        public static void AddInstance(RandomId objectId, Object instance)
        {
            if (objectId.IsDefault)
            {
                Debug.LogError($"Invalid argument. {nameof(objectId)} can not be default");
                return;
            }
            //null isntance is allowed


            if(ObjectIdToAssetInstance.TryGetValue(objectId, out var alreadyRegisteredAssetInstance))
            {
                var comparer = MyReferenceEqualityComparer.Instance;

                if(!comparer.Equals(instance, alreadyRegisteredAssetInstance)
                    || comparer.GetHashCode(instance) != comparer.GetHashCode(alreadyRegisteredAssetInstance))
                {
                    Debug.LogError($"ERROR {nameof(AssetIdMap)}: Can not add instance with id: {objectId} because an other instance has already been " +
                        $"registered with this id.\n" +
                        $"instance name: {instance?.name}\n" +
                        $"existing instance name: {alreadyRegisteredAssetInstance?.name}");
                }
            }
            else
            {
                ObjectIdToAssetInstance.Add(objectId, instance);
            }
        }


        public static bool HasInstance<TAsset>(RandomId id, out TAsset instance) where TAsset : Object
        {
            if (ObjectIdToAssetInstance.TryGetValue(id, out var instance2))
            {
                if (instance2 is TAsset asset)
                {
                    instance = asset;
                    return true;
                }
                else
                {
                    Debug.LogError($"Can not cast type: {instance2.GetType().CleanAssemblyQualifiedName()} into type: {typeof(TAsset).CleanAssemblyQualifiedName()}.\n" +
                        $"This error here means that an other object already created the instance for this asset savehandler so this handler does not have to " +
                        $"but the other object created it wrongly.\n" +
                        $"For example: a MeshFilter can precreate the mesh instance for the mesh savehandler but if it actually created and registered a material instead then this error will happen.\n" +
                        $"Asset's objectid: {id}");
                }
            }

            instance = default;
            return false;
        }


        public static void LogSharedPrivateCopiesError(RandomId objectId, RandomId sharedInstanceId, RandomId privateInstanceId)
        {
            Debug.LogError($"ERROR: Shared private copies. In Editor saves, get/set on the non-shared properties (like .mesh, .material) should cause a private copy to that component.\n" +
                $"Every component whose non-shared properties were accessed or assigned should have a unique instance to that component.\n" +
                $"It is therefore should not be possible that multiple components reference the same instance because each of them should have their own copy.\n" +
                $"This error can also happen if you tried to load a player time save in editor time.\n" +
                $"The instance will not be set for the non-shared property because assigning to the non-shared property would create a new unrelated copy." +
                $"Component id: {objectId}\n" +
            $"shared asset id: {sharedInstanceId}, non-shared asset id :{privateInstanceId}");
        }
    }


    public class AssetInitContext : InitContext
    {
        public RandomId instantiatedFromAssetId;
        public bool mutable;
    }
}


/*
| asset type                                       | original clone best practice                                                       |
| ------------------------------------------------ | ---------------------------------------------------------------------------------- |
| Material                                         | `new Material(mat)` (best) OR `Instantiate(mat)`                                   |
| Mesh                                             | `Instantiate(mesh)` **only**. (`new Mesh(mesh)` doesn’t exist)                     |
| Texture2D                                        | `Instantiate(tex)`                                                                 |
| Cubemap                                          | `Instantiate(cubemap)`                                                             |
| AudioClip                                        | `Instantiate(audioClip)` (works for non streaming clips)                           |
| Shader                                           | **cannot** really clone (they’re engine level singletons)                          |
| ComputeShader                                    | `Instantiate(computeShader)`                                                       |
| AnimationClip                                    | `Instantiate(clip)`                                                                |
| AnimatorController                               | `Instantiate(controller)`                                                          |
| AvatarMask                                       | `Instantiate(mask)`                                                                |
| PhysicsMaterial / PhysicsMaterial2D              | `Instantiate(mat)`                                                                 |
| Sprite                                           | `Instantiate(sprite)` OR `Sprite.Create(...)` if you want modify pixels/boundaries |
| Prefab instance                                  | you can't “clone” the asset, but you `Instantiate(prefab)` to spawn runtime clone  |
| ScriptableObject                                 | `Instantiate(scriptableObj)`                                                       |
| MeshCollider.sharedMesh clone                    | `mc.sharedMesh = Instantiate(originalMesh)`                                        |
| RenderTexture                                    | `new RenderTexture(rt)` (copy constructor)                                         |
| Font                                             | `Instantiate(font)`                                                                |
| VFXGraph asset                                   | `Instantiate(vfxGraph)`                                                            |
| Timeline PlayableAsset / PlayableDirector config | `Instantiate(playable)`                                                            |
*/





/*
# RENDERING (primary group)

These are the canonical ones.

---

## MeshFilter

| Property   | Meaning                          |
| ---------- | -------------------------------- |
| sharedMesh | Asset / shared geometry          |
| mesh       | Mutable geometry (copy-on-write) |

---

## SkinnedMeshRenderer

| Property   | Meaning      |
| ---------- | ------------ |
| sharedMesh | Asset mesh   |
| mesh       | Mutable mesh |

Same behavior as MeshFilter.

---

## Renderer (base class)

Affects:

* MeshRenderer
* SkinnedMeshRenderer
* SpriteRenderer
* TrailRenderer
* LineRenderer

### Materials

| Property        | Meaning           |
| --------------- | ----------------- |
| sharedMaterial  | Asset material    |
| material        | Instance material |
| sharedMaterials | Asset array       |
| materials       | Instance array    |

Copy-on-write when modifying material properties.

---

# UI SYSTEM

---

## Graphic (base class for UI)

Examples:

* Image
* RawImage
* TextMeshProUGUI

| Property       | Meaning  |
| -------------- | -------- |
| sharedMaterial | Asset    |
| material       | Instance |

TMP adds additional internal instancing layers, but same concept.

---

# TEXTURES (partial / implicit)

Textures don’t expose explicit `texture` vs `sharedTexture` properties, but **RenderTexture usage** behaves similarly:

* Many cameras may reference same RenderTexture
* Writing into it mutates shared content
* You must explicitly allocate your own if you want isolation

Not quite same API shape, but same mental model.

---

# ANIMATION CONTROLLERS

---

## Animator

| Property                  | Meaning               |
| ------------------------- | --------------------- |
| runtimeAnimatorController | Shared asset          |
| OverrideController        | Per-instance override |

Overrides behave like instance copies of binding tables.

Not copy-on-write in the same way, but same *shared vs instance intent* split.

---

# PHYSICS MATERIALS

---

## Collider

| Property       | Meaning  |
| -------------- | -------- |
| sharedMaterial | Asset    |
| material       | Instance |

Yes — PhysicMaterial also follows this pattern.

---

# SUMMARY TABLE

| Component           | Shared            | Instance    |
| ------------------- | ----------------- | ----------- |
| MeshFilter          | sharedMesh        | mesh        |
| SkinnedMeshRenderer | sharedMesh        | mesh        |
| Renderer            | sharedMaterial(s) | material(s) |
| Graphic (UI)        | sharedMaterial    | material    |
| Collider            | sharedMaterial    | material    |

These are the **core Unity-supported copy-on-write APIs**.

---

# Things that do NOT have this pattern

* Textures
* AudioClips
* AnimationClips
* ScriptableObjects
* Sprites
*/