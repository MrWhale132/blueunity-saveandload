using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.Infrastructure.AddressableInfra;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using Assets._Project.Scripts.UtilScripts;
using Assets._Project.Scripts.UtilScripts.Extensions;
using Newtonsoft.Json;
using Theblueway.Core.Runtime.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Theblueway.SaveAndLoad.Packages.com.theblueway.saveandload.Runtime.InfraScripts;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Theblueway.SaveAndLoad.Packages.com.theblueway.saveandload.Runtime;
using static Assets._Project.Scripts.SaveAndLoad.SaveAndLoadManager;
using Theblueway.Core.Runtime;
using ObjectFactory = Theblueway.Core.Runtime.ObjectFactory;



#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Assets._Project.Scripts.SaveAndLoad
{
    //TODO: remove it later once it moved into the bootstrap scene
    [DefaultExecutionOrder(-1000)]
    public class SaveAndLoadManager : MonoBehaviour
    {
        public static SaveAndLoadManager Singleton { get; private set; }
        public static SaveAndLoadManager S => Singleton;
        public static PrefabDescriptionRegistry PrefabDescriptionRegistry { get; set; }
        public static ScenePlacedObjectRegistry ScenePlacedObjectRegistry { get; set; }


        public enum SaveState
        {
            Start,
            Main,
            TempA,
            TempB,
            End,
            Terminate,
        }


        public int _appVersion = 1;

#if UNITY_EDITOR
        public bool _incrementAppVersion;
#endif


        [HideInInspector]
        public SaveState __currentSaveState = SaveState.Main;


        public List<ISaveAndLoad> __mainSaveHandlers = new();
        public List<ISaveAndLoad> __tempA_saveHandlers = new();
        public List<ISaveAndLoad> __tempB_saveHandlers = new();
        //state machine vars
        public List<ISaveAndLoad> __currentSaveHandlers;
        public List<ISaveAndLoad> __iteratedSaveHandlers;

        public Dictionary<RandomId, ISaveAndLoad> __saveHandlerByHandledObjectIdLookUp = new();

        public Dictionary<long, MigrationiPipeline> __migrationPipelinesByHandlerId => _coreService.__migrationPipelinesByHandlerId;


        public Dictionary<Type, Type> __saveHandlerTypeByHandledObjectTypeLookUp => _coreService.__saveHandlerTypeByHandledObjectTypeLookUp;
        public IEnumerable<SaveHandlerAttribute> _nonGenericHandlerInfos {
            get
            {
                foreach (var attr in _coreService.__saveHandlerAttributesByHandledType.Values)
                {
                    if (!attr.IsGeneric && attr.HandledType != typeof(Array))
                    {
                        yield return attr;
                    }
                }
            }
        }


        public bool IsIteratingSaveHandlers { get; private set; }





        public void Awake()
        {
            if (Singleton == null)
            {
                Singleton = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.LogError("Multiple instances of SaveAndLoadManager detected. Destroying the new instance.");
                Destroy(gameObject);
            }

            PrefabDescriptionRegistry = new();
            ScenePlacedObjectRegistry = new();

            __currentSaveHandlers = __mainSaveHandlers;


            InitSaveHandlerWork();
        }


        private void Start()
        {
            Infra.Singleton.RegisterSingleton(this);
            Infra.Singleton.RegisterSingleton(PrefabDescriptionRegistry);
            Infra.Singleton.RegisterSingleton(ScenePlacedObjectRegistry);
        }


        public void InitSaveHandlerWork()
        {
            LoadSingletonObjectIds();

            _coreService.BuildSaveDataAttributeLookup();

            CollectSaveHandlers(out var handlerInfos);

            _coreService.BuildSaveHandlerLookups(handlerInfos);

            CreateSaveHandlerFactories();

            CollectCustomSaveDatas();

            _coreService.BuildMigrationLookups();

            RegisterStaticSaveHandlers();

            EnsureSingletons();

#if UNITY_EDITOR
            UpdateSingletonObjectIds();
#endif
        }




        private void OnValidate()
        {
#if UNITY_EDITOR
            if (_incrementAppVersion)
            {
                _incrementAppVersion = false;
                IncrementAppVersion();
            }
#endif
        }




        public void EnsureSingletons()
        {
            foreach (var info in _coreService.__saveHandlerAttributesById.Values)
            {
                if (info.IsSingleton)
                {
                    GetOrCreateSingletonObjectIdBySaveHandlerId(info.Id);
                }
            }
        }



        public const string InheritanceChainFolderPath = "TheBlueWay/InheritanceChains";

        public string GetInheritanceChainFilePath(int appVersion)
        {
            string fileName = $"inheritance_chain_v{appVersion}.json";

            string path = Path.Combine(InheritanceChainFolderPath, fileName);

            return path;
        }


#if UNITY_EDITOR
        public void IncrementAppVersion()
        {
            int newVersion = _appVersion + 1;

            Dictionary<long, IEnumerable<long>> inheritanceChainById = new();

            foreach ((var id, var attr) in Service.__saveHandlerAttributesById)
            {
                List<long> inheritanceChanin = new();

                if (!attr.IsStatic)
                {
                    var baseType = attr.HandledType.BaseType;

                    while (baseType != null)
                    {
                        if (Service.__saveHandlerAttributesByHandledType.TryGetValue(baseType, out var baseAttr))
                        {
                            inheritanceChanin.Add(baseAttr.Id);
                        }
                        baseType = baseType.BaseType;
                    }
                }

                inheritanceChainById.Add(id, inheritanceChanin);
            }

            var filePath = Path.Combine(Application.streamingAssetsPath, GetInheritanceChainFilePath(newVersion));

            if (File.Exists(filePath))
            {
                Debug.LogError($"Can not increment appVersion from {_appVersion} to {newVersion} because a necessary file can not be created. " +
                    $"Reason: inheritance_chain file already exists with current appVersion. " +
                    $"Path: {filePath}");
                return;
            }

            var json = JsonConvert.SerializeObject(inheritanceChainById);

            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(filePath, json);

            int oldVersion = _appVersion;

            _appVersion = newVersion;

            Debug.Log($"Successfuly incremented appVersion from {oldVersion} to {newVersion}");
        }
#endif



        public Dictionary<long, IEnumerable<long>> GetInheritanceChainForAppVersion(int appversion)
        {
            var filePath = Path.Combine(Application.streamingAssetsPath, GetInheritanceChainFilePath(appversion));

            if (!File.Exists(filePath))
            {
                string msg = $"Did not find inheritance chain file for appversion: {appversion}";
                Debug.LogError(msg);
                throw new Exception(msg);
            }

            string json = File.ReadAllText(filePath);

            var inheritanceChain = JsonConvert.DeserializeObject<Dictionary<long, IEnumerable<long>>>(json);

            return inheritanceChain;
        }








        public void LoadSingletonObjectIds()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "SingletonObjectIds.json");

            if (!File.Exists(path))
            {
                __singletonObjectIdsBySaveHandlerIds = new();
            }
            else
            {
                using var fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);

                using var reader = new StreamReader(fileStream);

                var json = reader.ReadToEnd();

                ///this lookup is filled by <see cref="GetOrCreateSingletonObjectIdBySaveHandlerId(string)"/>
                ///which is called from savehandlers' Init which happens right below these lines
                ///so dont get confused that u dont see a reference to it right here
                __singletonObjectIdsBySaveHandlerIds = JsonConvert.DeserializeObject<Dictionary<long, RandomId>>(json) ?? new();
            }
        }


#if UNITY_EDITOR
        public void UpdateSingletonObjectIds()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "SingletonObjectIds.json");

            var json = JsonConvert.SerializeObject(__singletonObjectIdsBySaveHandlerIds);

            File.WriteAllText(path, json);
        }
