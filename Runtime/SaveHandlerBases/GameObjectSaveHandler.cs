
using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.UtilScripts;
using Assets._Project.Scripts.UtilScripts.Extensions;
using System;
using System.Collections.Generic;
using Theblueway.Core.Runtime.Debugging.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases
{
    [SaveHandler(id: 432468912, dataGroupName: nameof(GameObject), handledType: typeof(GameObject), order: -10)]
    public class GameObjectSaveHandler : SaveHandlerGenericBase<GameObject, GameObjectSaveData>
    {
        List<Component> _components = new List<Component>();
        public GOInfra _goInfra;

        InitContext _initContext;


        public ComponentAddingTracker ComponentAddingTracker { get; set; }

        public override bool IsValid => __instance != null;

        public bool ShouldRegisterComponents =>
            !IsPrefabAsset
            && (_initContext == null || (!_initContext.isPrefabPart && !_initContext.isScenePlaced))
            && (_goInfra == null || (!_goInfra.HasInlinedSceneParts && !_goInfra.HasInlinedPrefabParts));


        public bool IsPrefabAsset => __saveData.PrefabAssetId.IsNotDefault;



        public override void Init(object instance, InitContext context)
        {
            base.Init(instance, context);

            _initContext = context;


            __saveData.GameObjectId = __saveData._ObjectId_;

            var isProbablyPrefabAsset = __instance.IsProbablyPrefabAsset(); //todo: maybe we can use to check if Awake has ran instead?

            _goInfra = __instance.GetComponent<GOInfra>();


            if (isProbablyPrefabAsset)
            {
                string message = null;

                if (_goInfra == null)
                {
                    message = $"GOSaveHandler: It was detected that a gameobject instance might be a prefab asset and not an instance of it \n" +
                        $"but it does not have a {nameof(GOInfra)} that could tell which prefab this GameObject is.\n" +
                        $"ObjectId: {HandledObjectId}, name: {__instance.name}";
                }
                else if (_goInfra.PrefabAssetId.IsDefault)
                {
                    message = $"GOSaveHandler: It was detected that a gameobject instance might be a prefab asset and not an instance of it \n" +
                        $"but it does not have a prefab asset id that could tell which prefab this GameObject is.\n" +
                        $"ObjectId: {HandledObjectId}, name: {__instance.name}";
                }
                else
                {
                    __saveData.PrefabAssetId = _goInfra.PrefabAssetId;
                }


                if (message != null)
                {
                    BlueDebug.Error(message,__instance);

                    ///todo: for this exception to be effective, we need a SaveContext, it would catch the exception, map it to an error code
                    ///and return a response to <see cref="SaveAndLoadManager.Save"/> consumers to let them handle.
                    ///PLUS: save the savefile anyway for debugging and restoring purposes, but has to be marked as invalid
                    ///so (e.g) load menus can ignore it
                    //throw new Exception(message);
                    return;
                }
            }
            else if (!isProbablyPrefabAsset && ShouldRegisterComponents)
            {
                _goInfra = __instance.GetOrAddComponent<GOInfra>();
                __saveData._goInfra = GetObjectId(_goInfra);
            }


            //easier troubleshooting if this is set now so it can be used later
            __saveData.HierarchyPath = __instance.HierarchyPath();

            //if (__saveData.HierarchyPath.StartsWith("Demo_Tank -"))
            //{
            //    //Debug.Log("Here");
            //    MonoBehaviourLifeCycleHooks.Singleton.Hook(LifeCycleHookType.Update, Update);
            //}
        }

        //void Update()
        //{
        //    if (__instance != null)
        //        __saveData.HierarchyPath = __instance.HierarchyPath();
        //}

        public override void ReleaseObject()
        {
            base.ReleaseObject();

            __instance = null;
            _goInfra = null;
            //MonoBehaviourLifeCycleHooks.Singleton.Unhook(LifeCycleHookType.Update, Update);
        }



        public override void WriteSaveData()
        {
            base.WriteSaveData();

            __saveData.GameObjectName = __instance.name;
            __saveData.tag = __instance.tag;
            __saveData.layer = __instance.layer;
            __saveData.activeSelf = __instance.activeSelf;
            __saveData.IsStatic = __instance.isStatic;
            __saveData.sceneIndex = __instance.scene.buildIndex;
            __saveData.sceneHandle = __instance.scene.handle;
            __saveData.isRootInScene = __instance.transform.parent == null;
            __saveData._initContext = _initContext;
            __saveData.HierarchyPath = __instance.HierarchyPath();
            
            if (!IsPrefabAsset)
                __saveData.sceneInstanceId = Infra.SceneManagement.SceneIdByHandle(__instance.scene.handle);

            _components.Clear();
            __saveData.Components.Clear();


            __instance.GetComponents(typeof(Component), _components);
            //_components = _goInfra._cachedComponents;

            for (int i = 0; i < _components.Count; i++)
            {
                var comp = _components[i];

                if (IsPrefabAsset)
                {
                    if (Infra.Singleton.IsRegistered(comp))
                    {
                        var compId = Infra.Singleton.GetObjectId(comp, HandledObjectId);

                        __saveData.Components.Add(compId);
                    }
                    else
                        __saveData.Components.Add(RandomId.Default);
                }
                else if (ShouldRegisterComponents
                    || Infra.Singleton.IsRegistered(comp)) //Prefab and scene placed parts are may already registered
                {
                    var compId = Infra.Singleton.GetObjectId(comp, HandledObjectId);

                    __saveData.Components.Add(compId);
                }
            }
        }



        public override void LoadPhase1()
        {
            base.LoadPhase1();

            if (IsPrefabAsset) return;

            __instance.name = __saveData.GameObjectName;
            __instance.tag = __saveData.tag;
            __instance.layer = __saveData.layer;
            __instance.isStatic = __saveData.IsStatic;
            __instance.SetActive(__saveData.activeSelf);

            _goInfra = GetObjectById<GOInfra>(__saveData._goInfra);
            _initContext = __saveData._initContext;
        }




        public override void CreateObject()
        {
            base.CreateObject();

            HandledObjectId = __saveData.GameObjectId;

            _AssignInstance();

            ComponentAddingTracker = new ComponentAddingTracker() { GameObject = __instance };


            Infra.Singleton.RegisterReference(__instance, __saveData.GameObjectId, rootObject: __saveData._isRootObject_);


            //for easier troubleshoot
            __instance.name = __saveData.GameObjectName;
        }



        public override void _AssignInstance()
        {

            if (IsPrefabAsset)
            {
                __instance = GetAssetById2<GameObject>(__saveData.PrefabAssetId, null);
                
                return;
            }
            else if (SaveAndLoadManager.S.IsPartOfPrefabOrScenePlaced<GameObject>(HandledObjectId, out var instance))
            {
                __instance = instance;
            }
            else
            {
                __instance = new GameObject();
            }


            //its enough to move the roots only as the children will come along when they set their parent to this root
            if (__saveData.isRootInScene)
            {
                __instance.transform.SetParent(null);

                var scene = Infra.SceneManagement.SceneById(__saveData.sceneInstanceId);
                SceneManager.MoveGameObjectToScene(__instance, scene);
            }
        }






        public T AddComponent<T>() where T : Component
        {
            if (ComponentAddingTracker == null)
            {
                Debug.LogError("ComponentAddingTracker is null. Cannot add component via tracker");
                return null;
            }

            return ComponentAddingTracker.AddComponent<T>();
        }
    }

    public class GameObjectSaveData : SaveDataBase
    {
        public RandomId GameObjectId;
        public string GameObjectName;
        public string HierarchyPath;
        //public bool IsPrefabAsset;
        public RandomId PrefabAssetId;
        public List<RandomId> Components = new();
        public RandomId _goInfra;
        public InitContext _initContext;
        public string tag;
        public int layer;
        public bool activeSelf;
        public bool IsStatic;
        public int sceneIndex;
        public int sceneHandle;
        public RandomId sceneInstanceId;
        public bool isRootInScene;
    }





    //todo: optimize
    public class ComponentAddingTracker
    {
        //RequireComponent, .AddComponent in Awake, Start, etc
        public static Dictionary<Type, bool> _mayAddAdditionalComponents = new();


        public GameObject __gameobject;
        public GameObject GameObject {
            get => __gameobject;
            set { if (__gameobject != null) { Debug.LogError("This component tracker is already bound to a gameobject"); return; } __gameobject = value; }
        }


        public Dictionary<Type, List<Component>> _unboundComponentsByIndexPerType = new();


        public T AddComponent<T>() where T : Component
        {
            Type type = typeof(T);

            if (HasUnboundComponent<T>(out var comp))
            {
                return comp;
            }

            if (!_mayAddAdditionalComponents.ContainsKey(typeof(T)))
            {
                bool hasRequireComponent = typeof(T).IsDefined(typeof(RequireComponent), true);
                _mayAddAdditionalComponents[typeof(T)] = hasRequireComponent;
            }


            ///WARNING: even though the instances added here because of the RequireComponent are NOT HANDLED here
            ///the <see cref="SaveAndLoadManager.IsObjectLoading(Component)"/> apis will still work because
            ///they check the comp's gameobject's loading status instead
            if (_mayAddAdditionalComponents[typeof(T)])
            {
                int countBefore = GameObject.GetComponentCount();

                comp = GameObject.AddComponent<T>();

                int countAfter = GameObject.GetComponentCount();

                if (comp == null)
                {

                }


                for (int i = countBefore; i < countAfter; i++)
                {
                    var c = GameObject.GetComponentAtIndex(i);

                    if (c == comp) continue;

                    Type ctype = c.GetType();
                    if (!_unboundComponentsByIndexPerType.ContainsKey(ctype))
                    {
                        _unboundComponentsByIndexPerType[ctype] = new List<Component>();
                    }
                    _unboundComponentsByIndexPerType[ctype].Add(c);
                }

                return comp;
            }
            else
            {
                return GameObject.AddComponent<T>();
            }
        }
        public bool HasUnboundComponent<T>(out T comp) where T : Component
        {
            Type type = typeof(T);

            if (_unboundComponentsByIndexPerType.TryGetValue(type, out var byIndexes))
            {
                var comp2 = byIndexes[0];
                comp = comp2 as T;
                if (comp != null)
                {
                    byIndexes.RemoveAt(0);
                    if (byIndexes.Count == 0)
                        _unboundComponentsByIndexPerType.Remove(type);
                    return true;
                }
                else
                {
                    Debug.LogError("Something bad happend. This should not have happened. Type mismatch in unbound components");
                    return false;
                }
            }
            comp = null;
            return false;
        }
    }
}


/*
I just wanted to save this piece of info somwhere adn put it here


### 1) the switch table only works when:

* cases are **dense** (no large gaps)
* **integer based** (0..N)
* case values are **known at compile time** and constants

So if you define:

```
switch(idx) {
    case 0: ...
    case 1: ...
    case 2: ...
    case 3: ...
}
```

→ the compiler can emit a jump table. (IL op: `switch`)

### What *breaks* jump table efficiency

1. **big gaps** like:

```
case 0:
case 100:
```

→ compiler emits if-chain, not switch jump table.

2. **not starting at 0 is fine**
   This WON’T break it:

```
case 5:
case 6:
case 7:
case 8:
```

Compiler still can do jump table (offset-based).

3. **re-ordered cases** does NOT matter
   Compiler doesn’t care if you write

```
case 0
case 2
case 1
case 3
```

It reorders internally to build the jump table.

### What matters MOST

The **values must be dense and small**.

E.g. 0..N with no holes is optimal.
Holes like 0,1,5,6 breaks it into partial table + branch fallback or full compare chain.

### Missing single element

Example:

```
0,1,2,4,5
```

Missing 3 → this may already degrade into slower comparisons depending on compiler.

### Summary best practice for your save system

* never skip
* never allow indices to explode into thousands
*/