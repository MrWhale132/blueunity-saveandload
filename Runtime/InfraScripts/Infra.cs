using Assets._Project.Scripts.SaveAndLoad;
using Assets._Project.Scripts.SaveAndLoad.SavableDelegates;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using Assets._Project.Scripts.UtilScripts;
using Assets._Project.Scripts.UtilScripts.Misc;
using Newtonsoft.Json;
using System;
using Theblueway.Core.Runtime.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Theblueway.CodeGen.Runtime;
using Theblueway.SaveAndLoad.Packages.com.theblueway.saveandload.Runtime;
using Theblueway.SaveAndLoad.Packages.com.theblueway.saveandload.Runtime.InfraScripts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;



namespace Assets._Project.Scripts.Infrastructure
{//TODO: remove it later once it moved into the bootstrap scene
    [DefaultExecutionOrder(-10000)]
    public class Infra : MonoBehaviour
    {
        public static Infra Singleton { get; private set; }
        public static Infra S => Singleton;
        public static SceneManagement SceneManagement { get; set; } = new();
        public static UnityObjectLifeCycleManagementService UnityObjectLifeCycleManagement { get; set; } = new();

        //had been serialized once
        [HideInInspector]
        public RandomId _globalReferencing;
        //public RandomId GlobalReferencing => _globalReferencing;
        public static RandomId GlobalReferencing => Singleton._globalReferencing;


        public HashSet<RandomId> __rootObjectIds;
        //this is error prone since it only works with types whose GetHashCode() does not change
        //most ref types are safe, value types are not
        public Dictionary<object, RandomId> __objectIds;

        public Dictionary<RandomId, object> __globalReferenceCache;

        public Dictionary<RandomId, List<RandomId>> __objectReferences;





        private void Awake()
        {
            if (Singleton != null)
            {
                Debug.LogError("Infra instance already exists. Destroying the new instance.");
                Destroy(gameObject);
                return;
            }

            Singleton = this;
            DontDestroyOnLoad(gameObject);

            __rootObjectIds = new();
            __objectIds = new(MyReferenceEqualityComparer.Instance);
            __globalReferenceCache = new();
            __objectReferences = new();

            CreateDefaultJsonSerializerSettings();

        }



        private void Start()
        {
            RegisterSingleton(SceneManagement);
        }


        public void Update()
        {
            UnityObjectLifeCycleManagement.CleanDestroyedReferences();
        }



        public void RegisterSingleton(object singleton)
        {
            var handler = SaveAndLoadManager.Singleton.GetSaveHandlerFor(singleton);
            if (!handler.IsSingleton)
            {
                Debug.LogError("The save handler for the singleton object is not marked as singleton. Type: " + singleton.GetType().CleanAssemblyQualifiedName());
            }
            handler.Init(singleton);
            SaveAndLoadManager.Singleton.AddSaveHandler(handler);
        }




        public void StartNewReferenceGraph()
        {
            __objectReferences.Clear();
        }



        public void RemoveUnreferencedObjects()
        {
            var unreferenced = GetUnreferencedObjects();

            foreach (var id in unreferenced)
            {
                Unregister(id);
            }
        }


        public HashSet<RandomId> GetUnreferencedObjects()
        {
            var referenced = GetReferencedObjects();

            HashSet<RandomId> allRegisteredObjects = GetAllObjectIds();


            var unreferenced = new HashSet<RandomId>(allRegisteredObjects.Except(referenced));

            return unreferenced;
        }


        public HashSet<RandomId> GetReferencedObjects()
        {
            return _GetReachableObjects(__objectReferences, __rootObjectIds);
        }



        //depth first search algorithm
        public HashSet<RandomId> _GetReachableObjects(
                        Dictionary<RandomId, List<RandomId>> objectReferences,
                        IEnumerable<RandomId> rootIds)
        {
            var reachable = new HashSet<RandomId>();
            var stack = new Stack<RandomId>(rootIds);

            while (stack.Count > 0)
            {
                RandomId current = stack.Pop();
                if (!reachable.Add(current))
                    continue;

                if (objectReferences.TryGetValue(current, out var references))
                {
                    foreach (RandomId target in references)
                        stack.Push(target);
                }
            }

            return reachable;
        }


        public void _AddToReferenceGraph(RandomId referenced, RandomId referencedBy)
        {
            if (!__objectReferences.ContainsKey(referencedBy))
            {
                __objectReferences[referencedBy] = new List<RandomId>();
            }

            __objectReferences[referencedBy].Add(referenced);
        }


        public void KeepAlive(RandomId referenced, RandomId referencedBy)
        {
            if (referenced.IsDefault)
            {
                Debug.LogError("Referenced object id is not set.");
                return;
            }
            if (referencedBy.IsDefault)
            {
                Debug.LogError("ReferencedBy object is not set.");
                return;
            }

            _AddToReferenceGraph(referenced, referencedBy);
        }




        public bool IsRootObject(RandomId id)
        {
            return __rootObjectIds.Contains(id);
        }







        public void RegisterReference(object reference, RandomId key, bool rootObject = false)
        {
            bool isNull = reference is Object unityObject ?
                unityObject == null : reference == null;

            if (isNull)
            {
                Debug.LogError("Can not register null reference. Key: " + key.ToString());
                return;
            }

            if (key.IsDefault)
            {
                Debug.LogError("Can not register default id.");
                return;
            }

            if (__globalReferenceCache.ContainsKey(key))
            {
                bool same = reference == __globalReferenceCache[key];

                if (same)
                    Debug.LogError($"Infra: Reference with key {key} is already registered. Skipping registration.");
                else
                    Debug.LogError($"Infra: Object id {key} is already registered for a different object refernce. Skipping registration.");

                return;
            }

            if (__objectIds.ContainsKey(reference))
            {
                bool same = key == __objectIds[reference];

                if (same)
                    Debug.LogError($"Infra: Object Reference is already registered with this id: {key}");
                else
                    Debug.LogError($"Infra: Object reference is already registered with a different id. " +
                        $"Requested id: {key} " +
                        $"Found id: {__objectIds[reference]}", reference as UnityEngine.Object);

                return;
            }


            __objectIds.Add(reference, key);
            __globalReferenceCache.Add(key, reference);

            if (rootObject)
                __rootObjectIds.Add(key);
        }