#endif



        public Dictionary<long, RandomId> __singletonObjectIdsBySaveHandlerIds = new();


        public RandomId GetOrCreateSingletonObjectIdBySaveHandlerId(long handlerId)
        {
            if (!__singletonObjectIdsBySaveHandlerIds.ContainsKey(handlerId))
            {
                var objectId = RandomId.Get();
                //Debug.Log($"{handlerId} {objectId}");
                __singletonObjectIdsBySaveHandlerIds.Add(handlerId, objectId);
            }

            return __singletonObjectIdsBySaveHandlerIds[handlerId];
        }



        public void CollectSaveHandlers(out IEnumerable<SaveHandlerAttribute> handlerInfos)
        {
            _coreService.CollectSaveHandlers(out handlerInfos);
        }



        public void CreateSaveHandlerFactories()
        {
            foreach (var info in _nonGenericHandlerInfos)
            {
                Func<SaveHandlerBase> ctor = CreateTypedCtor<SaveHandlerBase>(info.HandlerType);

                __saveHandlerCreatorsById[info.Id] = ctor;
                __saveHandlerCreatorsByType[info.HandledType] = ctor;
            }
        }


        public void RegisterStaticSaveHandlers()
        {
            foreach (var info in _nonGenericHandlerInfos)
            {
                if (info.IsStatic)
                {
                    _coreService.__staticSaveHandlerAttributesByHandledType[info.StaticHandlerOf] = info;

                    var ctor = _coreService.__saveHandlerCreatorsByType[info.HandledType];

                    ///todo: dont forget about generic static classes. <see cref="GenericWithStaticExampleClass{T}"/>
                    var handler = ctor();
                    handler.Init(null);
                    AddSaveHandler(handler);
                }
            }
        }



        //        On IL2CPP:
        //Unity replaces it with a fallback “interpreter mode” for simple expressions only, but anything nontrivial will fail or run extremely slowly.
        //In many builds(especially older Unitys), it will just throw PlatformNotSupportedException.
        // Workaround: use Expression.Compile(preferInterpretation: true) on newer Unitys(2021.3+). That uses a safe interpreted mode — slower, but portable.
        public Func<TBase> CreateTypedCtor<TBase>(Type derivedType)
        {
            if (!typeof(TBase).IsAssignableFrom(derivedType))
                throw new ArgumentException($"{derivedType} is not assignable to {typeof(TBase)}");

            ConstructorInfo ctor = derivedType.GetConstructor(Type.EmptyTypes);

            if (ctor == null)
                throw new ArgumentException("Parameterless constructor not found.");

            NewExpression newExpr = Expression.New(ctor);

            // Cast to TBase (if needed — usually implicit, but good for clarity)
            UnaryExpression castExpr = Expression.Convert(newExpr, typeof(TBase));


            //todo:test this out
            // Bind dynamically:
            var del = (Func<TBase>)Delegate.CreateDelegate(typeof(Func<TBase>),
                typeof(CtorFactory<>).MakeGenericType(derivedType).GetMethod("Create"));


            return del;

            //return Expression.Lambda<Func<TBase>>(castExpr).Compile();
            //todo: test this out
            //return Expression.Lambda<Func<TBase>>(castExpr).Compile(preferInterpretation: true);
        }

        public static class CtorFactory<T> where T : new()
        {
            public static T Create() => new T();
        }



        //Some reflection and delegate creation APIs do still work — because they rely on existing, precompiled metadata:
        //Works fine:
        //Activator.CreateInstance(typeof(MyComponent));
        //Delegate.CreateDelegate(typeof(Action), target, "MethodName");
        //MethodInfo.Invoke(target, args);
        //FieldInfo.GetValue(obj);
        //These don’t generate new code — they just use metadata from already compiled IL2CPP stubs.





        public CoreService _coreService = new();

        public class CoreService
        {

            public Dictionary<Type, Func<SaveHandlerBase>> __saveHandlerCreatorsByType = new();
            public Dictionary<long, Func<SaveHandlerBase>> __saveHandlerCreatorsById = new();

            //the first Type is the HandledType's typedef, the second Type is the generic savehandler's typedef
            //the third one is the HandledType's concrete types
            public Dictionary<Type, (Type typeDef, Dictionary<Type, Func<SaveHandlerBase>> concreteTypes)> __genericSaveHandlerCreatorsByTypePerTypeDef = new();
            public Dictionary<int, (Type saveHandlerTypeDef, Dictionary<Type, Func<SaveHandlerBase>> concreteTypes)> __arraySaveHandlerCreatorsByTypePerDimension = new();


            public Dictionary<long, Dictionary<Type, Func<SaveHandlerBase>>> __genericSaveHandlerCreatorsByTypePerId = new();


            public Dictionary<Type, Type> __saveHandlerTypeByHandledObjectTypeLookUp = new();


            public Dictionary<Type, SaveHandlerAttribute> __staticSaveHandlerAttributesByHandledType = new();

            public Dictionary<Type, SaveHandlerAttribute> __saveHandlerAttributesByHandledType = new();
            public Dictionary<long, SaveHandlerAttribute> __saveHandlerAttributesById = new();


            //for current version only
            public Dictionary<Type, CustomSaveDataAttribute> __customSaveDataAttributesByHandledType = new();
            public Dictionary<long, CustomSaveDataAttribute> __customSaveDataAttributesById = new();
            //for past versions only
            public Dictionary<Type, CustomSaveDataAttribute> __versionedCustomSaveDataAttributesByHandledType = new();
            public Dictionary<long, Dictionary<int, CustomSaveDataAttribute>> __versionedCustomSaveDataAttributesByVersionById = new();
            //both
            public Dictionary<long, Dictionary<int, Type>> __customSaveDataHandledTypesByVersionById = new();


            //current savedata type only
            public Dictionary<long, Type> __saveDataTypesBySaveHandlerId = new();


            public bool HadBuiltVersionLookup { get; set; }

            public Dictionary<long, int> __currentVersionOfHandledTypeById = new();

            public Dictionary<long, MigrationiPipeline> __migrationPipelinesByHandlerId = new();

            //note: includes versioned types of the current type of SaveData
            public Dictionary<Type, long> __saveHandlerIdBySaveDataType = new();
            public Dictionary<long, Dictionary<int, Type>> __savedataTypesByVersionByHandlerId = new();






            public bool HasTypeId(Type type, bool isStatic, out long typeId)
            {
                if (HasSaveHandlerForType(type, isStatic, out var shAttribute))
                {
                    typeId = shAttribute.Id;
                    return true;
                }

                if (HasCustomSaveData(type, out var attribute))
                {
                    typeId = attribute.Id;
                    return true;
                }

                typeId = 0;
                return false;
            }






            public int GetCustomSaveDataAppVersionByHandledType(Type handledType)
            {
                if (__customSaveDataAttributesByHandledType.ContainsKey(handledType))
                {
                    return SaveAndLoadManager.Singleton._appVersion;
                }
                else if (__versionedCustomSaveDataAttributesByHandledType.TryGetValue(handledType, out var info))
                {
                    return info.AppVersion;
                }
                else
                {
                    Debug.LogError($"No CustomSaveDataAttribute found for handled type {handledType.CleanAssemblyQualifiedName()}");
                    return 1;
                }
            }


            public Type GetCustomSaveDataHandlerTypeByHandledType(Type handledType)
            {
                if (__customSaveDataAttributesByHandledType.TryGetValue(handledType, out var info))
                {
                    return info.SaveHandlerType;
                }
                else if (__versionedCustomSaveDataAttributesByHandledType.TryGetValue(handledType, out info))
                {
                    return info.SaveHandlerType;
                }

                return null;
            }



            public Type GetHandledTypeByHandlerId(long id)
            {
                if (__saveHandlerAttributesById.TryGetValue(id, out var handlerAttr))
                {
                    return handlerAttr.HandledType;
                }
                else if (__customSaveDataAttributesById.TryGetValue(id, out var saveDataAttribute))
                {
                    return saveDataAttribute.HandledType;
                }
                //todo: log error if id is array savehandler
                else return null;
            }




            public bool HasSaveHandlerForType(Type type, bool isStatic, out SaveHandlerAttribute attribute)
            {
                if (isStatic)
                {
                    if (__staticSaveHandlerAttributesByHandledType.TryGetValue(type, out attribute))
                    {
                        return true;
                    }
                    else
                    {
                        attribute = null;
                        return false;
                    }
                }

                if (__saveHandlerAttributesByHandledType.TryGetValue(type, out attribute))
                {
                    return true;
                }
                else
                {
                    attribute = null;
                    return false;
                }
            }


            public bool HasCustomSaveData(Type type, out CustomSaveDataAttribute attribute)
            {
                if (__customSaveDataAttributesByHandledType.TryGetValue(type, out attribute))
                {
                    return true;
                }
                else
                {
                    attribute = null;
                    return false;
                }
            }








            public void BuildMigrationLookups()
            {
                if (HadBuiltVersionLookup) return;

                if (__saveHandlerAttributesById is null or { Count: 0 })
                {
                    Debug.LogError("Cannot build save data version lookup before building save handler lookups.");
                    return;
                }

                if (__savedataTypesByVersionByHandlerId.Count > 0)
                {
                    var content = JsonConvert.SerializeObject(__savedataTypesByVersionByHandlerId);
                    Debug.LogError("It is not expected that this lookup already has any entry, since it is supposed to be built now, and not earlier." +
                        "Going to Clear() it. It's content was:\n" + content);
                    __savedataTypesByVersionByHandlerId.Clear();
                }




                var types = AppDomain.CurrentDomain.GetUserAssemblies().SelectMany(asm => asm.GetTypes());

                foreach (var type in types)
                {
                    MigrationsAttribute migrationsAttr = type.GetCustomAttribute<MigrationsAttribute>();


                    if (migrationsAttr == null)
                    {
                        continue;
                    }

                    if (__currentVersionOfHandledTypeById.ContainsKey(migrationsAttr.SaveHandlerId))
                    {
                        Debug.LogError($"Multiple MigrationsAttribute found for the same SaveHandlerId {migrationsAttr.SaveHandlerId}. " +
                            $"Only one MigrationsAttribute is allowed per SaveHandlerId. " +
                            $"Skipping the new one.");
                        continue;
                    }


                    IEnumerable<MigrationAttribute> migrationAttrs = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static)
                        .Where(m => m.IsDefined(typeof(MigrationAttribute), inherit: false))
                        .Select(m => m.GetCustomAttribute<MigrationAttribute>());

                    if (migrationAttrs.Count() == 0)
                    {
                        continue;
                    }


                    int currentVersion = migrationAttrs.Max(attr => attr.DataVersion) + 1;

                    __currentVersionOfHandledTypeById.Add(migrationsAttr.SaveHandlerId, currentVersion);


                    var pipeline = MigrationiPipeline.From(type);
                    __migrationPipelinesByHandlerId.Add(migrationsAttr.SaveHandlerId, pipeline);



                    Dictionary<int, Type> dataTypesByDataVersion = new();
                    __savedataTypesByVersionByHandlerId.Add(migrationsAttr.SaveHandlerId, dataTypesByDataVersion);

                    foreach (var attr in migrationAttrs)
                    {
                        dataTypesByDataVersion.Add(attr.DataVersion, attr.DataType);

                        //multiple migrations can operate on the same savedata type
                        if (!__saveHandlerIdBySaveDataType.ContainsKey(attr.DataType))
                        {
                            __saveHandlerIdBySaveDataType.Add(attr.DataType, migrationsAttr.SaveHandlerId);
                        }
                    }
                }


                //note: set a version of 1 for types that do not define migrations for themselves
                foreach (var handlerId in __saveHandlerAttributesById.Keys)
                {
                    if (!__currentVersionOfHandledTypeById.ContainsKey(handlerId))
                    {
                        __currentVersionOfHandledTypeById.Add(handlerId, 1);
                    }

                    int currentVersion = __currentVersionOfHandledTypeById[handlerId];

                    Type currentSaveDataType = __saveDataTypesBySaveHandlerId[handlerId];

                    if (!__saveHandlerIdBySaveDataType.ContainsKey(currentSaveDataType))
                    {
                        __saveHandlerIdBySaveDataType.Add(currentSaveDataType, handlerId);
                    }


                    if (!__savedataTypesByVersionByHandlerId.ContainsKey(handlerId))
                    {
                        __savedataTypesByVersionByHandlerId.Add(handlerId, new());
                    }

                    __savedataTypesByVersionByHandlerId[handlerId].Add(currentVersion, currentSaveDataType);
                }

                HadBuiltVersionLookup = true;
            }


            public int GetCurrentVersionOfTypeById(long id)
            {
                if (HadBuiltVersionLookup == false)
                {
                    Debug.Log("but why?");
                    BuildMigrationLookups();
                }

                if (__currentVersionOfHandledTypeById.TryGetValue(id, out var version))
                {
                    return version;
                }
                else
                {
                    Debug.LogError($"No version information found for SaveHandlerId {id}. " +
                        $"This should not have happened at this point. " +
                        $"When the lookup is built, even the handlers that do not define a version for themselves " +
                        $"should have their current version set. " +
                        $"Returning version 1 as default.");
                    return 1;
                }
            }





            public void BuildSaveDataAttributeLookup()
            {
                var types = AppDomain.CurrentDomain.GetUserAssemblies().SelectMany(asm => asm.GetTypes());

                foreach (var type in types)
                {
                    //todo: because of backward compatibility, remove this code once all savedata has an attribute
                    SaveHandlerAttribute attribute = type.GetCustomAttribute<SaveHandlerAttribute>();

                    if (attribute != null)
                    {
                        if (__saveDataTypesBySaveHandlerId.ContainsKey(attribute.Id))
                            continue;

                        Type baseType = type.BaseType;

                        while (!baseType.IsGenericType || baseType.GetGenericTypeDefinition() != typeof(SaveHandlerGenericBase<,>))
                        {
                            baseType = baseType.BaseType;
                        }

                        Type savedataType = baseType.GetGenericArguments()[1];

                        if (savedataType.IsGenericType)
                            savedataType = savedataType.GetGenericTypeDefinition();

                        __saveDataTypesBySaveHandlerId.Add(attribute.Id, savedataType);
                        continue;
                    }


                    SaveDataAttribute attr = type.GetCustomAttribute<SaveDataAttribute>();

                    if (attr == null) continue;

                    Type saveDataType = type;

                    if (type.IsGenericType)
                        saveDataType = type.GetGenericTypeDefinition();

                    //todo:backward comp, remove later this if
                    if (!__saveDataTypesBySaveHandlerId.ContainsKey(attr.SaveHandlerId))
                        __saveDataTypesBySaveHandlerId.Add(attr.SaveHandlerId, saveDataType);
                }
            }


            public void CollectSaveHandlers(out IEnumerable<SaveHandlerAttribute> handlerInfos)
            {
                var infos = new List<SaveHandlerAttribute>();

                var types = AppDomain.CurrentDomain.GetUserAssemblies().SelectMany(asm => asm.GetTypes());

                foreach (Type type in types)
                {
                    if (type.IsInterface || type.IsAbstract)
                        continue;

                    var attr = type.GetCustomAttribute<SaveHandlerAttribute>();
                    if (attr == null)
                    {
                        continue;
                    }

                    Type saveHandlerType = type;


                    if (attr.RequiresManualAttributeCreation)
                    {
                        var method = saveHandlerType.GetMethod("ManualSaveHandlerAttributeCreation", BindingFlags.Public | BindingFlags.Static);
                        if (method == null)
                        {
                            Debug.LogError($"SaveHandler {saveHandlerType.FullName} requires manual attribute creation but does not have a public static method named ManualSaveHandlerAttributeCreation. " +
                                $"Skipping this SaveHandler.");
                            continue;
                        }

                        var result = method.Invoke(null, null);

                        if (result is not SaveHandlerAttribute manualAttr)
                        {
                            Debug.LogError($"SaveHandler {saveHandlerType.FullName} ManualSaveHandlerAttributeCreation method did not return a SaveHandlerAttribute. " +
                                $"Skipping this SaveHandler.");
                            continue;
                        }

                        attr = manualAttr;
                    }


                    attr.HandlerType = saveHandlerType;

                    infos.Add(attr);
                }

                handlerInfos = infos;
            }


            public void BuildSaveHandlerLookups(IEnumerable<SaveHandlerAttribute> handlerInfos)
            {
                //Debug.Log("build");
                foreach (var info in handlerInfos)
                {
                    Type saveHandlerType = info.HandlerType;


                    void LogDuplicateIdError()
                    {
                        Debug.LogError($"SaveHandler with id {info.Id} is already registered. " +
                            $"This means that there are multiple SaveHandlers with the same id. " +
                            $"Please ensure that each SaveHandler has a unique id." +
                            $"Going to skip the new one.");
                    }

                    void LogDuplicateHandledTypeError()
                    {
                        Debug.LogError($"More then one savehandler found for the same handled type {info.HandledType.FullName}. " +
                            $"Only one savehandler is allowed per type");
                    }



                    bool CommonValidation(SaveHandlerAttribute info)
                    {
                        if (__saveHandlerAttributesById.ContainsKey(info.Id))
                        {
                            LogDuplicateIdError();
                            return false;
                        }

                        if (__saveHandlerAttributesByHandledType.ContainsKey(info.HandledType))
                        {
                            LogDuplicateHandledTypeError();
                            return false;
                        }

                        return true;
                    }



                    if (info.IsStatic)
                    {
                        __staticSaveHandlerAttributesByHandledType[info.StaticHandlerOf] = info;
                    }


                    if (info.HandledType.IsGenericTypeDefinition)
                    {
                        var genericHandlerTypeDef = saveHandlerType.GetGenericTypeDefinition();

                        if (__genericSaveHandlerCreatorsByTypePerId.ContainsKey(info.Id))
                        {
                            LogDuplicateIdError();
                            continue;
                        }

                        if (__genericSaveHandlerCreatorsByTypePerTypeDef.ContainsKey(info.HandledType))
                        {
                            LogDuplicateHandledTypeError();
                            continue;
                        }


                        bool isValid = CommonValidation(info);
                        if (!isValid) continue;


                        __saveHandlerAttributesById.Add(info.Id, info);
                        __saveHandlerAttributesByHandledType.Add(info.HandledType, info);


                        //factory methods for generic handlers are lazy loaded, they are created when they first requested
                        __genericSaveHandlerCreatorsByTypePerId.Add(info.Id, new());
                        __genericSaveHandlerCreatorsByTypePerTypeDef.Add(info.HandledType, new(genericHandlerTypeDef, new()));
                    }
                    else if (info.HandledType == typeof(Array) && info.ArrayDimension != 0)
                    {
                        var genericHandlerTypeDef = saveHandlerType.GetGenericTypeDefinition();

                        if (__genericSaveHandlerCreatorsByTypePerId.ContainsKey(info.Id))
                        {
                            LogDuplicateIdError();
                            continue;
                        }

                        if (__arraySaveHandlerCreatorsByTypePerDimension.ContainsKey(info.ArrayDimension))
                        {
                            LogDuplicateHandledTypeError();
                            continue;
                        }



                        bool isValid = CommonValidation(info);
                        if (!isValid) continue;


                        __saveHandlerAttributesById.Add(info.Id, info);
                        __saveHandlerAttributesByHandledType.Add(info.HandledType, info);


                        //same as for generics, except the groupping is by array dimension, not by handled type def
                        //array savehandlers are generics too on the element type, no need to track them in a seperate list
                        __genericSaveHandlerCreatorsByTypePerId.Add(info.Id, new());
                        __arraySaveHandlerCreatorsByTypePerDimension.Add(info.ArrayDimension, new(genericHandlerTypeDef, new()));
                    }
                    else
                    {
                        bool isValid = CommonValidation(info);
                        if (!isValid) continue;


                        __saveHandlerAttributesById.Add(info.Id, info);
                        __saveHandlerAttributesByHandledType.Add(info.HandledType, info);
                        __saveHandlerTypeByHandledObjectTypeLookUp.Add(info.HandledType, saveHandlerType);
                    }
                }
            }


            public void CollectCustomSaveDatas()
            {
                Dictionary<long, Dictionary<Type, int>> customSaveDataVersionsByHandledTypesById = new();


                var types = AppDomain.CurrentDomain.GetUserAssemblies().SelectMany(asm => asm.GetTypes());

                foreach (var type in types)
                {
                    var attr = type.GetCustomAttribute<CustomSaveDataAttribute>();

                    if (attr == null)
                    {
                        continue;
                    }

                    {
                        //if (type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() != typeof(CustomSaveData<>))
                        //{
                        //    Debug.LogError($"Found a CustomSaveData type that does not" +
                        //        $"inherit from CustomSaveData<T> where T is the type it saves. " +
                        //        $"Skipping this type.");
                        //    continue;
                        //}


                        if (attr.HandledType == null)
                        {
                            Debug.LogError("CustomSaveDataAttribute.HandledType is null. " +
                                "It should be set to the type that this CustomSaveData handles. ");
                            continue;
                        }


                        var handledType = attr.HandledType;

                        attr.SetHandlerType(type);



                        if (handledType.IsGenericType)
                        {
                            Debug.LogError("Generic custom save data types are not supported for now. " +
                                "Found type: " + handledType.CleanAssemblyQualifiedName());
                            continue;
                            //todo
                            //handledType = handledType.GetGenericTypeDefinition();
                        }


                        if (attr.IsPastVersion)
                        {
                            if (!__versionedCustomSaveDataAttributesByVersionById.ContainsKey(attr.Id))
                            {
                                __versionedCustomSaveDataAttributesByVersionById.Add(attr.Id, new());
                                customSaveDataVersionsByHandledTypesById.Add(attr.Id, new());
                            }
                            if (__versionedCustomSaveDataAttributesByVersionById[attr.Id].ContainsKey(attr.Version))
                            {
                                Debug.LogError($"Another custom save data type has already been added with this id and version. Id: {attr.Id}, version: {attr.Version}. " +
                                    $"Please ensure each type has a unique handler id and version combination. Ignoring this handler.");
                                continue;
                            }

                            if (customSaveDataVersionsByHandledTypesById[attr.Id].TryGetValue(attr.HandledType, out var version))
                            {
                                Debug.LogError($"Another custom save data type has already been added with this handledType: {attr.HandledType.CleanAssemblyQualifiedName()}\n" +
                                    $"for id: {attr.Id}, at version: {version}. " +
                                    $"Please ensure each handled type is handled only by one custom save data.  Ignoring this handler.");
                                continue;
                            }

                            __versionedCustomSaveDataAttributesByHandledType.Add(handledType, attr);
                            __versionedCustomSaveDataAttributesByVersionById[attr.Id].Add(attr.Version, attr);
                        }
                        else
                        {
                            //if (!handledType.IsStruct())
                            //{
                            //    Debug.LogError("A custom save data type found that operates on a non-struct type. " +
                            //        "Custom save datas should only be used with structs. " +
                            //        "Found type: " + handledType.CleanAssemblyQualifiedName());
                            //    continue;
                            //}


                            if (__customSaveDataAttributesById.ContainsKey(attr.Id))
                            {
                                Debug.LogError($"An other type has already been added with this id: {attr.Id}. " +
                                    $"Please ensure each type has a unique handler id. Ignoring this handler.");
                                continue;
                            }

                            if (__customSaveDataAttributesByHandledType.ContainsKey(handledType))
                            {
                                Debug.LogError($"An other handler has already been added with this handled type: {handledType.CleanAssemblyQualifiedName()}. " +
                                    $"Only one handler allowed per type. Ignoring this handler.");
                                continue;
                            }


                            __customSaveDataAttributesByHandledType.Add(handledType, attr);
                            __customSaveDataAttributesById.Add(attr.Id, attr);
                            //__customSaveDataHandledTypesByVersionById.Add(attr.Id, new());
                        }
                    }
                }




                foreach (var attr in __customSaveDataAttributesById.Values)
                {
                    __customSaveDataHandledTypesByVersionById.Add(attr.Id, new());

                    int currentVersion = 1;

                    if (__versionedCustomSaveDataAttributesByVersionById.TryGetValue(attr.Id, out var versionedAttrs))
                    {
                        currentVersion = versionedAttrs.Keys.Max() + 1;

                        foreach ((int version, var versionAttr) in versionedAttrs)
                        {
                            __customSaveDataHandledTypesByVersionById[attr.Id].Add(version, versionAttr.HandledType);
                        }
                    }

                    __currentVersionOfHandledTypeById.Add(attr.Id, currentVersion);
                    __customSaveDataHandledTypesByVersionById[attr.Id].Add(currentVersion, attr.HandledType);
                }
            }
        }



