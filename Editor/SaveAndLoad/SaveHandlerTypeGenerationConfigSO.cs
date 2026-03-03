
using Assets._Project.Scripts.SaveAndLoad;
using Packages.com.theblueway.saveandload.Editor.SaveAndLoad.HandledTypeNameSearchFeature;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Theblueway.SaveAndLoad;
using UnityEngine;
using Assets._Project.Scripts.UtilScripts.CodeGen;
using Assets._Project.Scripts.Infrastructure;

namespace Packages.com.theblueway.saveandload.Editor.SaveAndLoad
{

    [CreateAssetMenu(fileName = "SaveHandlerTypeGenerationConfig", menuName = "Scriptable Objects/SaveAndLoad/SaveHandlerTypeGenerationConfig")]
    public class SaveHandlerTypeGenerationConfigSO : UnityEngine.ScriptableObject
    {
        public SaveHandlerTypeGenerationConfig config;

        public void OnValidate()
        {
            config.CacheInvalidate();
            config.logContext = this;

            if (config._triggerValidate)
            {
                config._triggerValidate = false;
                bool result = config.IsValid(true, this);

                if (result)
                {
                    Debug.Log("Validation: success", this);
                }
            }
        }
    }



    [Serializable]
    public class SaveHandlerTypeGenerationConfig
    {
        public enum ConfiguredTypeState
        {
            None,
            Unassigned,
            Assigned,
            LostTracking,
        }


        public bool _triggerValidate;

        [HandledTypeSaveHandlerId]
        [Tooltip("Click the value area to select an existing savehandler to configure")]
        public long handlerIdOfConfiguredType;

        [ReadOnly]
        public ConfiguredTypeState _configuredTypeState;

        [ReadOnly]
        public long _lastKnownHandlerIdOfConfiguredType;
        [ReadOnly]
        public string _lastKnownTypeNameOfConfiguredType;
        [ReadOnly]
        public bool _wasStatic;


        public int _loadOrder;
        public List<MemberConfig> memberConfigs;

        [HideInInspector]
        public UnityEngine.Object logContext;


        public Dictionary<string, FieldInfo> _fieldInfoCache = new();
        public Dictionary<string, PropertyInfo> _propertyInfoCache = new();
        public Dictionary<string, MethodInfo> _methodInfoCache = new();
        public Dictionary<string, EventInfo> _eventInfoCache = new();

        [NonSerialized]
        public bool _cacheIsBuilt = false;



        public void DetermineState(out bool isValid, int recursionDepth, bool logErrorMessages = false, UnityEngine.Object context = null)
        {
            recursionDepth++;

            if (recursionDepth > 10)
            {
                Debug.LogError($"SaveHandlerTypeGenerationConfig: Recursion depth exceeded while determining state for type with handler id {handlerIdOfConfiguredType}. This likely means there is an infinite loop in the logic. This is surely a bug.", context);
                isValid = false;
                return;
            }


            Type configuredType = SaveAndLoadManager.Service.GetHandledTypeByHandlerId(handlerIdOfConfiguredType, log: false, out var isStatic);

            bool typeWasFound = configuredType != null;

            if (typeWasFound)
                _wasStatic = isStatic;


            if (_configuredTypeState is ConfiguredTypeState.None)
            {
                bool isAssigned = handlerIdOfConfiguredType != 0;
                if (isAssigned)
                {
                    _configuredTypeState = ConfiguredTypeState.Assigned;
                    _lastKnownHandlerIdOfConfiguredType = handlerIdOfConfiguredType;
                }
                else
                {
                    _configuredTypeState = ConfiguredTypeState.Unassigned;
                }
            }

            //if we are still lost, try find the type buy its last known name
            if (_configuredTypeState is ConfiguredTypeState.LostTracking)
            {
                var type = VersionedTypeResolver.Resolve(_lastKnownTypeNameOfConfiguredType);

                if (type != null)
                {
                    var id = SaveAndLoadManager.Service.GetHandlerIdByHandledType(type, _wasStatic);

                    if (id != 0)
                    {
                        _configuredTypeState = ConfiguredTypeState.Assigned;
                        handlerIdOfConfiguredType = id;
                        DetermineState(out isValid, recursionDepth, logErrorMessages, context);
                        return;
                    }
                    else
                    {
                        string isStatic2 = _wasStatic ? "(static)" : "";
                        if (logErrorMessages)
                            Debug.LogWarning($"SaveHandlerTypeGenerationConfig: Resolved type {isStatic2} '{type.CleanAssemblyQualifiedName()}' does not have a savehandler id associated.", context);
                        isValid = false;
                        return;
                    }
                }
                else
                {
                    Debug.LogWarning($"SaveHandlerTypeGenerationConfig: Attempting to resolve a type by its last known name was failed: '{_lastKnownTypeNameOfConfiguredType}'.", context);
                    isValid = false;
                    return;
                }
            }

            if (_configuredTypeState is ConfiguredTypeState.Unassigned)
            {
                if (logErrorMessages)
                    Debug.LogWarning($"SaveHandlerTypeGenerationConfig: Configured type is unassigned. Please select one", context);
                isValid = false;
                return;
            }
            else if (_configuredTypeState is ConfiguredTypeState.Assigned)
            {
                if (!typeWasFound)
                {
                    _configuredTypeState = ConfiguredTypeState.LostTracking;
                    DetermineState(out isValid, recursionDepth, logErrorMessages, context);
                    return;
                }
                else
                {
                    _lastKnownHandlerIdOfConfiguredType = handlerIdOfConfiguredType;
                    _lastKnownTypeNameOfConfiguredType = configuredType.CleanAssemblyQualifiedName();
                    isValid = true;
                    return;
                }
            }

            Debug.LogError("This point should have never reached.", context);
            isValid = false;
        }