        public object GetReference(RandomId key)
        {
            if (key == RandomId.Default)
            {
                return null;
            }

            if (__globalReferenceCache.TryGetValue(key, out var reference))
            {
                return reference;
            }
            else
            {
                Debug.LogError($"Infra: Reference with key {key} not found.");
                return null;
            }
        }

        public T GetObjectById<T>(RandomId id)
        {
            var reference = GetReference(id);


            if (reference == null) return default;


            if (reference is T typedReference)
            {
                return typedReference;
            }
            else
            {
                Debug.LogError($"Infra: Object with id {id} is not of type {typeof(T)}. Object type is: {reference?.GetType()}");
                return default;
            }
        }




        public RandomId GetObjectId(object obj, RandomId referencedBy, bool setLoadingOrder = false)
        {
            if (referencedBy.IsDefault)
            {
                Debug.LogError($"An object can not be referenced by a DefaultId.");
            }

            var objectId = _GetObjectIdWithoutReferencing(obj, autoRegister: true);

            if (objectId.IsDefault) return objectId;


            _AddToReferenceGraph(objectId, referencedBy);

            if (setLoadingOrder)
            {
                //hidden dependency jkanfiuhl5435huieurig
                _SetLoadingOrder(objectId, referencedBy);
            }

            return objectId;
        }


        //this order setting thing is may not necessary to keep track. It may can be done on saving by using the reference graph
        //the referencing object depends on the referenced object, so the referenced object should be loaded earlier
        public void _SetLoadingOrder(RandomId referenced, RandomId referencing)
        {
            //todo
            var lookUp = SaveAndLoadManager.Singleton.__saveHandlerByHandledObjectIdLookUp;
            if (lookUp.TryGetValue(referenced, out var dependency) && lookUp.TryGetValue(referencing, out var dependant))
            {
                dependency.Order = Math.Min(dependency.Order/* + 1*/, dependant.Order); //that + 1 needs some more thinking, it can create self-referencing loops more often
            }
        }





        public RandomId _GetObjectIdWithoutReferencing(object obj, bool autoRegister = true)
        {
            if ((obj is UnityEngine.Object unityObject && unityObject == null)
                || obj == null)
            {
                return RandomId.Default;
            }


            if (__objectIds.TryGetValue(obj, out RandomId id))
            {
                return id;
            }
            else if (!autoRegister)
            {
                return RandomId.Default;
            }

            //warning: this way if non-unity objects (pure c#) are unregistering themself they will be added back if someone
            //still referencing them, unable to unregister themself.

            id = Register(obj);

            return id;
        }



        /// <summary>
        /// This api is idempotent, subsequent calls beyond the first will return the already registered id.
        /// But, the different parameters can alter how an object is registered and if different systems try to register the same object with
        /// different parameters it can create erroneous behaviour. Thus, even though the api is idempotent, it is still validated that "everybody"
        /// who wants to register the same objects "thinks" the same about that object. They are all on the same page.
        /// For example imagine that one system wants to register an object as root object but an other does not. Which is correct then?
        /// This behaviour is turned on by default but can be opt-out by setting the <paramref name="ifHasntAlready"/> parameter to true.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="rootObject">If this is true, this object does not have to be referenced by other objects to not to be cleared out.</param>
        /// <param name="createSaveHandler"></param>
        /// <param name="context">Conext is supplied to the created savehandler to Init it.</param>
        /// <param name="ifHasntAlready"></param>
        /// <returns></returns>
        public RandomId Register(object obj, bool rootObject = false, bool createSaveHandler = true, InitContext context = null,
            bool ifHasntAlready = false)
        {
            if (ifHasntAlready && IsRegistered(obj))
            {
                var id = _GetObjectIdWithoutReferencing(obj, autoRegister: false);
                return id;
            }

            if (__objectIds.ContainsKey(obj))
            {
                var id = __objectIds[obj];

                if (context != null)
                {
                    Debug.LogError($"An object with id {id} is already registered but a non-null init context is supplied.\n" +
                        $"Initalization context can only be supplied once at the very first Registration attempt.\n" +
                        $"The context will be ignored.", obj as UnityEngine.Object);
                }
                //Debug.LogWarning($"Infra: object with id {id} is already registered. Skipping registration.");
                //todo: check if it has savehandler, it should if it is already enlisted. Except if multiple caller want to register this obj and they supply different value for createSaveHandler
                if (rootObject)
                {
                    if (!__rootObjectIds.Contains(id))
                    {
                        Debug.LogError($"An object with id {id} is already registered but it was not requested to be a root object that time.");
                    }
                }
                else
                {
                    if (__rootObjectIds.Contains(id))
                    {
                        Debug.LogError($"An object with id {id} is already registered as root object but now requested as a non-root object.");
                    }
                }

                return id;
            }


            RandomId objId = RandomId.Get();

            //todo: let the handlers decide if the object is rootobject and provide a Set api, for example, PromoteToRootObject
            if (obj is GameObject or Component or ScriptableObject)
            {
                rootObject = true;
            }

            //if (obj is GameObject go2 && go2.name == "BackgroundMusic")
            //{
            //    Debug.Log("here " + objId, go2);
            //}


            __objectIds.Add(obj, objId);
            __globalReferenceCache.Add(objId, obj);

            if (rootObject)
            {
                __rootObjectIds.Add(objId);
            }
            //else if (obj is GameObject go)
            //{
            //    Debug.LogWarning($"non-root GameObject registered: {go.HierarchyPath()} (id: {objId})");
            //}
            //else if (obj is Component component)
            //{
            //    Debug.LogWarning($"non-root Component registered: {component.name}, {component.gameObject.HierarchyPath()} (id: {objId})");
            //}


            if (createSaveHandler)
            {
                _CreateSaveHandler(obj, context);
            }


            return objId;
        }



