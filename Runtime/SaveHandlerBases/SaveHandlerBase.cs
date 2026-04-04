
using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.Infrastructure.AddressableInfra;
using Assets._Project.Scripts.SaveAndLoad.SavableDelegates;
using Assets._Project.Scripts.UtilScripts;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases
{
    [Serializable]
    public class InitContext
    {
        public bool isPrefabPart;
        public bool isScenePlaced;
    }


    public class SaveHandlerBase : ISaveAndLoad
    {
        public static Dictionary<Type, SaveHandlerAttribute> __attributeCache = new();

        public bool __hadInit;

        public bool __isHandledTypeGeneric;
        public bool __isHandledTypeStatic;

        public Dictionary<string, FieldInfo> __eventBackingFields = new();


        public SaveHandlerBase()
        {
            Type type = GetType();

            if (!__attributeCache.ContainsKey(type))
            {
                var attr = type.GetCustomAttribute<SaveHandlerAttribute>(false);

                if (attr == null)
                {
                    DebugUtil.LogFatal($"SaveHandlerBase: {type.Name} does not have SaveHandlerAttribute defined." +
                        $"Every SaveHandler must have a {nameof(SaveHandlerAttribute)}");
                }

                __attributeCache[type] = attr;
            }

            var attribute = __attributeCache[type];
            SaveHandlerId = attribute.Id;
            _handledType = attribute.HandledType;
            StaticHandlerOf = attribute.StaticHandlerOf;
            IsSingleton = attribute.IsSingleton;
            //Order = attribute.Order;

            ///todo: tmp fix, the attribute might have null HandledType if it is a manually created attribute, <see cref="RuntimeTypeSaveHandler"/>
            __isHandledTypeGeneric = attribute.HandledType?.IsGenericTypeDefinition??false;
            __isHandledTypeStatic = attribute.IsStatic;
        }


        public virtual ObjectMetaData MetaData { get => throw new NotSupportedException("we shouldnt got here"); }
        public virtual SaveDataBase SaveData => throw new NotSupportedException("we shouldnt got here");
        public long SaveHandlerId { get; set; }
        public Type _handledType;
        public Type HandledType { get => __isHandledTypeStatic ? StaticHandlerOf : _handledType; set => _handledType = value; }
        public RandomId HandledObjectId { get; protected set; }
        public virtual int Order { get; set; }
        public bool IsInitialized => HandledObjectId.IsNotDefault;
        public Type StaticHandlerOf { get; set; }
        public bool IsSingleton { get; set; }
        public virtual bool IsValid {
            get
            {
                Debug.LogError("IsValid check not implemented in "+GetType().Name);
                return true;
            }
        }





        ///these helper methods copied to <see cref="CustomSaveData"/>
        ///
        public RandomId GetObjectId(object obj, bool setLoadingOrder = false)
        {
            return Infra.Singleton.GetObjectId(obj, HandledObjectId, setLoadingOrder);
        }

        public RandomId GetAssetId(UnityEngine.Object asset)
        {
            return Infra.Singleton.GetObjectId(asset, HandledObjectId, setLoadingOrder:true);
            //return AddressableDb.Singleton.GetAssetIdByAssetName(asset);
        }

        public InvocationList GetInvocationList<T>(T del) where T : Delegate
        {
            return Infra.Singleton.GetInvocationList(del);
        }


        //properties cant use the .AssignById(ref) version because they cant be ref-ed
        public T GetObjectById<T>(RandomId id)
        {
            return Infra.Singleton.GetObjectById<T>(id);
        }

        public T GetAssetById<T>(RandomId id, T fallback)where T: UnityEngine.Object
        {
            var obj=Infra.Singleton.GetObjectById<T>(id);

            if (obj != null)
                return obj;

            return fallback;

            //var asset= AddressableDb.Singleton.GetAssetByIdOrFallback<T>(fallback, ref id);

            //return asset;
        }

        public T GetAssetById2<T>(RandomId id, T fallback)where T: UnityEngine.Object
        {
            var asset = AddressableDb.Singleton.GetAssetByIdOrFallback<T>(fallback, ref id);

            return asset;
        }

        public T GetDelegate<T>(InvocationList list) where T : Delegate
        {
            return Infra.Singleton.GetDelegate<T>(list);
        }






        public virtual void Init(object instance)
        {

        }
        public virtual void Init(object instance, InitContext context)
        {
            Init(instance);
        }


        //from interface

        public virtual void Accept(SaveDataBase data)
        {
            throw new System.NotImplementedException("This method should be overriden in every derived type and not be called.");
        }
        public virtual void LoadPhase1()
        {
        }

        public virtual void LoadPhase2()
        {
        }


        public virtual void WriteSaveData()
        {
        }

        public virtual void ArrangeSaveDataForSerialization()
        {
        }

        public virtual void Deserialize(string json)
        {
            throw new System.NotImplementedException();
        }

        public virtual void CreateObject()
        {

        }

        public virtual void ReleaseObject()
        {
            throw new NotImplementedException();
        }




        public static SaveHandlerAttribute ManualSaveHandlerAttributeCreation(Type handledType, Type theHandlerType)
        {
            var attr = theHandlerType.GetCustomAttribute<SaveHandlerAttribute>();

            attr.SetHandledType(handledType);
            attr.RequiresManualAttributeCreation = false;

            return attr;
        }

#if UNITY_EDITOR
        public class TypeMetaData
        {
            public Dictionary<string, long> MethodSignatureToMethodId = new();
        }
#endif

        public static Dictionary<string,long> _methodToId()
        {
            return null;
        }
    }


    public static class SaveAndLoadUtils
    {
        public static void AssignById<T>(this ref RandomId id, ref T reference) where T : class
        {
            var obj = Infra.Singleton.GetObjectById<T>(id);

            reference = obj;
        }

        //public static void AssignAssetById<T>(this ref RandomId id, ref T reference) where T : UnityEngine.Object
        //{
        //    var obj = AddressableDb.Singleton.GetAssetByIdOrFallback(reference, ref id);

        //    reference = obj;
        //}

        public static void AssignDelegateById<T>(this InvocationList list, ref T reference) where T : Delegate
        {
            var obj = Infra.Singleton.GetDelegate<T>(list);

            reference = obj;
        }
    }
}
