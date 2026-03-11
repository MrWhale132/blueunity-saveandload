using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.SaveAndLoad.SavableDelegates;
using Assets._Project.Scripts.UtilScripts;
using Newtonsoft.Json;
using Theblueway.Core.Runtime.Extensions;
using System;
using System.Reflection;
using Theblueway.SaveAndLoad.Packages.com.theblueway.saveandload.Runtime;
using UnityEngine;

namespace Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases
{
    public class SaveHandlerGenericBase<TSavable, TSaveData> : SaveHandlerBase
        where TSaveData : SaveDataBase, new()
    {
        public TSavable __instance;
        public TSaveData __saveData;



        public override ObjectMetaData MetaData => __saveData._MetaData_;
        public override int Order { get => __saveData._MetaData_.Order; set => __saveData._MetaData_.Order = value; }


        public override void Init(object instance)
        {
            base.Init(instance);

            _Init((TSavable)instance);
        }



        public void _Init(TSavable instance)
        {
            __instance = instance;
            __saveData = new TSaveData();

            _SetObjectId();


            __saveData._ObjectId_ = HandledObjectId;

            int version = SaveAndLoadManager.Singleton.GetCurrentVersionOfTypeById(SaveHandlerId);

            RandomId versionedType = VersionedType.From(__instance.GetType());


            __saveData._MetaData_ = new()
            {
                SaveHandlerId = SaveHandlerId,
                ObjectId = HandledObjectId,
                Version = version,
                HandledType = versionedType,
            };

            var attr = __attributeCache[GetType()];
            //hidden dependency jkanfiuhl5435huieurig
            ///this does not override <see cref="Infra._SetLoadingOrder(RandomId, RandomId)"/> because it happens after this Init call
            Order = attr.Order;

            __saveData._isRootObject_ = Infra.Singleton.IsRootObject(HandledObjectId);
        }


        public virtual void _SetObjectId()
        {
            if (IsSingleton)
            {
                HandledObjectId = SaveAndLoadManager.Singleton.GetOrCreateSingletonObjectIdBySaveHandlerId(SaveHandlerId);
                Infra.Singleton.RegisterReference(__instance, HandledObjectId, rootObject: true);
            }
            else
            HandledObjectId = Infra.Singleton.GetObjectId(__instance, Infra.GlobalReferencing);
        }



        public virtual void _AssignInstance()
        {

        }


        public override void LoadPhase2()
        {
            base.LoadPhase2();
            __saveData._MetaData_.HandledType = VersionedType.From(__instance.GetType());
        }

        public override void ReleaseObject()
        {
            //this boxes a new instance in case of value types
            //fix?: cache default values per type
            __instance = default;
        }


        public override void Accept(SaveDataBase data)
        {
            var savedata = data as TSaveData;

            if(savedata == null)
            {
                Debug.LogError($"The given savedata type does not match what the handler is expecting.\n" +
                    $"Type of given savedata: {data.GetType().CleanAssemblyQualifiedName()}.\n" +
                    $"Type of what handler is expecting: {typeof(TSaveData).CleanAssemblyQualifiedName()}");
            }
            __saveData = (TSaveData)data;
        }

        public override string Serialize()
        {
            return JsonConvert.SerializeObject(__saveData);
        }

        public override void Deserialize(string json)
        {
            //var metaData = JsonConvert.DeserializeObject<SavedObject>(json);
            //Debug.Log(metaData.MetaData.ObjectId);
            __saveData = JsonConvert.DeserializeObject<TSaveData>(json);
        }


        /// <see cref="FieldInfo.GetValue(object)"/> requires an instance, that is why this helper is here and in the base class
        public InvocationList GetInvocationList(string eventName)
        {
            if (!__eventBackingFields.ContainsKey(eventName))
            {
                var fieldToAdd = HandledType.GetField(eventName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
                if (fieldToAdd == null)
                {
                    Debug.LogError($"SaveHandlerBase: Could not find event backing field for event {eventName} in type {HandledType.Name}. ");
                    return null;
                }
                __eventBackingFields[eventName] = fieldToAdd;
            }

            var field = __eventBackingFields[eventName];
            var dlg = (Delegate)field.GetValue(__instance);

            return Infra.Singleton.GetInvocationList(dlg);
        }



        //these are quick fixes because dynamic object load order setting is not ready yet
        //todo: may cleanup later
        //[Obsolete]
        //public void GetAssetIdList(IEnumerable<UnityEngine.Object> objs, List<RandomId> ids)
        //{
        //    ids.Clear();
        //    int i = 0;
        //    foreach (var obj in objs)
        //    {
        //        if (obj != null)
        //        {
        //            var assetId = GetAssetId2(obj);
        //            Infra.Singleton.KeepAlive(assetId, HandledObjectId);
        //            ids.Add(assetId);
        //        }
        //        i++;
        //    }
        //}

        //[Obsolete]
        //public T[] GetAssetList<T>(List<RandomId> ids) where T : UnityEngine.Object
        //{
        //    T[] assets = new T[ids.Count];

        //    for (int i = 0; i < ids.Count; i++)
        //    {
        //        var id = ids[i];

        //        var asset = GetAssetById2<T>(id, null);

        //        if (asset != null)
        //            assets[i] = asset;
        //    }

        //    return assets;
        //}
    }
}