        public void _CreateSaveHandler(object obj, InitContext context = null)
        {
            var handler = SaveAndLoadManager.Singleton.GetSaveHandlerFor(obj);
            if (handler != null)
            {
                handler.Init(obj, context);
                SaveAndLoadManager.Singleton.AddSaveHandler(handler);
            }
            else
            {
                var msg = $"[Infra][{UnregisteredObjectTypeLogCode}] A type has no savehandler registered for it. {TypeAnnotation} {obj.GetType().CleanAssemblyQualifiedName()}";
                Debug.LogWarning(msg);
            }
        }

        //used by editor scripts
        public const string TypeAnnotation = "$type:";
        public const int UnregisteredObjectTypeLogCode = 10;



        public void Unregister(List<RandomId> ids)
        {
            if(ids == null) return;

            for (int i = 0; i < ids.Count; i++) Unregister(ids[i]);
        }

        public void Unregister(RandomId id)
        {
            if (__globalReferenceCache.TryGetValue(id, out var obj))
            {
                Unregister(obj);
            }
        }

        public void Unregister(object obj)
        {
            if (!__objectIds.TryGetValue(obj, out var id))
            {
                //Debug.LogWarning($"Infra: object is not registered. Skipping unregistration.");
                return;
            }


            SaveAndLoadManager.Singleton.RemoveSaveHandler(id);

            SaveAndLoadManager.PrefabDescriptionRegistry.RemoveIfPartOfPrefab(id);
            SaveAndLoadManager.ScenePlacedObjectRegistry.RemoveIfScenePlaced(id);


            if (__rootObjectIds.Contains(id))
            {
                __rootObjectIds.Remove(id);
            }

            if (__delegateMapPerInstance.ContainsKey(id))
            {
                __delegateMapPerInstance.Remove(id);
            }

            if (__routinesByTargetMono.ContainsKey(id))
            {
                var routines = __routinesByTargetMono[id];

                foreach (var routine in routines)
                {
                    var routineHandler = __coroutineSaveDataLookUp[routine];

                    Unregister(routineHandler._routineState);

                    __coroutineSaveDataLookUp.Remove(routine);
                }

                __routinesByTargetMono.Remove(id);
            }

            __objectIds.Remove(obj);
            __globalReferenceCache.Remove(id);
        }


        public bool IsNotRegistered(RandomId id)
        {
            return !IsRegistered(id);
        }

        public bool IsRegistered(RandomId id)
        {
            bool registered = __globalReferenceCache.ContainsKey(id);
            return registered;
        }

        public bool IsNotRegistered(object obj)
        {
            return !IsRegistered(obj);
        }

        //maybe this does not need a UnityEngine.Object overload (?)
        public bool IsRegistered(object obj)
        {
            if (obj == null) return false;

            return __objectIds.ContainsKey(obj);
        }




        public HashSet<RandomId> GetAllObjectIds()
        {
            return __globalReferenceCache.Keys.ToHashSet();
        }





        public Dictionary<Type, Dictionary<string, long>> __methodIdsByMethodSignaturePerType = new();
        public Dictionary<Type, Func<long, Func<object, Delegate>>> __methodGetterFactoryPerType = new();
        public Dictionary<Type, Dictionary<long, Func<object, Delegate>>> __methodGettersByIdPerType = new();

        public Dictionary<Type, Func<long, MethodInfo>> __methodInfoGettersPerType = new();
        public Dictionary<Type, Dictionary<long, MethodInfo>> __genericMethodDefGettersByIdPerType = new();

        public Dictionary<RandomId, Dictionary<long, Dictionary<long, Delegate>>> __delegateMapPerInstance = new();





        public void AddMethodSignatureToMethodIdMap(Type type, Dictionary<string, long> methodToIdMap)
        {
            if (!__methodIdsByMethodSignaturePerType.ContainsKey(type))
            {
                __methodIdsByMethodSignaturePerType.Add(type, methodToIdMap);
            }
            else
            {
                var dict = __methodIdsByMethodSignaturePerType[type];
                foreach (var pair in methodToIdMap)
                {
                    if (!dict.ContainsKey(pair.Key))
                    {
                        dict.Add(pair.Key, pair.Value);
                    }
                    else
                    {
                        Debug.LogError($"The method signature {pair.Key} is already registered for type {type.CleanAssemblyQualifiedName()}. Skipping registration.");
                    }
                }
            }
        }


        public void AddMethodIdToMethodMap(Type type, Func<long, Func<object, Delegate>> idToMethodMap)
        {
            if (!__methodGetterFactoryPerType.ContainsKey(type))
                __methodGetterFactoryPerType.Add(type, idToMethodMap);
            else
                __methodGetterFactoryPerType[type] += idToMethodMap;
        }

        public void AddMethodIdToMethodInfoMap(Type type, Func<long, MethodInfo> idToMethodInfoMap)
        {
            if (!__methodInfoGettersPerType.ContainsKey(type))
                __methodInfoGettersPerType.Add(type, idToMethodInfoMap);
            else
                __methodInfoGettersPerType[type] += idToMethodInfoMap;
        }