#if UNITY_EDITOR

        public static EditorService _editorServiceInstance = new();
        public static EditorService Service {
            get
            {
                _editorServiceInstance.InitServiceIfNeeded();
                return _editorServiceInstance;
            }
        }

        public class EditorService
        {
            //the first Type is the HandledType's typedef, the second Type is the generic savehandler's typedef
            //the third one is the HandledType's concrete types
            public Dictionary<Type, (Type typeDef, Dictionary<Type, Func<SaveHandlerBase>> concreteTypes)> __genericSaveHandlerCreatorsByTypePerTypeDef => coreService.__genericSaveHandlerCreatorsByTypePerTypeDef;
            public Dictionary<int, (Type saveHandlerTypeDef, Dictionary<Type, Func<SaveHandlerBase>> concreteTypes)> __arraySaveHandlerCreatorsByTypePerDimension => coreService.__arraySaveHandlerCreatorsByTypePerDimension;

            public Dictionary<long, Dictionary<Type, Func<SaveHandlerBase>>> __genericSaveHandlerCreatorsByTypePerId => coreService.__genericSaveHandlerCreatorsByTypePerId;


            public HashSet<Type> __serializeableTypes;
            public Dictionary<Type, Type> __saveHandlerTypeByHandledObjectTypeLookUp => coreService.__saveHandlerTypeByHandledObjectTypeLookUp;



            public void CollectSerilaizeableTypes()
            {
                HashSet<Type> hasJsonConverter = new HashSet<Type>();

                var converters = JsonConvert.DefaultSettings().Converters;

                foreach (var converter in converters)
                {
                    //todo: this validation should happen when these converters collected
                    if (converter.GetType().IsGenericType)
                    {
                        Debug.LogWarning("There is a generic json converter which is not supported for now. Newtonsoft doesnt handle very well open generics." +
                            "It requires to register every possible closed type variant of that generic beforehand." +
                            "It can be workaround but its not priority at the time I wrote this.");
                        continue;
                    }

                    var baseType = converter.GetType().BaseType;

                    if ((baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(JsonConverter<>)))
                    {
                        var typeConverted = baseType.GetGenericArguments()[0];

                        hasJsonConverter.Add(typeConverted);
                    }
                    else
                    {
                        if (baseType != typeof(JsonConverter) 
                            && converter.GetType().Assembly.GetName().Name != typeof(JsonConverter).Assembly.GetName().Name)
                        {

                            Debug.LogWarning("There is a json converter that does not directly inherits from JsonConverter<> or from JsonConverter. " +
                                $"Type: {converter.GetType().CleanAssemblyQualifiedName()}. " +
                                "It may not be a problem." +
                                "This is just a notice to know about.");
                        }
                    }
                }

                __serializeableTypes = hasJsonConverter;
            }







            public bool __hadInit;

            public void InitServiceIfNeeded()
            {
                if (!__hadInit)
                {
                    __hadInit = true;
                    coreService.CollectSaveHandlers(out var handlerInfos);
                    coreService.BuildSaveHandlerLookups(handlerInfos);
                    coreService.CollectCustomSaveDatas();
                }
            }




            public bool HasTypeId(Type type, bool isStatic, out long typeId)
            {
                return coreService.HasTypeId(type, isStatic, out typeId);
            }






            //todo: do we need to check serializables too?
            public bool IsTypeHandled_Editor(Type type, bool isStatic, out Type handlerType)
            {
                if (type.IsGenericType && !type.IsGenericTypeDefinition)
                {
                    Debug.LogError($"This api is not designed to work on close constructed generic types. It expects generic type definitions only. " +
                        $"It will use the gen type def anyway. " +
                        $"Type: {type.CleanAssemblyQualifiedName()}, isStatic:{isStatic}");
                }


                InitServiceIfNeeded();


                if (type.IsGenericType) type = type.GetGenericTypeDefinition();

                if (isStatic)
                {
                    if (type.IsArray)
                    {
                        //todo:
                        throw new NotSupportedException("in progeress");
                    }

                    if (__staticSaveHandlerAttributesByHandledType.TryGetValue(type, out var attr2))
                    {
                        handlerType = attr2.HandlerType;
                        return true;
                    }

                    handlerType = null;
                    return false;
                }


                if (type.IsArray)
                {
                    if (__arraySaveHandlerCreatorsByTypePerDimension.TryGetValue(type.GetArrayRank(), out var lookup))
                    {
                        handlerType = lookup.saveHandlerTypeDef;
                        return true;
                    }

                    handlerType = null;
                    return false;
                }


                if (__saveHandlerAttributesByHandledType.TryGetValue(type, out var attr))
                {
                    handlerType = attr.HandlerType;
                    return true;
                }

                if (__customSaveDataAttributesByType.TryGetValue(type, out var customSaveDataAttribute))
                {
                    handlerType = customSaveDataAttribute.SaveHandlerType;
                    return true;
                }

                handlerType = null;
                return false;
            }


            public bool IsTypeHandled_Editor(Type type, bool isStatic)
            {
                if (isStatic)
                {
                    return HasSaveHandlerForType_Editor(type, isStatic: true);
                }
                else
                {

                    if (HasSaveHandlerForType_Editor(type, isStatic: false)) return true;

                    if (HasCustomSaveData_Editor(type)) return true;

                    return HasSerializer_Editor(type);
                }
            }

            public bool IsTypeHandled_Editor(Type type)
            {
                return IsTypeHandled_Editor(type, isStatic: false) || IsTypeHandled_Editor(type, isStatic: true);
            }

            public bool IsTypeManuallyHandled_Editor(Type type, bool isStatic)
            {
                IsTypeManuallyHandled_Editor(type, out var hasManualInstanceHandler, out var hasManualStaticHandler);

                if (isStatic)
                {
                    return hasManualStaticHandler;
                }
                else
                {
                    return hasManualInstanceHandler;
                }
            }

            public void IsTypeManuallyHandled_Editor(Type type, out bool hasManualInstanceHandler, out bool hasManualStaticHandler)
            {
                if (type.IsArray)
                {
                    hasManualInstanceHandler = true;
                    hasManualStaticHandler = true;
                    return;
                }


                if (HasSaveHandlerForType_Editor(type, isStatic: true))
                {
                    var attr = GetSaveHandlerAttributeForType_Editor(type, isStatic: true);
                    hasManualStaticHandler = attr.GenerationMode is SaveHandlerGenerationMode.Manual;
                }
                else
                {
                    hasManualStaticHandler = false;
                }


                if (HasSaveHandlerForType_Editor(type, isStatic: false))
                {
                    var attr = GetSaveHandlerAttributeForType_Editor(type, isStatic: false);
                    hasManualInstanceHandler = attr.GenerationMode is SaveHandlerGenerationMode.Manual;
                    return;
                }


                if (HasCustomSaveData_Editor(type))
                {
                    var attr = GetCustomSaveDataAttribute_Editor(type);
                    hasManualInstanceHandler = attr.GenerationMode is SaveHandlerGenerationMode.Manual;
                    return;
                }


                if (HasSerializer_Editor(type))
                {
                    hasManualInstanceHandler = true;
                    return;
                }

                hasManualInstanceHandler = false;
            }





            public bool HasSaveHandlerForType(Type type, bool isStatic, out SaveHandlerAttribute attribute)
            {
                if (HasSaveHandlerForType_Editor(type, isStatic))
                {
                    var attr = GetSaveHandlerAttributeForType_Editor(type, isStatic);
                    attribute = attr;
                    return true;
                }
                attribute = null;
                return false;
            }



            public bool HasCustomSaveData(Type type, out CustomSaveDataAttribute attribute)
            {
                if (HasCustomSaveData_Editor(type))
                {
                    var attr = GetCustomSaveDataAttribute_Editor(type);
                    attribute = attr;
                    return true;
                }
                attribute = null;
                return false;
            }



            public SaveHandlerAttribute GetSaveHandlerAttributeForType_Editor(Type type, bool isStatic)
            {
                InitServiceIfNeeded();

                if (isStatic)
                {
                    if (__staticSaveHandlerAttributesByHandledType.TryGetValue(type, out var attr2))
                    {
                        return attr2;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (__saveHandlerAttributesByHandledType.TryGetValue(type, out var attr))
                {
                    return attr;
                }
                else
                {
                    return null;
                }
            }



            public bool HasSaveHandlerForType_Editor(Type type)
            {
                return HasSaveHandlerForType_Editor(type, isStatic: false) && HasSaveHandlerForType_Editor(type, isStatic: true);
            }

            public bool HasSaveHandlerForType_Editor(Type type, bool isStatic)
            {
                InitServiceIfNeeded();

                if (isStatic)
                {
                    if (type.IsArray) return true;
                    if (type.IsGenericType) type = type.GetGenericTypeDefinition();

                    if (__staticSaveHandlerAttributesByHandledType.ContainsKey(type))
                    {
                        return true;
                    }

                    return false;
                }

                if (type.IsGenericType)
                    return __genericSaveHandlerCreatorsByTypePerTypeDef.ContainsKey(type.GetGenericTypeDefinition());

                else if (type.IsArray)
                    return __arraySaveHandlerCreatorsByTypePerDimension.ContainsKey(type.GetArrayRank());

                return coreService.__saveHandlerAttributesByHandledType.ContainsKey(type);
            }




            public bool HasSerializer_Editor(Type type)
            {
                if (__serializeableTypes == null) CollectSerilaizeableTypes();

                if (type.IsGenericType) type = type.GetGenericTypeDefinition();

                return __serializeableTypes.Contains(type);
            }





            public SaveHandlerAttribute GetSaveHandlerAttributeOfType_Editor(Type type, bool isStatic)
            {
                InitServiceIfNeeded();

                if (isStatic)
                {
                    if (__staticSaveHandlerAttributesByHandledType.TryGetValue(type, out var attr2))
                    {
                        return attr2;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (__saveHandlerAttributesByHandledType.TryGetValue(type, out var attr))
                {
                    return attr;
                }
                else
                {
                    return null;
                }
            }










            public Type GetSaveHandlerTypeFrom(Type objectType, bool isStatic)
            {
                if (isStatic)
                {
                    if (_coreServiceInstance.__staticSaveHandlerAttributesByHandledType.TryGetValue(objectType, out var attr2))
                    {
                        return attr2.HandlerType;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    if (_coreServiceInstance.__saveHandlerAttributesByHandledType.TryGetValue(objectType, out var attr))
                    {
                        return attr.HandlerType;
                    }
                    //else if (_coreServiceInstance.__customSaveDataAttributesByHandledType.TryGetValue(objectType, out var customSaveDataAttribute))
                    //{
                    //    return customSaveDataAttribute.SaveHandlerType;
                    //}
                    else
                    {
                        return null;
                    }
                }
            }










            public Dictionary<Type, SaveHandlerAttribute> __staticSaveHandlerAttributesByHandledType => coreService.__staticSaveHandlerAttributesByHandledType;

            public Dictionary<Type, SaveHandlerAttribute> __saveHandlerAttributesByHandledType => coreService.__saveHandlerAttributesByHandledType;



            public CoreService _coreServiceInstance = new();
            public CoreService coreService {
                get
                {
                    InitServiceIfNeeded();
                    return _coreServiceInstance;
                }
            }







            public Dictionary<long, SaveHandlerAttribute> __saveHandlerAttributesById => coreService.__saveHandlerAttributesById;
            public Dictionary<long, SaveHandlerAttribute> __staticSaveHandlerAttributesById;
            public bool HadBuiltSaveHandlerIdByTypeLookups => __staticSaveHandlerAttributesById != null;

            public Type GetHandledTypeByHandlerId(long id)
            {
                return _coreServiceInstance.GetHandledTypeByHandlerId(id);
            }



            public long GetHandlerIdByHandledType(Type handledType, bool isStatic)
            {
                return GetHandlerIdByHandledType(handledType, isStatic, out var _);
            }

            public long GetHandlerIdByHandledType(Type handledType, bool isStatic, out bool found)
            {
                InitServiceIfNeeded();

                if (handledType == null)
                {
                    found = false;
                    return default;
                };


                if (isStatic)
                {
                    if (__staticSaveHandlerAttributesByHandledType.TryGetValue(handledType, out var attr2))
                    {
                        found = true;
                        return attr2.Id;
                    }
                    else
                    {
                        found = false;
                        return default;
                    }
                }

                if (__saveHandlerAttributesByHandledType.TryGetValue(handledType, out var attr))
                {
                    found = true;
                    return attr.Id;
                }
                else
                {
                    found = false;
                    return default;
                }
            }









            public Dictionary<Type, CustomSaveDataAttribute> __customSaveDataAttributesByType => coreService.__customSaveDataAttributesByHandledType;



            public bool HasCustomSaveData_Editor(Type type)
            {
                return HasCustomSaveData_Editor(type, out _);
            }

            public bool HasCustomSaveData_Editor(Type type, out Type customSaveDataType)
            {
                InitServiceIfNeeded();

                if (__customSaveDataAttributesByType.TryGetValue(type, out var attribute))
                {
                    customSaveDataType = attribute.SaveHandlerType;
                    return true;
                }
                else
                {
                    customSaveDataType = null;
                    return false;
                }
            }


            public CustomSaveDataAttribute GetCustomSaveDataAttribute_Editor(Type type)
            {
                InitServiceIfNeeded();

                if (__customSaveDataAttributesByType.TryGetValue(type, out var attr))
                {
                    return attr;
                }
                else
                {
                    return null;
                }
            }
        }

#endif










        public Dictionary<Type, Func<SaveHandlerBase>> __saveHandlerCreatorsByType => _coreService.__saveHandlerCreatorsByType;
        public Dictionary<long, Func<SaveHandlerBase>> __saveHandlerCreatorsById => _coreService.__saveHandlerCreatorsById;

        //the first Type is the HandledType's typedef, the second Type is the generic savehandler's typedef
        //the third one is the HandledType's concrete types
        public Dictionary<Type, (Type typeDef, Dictionary<Type, Func<SaveHandlerBase>> concreteTypes)> __genericSaveHandlerCreatorsByTypePerTypeDef => _coreService.__genericSaveHandlerCreatorsByTypePerTypeDef;
        public Dictionary<int, (Type saveHandlerTypeDef, Dictionary<Type, Func<SaveHandlerBase>> concreteTypes)> __arraySaveHandlerCreatorsByTypePerDimension => _coreService.__arraySaveHandlerCreatorsByTypePerDimension;

        public Dictionary<long, Dictionary<Type, Func<SaveHandlerBase>>> __genericSaveHandlerCreatorsByTypePerId => _coreService.__genericSaveHandlerCreatorsByTypePerId;







        public int GetCustomSaveDataAppVersionByHandledType(Type handledType)
        {
            return _coreService.GetCustomSaveDataAppVersionByHandledType(handledType);
        }

        public Type GetCustomSaveDataHandlerTypeByHandledType(Type handledType)
        {
            return _coreService.GetCustomSaveDataHandlerTypeByHandledType(handledType);
        }

        public Type GetHandledTypeByHandlerId(long id)
        {
            return _coreService.GetHandledTypeByHandlerId(id);
        }


        public bool HasSavedataTypeForVersionedId(long id, int version, out Type type)
        {
            if (_coreService.__savedataTypesByVersionByHandlerId.TryGetValue(id, out var versions))
                if (versions.TryGetValue(version, out type))
                {
                    return true;
                }

            if (_coreService.__customSaveDataHandledTypesByVersionById.TryGetValue(id, out versions))
                if (versions.TryGetValue(version, out type))
                {
                    return true;
                }

            type = null;
            return false;
        }


        public bool HasTypeId(Type type, bool isStatic, out long typeId)
        {
            return _coreService.HasTypeId(type, isStatic, out typeId);
        }


        public int GetCurrentVersionOfTypeById(long id)
        {
            return _coreService.GetCurrentVersionOfTypeById(id);
        }

        public int GetCurrentVersionOfTypeById(string id)
        {
            return GetCurrentVersionOfTypeById(long.Parse(id));
        }



        public bool HasSaveHandlerForType(Type type)
        {
            if (type.IsGenericType)
                return __genericSaveHandlerCreatorsByTypePerTypeDef.ContainsKey(type.GetGenericTypeDefinition());

            else if (type.IsArray)
                return __arraySaveHandlerCreatorsByTypePerDimension.ContainsKey(type.GetArrayRank());

            return __saveHandlerCreatorsByType.ContainsKey(type);
        }






        public bool _muteMissingSaveHandlerWarnings = false;


        public T GetSaveHandlerById<T>(RandomId id) where T : SaveHandlerBase
        {
            if (__saveHandlerByHandledObjectIdLookUp.TryGetValue(id, out var saveHandler))
            {
                var handler = saveHandler as T;

                if (handler == null)
                {
                    Debug.LogError($"SaveHandler with id {id} is not of type {typeof(T).CleanAssemblyQualifiedName()}. " +
                        $"This means that the requested type does not match the registered SaveHandler type.");
                    return null;
                }
                return handler;
            }
            else
            {
                Debug.LogError($"No SaveHandler found for id {id}. " +
                    $"This means that this id does not have a SaveHandler registered for it. ");
                return default;
            }
        }


        public ISaveAndLoad GetSaveHandlerById(ObjectMetaData metaData, VersionedType versionedType)
        {
            if (__saveHandlerByHandledObjectIdLookUp.TryGetValue(metaData.ObjectId, out var saveHandler))
                return saveHandler;


            if (versionedType.IsGeneric || versionedType.IsArray)
                return GetSaveHandlerById(metaData.SaveHandlerId, versionedType);

            return GetSaveHandlerById(metaData.SaveHandlerId);
        }


        public SaveHandlerBase GetSaveHandlerById(long id, VersionedType versionedType)
        {
            if (__genericSaveHandlerCreatorsByTypePerId.TryGetValue(id, out var dict))
            {
                Type handlerTypedef = _coreService.__saveHandlerAttributesById[id].HandlerType;

                Type handledType = versionedType.ResolveForCurrentHandledType();

                Type[] args;

                if (versionedType.IsArray)
                {
                    args = new Type[] { handledType.GetElementType() };
                }
                else
                {
                    args = handledType.GetGenericArguments();
                }


                Type handlerType;

                try
                {
                    handlerType = handlerTypedef.MakeGenericType(args);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to construct generic SaveHandler type for id {id} and handled type {handledType.CleanAssemblyQualifiedName()}. " +
                        $"This usually happens when the number of generic arguments do not match or the generic constraints do not satisfied. " +
                        $"Exception: {e}");
                    throw;
                }


                if (!dict.ContainsKey(handlerType))
                {
                    Func<SaveHandlerBase> ctor = CreateTypedCtor<SaveHandlerBase>(handlerType);

                    dict.Add(handlerType, ctor);
                }

                var factory = dict[handlerType];

                var newHandler = factory();
                return newHandler;
            }
            else
            {
                if (!_muteMissingSaveHandlerWarnings)
                    Debug.LogError($"No SaveHandler found for id {id}. " +
                        $"This means that this id does not have a SaveHandler registered for it. ");
                return null;
            }
        }


        public SaveHandlerBase GetSaveHandlerById(long id)
        {
            if (__saveHandlerCreatorsById.TryGetValue(id, out var factory))
            {
                return factory();
            }
            else
            {
                if (!_muteMissingSaveHandlerWarnings)
                    Debug.LogError($"No SaveHandler found for id {id}. " +
                        $"This means that this id does not have a SaveHandler registered for it. ");
                return null;
            }
        }


        public SaveHandlerBase GetSaveHandlerFor(object instance)
        {
            Type objectType = instance.GetType();



            if (objectType.IsGenericType)
            {
                Type objectTypeDef = objectType.GetGenericTypeDefinition();


                if (__genericSaveHandlerCreatorsByTypePerTypeDef.TryGetValue(objectTypeDef, out var tuple))
                {
                    if (!tuple.concreteTypes.ContainsKey(objectType))
                    {
                        Type handlerTypeDef = tuple.typeDef;

                        var objectTypeArgs = objectType.GetGenericArguments();

                        Type constructedGenericHandlerType = handlerTypeDef.MakeGenericType(objectTypeArgs);

                        var ctor = CreateTypedCtor<SaveHandlerBase>(constructedGenericHandlerType);

                        tuple.concreteTypes[objectType] = ctor;
                    }

                    var factory2 = tuple.concreteTypes[objectType];
                    var newHandler = factory2();

                    return newHandler;
                }
            }
            else if (objectType.IsArray)
            {
                Type elementType = objectType.GetElementType();
                int dimRank = objectType.GetArrayRank();


                if (__arraySaveHandlerCreatorsByTypePerDimension.TryGetValue(dimRank, out var tuple))
                {
                    if (!tuple.concreteTypes.ContainsKey(elementType))
                    {
                        Type handlerTypeDef = tuple.saveHandlerTypeDef;

                        var objectTypeArgs = new Type[] { elementType };

                        Type constructedGenericHandlerType = handlerTypeDef.MakeGenericType(objectTypeArgs);

                        var ctor = CreateTypedCtor<SaveHandlerBase>(constructedGenericHandlerType);

                        tuple.concreteTypes[objectType] = ctor;
                    }

                    var factory2 = tuple.concreteTypes[objectType];
                    var newHandler = factory2();

                    return newHandler;
                }
            }
            else if (__saveHandlerCreatorsByType.TryGetValue(objectType, out var factory))
            {
                return factory();
            }


            {
                if (!_muteMissingSaveHandlerWarnings)
                    Debug.LogError($"No SaveHandler found for type {objectType.CleanAssemblyQualifiedName()}. " +
                        $"This means that this type does not have a SaveHandler registered for it. ");
                return null;
            }
        }


        //todo: remove refernces and cleanup this
        public Type GetSaveHandlerTypeFrom(Type objectType)
        {
            //I actually dont know what workflow causes this method to be called
            //if every type and every method is registered that needs to be saved then I geuss this method will never be called

            if (__saveHandlerTypeByHandledObjectTypeLookUp.TryGetValue(objectType, out var handlerType))
            {
                return handlerType;
            }

            if (objectType.IsGenericType)
            {
                Type objectTypeDef = objectType.GetGenericTypeDefinition();


                if (__genericSaveHandlerCreatorsByTypePerTypeDef.TryGetValue(objectTypeDef, out var tuple))
                {
                    Type handlerTypeDef = tuple.typeDef;

                    var objectTypeArgs = objectType.GetGenericArguments();

                    Type constructedGenericHandlerType = handlerTypeDef.MakeGenericType(objectTypeArgs);

                    __saveHandlerTypeByHandledObjectTypeLookUp.Add(objectType, constructedGenericHandlerType);

                    return constructedGenericHandlerType;
                }
            }
            else if (objectType.IsArray)
            {
                Type elementType = objectType.GetElementType();
                int dimRank = objectType.GetArrayRank();


                if (__arraySaveHandlerCreatorsByTypePerDimension.TryGetValue(dimRank, out var tuple))
                {
                    Type handlerTypeDef = tuple.saveHandlerTypeDef;

                    var objectTypeArgs = new Type[] { elementType };

                    Type constructedGenericHandlerType = handlerTypeDef.MakeGenericType(objectTypeArgs);

                    __saveHandlerTypeByHandledObjectTypeLookUp.Add(objectType, constructedGenericHandlerType);

                    return constructedGenericHandlerType;
                }
            }

            {
                if (!_muteMissingSaveHandlerWarnings)
                    Debug.LogError($"No SaveHandler type found for type {objectType.FullName}. " +
                        $"This means that this type does not have a SaveHandler registered for it. ");
                return null;
            }
        }




        #region GameLoopIntegrators


        public HashSet<IGameLoopIntegrator> __integrators = new();

        public void RegisterIntegrator(IGameLoopIntegrator integrator)
        {
            if (integrator == null)
            {
                Debug.LogError("IGameLoopIntegrator is null. Return");
                return;
            }
            if (__integrators.Contains(integrator))
            {
                Debug.LogError("IGameLoopIntegrator is already added. Return.");
                return;
            }

            __integrators.Add(integrator);
        }

        #endregion




        public Dictionary<Type, Func<CustomSaveData>> __customSaveDataFactories = new();
        public Dictionary<Type, Func<CustomSaveData>> __pastVersionCustomSaveDataFactories = new();




        public void CollectCustomSaveDatas()
        {
            _coreService.CollectCustomSaveDatas();

            BuildCustomSaveDataLookUps();
        }

        public void BuildCustomSaveDataLookUps()
        {
            foreach (var attr in _coreService.__customSaveDataAttributesByHandledType.Values)
            {
                var ctor = CreateTypedCtor<CustomSaveData>(attr.SaveHandlerType);
                __customSaveDataFactories.Add(attr.HandledType, ctor);
            }
        }


        public CustomSaveData<T> CreateCustomSaveDataInstanceFor<T>()
        {
            return CreateCustomSaveDataInstanceFor<T>(typeof(T));
        }

        public CustomSaveData<T> CreateCustomSaveDataInstanceFor<T>(Type type)
        {
            var csd = CreateCustomSaveDataInstanceFor(type);

            if (csd is CustomSaveData<T> typedCsd)
            {
                return typedCsd;
            }
            else
            {
                Debug.LogError($"The registered customsavedata for type {type.CleanAssemblyQualifiedName()} is not assigable to CustomSaveData<{typeof(T).CleanAssemblyQualifiedName()}>. " +
                    $"This means that the registered CustomSaveData type does not match the requested generic type.\n" +
                    $"Going to return null.");
                return null;
            }
        }

        public CustomSaveData CreateCustomSaveDataInstanceFor(Type type)
        {
            var typeToHandle = type;

            if (typeToHandle.IsClass)
            {
                if (!__pastVersionCustomSaveDataFactories.ContainsKey(typeToHandle))
                {
                    if (_coreService.__versionedCustomSaveDataAttributesByHandledType.TryGetValue(typeToHandle, out var info))
                    {
                        var handlerType = info.SaveHandlerType;
                        var ctor = CreateTypedCtor<CustomSaveData>(handlerType);
                        __pastVersionCustomSaveDataFactories.Add(typeToHandle, ctor);
                    }
                }

                var factory2 = __pastVersionCustomSaveDataFactories[typeToHandle];
                return factory2();
            }



            if (__customSaveDataFactories.TryGetValue(typeToHandle, out var factory))
            {
                return factory();
            }
            else
            {
                Debug.LogError($"No CustomSaveData found for type {typeToHandle.CleanAssemblyQualifiedName()}. " +
                    $"This means that this type does not have a CustomSaveData registered for it.");
                return null;
            }
        }


        public bool HasCustomSaveData(Type type)
        {
            var typeToHandle = type;


            if (_coreService.__customSaveDataAttributesByHandledType.ContainsKey(typeToHandle))
            {
                return true;
            }
            else if (_coreService.__versionedCustomSaveDataAttributesByHandledType.ContainsKey(typeToHandle))
            {
                return true;
            }

            return false;
        }








        public bool IsTypeInheritsFromGenericBaseType(Type type, Type genericTypeDefinition)
        {
            if (type == null || genericTypeDefinition == null)
                return false;

            if (!genericTypeDefinition.IsGenericTypeDefinition)
                throw new ArgumentException($"{genericTypeDefinition} must be a generic type definition");

            // Check base types
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDefinition)
                    return true;

                type = type.BaseType;
            }

            return false;
        }









        //todo: use ids as keys instead.
        //public Dictionary<Object, bool> __isUnityObjectLoading;
        public Dictionary<RandomId, bool> __isObjectLoading = new();

        public bool ExpectingIsObjectLoadingRequest { get; set; }


        public static bool IsObjectLoading(Component component)
        {
            return Singleton == null ? false : Singleton._IsObjectLoading(component);
        }

        public bool _IsObjectLoading(Component component)
        {
            if (component == null) return false;

            return _IsObjectLoading(component.gameObject);
        }

        public static bool IsObjectLoading(Object go)
        {
            return Singleton == null ? false : Singleton._IsObjectLoading(go);
        }

        public bool _IsObjectLoading(Object go)
        {
            if (go == null)
            {
                Debug.LogError("Unity object is null.");
                return false;
            }

            if (ExpectingIsObjectLoadingRequest) return true;


            var id = Infra.Singleton._GetObjectIdWithoutReferencing(go, autoRegister: false);

            if (id.IsDefault) return false;


            if (__isObjectLoading.ContainsKey(id))
            {
                return __isObjectLoading[id];
            }
            else
            {
                //Debug.LogError(go.name + " - GameObject not found in __isGOLoading dictionary. Going to assume its no loading from disk");
                return false;
            }
        }

        public void SetObjectLoading(RandomId id, bool isLoading)
        {
            if (id.IsDefault)
            {
                Debug.LogError("Parameter object id is default.");
                return;
            }


            __isObjectLoading[id] = isLoading;
        }



        public static bool IsObjectLoading(object obj)
        {
            return Singleton == null ? false : Singleton._IsObjectLoading(obj);
        }


        public bool _IsObjectLoading(object obj)
        {
            if (obj == null)
            {
                Debug.LogError("GameObject is null.");
                return false;
            }

            if (ExpectingIsObjectLoadingRequest) return true;


            var id = Infra.Singleton._GetObjectIdWithoutReferencing(obj, autoRegister: false);


            if (id.IsDefault) return false;


            if (__isObjectLoading.ContainsKey(id))
            {
                return __isObjectLoading[id];
            }
            else
            {
                //Debug.LogError(go.name + " - GameObject not found in __isGOLoading dictionary. Going to assume its no loading from disk");
                return false;
            }
        }

        //public void SetObjectLoading(object obj, bool isLoading)
        //{
        //    if (obj == null)
        //    {
        //        Debug.LogError("GameObject is null.");
        //        return;
        //    }

        //    var id = Infra.Singleton._GetObjectIdWithoutReferencing(obj);

        //    __isObjectLoading[id] = isLoading;
        //}







        public bool _NewSaveHandlersWereAdded()
        {
            return __currentSaveHandlers.Count != 0;
        }


        public void _MergeTempCollectionToMainCollection()
        {
            if (__iteratedSaveHandlers == __mainSaveHandlers)
            {
                Debug.LogError($"{nameof(_MergeTempCollectionToMainCollection)} should only be called when the current collection " +
                    $"is not the main collection. Merging the main collection to it self woudl result in duplicates." +
                    $"Cancel merging and do nothing.");
                return;
            }


            //foreach(var handler in __iteratedSaveHandlers)
            //{
            //    Debug.LogWarning((handler.HandledType, "  ",handler.HandledObjectId));
            //}
            __mainSaveHandlers.AddRange(__iteratedSaveHandlers);
            __iteratedSaveHandlers.Clear();
        }



        [HideInInspector]
        public bool __debugSaveStateMachine = false;

        public void _MoveToNextState()
        {
            SaveState nextState;

            switch (__currentSaveState)
            {
                case SaveState.Start:
                    nextState = SaveState.Main;
                    break;

                case SaveState.Main:
                    if (_NewSaveHandlersWereAdded())
                        nextState = SaveState.TempA;
                    else
                        nextState = SaveState.End;
                    if (__debugSaveStateMachine)
                    {
                        Debug.Log("From main to " + nextState);
                    }

                    break;

                case SaveState.TempA:
                    if (_NewSaveHandlersWereAdded())
                    {
                        nextState = SaveState.TempB;
                    }
                    else
                        nextState = SaveState.End;

                    if (__debugSaveStateMachine)
                    {
                        Debug.Log("From A to " + nextState);
                    }

                    _MergeTempCollectionToMainCollection();
                    break;

                case SaveState.TempB:
                    if (_NewSaveHandlersWereAdded())
                    {
                        nextState = SaveState.TempA;
                    }
                    else
                        nextState = SaveState.End;

                    if (__debugSaveStateMachine)
                    {
                        Debug.Log("From B to " + nextState);
                    }

                    _MergeTempCollectionToMainCollection();
                    break;

                case SaveState.End:
                    Debug.LogError($"{nameof(SaveAndLoadManager)}: The current state of the save statemachine is already in End state, " +
                        $"since the machine can not go to any other state from the End state, its an error to call a NextState on it.");
                    nextState = SaveState.Terminate;
                    return;

                default:
                    Debug.LogError($"{nameof(SaveAndLoadManager)}: Save statemachine encountered a state that it is not prepared for. " +
                        $"Please handle the missing cases(s). Missing case: {__currentSaveState}");
                    nextState = SaveState.Terminate;
                    return;
            }


            switch (nextState)
            {
                case SaveState.Start:
                    Debug.LogError($"{nameof(SaveAndLoadManager)}: Start state can not be the next state for the save statemachine.");
                    nextState = SaveState.Terminate;
                    break;

                case SaveState.Main:
                    __iteratedSaveHandlers = __mainSaveHandlers;
                    __currentSaveHandlers = __tempA_saveHandlers;
                    break;

                case SaveState.TempA:
                    __iteratedSaveHandlers = __tempA_saveHandlers;
                    __currentSaveHandlers = __tempB_saveHandlers;
                    break;

                case SaveState.TempB:
                    __iteratedSaveHandlers = __tempB_saveHandlers;
                    __currentSaveHandlers = __tempA_saveHandlers;
                    break;

                case SaveState.End:
                    __iteratedSaveHandlers = null;
                    __currentSaveHandlers = __mainSaveHandlers;
                    break;

                default:
                    Debug.LogError($"{nameof(SaveAndLoadManager)}: The next state of the Save statemachine is not defined. " +
                        $"Please handle the missing state. Missing state: {nextState}. " +
                        $"Setting next state to {SaveState.Terminate}");
                    nextState = SaveState.Terminate;
                    break;
            }

            __currentSaveState = nextState;
        }


        public bool _ShouldStopIteratingSaveHandlers()
        {
            return __currentSaveState switch
            {
                SaveState.End or SaveState.Terminate => true,
                _ => false,
            };
        }


        public void _ResetSaveStateMachine()
        {
            __currentSaveState = SaveState.Start;
        }





        public bool _saveingIsInProgress;


        public void Save()
        {
            if (_saveingIsInProgress)
            {
                Debug.LogError("A save process is already in progress. Returning.");
                return;
            }


            StartCoroutine(SaveRoutine());

        }


        internal IEnumerator SaveRoutine()
        {
            _saveingIsInProgress = true;



            IsIteratingSaveHandlers = true;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            Infra.Singleton.StartNewReferenceGraph();

            var handlersToRemove = new List<ISaveAndLoad>();


            _ResetSaveStateMachine();
            _MoveToNextState();

            do
            {
                foreach (var handler in __iteratedSaveHandlers)
                {
                    if (handler == null)
                    {
                        Debug.LogError("Save handler is null, it means it wasnt removed from save manager when the object was destroyed. " +
                            "Skipping.");
                        continue;
                    }

                    if (!handler.IsValid)
                    {
                        handlersToRemove.Add(handler);
                        continue;
                    }

                    handler.WriteSaveData();
                }

                _MoveToNextState();
            }
            while (!_ShouldStopIteratingSaveHandlers());

            //Debug.LogWarning("write " + stopwatch.ElapsedMilliseconds / 1000f);

            IsIteratingSaveHandlers = false;


            if (__currentSaveState == SaveState.Terminate)
            {
                Debug.LogError($"The saving of the objects was terminated. Going to skip the rest of process.");
                yield break;
            }


            foreach (var handler in handlersToRemove)
            {
                //Debug.Log($"[Trace] Removing invalid save handler, datagroup: {handler.DataGroupId}, handled object id: {handler.HandledObjectId}");
                //__mainSaveHandlers.Remove(handler);
                Infra.Singleton.Unregister(handler.HandledObjectId);

                //if (handler is GameObjectSaveHandler goHandler)
                {
                    //Debug.LogWarning(goHandler.__saveData.HierarchyPath);
                }
            }

            Infra.Singleton.RemoveUnreferencedObjects();



            //todo: create an immutable snapshot of the data accesd by background tasks

            var snapshot = new List<ISaveAndLoad>(__mainSaveHandlers);


            RandomId id = Infra.Singleton.GetObjectId(this, Infra.GlobalReferencing);

            for (int i = 0; i < snapshot.Count; i++)
            {
                var handler = snapshot[i];

                if (handler.HandledObjectId == id)
                {
                    var first = snapshot[0];
                    snapshot[0] = handler;
                    snapshot[i] = first;
                    break;
                }
            }


            //perf test
            //for (int i = 0; i < 100; i++)
            //{
            //    snapshot.AddRange(__mainSaveHandlers);
            //}

            var serTask = Task.Run(() => { return SerializeSnapshot(snapshot); });

            while (!serTask.IsCompleted)
                yield return null;

            if (serTask.Exception != null)
            {
                Debug.LogError("Exception during serialization of save data: " + serTask.Exception);
                yield break;
            }

            IEnumerable<string> flatList = serTask.Result;

            //Debug.LogWarning("serialize " + stopwatch.ElapsedMilliseconds / 1000f);


            var absPath = CreateAbsPathForSaveFile();

            Debug.Log($"Saving data to {absPath}.");

            var writeTask = Task.Run(() => { WriteSnapshotToDisk(absPath, flatList); });


            while (!writeTask.IsCompleted)
                yield return null;


            if (writeTask.Exception != null)
            {
                Debug.LogError("Exception during writing to disk of save data: " + writeTask.Exception);
                yield break;
            }


            stopwatch.Stop();
            //Debug.LogWarning("save to disk " + stopwatch.ElapsedMilliseconds / 1000f);


            _saveingIsInProgress = false;

            Debug.Log("Save completed.");
        }



        public void WriteSnapshotToDisk(string path, IEnumerable<string> snapshot)
        {
            JsonUtil.WriteObjects(path, snapshot, relative: false);
        }


        public IEnumerable<string> SerializeSnapshot(IEnumerable<ISaveAndLoad> handlers)
        {
            var saveData = new Dictionary<long, List<string>>();


            foreach (var handler in handlers)
            {
                {
                    if (handler == null)
                    {
                        Debug.LogError("Save handler is null, it means it wasnt removed from save manager when the object was destroyed. " +
                            "Skipping.");
                        continue;
                    }

                    var data = handler.Serialize();


                    if (!saveData.ContainsKey(handler.SaveHandlerId))
                    {
                        var list = new List<string>();
                        saveData[handler.SaveHandlerId] = list;
                    }

                    saveData[handler.SaveHandlerId].Add(data);
                }
            }


            IEnumerable<string> flatList = saveData.SelectMany(x => x.Value);

            return flatList;
        }




        public string CreateAbsPathForSaveFile()
        {
            string fileTag = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            string fileName = "/savedata_" + fileTag + ".json";

            string relPath = Paths.Singleton.WorldSavePath + fileName;

            var basePath = Application.persistentDataPath; //this is not thread-safe, thats why it is here
            var absPath = Path.Combine(basePath, relPath);

            return absPath;
        }


        public string GetFileTagFromPath(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);

            int expectedTagLength = "yyyy-MM-dd_HH-mm-ss".Length;

            int tagStart = name.Length - expectedTagLength;

            string tag = name.Substring(tagStart);

            return tag;
        }






















        public Coroutine __loadCoroutine;



        public enum LoadingStage
        {
            LoadingObjects,
            LoadingScenes,
            Completed,
        }

        public class LoadingDebugContext
        {
            public LoadingStage CurrentStage;
            public ISaveAndLoad handler;
        }

        public LoadingDebugContext d_laodingContext = new();






        public void Load(string saveFileAbsPath)
        {
            __loadCoroutine = StartCoroutine(LoadRoutine(saveFileAbsPath));
        }


        public IEnumerator LoadRoutine(string saveFileAbsPath)
        {
            if (string.IsNullOrEmpty(saveFileAbsPath))
            {
                Debug.LogError("Save file path is null or empty.");
                yield break;
            }

            if (!System.IO.File.Exists(saveFileAbsPath))
            {
                Debug.LogError($"Save file does not exist at path: {saveFileAbsPath}");
                yield break;
            }




            Debug.Log($"Loading save file from {saveFileAbsPath}.");


            RandomId thisId = Infra.Singleton.GetObjectId(this, Infra.GlobalReferencing);

            ISaveAndLoad thisHandler = GetSaveHandlerById<SaveHandlerBase>(thisId);


            List<string> serializedDataList = JsonUtil.ReadObjects(saveFileAbsPath, relative: false);


            var first = JsonConvert.DeserializeObject<SaveDataBase>(serializedDataList[0]);

            if (first._MetaData_.SaveHandlerId != thisHandler.SaveHandlerId)
            {
                Debug.LogError("The first saved data is not the SaveAndLoadManager's own data, " +
                    "which contains necessary information of how to load this file. Cancel loading.");
                yield break;
            }

            SaveAndLoadManagerSaveData saveAndLoadManagerData = JsonConvert.DeserializeObject<SaveAndLoadManagerSaveData>(serializedDataList[0]);

            serializedDataList.RemoveAt(0);


            int pastAppVersion = saveAndLoadManagerData._appVersion;
            int currentAppVersion = _appVersion;

            Dictionary<RandomId, VersionedType> versionedTypesByTypeInstanceId = new();

            foreach (var versionedType in saveAndLoadManagerData.versionedTypeCache.RegisteredTypes)
            {
                versionedTypesByTypeInstanceId.Add(versionedType.instanceId, versionedType);
            }





            LoadContext loadContext = new LoadContext(saveFileAbsPath, versionedTypesByTypeInstanceId);



            if (pastAppVersion != currentAppVersion)
            {
                ///<see cref="LoadContext.IsMigrating"/> relies on this to be set and not to be null
                var migrationContext = new MigrationContext();
                loadContext.MigrationContext = migrationContext;
            }


            List<ObjectMetaData> metadataList = new();
            List<SaveDataBase> savedataList = new();
            Dictionary<RandomId, string> serDataById = new();

            RandomId d_id = default;

            try
            {

                for (int i = 0; i < serializedDataList.Count; i++)
                {
                    var serData = serializedDataList[i];
                    var saveInfo = JsonConvert.DeserializeObject<SavedObject>(serData);

                    if (saveInfo is null or { _MetaData_: null })
                    {
                        Debug.LogError($"SaveObject or its MetaData is null at {i}th object. Skipping this object.\nSerData: {serData}");
                        continue;
                    }

                    d_id = saveInfo._MetaData_.ObjectId;



                    Type savedataType;
                    VersionedType d_versionedType = null;

                    try
                    {
                        d_versionedType = versionedTypesByTypeInstanceId[saveInfo._MetaData_.HandledType];
                        savedataType = d_versionedType.ResolveForVersionedHandledType();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Can not load save file because an error occured during resolving the past versions of types. " +
                            $"\nError occured at resolving type: {d_versionedType.instanceId}.\nError: {e}");
                        throw;
                    }



                    if (savedataType.HasElementType)
                    {
                        var elementType = savedataType.GetElementType();

                        //todo: if pointer type, if byref? is it possible?
                        if (savedataType.IsSZArray || savedataType.IsArray)
                        {
                            //todo:refine this
                            Type arraySavedata = savedataType.GetArrayRank() switch
                            {
                                1 => _coreService.__saveDataTypesBySaveHandlerId[897213743298234],
                                _ => throw new Exception($"unsupported array dimension: {savedataType.GetArrayRank()}"),
                            };

                            Type genericArrayHandler = arraySavedata.MakeGenericType(elementType);

                            savedataType = genericArrayHandler;
                        }
                    }

                    //Debug.Log(("Deser type: ", savedataType.CleanAssemblyQualifiedName()));

                    var savedata = JsonConvert.DeserializeObject(serData, savedataType) as SaveDataBase;

                    if (savedata is null)
                    {
                        Debug.LogError($"Error during deser of savedata. Couldn't deser data {d_id} into type: {savedataType.CleanAssemblyQualifiedName()}");
                        continue;
                    }

                    savedataList.Add(savedata);

                    metadataList.Add(saveInfo._MetaData_);
                    serDataById.Add(saveInfo._MetaData_.ObjectId, serData);
                }

            }
            catch (Exception e)
            {
                Debug.LogError($"Error during deser of savedata: {d_id}.\n Exception: {e}");
                throw;
            }




            HashSet<RandomId> objectIdsForContext = new();

            foreach (var data in savedataList)
            {
                _loadContextByObjectId.Add(data._MetaData_.ObjectId, loadContext);
                objectIdsForContext.Add(data._MetaData_.ObjectId);
            }

            _objectIdsByLoadContext.Add(loadContext, objectIdsForContext);




            if (pastAppVersion != currentAppVersion)
            {
                var migrationContext = loadContext.MigrationContext;

                migrationContext.Init(pastAppVersion, savedataList);

                loadContext.MigrationContext = migrationContext;

                int migratedVersion = pastAppVersion;

                while (migratedVersion != currentAppVersion)
                {
                    migrationContext.Migrate(migratedVersion);

                    migratedVersion++;
                }

                savedataList = migrationContext._saveDatasByObjectId.Values.ToList();


                //todo: optimize this
                serializedDataList.Clear();

                foreach (var data in savedataList)
                {
                    var json = JsonConvert.SerializeObject(data);
                    serializedDataList.Add(json);
                }


                //string d_text = string.Join("\n", serializedDataList);
                //File.WriteAllText("c:/temp/migrated_list.json", d_text);


                //todo: enable it via config + path
                string now = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

                string tag = GetFileTagFromPath(saveFileAbsPath);

                string debugJson = JsonConvert.SerializeObject(migrationContext);
                File.WriteAllText($"MigrationContext_{tag}_{now}.json", debugJson);

                loadContext.MigrationContext = null;

                //void DisposeMigrationScope()
                //{
                //    foreach (var id in migrationContext._saveDatasByObjectId.Keys)
                //    {
                //        if (_objectIdsByMigrationContext.ContainsKey(id))
                //            _objectIdsByMigrationContext.Remove(id);
                //        else
                //        {
                //            Debug.LogError($"Error during disposing migration scope after migration has finisihed. " +
                //                $"A migration context knows about an objectId from which the migration context's containing loading context does not.\n" +
                //                $"A loading context must know that a given objectId which migration context it belongs to. (If there is any migration)\n" +
                //                $"This most likely means an object has been added or removed to this migration context but it does not added or removed " +
                //                $"to its corresponding loading context");
                //        }
                //    }
                //}

                //DisposeMigrationScope();
            }



            List<ISaveAndLoad> saveHandlers = new List<ISaveAndLoad>();


            int j = 0;

            //SaveDataBase d_savedata = null;
            SavedObject d_savedata = null;

            try
            {

                foreach (string savedata in serializedDataList)
                {
                    var saveInfo = JsonConvert.DeserializeObject<SavedObject>(savedata);
                    d_savedata = saveInfo;

                    if (saveInfo == null || saveInfo._MetaData_ == null)
                    {
                        Debug.LogError($"SaveObject or its MetaData is null at {j}th object. Skipping this object.\nSerData: {savedata}");
                        continue;
                    }

                    var versionedType = versionedTypesByTypeInstanceId[saveInfo._MetaData_.HandledType];
                    var handler = GetSaveHandlerById(saveInfo._MetaData_, versionedType);

                    if (handler == null)
                    {
                        Debug.LogError($"No SaveHandler found for id {saveInfo._MetaData_.SaveHandlerId}. Skipping this object.");
                        continue;
                    }

                    //handler.Deserialize(savedata);
                    handler.Deserialize(savedata);
                    saveHandlers.Add(handler);

                    SetObjectLoading(saveInfo._MetaData_.ObjectId, true);

                    j++;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during loading from save file at object {d_savedata._MetaData_.ObjectId}: {e.Message}.");
                throw;
            }




            d_laodingContext.CurrentStage = LoadingStage.LoadingObjects;


            try
            {
                var ordered = saveHandlers.GroupBy(handler => handler.MetaData.Order).OrderBy(group => group.Key);

                //these are different stages that build upon each other, iterating over them multiple times is by design, dont put them in one loop
                foreach (var group in ordered)
                {
                    d_loadedOrder = group.Key;

                    foreach (var handler in group)
                    {
                        d_laodingContext.handler = handler;
                        //Debug.Log(handler.MetaData.ObjectId);

                        handler.CreateObject();

                        //if (d_suspendLoading && !d_found)
                        //{
                        //var test = Component.FindAnyObjectByType<Canvas>();
                        //if (test != null)
                        //{
                        //    Debug.Log(handler.HandledObjectId);
                        //    yield return new WaitWhile(() => !d_continue);
                        //    d_continue = false;
                        //        d_found = true;
                        //}
                        //}

                        //if (handler.HandledObjectId.ToString() == "956694569789028744")
                        //{

                        //    yield return new WaitWhile(() =>
                        //    {
                        //        return !d_continue;
                        //    });

                        //    d_continue = false;
                        //}

                        AddSaveHandler(handler);//to add a savehandler it needs to have a HandledObjectId assigned, which happens in CreateObject
                    }

                    foreach (var handler in group)
                    {
                        d_laodingContext.handler = handler;
                        handler.LoadPhase1();
                    }

                    foreach (var handler in group)
                    {
                        d_laodingContext.handler = handler;
                        handler.LoadPhase2();
                    }



                    foreach (var handler in group)
                    {
                        if (handler.HandledType == typeof(SceneManagement))
                        {
                            yield return LoadAllSavedScenes();
                        }
                    }


                    if (d_suspendLoading)
                    {

                        yield return new WaitWhile(() =>
                        {
                            return !d_continue;
                        });

                        d_continue = false;
                    }
                }

                d_laodingContext.CurrentStage = LoadingStage.Completed;
            }
            //you cant have catch if you use yield in try
            finally
            {
                if (d_laodingContext.CurrentStage != LoadingStage.Completed)
                {
                    Debug.LogError($"Error occurred while loading object with id {d_laodingContext.handler.HandledObjectId}. " +
                                   $"Handled type: {d_laodingContext.handler.HandledType.CleanAssemblyQualifiedName()}. " +
                                   $"At loading stage: {d_laodingContext.CurrentStage}");
                }
            }




            if (d_suspendLoading)
            {

                yield return new WaitWhile(() =>
                {
                    return !d_continue;
                });

                d_continue = false;
            }



            foreach (var integrator in __integrators)
            {
                integrator.StartIntegration();
            }

            //let collisions and everything else we dont have control over trigger
            //our components should filter these if they are loading
            //todo:
            yield return null;
            yield return null;
            yield return null;


            Scene activeScene = Infra.SceneManagement.ActiveSceneInstanceIdFromSaveFile;
            SceneManager.SetActiveScene(activeScene);


            __isObjectLoading.Clear();
            __integrators.Clear();

            //todo
            Infra.SceneManagement._savedSceneInfos.Clear();
            Infra.SceneManagement._activeSceneInstanceIdFromSaveFile = RandomId.Default;

            ///todo: clear <see cref="PrefabDescriptionRegistry"/> and <see cref="ScenePlacedObjectRegistry"/>



            void DisposeScope(LoadContext context)
            {
                if (_objectIdsByLoadContext.TryGetValue(context, out var objectsOfThisScope))
                {
                    foreach (var objId in objectsOfThisScope)
                    {
                        _loadContextByObjectId.Remove(objId);

                        if (AssetIdMap.ObjectIdToAssetInstance.ContainsKey(objId))
                            AssetIdMap.ObjectIdToAssetInstance.Remove(objId);
                    }

                    _objectIdsByLoadContext.Remove(context);
                }


                if (_versionedTypesByLoadContext.TryGetValue(context, out var versionedTypesOfThisScope))
                {
                    foreach (var typeId in versionedTypesOfThisScope)
                    {
                        _versionedTypesByInstanceId.Remove(typeId);
                        _loadContextByVersionedTypeIds.Remove(typeId);
                    }

                    _versionedTypesByLoadContext.Remove(context);
                }
            }

            LoadingLoadContextCompleted(loadContext);

            DisposeScope(loadContext);


            Debug.Log("loading completed");
        }


        [Header("Debug")]
        public bool d_continue;
        public bool d_suspendLoading;
        public int d_loadedOrder;






        public event Action<LoadContext> LoadingLoadContextCompleted;







        public IEnumerator LoadAllSavedScenes()
        {
            SaveAndLoadManager.Singleton.ExpectingIsObjectLoadingRequest = true;

            yield return Infra.SceneManagement.EnsureScenesAreLoadedFromSaveFile();

            SaveAndLoadManager.Singleton.ExpectingIsObjectLoadingRequest = false;
        }





        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Debug.Log("Saving game...");
                Save();
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                //Debug.Log("Loading game...");
                //Load();
            }
        }


        public void AddSaveHandler(ISaveAndLoad saveHandler)
        {
            if (!saveHandler.IsInitialized)
            {
                Debug.LogError($"SaveHandler is uninitialized. This api can not be used with an uninitialized savehandler. " +
                    $"HandlerTypeName: {saveHandler.GetType().CleanAssemblyQualifiedName()} handler id: {saveHandler.SaveHandlerId}");
                return;
            }


            //already added before
            if (__saveHandlerByHandledObjectIdLookUp.ContainsKey(saveHandler.HandledObjectId))
                return;



            __currentSaveHandlers.Add(saveHandler);
            __saveHandlerByHandledObjectIdLookUp.Add(saveHandler.HandledObjectId, saveHandler);
        }


        public void RemoveSaveHandler(RandomId objectId)
        {
            if (!__saveHandlerByHandledObjectIdLookUp.ContainsKey(objectId))
            {
                var obj = Infra.Singleton.GetReference(objectId);

                Debug.LogError($"No save handlers found for object with id: {objectId}. " +
                    $"This means that the handler was never added or already have been removed. Object type: {obj.GetType().CleanAssemblyQualifiedName()}");
                return;
            }

            var handler = __saveHandlerByHandledObjectIdLookUp[objectId];
            RemoveSaveHandler(handler);
        }

        public void RemoveSaveHandler(ISaveAndLoad saveHandler)
        {
            if (IsIteratingSaveHandlers)
            {
                Debug.LogError($"{nameof(SaveAndLoadManager)}: Removing a save handler while saving is not supported currently. " +
                    $"Going to cancel removing.");
                return;
            }


            if (!saveHandler.IsInitialized)
            {
                Debug.LogError($"SaveHandler is uninitialized. This api can not be used with an uninitialized savehandler.");
                return;
            }


            __mainSaveHandlers.Remove(saveHandler);
            __saveHandlerByHandledObjectIdLookUp.Remove(saveHandler.HandledObjectId);

            //todo: remove the handled object's delegates from the delegate map



            saveHandler.ReleaseObject();
            //else
            //{
            //    Debug.LogError($"No save handlers found for DataGroupId: {dataGroupId}." +
            //        $"This means that the handler was never added or already have been removed and the object of this handler wants to desubscribe " +
            //        $"a second time. Since this unsubscribing happens mostly on Destroy(), " +
            //        $"it might means the object needs to check if it has already been destroyed.");
            //}
        }








        public bool IsPartOfPrefabOrScenePlaced<T>(RandomId instanceId, out T instance)
        {
            bool isPartOfPrefab = PrefabDescriptionRegistry.IsPartOfPrefab<T>(instanceId, out instance);

            if (isPartOfPrefab)
            {
                return true;
            }

            bool isPartOfScenePlaced = ScenePlacedObjectRegistry.IsScenePlaced<T>(instanceId, out instance);

            if (isPartOfScenePlaced)
            {
                return true;
            }

            instance = default;
            return false;

        }






        public Dictionary<RandomId, LoadContext> _loadContextByObjectId = new();
        public Dictionary<LoadContext, HashSet<RandomId>> _objectIdsByLoadContext = new();

        public Dictionary<RandomId, VersionedType> _versionedTypesByInstanceId = new();
        public Dictionary<RandomId, LoadContext> _loadContextByVersionedTypeIds = new();
        public Dictionary<LoadContext, HashSet<RandomId>> _versionedTypesByLoadContext = new();


        public class LoadContext
        {
            public string _saveFilePath;
            public string _fileTag;

            public MigrationContext MigrationContext { get; set; }

            public LoadContext(string saveFilePath, Dictionary<RandomId, VersionedType> versionedTypesByInstanceId)
            {
                _saveFilePath = saveFilePath;
                _fileTag = SaveAndLoadManager.S.GetFileTagFromPath(saveFilePath);

                foreach ((var typeInstanceId, var versionedType) in versionedTypesByInstanceId)
                {
                    SaveAndLoadManager.Singleton._versionedTypesByInstanceId.Add(typeInstanceId, versionedType);
                    SaveAndLoadManager.Singleton._loadContextByVersionedTypeIds.Add(typeInstanceId, this);
                }

                SaveAndLoadManager.Singleton._versionedTypesByLoadContext.Add(this, versionedTypesByInstanceId.Keys.ToHashSet());
            }

            public bool IsMigrating() => MigrationContext != null;
        }



        public LoadContext GetLoadContextOf(RandomId instance)
        {
            if (_loadContextByObjectId.TryGetValue(instance, out var context)) return context;
            else
            {
                Debug.LogError($"SaveAndLoad: It is not expected that semething asks for the load context of an object that is not actually loading.");
                return null;
            }
        }



        public bool IsVersionedTypeLoading(RandomId versionedTypeId, out LoadContext loadContext)
        {
            if (_loadContextByVersionedTypeIds.TryGetValue(versionedTypeId, out loadContext))
                return true;
            loadContext = null;
            return false;
        }


        public bool IsObjectMigrating(RandomId id, out MigrationContext context)
        {
            if (_loadContextByObjectId.TryGetValue(id, out var loadContext))
            {
                context = loadContext.MigrationContext;

                if (context != null)
                    return true;
                else return false;
            }
            context = null;
            return false;
        }

        public VersionedType GetVersionedType(RandomId typeInstanceId)
        {
            return _versionedTypesByInstanceId[typeInstanceId];
        }


        //public void AddObjectToContextAs(RandomId objectId, RandomId @as, MigrationContext context)
        //{
        //    _objectIdsByMigrationContext.Add(objectId, context);
        //    AddObjectToContextAs(objectId, @as);
        //}

        public void AddObjectToContextAs(RandomId objectId, RandomId @as)
        {
            var context = _loadContextByObjectId[@as];
            _loadContextByObjectId.Add(objectId, context);
            _objectIdsByLoadContext[context].Add(objectId);
        }








        [SaveHandler(343438274382730000, nameof(SaveAndLoadManager), typeof(SaveAndLoadManager), order: -100, singleton: true)]
        public class SaveAndLoadManagerSaveHandler : UnmanagedSaveHandler<SaveAndLoadManager, SaveAndLoadManagerSaveData>
        {
            public override void Init(object instance)
            {
                base.Init(instance);
                __saveData._appVersion = __instance._appVersion;
                __saveData.versionedTypeCache = VersionedTypeCache.Singleton;
            }

            public override void CreateObject()
            {
                //do nothing
            }

            public override void _AssignInstance()
            {
                //do nothing
            }
        }

        public class SaveAndLoadManagerSaveData : SaveDataBase
        {
            public int _appVersion;
            public VersionedTypeCache versionedTypeCache;
        }

    }











    public class MemberToInstanceId
    {
        public RandomId memberId;
        public RandomId instanceId;
    }

    public class ArrayMemberToElementIds
    {
        public RandomId memberId;
        public List<RandomId> elementIds = new();
    }



    public class PrefabDescriptionRegistry
    {
        public class PrefabDescription
        {
            public RandomId prefabAssetId;
            public List<MemberToInstanceId> memberToInstanceIds = new();
            public List<ArrayMemberToElementIds> arrayMemberToElementIdsList = new();
        }

        public Dictionary<RandomId, PrefabDescription> _prefabDescriptionByPrefabPartInstanceId = new();

        public Dictionary<RandomId, object> _prefabPartsByInstanceId = new();

        public Dictionary<LoadContext, HashSet<RandomId>> _instantiatedPrefabPartsByLoadContext = new();


        public PrefabDescriptionRegistry()
        {
            SaveAndLoadManager.S.LoadingLoadContextCompleted += DisposeScope;
        }


        public void DisposeScope(LoadContext context)
        {
            ///
            if (_instantiatedPrefabPartsByLoadContext.TryGetValue(context, out var prefabParts))
            {
                foreach (var id in prefabParts)
                {
                    var part = _prefabPartsByInstanceId[id];
                    _prefabPartsByInstanceId.Remove(id);

                    if (Infra.S.IsNotRegistered(id) && part is Object obj)
                    {
                        //BlueDebug.Debug((id, obj.name, obj.GetType().Name));
                        Infra.S.Destroy(obj);
                    }
                }

                _instantiatedPrefabPartsByLoadContext.Remove(context);
            }
        }


        public void RemoveIfPartOfPrefab(RandomId instanceId)
        {
            if (!_IsPartOfPrefab(instanceId)) return;

            _prefabDescriptionByPrefabPartInstanceId.Remove(instanceId);
            _prefabPartsByInstanceId.Remove(instanceId);
        }


        public bool IsPartOfPrefab<T>(RandomId instanceId, out T instance)
        {
            bool isPartOfPrefab = _IsPartOfPrefab(instanceId);

            if (isPartOfPrefab)
            {
                instance = _GetPrefabPart<T>(instanceId);
            }
            else
            {
                instance = default;
            }

            return isPartOfPrefab;
        }


        public bool _IsPartOfPrefab(RandomId instanceId)
        {
            bool isPartOfPrefab = _prefabDescriptionByPrefabPartInstanceId.ContainsKey(instanceId);

            return isPartOfPrefab;
        }


        public T _GetPrefabPart<T>(RandomId prefabPartInstanceId)
        {
            bool isPartOfPrefab = _IsPartOfPrefab(prefabPartInstanceId);

            if (isPartOfPrefab && !_IsPrefabPartCollected(prefabPartInstanceId))
            {
                _CollectPrefabParts(prefabPartInstanceId);
            }

            var part = _prefabPartsByInstanceId[prefabPartInstanceId];

            return (T)part;
        }


        public bool _IsPrefabPartCollected(RandomId prefabPartInstanceId)
        {
            return _prefabPartsByInstanceId.ContainsKey(prefabPartInstanceId);
        }


        public void _CollectPrefabParts(RandomId prefabPartInstanceId)
        {
            LoadContext loadContext = SaveAndLoadManager.S.GetLoadContextOf(prefabPartInstanceId);

            if (!_instantiatedPrefabPartsByLoadContext.ContainsKey(loadContext))
            {
                _instantiatedPrefabPartsByLoadContext.Add(loadContext, new());
            }


            var desc = _prefabDescriptionByPrefabPartInstanceId[prefabPartInstanceId];

            var prefab = AddressableDb.Singleton.GetAssetByIdOrFallback<GameObject>(null, ref desc.prefabAssetId);


            SaveAndLoadManager.Singleton.ExpectingIsObjectLoadingRequest = true;

            var instance = Object.Instantiate(prefab);

            SaveAndLoadManager.Singleton.ExpectingIsObjectLoadingRequest = false;


            var infra = instance.GetComponent<GOInfra>();

            if (infra == null)
            {
                Debug.LogError($"Invalid workflow. An instance of an object was saved as part of a prefab but the root of its prefab" +
                    $"does not have a {nameof(GOInfra)} component. The root gameobject should have a component that handles prefab workflows." +
                    $"Solution: add a {nameof(GOInfra)} component to the root of the prefab (and set it up correctly).");
            }

            var results = infra.CollectPrefabParts();

            Dictionary<RandomId, object> prefabPartsByPrefabPartId = new();
            Dictionary<RandomId, List<object>> arrayElementsByArrayMemberId = new();


            //flatten out the list of lists
            foreach (var result in results)
            {
                foreach (var (id, member) in result.membersById)
                {
                    prefabPartsByPrefabPartId.Add(id, member);
                }
                foreach (var arrayPair in result.arrayElementMembersByArrayMemberId)
                {
                    arrayElementsByArrayMemberId.Add(arrayPair.Key, arrayPair.Value);
                }
            }


            foreach (var idPair in desc.memberToInstanceIds)
            {
                var part2 = prefabPartsByPrefabPartId[idPair.memberId];

                _prefabPartsByInstanceId.Add(idPair.instanceId, part2);
                _instantiatedPrefabPartsByLoadContext[loadContext].Add(idPair.instanceId);
            }

            foreach (var arrayPair in desc.arrayMemberToElementIdsList)
            {
                var elements = arrayElementsByArrayMemberId[arrayPair.memberId];

                for (int i = 0; i < arrayPair.elementIds.Count; i++)
                {
                    var elementId = arrayPair.elementIds[i];
                    if (i >= elements.Count)
                    {
                        Debug.LogError("Mismatch in number of array elements found in prefab instance and the number of element Ids stored in PrefabDescription. " +
                            $"GameObject: {instance.HierarchyPath()}, Prefab asset id: {desc.prefabAssetId}, array member id: {arrayPair.memberId}. " +
                            $"Going to skip the rest of the elements.");

                        var idlist = string.Join(", ", arrayPair.elementIds);
                        var types = string.Join(", ", elements.Select(e => e.GetType().Name));

                        Debug.LogError($"Element Ids: {idlist}");
                        Debug.LogError($"Element types: {types}");
                        break;
                    }
                    var element = elements[i];

                    _prefabPartsByInstanceId.Add(elementId, element);
                    _instantiatedPrefabPartsByLoadContext[loadContext].Add(elementId);
                }
            }
        }



        public void Register(GOInfra infra, List<GraphWalkingResult> results)
        {
            var idPairs = new List<MemberToInstanceId>();
            var arraysAndTheirElementIds = new List<ArrayMemberToElementIds>();


            foreach (var result in results)
            {
                for (int i = 0; i < result.memberIds.Count; i++)
                {
                    var pair = new MemberToInstanceId()
                    {
                        memberId = result.memberIds[i],
                        instanceId = result.generatedIds[i],
                    };

                    idPairs.Add(pair);
                }

                if (result.arrayMemberIds.IsNotNullAndNotEmpty())
                    for (int i = 0; i < result.arrayMemberIds.Count; i++)
                    {
                        var pair = new ArrayMemberToElementIds()
                        {
                            memberId = result.arrayMemberIds[i],
                            elementIds = result.arrayElementMemberIdsPerArrayMembers[i],
                        };

                        arraysAndTheirElementIds.Add(pair);
                    }
            }




            var description = new PrefabDescription()
            {
                prefabAssetId = infra.PrefabAssetId,
                memberToInstanceIds = idPairs,
                arrayMemberToElementIdsList = arraysAndTheirElementIds,
            };

            //Debug.Log(infra.gameObject.HierarchyPath());

            //        Debug.Log(string.Join(", ", _prefabDescriptionByPrefabPartInstanceId.Keys));
            //        Debug.Log(string.Join(", ", instanceIds));


            foreach (var pair in idPairs)
            {
                var id = pair.instanceId;

                if (_prefabDescriptionByPrefabPartInstanceId.ContainsKey(id))
                {
                    Debug.LogError("Duplicate instance id registration in PrefabDescriptionRegistry. " +
                        $"Duplicate instance Id: {id} of memberId: {pair.memberId}");
                    continue;
                }
                _prefabDescriptionByPrefabPartInstanceId.Add(id, description);
            }

            foreach (var pair in arraysAndTheirElementIds)
            {
                foreach (var id in pair.elementIds)
                {
                    if (_prefabDescriptionByPrefabPartInstanceId.ContainsKey(id))
                    {
                        Debug.LogError("Duplicate instance id registration in PrefabDescriptionRegistry. " +
                            $"Duplicate instance Id: {id} of memberId: {pair.memberId}");
                        continue;
                    }
                    _prefabDescriptionByPrefabPartInstanceId.Add(id, description);
                }
            }
        }


        [SaveHandler(id: 107204973066903000, nameof(PrefabDescriptionRegistry), typeof(PrefabDescriptionRegistry), order: -90, singleton: true)]
        public class PrefabDescriptionRegistrySaveHandler : UnmanagedSaveHandler<PrefabDescriptionRegistry, PrefabDescriptionRegistrySaveData>
        {
            public override void WriteSaveData()
            {
                base.WriteSaveData();

                __saveData._prefabDescriptionByPrefabPartInstanceId = GetObjectId(__instance._prefabDescriptionByPrefabPartInstanceId, setLoadingOrder: true);
            }


            public override void LoadPhase1()
            {
                base.LoadPhase1();
                __instance._prefabDescriptionByPrefabPartInstanceId = GetObjectById<Dictionary<RandomId, PrefabDescription>>(__saveData._prefabDescriptionByPrefabPartInstanceId);
            }
        }

        public class PrefabDescriptionRegistrySaveData : SaveDataBase
        {
            public RandomId _prefabDescriptionByPrefabPartInstanceId;
        }



        [SaveHandler(id: 937204973066903000, nameof(PrefabDescription), typeof(PrefabDescription))]
        public class PrefabDescriptionSaveHandler : UnmanagedSaveHandler<PrefabDescription, PrefabDescriptionSaveData>
        {
            public override void WriteSaveData()
            {
                base.WriteSaveData();
                __saveData.prefabAssetId = __instance.prefabAssetId;
                __saveData.memberToInstanceIds = GetObjectId(__instance.memberToInstanceIds, setLoadingOrder: true);
                __saveData.arrayMemberToElementIdsList = GetObjectId(__instance.arrayMemberToElementIdsList, setLoadingOrder: true);
            }

            public override void LoadPhase1()
            {
                base.LoadPhase1();
                __instance.prefabAssetId = __saveData.prefabAssetId;
                __instance.memberToInstanceIds = GetObjectById<List<MemberToInstanceId>>(__saveData.memberToInstanceIds);
                __instance.arrayMemberToElementIdsList = GetObjectById<List<ArrayMemberToElementIds>>(__saveData.arrayMemberToElementIdsList);
            }
        }

        public class PrefabDescriptionSaveData : SaveDataBase
        {
            public RandomId prefabAssetId;
            public RandomId memberToInstanceIds;
            public RandomId arrayMemberToElementIdsList;
        }
    }










    public class SceneDescription
    {
        public RandomId sceneId;
        public List<MemberToInstanceId> memberToInstanceIds = new();
        public List<ArrayMemberToElementIds> arrayMemberToElementIdsList = new();
    }


    public class ScenePlacedObjectRegistry
    {
        public Dictionary<RandomId, SceneDescription> _sceneDescriptionBySceneObjectId = new();

        public Dictionary<RandomId, object> _sceneObjectsById = new();

        public Dictionary<LoadContext, HashSet<RandomId>> _loadedSceneObjectsByLoadContext = new();



        public ScenePlacedObjectRegistry()
        {
            SaveAndLoadManager.S.LoadingLoadContextCompleted += DisposeScope;
        }


        public void DisposeScope(LoadContext context)
        {
            if (_loadedSceneObjectsByLoadContext.TryGetValue(context, out var prefabParts))
            {
                foreach (var id in prefabParts)
                {
                    var part = _sceneObjectsById[id];
                    _sceneObjectsById.Remove(id);

                    if (Infra.S.IsNotRegistered(id) && part is Object obj)
                    {
                        //BlueDebug.Debug((id, obj.name, obj.GetType().Name));
                        Infra.S.Destroy(obj);
                    }
                }

                _loadedSceneObjectsByLoadContext.Remove(context);
            }
        }



        public void RemoveIfScenePlaced(RandomId objectId)
        {
            if (!_IsScenePlaced(objectId)) return;

            _sceneDescriptionBySceneObjectId.Remove(objectId);
            _sceneObjectsById.Remove(objectId);
        }


        public bool IsScenePlaced<T>(RandomId instanceId, out T instance)
        {
            bool isScenePlaced = _IsScenePlaced(instanceId);

            if (isScenePlaced)
            {
                instance = _GetSceneObject<T>(instanceId);
            }
            else
            {
                instance = default;
            }

            return isScenePlaced;
        }


        public bool _IsScenePlaced(RandomId instanceId)
        {
            bool isScenePlaced = _sceneDescriptionBySceneObjectId.ContainsKey(instanceId);

            return isScenePlaced;
        }


        public T _GetSceneObject<T>(RandomId sceneObjectInstanceId)
        {
            bool isScenePlaced = _IsScenePlaced(sceneObjectInstanceId);


            if (isScenePlaced && !_sceneObjectsById.ContainsKey(sceneObjectInstanceId))
            {
                var desc = _sceneDescriptionBySceneObjectId[sceneObjectInstanceId];

                Scene scene = Infra.SceneManagement.SceneById(desc.sceneId);

                SceneInfra infra = Infra.SceneManagement.SceneInfrasBySceneHandle[scene.handle];


                var results = infra.CollectScenePlacedObjects();

                Dictionary<RandomId, object> sceneObjectsByMemberId = new();
                Dictionary<RandomId, List<object>> arrayElementsByArrayMemberId = new();

                foreach (var result in results)
                {
                    foreach (var idPair in result.membersById)
                    {
                        sceneObjectsByMemberId.Add(idPair.Key, idPair.Value);
                    }
                    foreach (var arrayPair in result.arrayElementMembersByArrayMemberId)
                    {
                        arrayElementsByArrayMemberId.Add(arrayPair.Key, arrayPair.Value);
                    }
                }


                foreach (var idPair in desc.memberToInstanceIds)
                {
                    var part2 = sceneObjectsByMemberId[idPair.memberId];

                    _sceneObjectsById.Add(idPair.instanceId, part2);
                }

                foreach (var arrayPair in desc.arrayMemberToElementIdsList)
                {
                    var elements = arrayElementsByArrayMemberId[arrayPair.memberId];

                    for (int i = 0; i < arrayPair.elementIds.Count; i++)
                    {
                        var elementId = arrayPair.elementIds[i];
                        var element = elements[i];

                        _sceneObjectsById.Add(elementId, element);
                    }
                }
            }

            var part = _sceneObjectsById[sceneObjectInstanceId];

            return (T)part;
        }



        public void Register(SceneInfra infra, List<GraphWalkingResult> results)
        {
            var idPairs = new List<MemberToInstanceId>();
            var arraysAndTheirElementIds = new List<ArrayMemberToElementIds>();

            foreach (var result in results)
            {
                for (int i = 0; i < result.memberIds.Count; i++)
                {
                    var pair = new MemberToInstanceId()
                    {
                        memberId = result.memberIds[i],
                        instanceId = result.generatedIds[i],
                    };

                    idPairs.Add(pair);
                }

                if (result.arrayMemberIds.IsNotNullAndNotEmpty())
                    for (int i = 0; i < result.arrayMemberIds.Count; i++)
                    {
                        var pair = new ArrayMemberToElementIds()
                        {
                            memberId = result.arrayMemberIds[i],
                            elementIds = result.arrayElementMemberIdsPerArrayMembers[i],
                        };

                        arraysAndTheirElementIds.Add(pair);
                    }
            }



            var description = new SceneDescription()
            {
                sceneId = Infra.SceneManagement.SceneIdByHandle(infra.gameObject.scene.handle),
                memberToInstanceIds = idPairs,
                arrayMemberToElementIdsList = arraysAndTheirElementIds,
            };

            //Debug.Log(infra.gameObject.HierarchyPath());

            //        Debug.Log(string.Join(", ", _prefabDescriptionByPrefabPartInstanceId.Keys));
            //        Debug.Log(string.Join(", ", instanceIds));


            foreach (var pair in idPairs)
            {
                var id = pair.instanceId;

                if (_sceneDescriptionBySceneObjectId.ContainsKey(id))
                {
                    Debug.LogError("Duplicate instance id registration in ScenePlacedObjectRegistry. " +
                        $"Duplicate instance Id: {id} of memberId: {pair.memberId}");
                    continue;
                }
                _sceneDescriptionBySceneObjectId.Add(id, description);
            }

            foreach (var pair in arraysAndTheirElementIds)
            {
                foreach (var id in pair.elementIds)
                {
                    if (_sceneDescriptionBySceneObjectId.ContainsKey(id))
                    {
                        Debug.LogError("Duplicate instance id registration in ScenePlacedObjectRegistry. " +
                            $"Duplicate instance Id: {id} of memberId: {pair.memberId}");
                        continue;
                    }
                    _sceneDescriptionBySceneObjectId.Add(id, description);
                }
            }
        }






        [SaveHandler(id: 207204973066903000, nameof(ScenePlacedObjectRegistry), typeof(ScenePlacedObjectRegistry), order: -90, singleton: true)]
        public class ScenePlacedObjectRegistrySaveHandler : UnmanagedSaveHandler<ScenePlacedObjectRegistry, ScenePlacedObjectRegistrySaveData>
        {
            public override void WriteSaveData()
            {
                base.WriteSaveData();

                __saveData._sceneDescriptionBySceneObjectId = GetObjectId(__instance._sceneDescriptionBySceneObjectId, setLoadingOrder: true);
            }

            public override void LoadPhase1()
            {
                base.LoadPhase1();
                __instance._sceneDescriptionBySceneObjectId = GetObjectById<Dictionary<RandomId, SceneDescription>>(__saveData._sceneDescriptionBySceneObjectId);
            }
        }

        public class ScenePlacedObjectRegistrySaveData : SaveDataBase
        {
            public RandomId _sceneDescriptionBySceneObjectId;
        }



        [SaveHandler(id: 900204973066903000, nameof(SceneDescription), typeof(SceneDescription))]
        public class SceneDescriptionSaveHandler : UnmanagedSaveHandler<SceneDescription, SceneDescriptionSaveData>
        {
            public override void WriteSaveData()
            {
                base.WriteSaveData();
                __saveData.sceneId = __instance.sceneId;
                __saveData.memberToInstanceIds = GetObjectId(__instance.memberToInstanceIds, setLoadingOrder: true);
                __saveData.arrayMemberToElementIdsList = GetObjectId(__instance.arrayMemberToElementIdsList, setLoadingOrder: true);
            }

            public override void LoadPhase1()
            {
                base.LoadPhase1();
                __instance.sceneId = __saveData.sceneId;
                __instance.memberToInstanceIds = GetObjectById<List<MemberToInstanceId>>(__saveData.memberToInstanceIds);
                __instance.arrayMemberToElementIdsList = GetObjectById<List<ArrayMemberToElementIds>>(__saveData.arrayMemberToElementIdsList);
            }
        }

        public class SceneDescriptionSaveData : SaveDataBase
        {
            public RandomId sceneId;
            public RandomId memberToInstanceIds;
            public RandomId arrayMemberToElementIdsList;
        }
    }







    public class ObjectMetaData
    {
        public long SaveHandlerId;
        public int Order;
        public int Version;
        public RandomId HandledType;
        public RandomId ObjectId;
    }

    public class SavedObject
    {
        public ObjectMetaData _MetaData_;
    }









    public class MigrationContext
    {
        public MigrationContext()
        {
        }


        public void Init(int appVersionToStartMigratingFrom, List<SaveDataBase> savedataList)
        {

            foreach (var data in savedataList)
            {
                var metadata = data._MetaData_;

                int pastDataVersion = metadata.Version;
                int currentDataVersion = SaveAndLoadManager.Singleton.GetCurrentVersionOfTypeById(metadata.SaveHandlerId);

                _saveDatasByObjectId.Add(data._ObjectId_, data);

                _inheritanceChainByTypeId = SaveAndLoadManager.Singleton.GetInheritanceChainForAppVersion(appVersionToStartMigratingFrom);

                if (pastDataVersion != currentDataVersion)
                {
                    CreatePipelinesForObject(metadata.ObjectId, metadata.SaveHandlerId);
                }
            }
        }


        public class MigrationStep
        {
            public MigrationStep(int appVersion) { this.appVersion = appVersion; }
            public int appVersion;
            public HashSet<SaveDataBase> changedSet = new();
            public HashSet<SaveDataBase> addedSet = new();
            public HashSet<SaveDataBase> removedSet = new();
        }

        public List<MigrationStep> _migrationSteps = new();


        [JsonIgnore]
        public Dictionary<long, IEnumerable<long>> _inheritanceChainByTypeId;
        [JsonIgnore]
        public Dictionary<RandomId, SaveDataBase> _saveDatasByObjectId = new();
        [JsonIgnore]
        public Dictionary<RandomId, List<MigrationiPipeline>> _pipelinesByObjectId = new();


        [JsonIgnore]
        public Queue<RandomId> _objectsToMigrate = new();

        [JsonIgnore]
        public MigrationStep CurrentStep => _migrationSteps.Last();



        [JsonIgnore]
        public HashSet<Data> _dataInstances = new();
        [JsonIgnore]
        public Dictionary<RandomId, HashSet<Data>> _dataInstancesByReferencedById = new();

        public void AddDataInstance(Data data)
        {
            if (_dataInstances.Contains(data))
            {
                Debug.LogError("This data instance had already been added. Redundant calls should not happen.");
                return;
            }

            _dataInstances.Add(data);
            if (!_dataInstancesByReferencedById.ContainsKey(data.ReferencedBy))
            {
                _dataInstancesByReferencedById.Add(data.ReferencedBy, new HashSet<Data>());
            }

            _dataInstancesByReferencedById[data.ReferencedBy].Add(data);
        }



        public RandomId AddComponent<T>(T data, RandomId gameobjectId, RandomId addedBy, int? order = null) where T : MonoSaveDataBase
        {
            var go = GetObject<GameObjectSaveData>(gameobjectId);
            go.Components.Add(data._ObjectId_);
            _AddChanged(go);
            data.GameObjectId = gameobjectId;

            if (order == null)
            {
                order = go._MetaData_.Order;
            }

            var id = _AddNew(data, addedBy, isRootOject: true, order: order);
            return id;
        }


        public RandomId AddNew<T>(T data, RandomId addedBy, bool? isRootOject = null, int? order = null) where T : SaveDataBase
        {
            if (data is MonoSaveDataBase)
            {
                Debug.LogError($"This api should not be used with components. Use the {nameof(AddComponent)} instead. " +
                    $"ObjectId: {data._ObjectId_}, ActualType: {typeof(T).CleanAssemblyQualifiedName()}" +
                    $"Returning default id.");
                return RandomId.Default;
            }

            return _AddNew(data, addedBy, isRootOject, order);
        }

        public RandomId _AddNew<T>(T data, RandomId addedBy, bool? isRootOject = null, int? order = null) where T : SaveDataBase
        {
            RandomId objectId = RandomId.Get();

            long savehandlerId = SaveAndLoadManager.Singleton._coreService.__saveHandlerIdBySaveDataType[typeof(T)];

            var info = SaveAndLoadManager.Singleton._coreService.__saveHandlerAttributesById[savehandlerId];

            data._ObjectId_ = objectId;

            if (isRootOject == null)
            {
                //todo: scriptableobjects. get the handlerid of the scriptableobject type and check if it is in the inheritance chain of type T
                if (data is GameObjectSaveData or StaticSaveDataBase)
                {
                    isRootOject = true;
                }
                else
                    isRootOject = false;
            }

            data._isRootObject_ = isRootOject.Value;


            if (order == null)
            {
                var referencing = _saveDatasByObjectId[addedBy];
                order = referencing._MetaData_.Order;
            }

            ObjectMetaData metaData = new()
            {
                ObjectId = objectId,
                SaveHandlerId = savehandlerId,
                Version = CurrentStep.appVersion,
                Order = order.Value,
            };

            data._MetaData_ = metaData;


            _saveDatasByObjectId.Add(objectId, data);
            CurrentStep.addedSet.Add(data);

            SaveAndLoadManager.Singleton.AddObjectToContextAs(objectId, addedBy);


            bool needMigration = CreatePipelinesForObject(objectId, savehandlerId);
            if (needMigration)
                _objectsToMigrate.Enqueue(objectId);

            return objectId;
        }



        public bool CreatePipelinesForObject(RandomId objectId, long savehandlerId)
        {
            var inheritanceChain = _inheritanceChainByTypeId[savehandlerId];

            List<MigrationiPipeline> pipelines = new();

            if (SaveAndLoadManager.Singleton.__migrationPipelinesByHandlerId.TryGetValue(savehandlerId, out var pipeline))
            {
                pipelines.Add(pipeline);
            }

            foreach (var id in inheritanceChain)
            {
                if (SaveAndLoadManager.Singleton.__migrationPipelinesByHandlerId.TryGetValue(id, out pipeline))
                {
                    pipelines.Add(pipeline);
                }
            }

            //note: so that the most base type is the first (System.Object) and the actual type is the last
            //as derived types have the right to override base types' migration
            pipelines.Reverse();

            if (pipelines.Count > 0)
            {
                _pipelinesByObjectId.Add(objectId, pipelines);
                return true;
            }
            else
                return false;
        }





        public void EnsureChangePersists(SaveDataBase changedData, params string[] changedProperties)
        {
            var original = _saveDatasByObjectId[changedData._ObjectId_];

            if (original != changedData)
            {
                CopyIdenticalMembers(from: changedData, to: original, changedProperties);
                ClearConstructedObjectCacheFor(original._ObjectId_);
            }

            _AddChanged(changedData);
        }


        public void _AddChanged(SaveDataBase data)
        {
            RandomId objectId = data._ObjectId_;

            if (CurrentStep.changedSet.Contains(data))
            {
                //Debug.LogError($"It is not expected to try to add the same objectId [{objectId}] multiple times " +
                //    $"as a changed object during a single migration step. This fact may or may not signs of bugs.");
                //update: it is possible. Multiple object may contribute to the migration of another object, each of them will report that object as changed
            }
            else
            {
                CurrentStep.changedSet.Add(data);

                if (!_saveDatasByObjectId.ContainsKey(objectId))
                {
                    _saveDatasByObjectId[objectId] = data;
                }
            }
        }



        /// <summary>
        /// this cache is particularly needed because <see cref="Data{T}"/> can repeatedly call <see cref="GetObject{T}(RandomId, bool)"/>
        /// such that if the requested type is not the actual type of the object, it will create a new instance of the requested type
        /// </summary>
        public Dictionary<RandomId, Dictionary<Type, object>> _constructedRequestedDataByPerObjectCache = new();


        public void ClearConstructedObjectCacheFor(RandomId objectId)
        {
            if (_constructedRequestedDataByPerObjectCache.ContainsKey(objectId))
                _constructedRequestedDataByPerObjectCache.Remove(objectId);
        }


        /// <summary>
        /// This api does not guarantee that the object it returns will be the same object that the object's migration context track.
        /// If you make changes to the object you get via this api, then always call <see cref="EnsureChangePersists(SaveDataBase, string[])"/> on that object.
        /// </summary>
        /// <typeparam name="T">The "requested" type of the object</typeparam>
        /// <param name="objectId"></param>
        /// <param name="inheritsOrImplements">If set to true and If the requested type does not match the requested object's actual type 
        ///     then a new instance of the requested type will be created and everything that can be will copied from the requested object to that new instance.</param>
        /// <returns></returns>
        public T GetObject<T>(RandomId objectId, bool inheritsOrImplements = false) ///where T: SaveDataBase cant because of <see cref="Data{T}"/> must accept any kind of T
        {
            Type requestedType = typeof(T);

            if (_constructedRequestedDataByPerObjectCache.TryGetValue(objectId, out var constructedObjectsByRequestedType))
            {
                if (constructedObjectsByRequestedType.TryGetValue(requestedType, out var obj))
                {
                    return (T)obj;
                }
            }



            if (_saveDatasByObjectId.TryGetValue(objectId, out var data))
            {
                if (data is T requestedData)
                {
                    return requestedData;
                }
                else
                {
                    if (inheritsOrImplements)
                    {
                        T requested = ObjectFactory.CreateInstance<T>();
                        CopyIdenticalMembers(data, requested);

                        if (!_constructedRequestedDataByPerObjectCache.ContainsKey(data._ObjectId_))
                        {
                            _constructedRequestedDataByPerObjectCache.Add(data._ObjectId_, new());
                        }
                        if (!_constructedRequestedDataByPerObjectCache[data._ObjectId_].ContainsKey(requestedType))
                        {
                            _constructedRequestedDataByPerObjectCache[data._ObjectId_].Add(requestedType, new());
                        }

                        _constructedRequestedDataByPerObjectCache[data._ObjectId_][requestedType] = requested;

                        return requested;
                    }
                    else
                    {
                        Debug.LogError($"Type mismatch. The requested type for object [{objectId}] is not compatible with the object's actual type. " +
                            $"Requested type: {typeof(T).CleanAssemblyQualifiedName()}, Object's actual type: {data.GetType().CleanAssemblyQualifiedName()}");
                        return default;
                    }
                }
            }
            else
            {
                Debug.LogError($"No object found with id [{objectId}]. Returning default value.");
                return default;
            }
        }



        //public TTo FromCopyUnchangedMembers<TFrom, TTo>(TFrom from) where TFrom : SaveDataBase where TTo : SaveDataBase, new()
        //{
        //    TTo to = new();
        //    CopyUnchangedMembers(from, to);
        //    return to;
        //}




        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TTo">The type of new version we migrating to</typeparam>
        /// <param name="from">The instance of the previous version we migrating from</param>
        /// <param name="current">The current progress that represents what has been migrated so far.</param>
        /// <returns></returns>
        public (TFrom, TTo) CreateFromCurrent<TFrom, TTo>(object from, object current) where TFrom : SaveDataBase, new() where TTo : SaveDataBase, new()
        {
            TTo to = new();
            TFrom from_ = new();
            CopyIdenticalMembers(from, from_);
            CopyIdenticalMembers(from, to);
            CopyIdenticalMembers(current, to);
            return (from_, to);
        }



        [JsonIgnore]
        public Dictionary<Type, FieldInfo[]> _fieldsCachePerType = new();
        [JsonIgnore]
        public Dictionary<Type, Dictionary<string, FieldInfo>> _fieldsLookupByNamePerTypeCache = new();


        public FieldInfo[] GetFields(Type type)
        {
            if (!_fieldsCachePerType.ContainsKey(type))
            {
                _fieldsCachePerType.Add(type, type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
            }
            return _fieldsCachePerType[type];
        }

        public Dictionary<string, FieldInfo> GetFieldsLookupByName(Type type)
        {
            if (!_fieldsLookupByNamePerTypeCache.ContainsKey(type))
            {
                var fields = GetFields(type);
                _fieldsLookupByNamePerTypeCache.Add(type, fields.ToDictionary(field => field.Name));
            }
            return _fieldsLookupByNamePerTypeCache[type];
        }




        public void CopyIdenticalMembers(object from, object to, bool deepCopy)
        {
            CopyIdenticalMembers(from, to, deepCopy, Array.Empty<string>());
        }

        public void CopyIdenticalMembers(object from, object to, params string[] membersToCopy)
        {
            CopyIdenticalMembers(from, to, deepCopy: false, membersToCopy);
        }


        public void CopyIdenticalMembers(object from, object to, bool deepCopy, string[] membersToCopy)
        {
            var fromFields = GetFieldsLookupByName(from.GetType());
            var toFields = GetFieldsLookupByName(to.GetType());

            List<FieldInfo> fieldsToCopy = new List<FieldInfo>();

            if (membersToCopy.Length != 0)
                foreach (var member in membersToCopy)
                {
                    if (fromFields.TryGetValue(member, out var fieldInfo))
                    {
                        fieldsToCopy.Add(fieldInfo);
                    }
                }
            else
                fieldsToCopy = fromFields.Values.ToList();


            foreach (var fromField in fieldsToCopy)
            {
                if (toFields.TryGetValue(fromField.Name, out var toField))
                {
                    if (fromField.FieldType == toField.FieldType)
                    {
                        var val = fromField.GetValue(from);

                        if (deepCopy
                            && val != default && val.GetType().IsClass && val.GetType() != typeof(string)
                            && !val.GetType().IsAssignableTo(typeof(Delegate)))
                        {
                            //if (!val.GetType().IsAssignableTo(typeof(Delegate)))
                            {
                                var copy = ObjectFactory.CreateInstance(val.GetType());
                                CopyIdenticalMembers(val, copy, deepCopy: true);
                                toField.SetValue(to, copy);
                            }
                        }
                        else
                        {
                            toField.SetValue(to, fromField.GetValue(from));
                        }
                    }
                    else if (fromField.FieldType.IsAssignableTo(typeof(CustomSaveData))
                        && toField.FieldType.IsAssignableTo(typeof(CustomSaveData)))
                    {
                        //todo: check if the types they handle are the different versions of the same typeid, in other words, they are compatible
                        var prevVersion = fromField.GetValue(from) as CustomSaveData;

                        try
                        {
                            var nextVersion = prevVersion.MigrateIfNeeded(this, out bool didMigrate);

                            if (!didMigrate) throw new Exception("MigrateIfNeeded returned null when it was not expected to do so.");

                            toField.SetValue(to, nextVersion);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Failed to migrate custom savedata field '{fromField.Name}' from type '{fromField.FieldType.CleanAssemblyQualifiedName()}' " +
                                $"to type '{toField.FieldType.CleanAssemblyQualifiedName()}'. Make sure that the two customsavedata types handle different versions of the same type id.\n" +
                                $"Exception: {e}");
                        }
                    }
                    //todo: check if the types they handle are the different versions of the same typeid, in other words, they are compatible
                    else if (fromField.FieldType.IsAssignableTo(typeof(Data))
                        && toField.FieldType.IsAssignableTo(typeof(Data)))
                    {
                        var fromData = fromField.GetValue(from) as Data;
                        var toData = toField.GetValue(to) as Data;

                        try
                        {
                            toData.CopyFrom(fromData, this);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Failed to migrate Data field '{fromField.Name}' from type '{fromField.FieldType.CleanAssemblyQualifiedName()}' " +
                                $"to type '{toField.FieldType.CleanAssemblyQualifiedName()}'.\n" +
                                //$" Make sure that the two Data types handle different versions of the same type id.\n" +
                                $"Exception: {e}");
                        }
                    }
                }
            }
        }





        public void Migrate(int appVersion)
        {
            PrepareNextVersionMigration(appVersion);

            _objectsToMigrate = new(_pipelinesByObjectId.Keys);

            while (_objectsToMigrate.Count > 0)
            {
                var id = _objectsToMigrate.Dequeue();

                SaveDataBase pastData = _saveDatasByObjectId[id];

                if (!_pipelinesByObjectId.ContainsKey(id))
                {
                    Debug.LogError(JsonConvert.SerializeObject(pastData));
                }


                //the pipelines are in the same order as the target type's inheritance chain it had at that appversion
                //starting from the most base type, System.Object
                var pipelines = _pipelinesByObjectId[id];


                SaveDataBase current = pastData;
                bool didMigrateInLastPipeline = false;
                bool didMigrateAny = false;

                foreach (var pipeline in pipelines)
                {
                    current = (SaveDataBase)pipeline.Migrate(pastData, current, appVersion, this, out bool didMigrate);

                    ClearConstructedObjectCacheFor(id);

                    didMigrateAny |= didMigrate;
                    didMigrateInLastPipeline = didMigrate;
                }


                if (didMigrateAny)
                {
                    var final = current;

                    if (!didMigrateInLastPipeline)
                    {
                        Debug.LogError($"An object did not migrate in its last migration pipeline. This is not allowed, it should not have happend. " +
                            $"Id: {id}");
                        CopyIdenticalMembers(current, pastData);
                        final = pastData;
                    }

                    _saveDatasByObjectId[id] = final;

                    _AddChanged(final);
                }
            }

            FinalizeMigrationStep();
        }


        public void PrepareNextVersionMigration(int appVersion)
        {
            var step = new MigrationStep(appVersion);
            _migrationSteps.Add(step);

            _inheritanceChainByTypeId = SaveAndLoadManager.Singleton.GetInheritanceChainForAppVersion(appVersion);

            foreach (var id in _pipelinesByObjectId.Keys.ToList())
            {
                var data = _saveDatasByObjectId[id];

                _pipelinesByObjectId.Remove(id);
                CreatePipelinesForObject(id, data._MetaData_.SaveHandlerId);
            }
        }


        public void FinalizeMigrationStep()
        {
            foreach (var data in _dataInstances)
            {
                data.FinalizeMigrationStep(this);
            }


            Clone(CurrentStep.changedSet);
            Clone(CurrentStep.addedSet);
            Clone(CurrentStep.removedSet);

            void Clone(HashSet<SaveDataBase> set)
            {
                foreach (var data in set.ToList())
                {
                    var clone = ObjectFactory.CreateInstance<SaveDataBase>(data.GetType());
                    CopyIdenticalMembers(from: data, to: clone, deepCopy: true);
                    set.Remove(data);
                    set.Add(clone);
                }
            }
        }
    }

}