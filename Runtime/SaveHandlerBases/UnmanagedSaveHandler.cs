
using Assets._Project.Scripts.Infrastructure;
using System;
using Theblueway.SaveAndLoad.Packages.com.theblueway.saveandload.Runtime;
using UnityEngine;

namespace Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases
{
    public class UnmanagedSaveHandler<TSavable, TSaveData> : SaveHandlerGenericBase<TSavable, TSaveData>
        where TSavable : class
        where TSaveData : SaveDataBase, new()
    {
        public override bool IsValid => __instance != null;


        public override void CreateObject()
        {
            if (IsSingleton) return;

            base.CreateObject();


            HandledObjectId = __saveData._ObjectId_;

            SaveAndLoadManager.Singleton.ExpectingIsObjectLoadingRequest = true;

            _AssignInstance();

            SaveAndLoadManager.Singleton.ExpectingIsObjectLoadingRequest = false;

            Infra.Singleton.RegisterReference(__instance, HandledObjectId, rootObject: __saveData._isRootObject_);
        }


        public override void _AssignInstance()
        {
            VersionedType versionedType = SaveAndLoadManager.Singleton.GetVersionedType(__saveData._MetaData_.HandledType);
            Type instanceType = versionedType.ResolveForCurrentHandledType();

            if (instanceType == null)
            {
                //debug error
                Debug.LogError($"Couldn't load the type that was in the save file for object: {__saveData._ObjectId_}. Type instance: {__saveData._MetaData_.HandledType}. " +
                    $"Cant do anything so let it go on.");
            }

            __instance = (TSavable)Activator.CreateInstance(instanceType);
        }
    }
}