        public void EnsureSaveHandlerTypeIsInitialized(Type type)
        {
            if (type != null)
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle); //enforce static ctor if not yet ran
        }


        //delegate helpers

        public Func<long, Func<object, Delegate>> GetIdToMethodMapForType(Type type)
        {
            if (!__methodGetterFactoryPerType.ContainsKey(type))
            {
                Type saveHandler = SaveAndLoadManager.Singleton.GetSaveHandlerTypeFrom(type);

                EnsureSaveHandlerTypeIsInitialized(saveHandler);

                if (!__methodGetterFactoryPerType.ContainsKey(type))
                {
                    Debug.LogError($"The type {type.CleanAssemblyQualifiedName()} does not have a methodid to method map registered. " +
                    $"This means the type's savehandler, if it is exists, initialization logic did not run before something " +
                    $"else queried saveandload logic about this type. (Possible culprit: method reference through reflection during runtime.)" +
                    $" | 25.9");

                    return (id) => null;
                }
            }

            return __methodGetterFactoryPerType[type];
        }

        public Func<long, MethodInfo> GetMethodInfoIdToMethodMapForType(Type type)
        {
            if (!__methodInfoGettersPerType.ContainsKey(type))
            {
                Type saveHandler = SaveAndLoadManager.Singleton.GetSaveHandlerTypeFrom(type);

                EnsureSaveHandlerTypeIsInitialized(saveHandler);

                if (!__methodInfoGettersPerType.ContainsKey(type))
                {
                    Debug.LogError($"The type {type.CleanAssemblyQualifiedName()} does not have a gen method def id to method map registered. " +
                    $"This means the type's savehandler, if it is exists, initialization logic did not run before something " +
                    $"else queried saveandload logic about this type. (Possible culprit: method reference through reflection during runtime.)" +
                    $" | 25.9");

                    return (id) => null;
                }
            }

            return __methodInfoGettersPerType[type];
        }







        //for loading
        public T GetDelegate<T>(InvocationList invocationList)
        {
            if (typeof(Delegate).IsAssignableFrom(typeof(T)) == false)
            {
                Debug.LogError($"The type {typeof(T).Name} is not a delegate type. " +
                                $"Please ensure you are trying to get a delegate of the correct type. " +
                                $"Going to return default value.");
                return default;
            }

            if (invocationList == null || invocationList.Delegates == null || invocationList.Delegates.Count == 0)
            {
                return default;
            }


            var dells = new List<Delegate>();

            foreach (var saveInfo in invocationList.Delegates)
            {
                var singleDel = GetDelegate<T>(saveInfo);

                if (singleDel != null)
                {
                    dells.Add((Delegate)(object)singleDel);
                }
            }

            T del = (T)(object)Delegate.Combine(dells.ToArray());
            return del;
        }



        public T GetDelegate<T>(DelegateSaveInfo saveInfo)
        {
            if (saveInfo == null)
            {
                return default;
            }

            if (!typeof(Delegate).IsAssignableFrom(typeof(T)))
            {
                Debug.LogError($"The type {typeof(T).Name} is not a delegate type. " +
                    $"Please ensure you are trying to get a delegate of the correct type. " +
                    $"Going to return default value.");
                return default;
            }

            RandomId targetId = saveInfo.TargeId;
            long methodId = saveInfo.MethodId;
            long variantId = saveInfo.IsGeneric ? saveInfo.GenericVairantId : methodId;


            if (!__delegateMapPerInstance.ContainsKey(targetId))
            {
                __delegateMapPerInstance[targetId] = new();
            }
            if (!__delegateMapPerInstance[targetId].ContainsKey(methodId))
            {
                __delegateMapPerInstance[targetId][methodId] = new();
            }


            //if cached, return it
            if (__delegateMapPerInstance[targetId][methodId].ContainsKey(variantId))
            {
                var del = __delegateMapPerInstance[targetId][methodId][variantId];


                T typedDel = TryGetOrCreateDelegate(del);

                if (typedDel != null) //todo: ??? , why null does work and default does not? shouldnt be the opposite? (replace null with default and see the error)
                {
                    //__delegateMapPerInstance[targetId][methodId].Add(variantId, del);
                }

                return typedDel;
            }
            //if not, get it and cache it
            else
            {
                //pay special attention to static methods, they have a targetId too that points to the StaticSubtitute instance
                //also pay attention that in case of generics, method id represents the generic method def id
                //in case of non-generics, method id and variant id are the same

                var target = GetReference(targetId);

                if (target == null) return default;


                //todo: build lookup
                Type declaringTpye = target is StaticSubtitute subtitute
                                ? subtitute.SubtitutedType
                                : target.GetType();


                //if (declaringTpye.IsGenericType) declaringTpye = declaringTpye.GetGenericTypeDefinition();


                if (!__methodGettersByIdPerType.ContainsKey(declaringTpye))
                {
                    __methodGettersByIdPerType[declaringTpye] = new();
                }
                if (!__genericMethodDefGettersByIdPerType.ContainsKey(declaringTpye))
                {
                    __genericMethodDefGettersByIdPerType[declaringTpye] = new();
                }

                if (!saveInfo.GetByMethodInfo)
                {
                    if (!__methodGettersByIdPerType[declaringTpye].ContainsKey(methodId))
                    {
                        __methodGettersByIdPerType[declaringTpye][methodId] = __methodGetterFactoryPerType[declaringTpye](methodId);
                    }
                }
                else
                {
                    if (!__methodGettersByIdPerType[declaringTpye].ContainsKey(variantId))
                    {
                        //get the closed generic method that can be used to get the methodDef and cache it
                        if (!__genericMethodDefGettersByIdPerType[declaringTpye].ContainsKey(methodId))
                        {
                            __genericMethodDefGettersByIdPerType[declaringTpye][methodId] = __methodInfoGettersPerType[declaringTpye](methodId);
                        }

                        MethodInfo methodInfo = __genericMethodDefGettersByIdPerType[declaringTpye][methodId];

                        if (saveInfo.IsGeneric)
                        {
                            Type[] typeArgs = new Type[saveInfo.GenericTypeArguments.Count];

                            for (int i = 0; i < typeArgs.Length; i++)
                            {
                                VersionedType versionedType = SaveAndLoadManager.S.GetVersionedType(saveInfo.GenericTypeArguments[i]);
                                Type resolvedType = versionedType.ResolveForCurrentHandledType();

                                typeArgs[i] = resolvedType;
                            }

                            MethodInfo concreteGeneric = methodInfo.MakeGenericMethod(typeArgs);

                            methodInfo = concreteGeneric;
                        }

                        Func<object, Delegate> getter = (instance) => methodInfo.CreateDelegate(typeof(T), instance);

                        //update the method getter cache to directly create the closed generic next time
                        __methodGettersByIdPerType[declaringTpye][variantId] = getter;
                    }
                }

                //calling the getter with an other type in case of static methods is fine as the instance is not used
                var del = __methodGettersByIdPerType[declaringTpye][variantId](target);


                T typedDel = TryGetOrCreateDelegate(del);

                if (typedDel != null) //todo: ??? , why null does work and default not? shouldnt be the opposite? (replace null with default and see error)
                {
                    __delegateMapPerInstance[targetId][methodId].Add(variantId, del);
                }

                return typedDel;
            }


            T TryGetOrCreateDelegate(Delegate del)
            {
                if (del is T typedDel)
                {
                    return typedDel;
                }


                //as of now, the save system only works with Action and Func, if the above type check failes because the requested
                //type is a for example UnityAction, or any other delegate type, then we try to cast the Action or Func into that type

                Type systemDelType = typeof(System.Action);
                Type targetType = typeof(T);

                bool targetisActionOrFunc = targetType.Assembly.FullName == systemDelType.Assembly.FullName
                    && (targetType.Name.StartsWith("Action") || targetType.Name.StartsWith("Func"));


                if (!targetisActionOrFunc)
                {
                    try
                    {
                        typedDel = (T)(object)Delegate.CreateDelegate(targetType, del.Target, del.Method);

                        return typedDel;
                    }
                    catch { Debug.Log("catch"); }
                }

                Debug.LogError($"The delegate found for targetId {targetId}, methodId {methodId}, variantId {variantId} " +
                    $"is not of the expected type {typeof(T).AssemblyQualifiedName}. It is of type {del.GetType().AssemblyQualifiedName}. " +
                    $"Going to return default value.");
                return default;
            }
        }




