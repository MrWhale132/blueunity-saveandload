using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using Assets._Project.Scripts.UtilScripts;
using Newtonsoft.Json;
using UnityEngine;


namespace Assets._Project.Scripts.SaveAndLoad.ThirdPartySaveHandlers
{
    [SaveHandler(id: 32989643, dataGroupName: nameof(Transform), typeof(Transform), order: -9)]
    public class TransformSaveHandler : MonoSaveHandler<Transform, TransformSaveData>
    {
        public TransformSaveHandler() { }


        public override void WriteSaveData()
        {
            base.WriteSaveData();

            __saveData.localRotation = __instance.localRotation;
            __saveData.localScale = __instance.localScale;
            __saveData.localPosition = __instance.localPosition;
            __saveData.SiblingIndex = __instance.GetSiblingIndex();

            if (__instance.parent != null)
            {
                __saveData.ParentGOId = Infra.Singleton.GetObjectId(__instance.parent.gameObject, HandledObjectId);
            }
        }



        public override void LoadPhase1()
        {
            base.LoadPhase1();

            GameObject parent = Infra.Singleton.GetObjectById<GameObject>(__saveData.ParentGOId);
            if (parent != null)
                __instance.SetParent(parent.transform);
            else __instance.SetParent(null);

            __instance.localPosition = __saveData.localPosition;
            __instance.localRotation = __saveData.localRotation;
            __instance.localScale = __saveData.localScale;
        }


        //doc: SetSiblingIndex can cause issues if the sibling index is out of range (e.g. if the parent has less children than the sibling index). So we set the sibling index in a separate phase after all objects have been loaded and parented to ensure that the sibling index is valid.
        public override void LoadPhase2()
        {
            base.LoadPhase2();

            __instance.SetSiblingIndex(__saveData.SiblingIndex);
        }



        public override void _AssignInstance()
        {
            var go = Infra.Singleton.GetObjectById<GameObject>(__saveData.GameObjectId);

            __instance = go.transform;
        }
    }


    public class TransformSaveData : MonoSaveDataBase
    {
        public RandomId ParentGOId;
        public Quaternion localRotation;
        public Vector3 localScale;
        public Vector3 localPosition;
        public int SiblingIndex;
    }













}