        public bool IsValid(out ConfiguredTypeState configState, bool logErrorMessages = false, UnityEngine.Object context = null)
        {
            bool isValid = IsValid(logErrorMessages, context);
            configState = _configuredTypeState;

            return isValid;
        }

        public bool IsValid(bool logErrorMessages = false, UnityEngine.Object context = null)
        {
            bool isValid = true;

            DetermineState(out isValid, recursionDepth: 0, logErrorMessages, context);

            if (!isValid) return false;


            Type configuredType = SaveAndLoadManager.Service.GetHandledTypeByHandlerId(handlerIdOfConfiguredType, out var isStatic);

            bool typeWasFound = configuredType != null;


            if (!typeWasFound)
            {
                if (logErrorMessages)
                    Debug.LogError($"SaveHandlerTypeGenerationConfig: Configured type with handler ID {handlerIdOfConfiguredType} could not be found.", context);
                return false;
            }

            bool manuallyHandled = SaveAndLoadManager.Service.IsTypeManuallyHandled_Editor(configuredType, isStatic);

            if (manuallyHandled)
            {
                if (logErrorMessages)
                {
                    Debug.LogError($"SaveHandlerTypeGenerationConfig: Configured type {configuredType.CleanAssemblyQualifiedName()} {handlerIdOfConfiguredType} is marked as manually handled. Cannot generate save handler for manually handled types.", context);
                }
                return false;
            }


            if (memberConfigs.Count > 0)
            {
                BuildCache();

                var checkedMemberNames = new HashSet<string>();

                foreach (var memberConfig in memberConfigs)
                {
                    if (memberConfig.methodId != 0)
                    {
                        if (memberConfig.inclusionMode is MemberInclusionMode.Exclude)
                        {
                            Debug.LogError($"Methods can not be excluded via UI by refering them by their methodid.");
                        }
                        continue;
                    }


                    if (checkedMemberNames.Contains(memberConfig.memberName))
                    {
                        if (logErrorMessages)
                            Debug.LogError($"SaveHandlerTypeGenerationConfig: Duplicate member name '{memberConfig.memberName}' found in configuration for type {configuredType.CleanAssemblyQualifiedName()} {handlerIdOfConfiguredType}.", context);
                        isValid = false;
                    }

                    checkedMemberNames.Add(memberConfig.memberName);


                    bool memberExists;

                    if (_fieldInfoCache.TryGetValue(memberConfig.memberName, out FieldInfo field))
                    {
                        memberExists = true;
                        memberConfig.MemberInfo = field;
                    }
                    else if (_propertyInfoCache.TryGetValue(memberConfig.memberName, out var property))
                    {
                        memberExists = true;
                        memberConfig.MemberInfo = property;
                    }
                    else if (_eventInfoCache.TryGetValue(memberConfig.memberName, out var evt))
                    {
                        memberExists = true;
                        memberConfig.MemberInfo = evt;
                    }
                    else
                    {
                        memberExists = false;
                    }


                    if (!memberExists)
                    {
                        if (logErrorMessages)
                            Debug.LogError($"SaveHandlerTypeGenerationConfig: Member '{memberConfig.memberName}' does not exist in configured type {configuredType.CleanAssemblyQualifiedName()} {handlerIdOfConfiguredType}.", context);
                        isValid = false;
                    }
                }
            }

            //if (logErrorMessages)
            //{
            //    Debug.Log("Validation was " + (isValid ?"success":"failed"));
            //}

            return isValid;
        }


        public void BuildCache()
        {
            if (_cacheIsBuilt) return;

            Type configuredType = SaveAndLoadManager.Service.GetHandledTypeByHandlerId(handlerIdOfConfiguredType);

            FieldInfo[] fields = configuredType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            PropertyInfo[] properties = configuredType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            MethodInfo[] methods = configuredType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            EventInfo[] events = configuredType.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            foreach (var field in fields) _fieldInfoCache.Add(field.Name, field);
            foreach (var property in properties) _propertyInfoCache.Add(property.Name, property);
            //cant create dict because of method overloads. Should get the methodids instead.
            //foreach (var method in methods) _methodInfoCache.Add(method.Name, method);
            foreach (var ev in events) _eventInfoCache.Add(ev.Name, ev);

            _cacheIsBuilt = true;
        }

        public void CacheInvalidate()
        {
            _fieldInfoCache.Clear();
            _propertyInfoCache.Clear();
            _methodInfoCache.Clear();
            _eventInfoCache.Clear();

            _cacheIsBuilt = false;
        }


        [Serializable]
        public class MemberConfig
        {
            public string memberName;
            //public MemberType memberType = MemberType.Property;
            public MemberInclusionMode inclusionMode = MemberInclusionMode.Include;
            public string directive;
            public long methodId; //todo: add validation that this id exists

            public MemberInfo MemberInfo { get; set; }
        }
    }

    public enum MemberType
    {
        Field,
        Property,
        Method,
        Event,
    }
    public enum MemberInclusionMode
    {
        Include,
        Exclude,
    }
}