        //for saving
        public InvocationList GetInvocationList<T>(T del) where T : System.Delegate
        {
            if (del == null)
            {
                return null;
            }

            var delegates = del.GetInvocationList();
            var saveInfos = new List<DelegateSaveInfo>();

            foreach (var singleDel in delegates)
            {
                var saveInfo = GetDelegateSaveInfo(singleDel);

                if (saveInfo != null)
                {
                    saveInfos.Add(saveInfo);
                }
            }

            if (saveInfos.Count == 0)
            {
                return null;
            }

            return new InvocationList { Delegates = saveInfos };
        }




        public DelegateSaveInfo GetDelegateSaveInfo(Delegate del)
        {
            return GetDelegateSaveInfo(del.Method, del.Target);
        }


        public DelegateSaveInfo GetDelegateSaveInfo(MethodInfo Method, object Target)
        {
            if (Target != null && Target.GetType().IsDefined(typeof(CompilerGeneratedAttribute)))
            {
                //Debug.LogError($"Can not get delegate save info for compiler generated types. Method: {Method.Name}, Target Type: {Target.GetType().CleanAssemblyQualifiedName()} . " +
                //    $"Please ensure you are not trying to save delegates that point to anonymous methods or lambda expressions. " +
                //    $"Going to return null.");
                return null;
            }


            bool isStatic = Target == null;

            RandomId targetId = isStatic ?
                  GetStaticSubtituteId(Method.DeclaringType)
                //TODO: accept the caller object too and use it as the referencee to count the target as referenced
                //or just use .Keepalive() on it
                : _GetObjectIdWithoutReferencing(Target);

            Type targetType = Method.DeclaringType;

            //if (targetType.IsGenericType) targetType = targetType.GetGenericTypeDefinition();


            string signature = TypeUtils.GetMethodSignature(Method);





            if (!__methodIdsByMethodSignaturePerType.ContainsKey(targetType))
            {
                Type saveHandler = SaveAndLoadManager.Singleton.GetSaveHandlerTypeFrom(targetType);

                EnsureSaveHandlerTypeIsInitialized(saveHandler);

                if (!__methodIdsByMethodSignaturePerType.ContainsKey(targetType))
                {
                    Debug.LogError($"No method ids registered for type {targetType}. Method name: {Method.Name}, TargetId: {targetId} . " +
                        $"Please ensure you register method ids for this type before trying to get delegate save infos for its methods. " +
                        $"Going to return null.");
                    return null;
                }
            }



            //if the delegate registered return a saveinfo
            if (__methodIdsByMethodSignaturePerType[targetType].TryGetValue(signature, out var variantId))
            {
                long methodId = variantId;

                if (Method.IsGenericMethod)
                {
                    //todo: cache the signature per MethodInfo
                    string methodDefSignature = TypeUtils.GetMethodSignature(Method.GetGenericMethodDefinition());
                    methodId = __methodIdsByMethodSignaturePerType[targetType][methodDefSignature];
                }


                //todo: this instance should be cached too
                var saveInfo = new DelegateSaveInfo(targetId, methodId)
                {
                    GetByMethodInfo = Method.IsGenericMethod || Method.GetParameters().Any(p => p.ParameterType.CanNotBeUsedAsGenericParameter()) || Method.ReturnType.CanNotBeUsedAsGenericParameter(),
                    GenericVairantId = variantId,
                    //todo: to get the type args every single time is costly, we could cache them per method info
                    GenericTypeArguments = Method.IsGenericMethod
                        ? Method.GetGenericArguments().Select(arg => VersionedType.From(arg)).ToList()
                        : null,
                };
                return saveInfo;
            }
            //if not, check if generic, if it is, no problem, their different closed construct versions registered dynamicaly, otherwise its an error
            else
            {
                if (Method.IsGenericMethod)
                {
                    var methodDef = Method.GetGenericMethodDefinition();

                    var methodDefSignature = TypeUtils.GetMethodSignature(methodDef);


                    if (__methodIdsByMethodSignaturePerType[targetType].TryGetValue(methodDefSignature, out var methodId))
                    {
                        variantId = long.Parse(RandomId.Get().ToString());

                        var saveInfo = new DelegateSaveInfo(targetId, methodId)
                        {
                            GetByMethodInfo = Method.IsGenericMethod || Method.GetParameters().Any(p => p.ParameterType.CanNotBeUsedAsGenericParameter()) || Method.ReturnType.CanNotBeUsedAsGenericParameter(),
                            GenericVairantId = variantId,
                            GenericTypeArguments = Method.GetGenericArguments().Select(arg => VersionedType.From(arg)).ToList(),
                        };

                        __methodIdsByMethodSignaturePerType[targetType][signature] = variantId;

                        return saveInfo;
                    }
                    else
                    {
                        Debug.LogError($"No method id found for generic method definition signature {methodDefSignature} in type {targetType}. " +
                            $"Please ensure you register a method id for this method before trying to get delegate save infos for it. " +
                            $"Going to return null.");
                        return null;
                    }
                }
                else
                {
                    Debug.LogError($"No method id found for method signature {signature} in type {targetType}. " +
                        $"Please ensure you register a method id for this method before trying to get delegate save infos for it. " +
                        $"Going to return null.");
                    return null;
                }
            }
        }





