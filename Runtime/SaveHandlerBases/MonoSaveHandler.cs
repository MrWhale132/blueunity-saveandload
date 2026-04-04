
using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.UtilScripts.Extensions;
using UnityEngine;

namespace Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases
{
    public class MonoSaveHandler<TSavable, TSaveData> : SaveHandlerGenericBase<TSavable, TSaveData>
        where TSavable : UnityEngine.Component
        where TSaveData : MonoSaveDataBase, new()
    {
        public override bool IsValid => __instance != null;

        public override void Init(object instance)
        {
            base.Init(instance);


            __saveData.GameObjectId = GetObjectId(__instance.gameObject);
            __saveData.IsFromPrefabAsset = __instance.gameObject.IsProbablyPrefabAsset();
        }


        public override void CreateObject()
        {
            base.CreateObject();

            HandledObjectId = __saveData._ObjectId_;

            _AssignInstance();

            Infra.Singleton.RegisterReference(__instance, __saveData._ObjectId_, rootObject: __saveData._isRootObject_);
        }

        public override void _AssignInstance()
        {
            if (SaveAndLoadManager.Singleton.IsPartOfPrefabOrScenePlaced<TSavable>(HandledObjectId, out var instance))
            {
                __instance = instance;
            }
            else
            {
                var goSH = SaveAndLoadManager.Singleton.GetSaveHandlerById<GameObjectSaveHandler>(__saveData.GameObjectId);

                if (goSH.IsPrefabAsset)
                {
                    var go = Infra.S.GetObjectById<GameObject>(__saveData.GameObjectId);
                    __instance = go.GetComponent<TSavable>();
                }
                else
                    __instance = goSH.AddComponent<TSavable>();
            }
        }
    }
}