        //kinda old, may no longer true

        //with dynamic registration it is possible that a delegate has multiple registrations with different ids
        //lets imagine a chunk based loading with chunk A and B
        //during a previous play, a delegate was registered dynamically, meaning an id was generated for it during runtime,
        //and was saved with chunk B. (Game is exited and loaded again)
        //Next time the game is loaded with chunk A only, so the game doesnt know about that that delegate was registered before.
        //When something tries to get the delegate save info, it will see that it is not registered and so it will register it.
        //after that, B loads and it loads a saveinfo that represents the same delegate but with a different id
        //thus, the same delegate is identified by multiple ids
        //as long as they point to the same target and method, it should not cause problems, I think...
        //except some performance overhead because we do not register the variant id again if the method and target are the same
        ///thus, closed generic methods will be created again and again, <see cref="GetDelegate{T}(DelegateSaveInfo)"/>
        //currently, only generic variants are registered dynamically

        //fix, todo, error: maybe we could use a bool flag in the saveinfo to indicate that this delegate was registered dynamically
        ///and if true, we dont check if <see cref="__delegatSaveDataByMethodInfo"/> already contains the method info









        public Dictionary<Coroutine, CoroutineHandler> __coroutineSaveDataLookUp = new();
        public Dictionary<RandomId, HashSet<Coroutine>> __routinesByTargetMono = new();


        public void RegisterCoroutine(Coroutine routine, CoroutineHandler saveData)
        {
            if (__coroutineSaveDataLookUp.ContainsKey(routine))
            {
                Debug.LogError("Coroutine is already registered. Return");
                return;
            }
            if (!__routinesByTargetMono.ContainsKey(saveData._targetMonoId))
            {
                __routinesByTargetMono[saveData._targetMonoId] = new HashSet<Coroutine>();
            }


            __coroutineSaveDataLookUp[routine] = saveData;
            __routinesByTargetMono[saveData._targetMonoId].Add(routine);
        }

        public CoroutineHandler GetCoroutineHandler(Coroutine routine)
        {
            if (!__coroutineSaveDataLookUp.ContainsKey(routine))
            {
                Debug.LogError("Coroutine is not registered. Return default.");
                return null;
            }

            return __coroutineSaveDataLookUp[routine];
        }


        public HashSet<Coroutine> GetAllCoroutinesByMono(MonoBehaviour mono)
        {
            var id = _GetObjectIdWithoutReferencing(mono);

            if (id.IsDefault) return new();

            var routines = __routinesByTargetMono[id];

            return routines;
        }




        public Dictionary<Type, RandomId> __staticSubtituteIdsByType = new();
        //public Dictionary<RandomId, Type> __typesByStaticSubtituteId = new();


        public void RegisterStaticSubtitute<T>(T subtitute, RandomId id) where T : StaticSubtitute
        {
            Type type = subtitute.SubtitutedType;

            if (__staticSubtituteIdsByType.ContainsKey(type))
            {
                Debug.LogError($"Infra: Static subtitute of type {type} is already registered. Skipping registration.");
                return;
            }
            //if (__typesByStaticSubtituteId.ContainsKey(id))
            //{
            //    Debug.LogError($"Infra: Static subtitute id {id} is already registered. Skipping registration.");
            //    return;
            //}

            __staticSubtituteIdsByType[type] = id;
            //__typesByStaticSubtituteId[id] = type;
        }

        public RandomId GetStaticSubtituteId(Type type)
        {

            if (__staticSubtituteIdsByType.TryGetValue(type, out var id))
            {
                return id;
            }
            else
            {
                Debug.LogError($"Infra: Static subtitute of type {type} is not registered.");
                return RandomId.Default;
            }
        }





        new public void Destroy(Object obj)
        {
            UnityObjectLifeCycleManagement.ScheduleToDestroy(obj);
        }


        public bool IsScheduledOrDestroyed(Object obj)
        {
            return UnityObjectLifeCycleManagement.IsScheduledOrDestroyed(obj);
        }





        public void CreateDefaultJsonSerializerSettings()
        {
            //todo: this searching method assumes there is no inheritance between converters
            var converters = AppDomain.CurrentDomain.GetUserAssemblies().SelectMany(asm => asm.GetTypes())
                .Where(t => !t.IsAbstract && typeof(JsonConverter).IsAssignableFrom(t))
                .Select(t => (JsonConverter)Activator.CreateInstance(t))
                .ToList();

            //Debug.Log($"Found {converters.Count} JsonConverters in the assembly.");
            //Debug.Log("Found converters: \n" + string.Join("\n", converters.Select(c => c.GetType().CleanAssemblyQualifiedName())));

            //todo: as this code is part of a package, we should not override the default settings of JsonConvert globally
            //instead, we should create our own JsonSerializerSettings instance and use it wherever needed
            //or contribute to the already an already existing instance.
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                Converters = converters,
                ///note: this settings is important for both <see cref="CustomSaveData{TStruct}"/> and <see cref="Data{T}"/>
                ///as the instances of the derived types of these types are assigned with field initializers
                ObjectCreationHandling = ObjectCreationHandling.Auto,
                //TypeNameHandling = TypeNameHandling.Auto,
                //WARNING: DO NOT SET THIS. From 0.03 to 1.8s on first call, then worse and worse on subsequent calls.
                //if newtonsoft tries to read a field or property via reflection in a unityobject, unity might trigger events to them, for example gpu readbacks
                //ContractResolver = new DefaultContractResolver
                //{
                //    DefaultMembersSearchFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                //}
            };
        }











        public class UnityObjectLifeCycleManagementService
        {
            public Dictionary<int, WeakReference<Object>> _trackedObjects = new();

            public void ScheduleToDestroy(Object obj)
            {
                if (obj == null) return;

                if (!_trackedObjects.ContainsKey(obj.GetInstanceID()))
                {
                    _trackedObjects.Add(obj.GetInstanceID(), new WeakReference<Object>(obj));
                    Object.Destroy(obj);
                }
            }

            public bool IsScheduledOrDestroyed(Object obj)
            {
                if (obj == null) return true;

                if (_trackedObjects.ContainsKey(obj.GetInstanceID())) return true;
                else return false;
            }


            public List<int> _keysToRemove = new();

            public void CleanDestroyedReferences()
            {
                _keysToRemove.Clear();

                foreach (var (key, weakRef) in _trackedObjects)
                {
                    bool managedPartExists = weakRef.TryGetTarget(out var obj);

                    // if the c# managed object is not GC-ed
                    if (managedPartExists)
                    {
                        bool enginePartExists = obj != null;

                        //but the engine side part is destroyed
                        if (!enginePartExists)
                            _keysToRemove.Add(key);
                    }
                    else
                        _keysToRemove.Add(key);
                }

                foreach (var key in _keysToRemove)
                {
                    _trackedObjects.Remove(key);
                }

                _keysToRemove.Clear();
            }
        }







        public static ComponentCacheService ComponentCache { get; set; } = new ComponentCacheService();

        public class ComponentCacheService
        {
            public Dictionary<GameObject, Dictionary<Type, List<Component>>> _componentCachePerGO = new();


            public void SetCache(GameObject go, List<Component> components)
            {
                if (!_componentCachePerGO.ContainsKey(go))
                {
                    _componentCachePerGO[go] = new Dictionary<Type, List<Component>>();
                }
                else
                {
                    _componentCachePerGO[go].Clear();
                }

                foreach (var component in components)
                {
                    var type = component.GetType();
                    if (!_componentCachePerGO[go].ContainsKey(type))
                    {
                        _componentCachePerGO[go][type] = new List<Component>();
                    }
                    _componentCachePerGO[go][type].Add(component);
                }
            }


            public T GetCachedComponent<T>(GameObject go) where T : Component
            {
                if (!_componentCachePerGO.ContainsKey(go))
                {
                    _componentCachePerGO[go] = new Dictionary<Type, List<Component>>();
                }

                var type = typeof(T);

                if (!_componentCachePerGO[go].ContainsKey(type))
                {
                    var comp = go.GetComponent<T>();
                    if (comp != null)
                    {
                        _componentCachePerGO[go][type] = new() { comp };
                    }
                    return comp;
                }
                else
                {
                    return (T)_componentCachePerGO[go][type][0];
                }
            }

            public List<Component> GetCachedComponents(GameObject go)
            {
                if (!_componentCachePerGO.ContainsKey(go))
                {
                    _componentCachePerGO[go] = new Dictionary<Type, List<Component>>();
                }

                var allComponents = new List<Component>();

                foreach (var kvp in _componentCachePerGO[go])
                {
                    allComponents.AddRange(kvp.Value);
                }

                return allComponents;
            }
        }
    }





    public class SceneManagement
    {
        public class SceneInfo
        {
            public int buildIndex;
            public int sceneHandle;
            public RandomId InstanceId;
        }

        public List<SceneInfo> _loadedSceneInfos = new();
        public List<SceneInfo> _savedSceneInfos = new();

        public Dictionary<int, SceneInfo> _byHandle = new();
        public Dictionary<RandomId, SceneInfo> _byInstanceId = new();

        public Dictionary<RandomId, Scene> _scenesByInstanceId = new();

        public RandomId _activeSceneInstanceIdFromSaveFile;
        public Scene ActiveSceneInstanceIdFromSaveFile { get => SceneById(_activeSceneInstanceIdFromSaveFile); }


        //todo: perhaps replace handle with instanceId?
        //con: currently SceneInfra calls this during Awake which also happens during scene loading before we can assign instanceIds
        public Dictionary<int, SceneInfra> SceneInfrasBySceneHandle { get; } = new();


        public SceneManagement()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            var id = RandomId.Get();

            var info = new SceneInfo
            {
                buildIndex = scene.buildIndex,
                sceneHandle = scene.handle,
                InstanceId = id,
            };

            _loadedSceneInfos.Add(info);
            _byHandle.Add(scene.handle, info);
            _byInstanceId.Add(id, info);
            _scenesByInstanceId.Add(id, scene);
        }

        public bool _firstUnload = true;
        private void OnSceneUnloaded(Scene scene)
        {
            if (_firstUnload) { _firstUnload = false; return; }

            var info = _byHandle[scene.handle];
            _loadedSceneInfos.Remove(info);
            _byHandle.Remove(scene.handle);
            _byInstanceId.Remove(info.InstanceId);
            _scenesByInstanceId.Remove(info.InstanceId);
        }


        public IEnumerator EnsureScenesAreLoadedFromSaveFile()
        {
            var tasks = new List<AsyncOperation>();

            foreach (var sceneInfo in _savedSceneInfos)
            {
                var oldSceneIdFromSaveFile = sceneInfo.InstanceId;

                if (!_byInstanceId.ContainsKey(oldSceneIdFromSaveFile))
                {
                    UnityAction<Scene, LoadSceneMode> onSceneLoaded = null;
                    onSceneLoaded = OverrideGeneratedIdWithIdFromSaveFile;

                    SceneManager.sceneLoaded += onSceneLoaded;
                    var op = SceneManager.LoadSceneAsync(sceneInfo.buildIndex, LoadSceneMode.Additive);


                    tasks.Add(op);

                    void OverrideGeneratedIdWithIdFromSaveFile(Scene scene, LoadSceneMode loadSceneMode)
                    {
                        //Debug.Log($"Overriding generated scene id with id from save file: {oldSceneIdFromSaveFile} for scene {scene.handle}, {scene.name}, {sceneInfo.buildIndex}.");

                        bool notTheExptected = scene.buildIndex != sceneInfo.buildIndex;
                        if (notTheExptected) return;

                        var info = _byHandle[scene.handle];
                        var generatedId = info.InstanceId;
                        info.InstanceId = oldSceneIdFromSaveFile;
                        _byInstanceId.Remove(generatedId);
                        _byInstanceId.Add(oldSceneIdFromSaveFile, info);
                        _scenesByInstanceId.Remove(generatedId);
                        _scenesByInstanceId.Add(oldSceneIdFromSaveFile, scene);

                        SceneManager.sceneLoaded -= onSceneLoaded;
                    }
                }
            }

            while (tasks.Any(t => !t.isDone))
            {
                yield return null;
            }
        }


        public RandomId SceneIdByHandle(int sceneHandle)
        {
            var info = _byHandle[sceneHandle];
            return info.InstanceId;
        }

        public Scene SceneById(RandomId sceneId)
        {
            return _scenesByInstanceId[sceneId];
        }


        [SaveHandler(id: 102204973066903000, nameof(SceneManagement), typeof(SceneManagement), order: -90, singleton: true)]
        public class SceneManagementSaveHandler : UnmanagedSaveHandler<SceneManagement, SceneManagementSaveData>
        {
            public override void WriteSaveData()
            {
                base.WriteSaveData();
                __saveData._loadedSceneInfos = GetObjectId(__instance._loadedSceneInfos, setLoadingOrder: true);
                Scene activeScene = SceneManager.GetActiveScene();
                __saveData.ActiveSceneInstanceIdFromSaveFile = Infra.SceneManagement.SceneIdByHandle(activeScene.handle);
            }


            public override void LoadPhase1()
            {
                base.LoadPhase1();
                ///note: you see <see cref="SceneManagementSaveData._loadedSceneInfos"/> removed as unreferenced beacuse of this line. It's not assigned back to its field.
                __instance._savedSceneInfos = GetObjectById<List<SceneInfo>>(__saveData._loadedSceneInfos);
                __instance._activeSceneInstanceIdFromSaveFile = __saveData.ActiveSceneInstanceIdFromSaveFile;
            }
        }

        public class SceneManagementSaveData : SaveDataBase
        {
            public RandomId _loadedSceneHandleToPersistentSceneInstanceId;
            public RandomId _loadedSceneInfos;
            public RandomId ActiveSceneInstanceIdFromSaveFile;
        }

    }






    public static class GameObjectExtensions
    {
        public static T GetCachedComponent<T>(this GameObject go) where T : Component
        {
            return Infra.ComponentCache.GetCachedComponent<T>(go);
        }

        public static List<Component> GetCachedComponents(this GameObject go)
        {
            return Infra.ComponentCache.GetCachedComponents(go);
        }
    }







    public class StaticInfraSubtitute : StaticSubtitute<Infra> { }

    [SaveHandler(832974395872348320, nameof(StaticInfraSubtitute), typeof(StaticInfraSubtitute), staticHandlerOf: typeof(Infra))]
    public class StaticInfraSaveHandler : StaticSaveHandlerBase<StaticInfraSubtitute, StaticInfraSaveData>
    {

    }

    public class StaticInfraSaveData : StaticSaveDataBase
    {

    }
}
