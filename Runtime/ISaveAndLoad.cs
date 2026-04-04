
using Assets._Project.Scripts.UtilScripts;
using System;
using UnityEngine.Scripting;

namespace Assets._Project.Scripts.SaveAndLoad
{
    [Preserve]
    public interface ISaveAndLoad
    {
        public ObjectMetaData MetaData { get; }
        public long SaveHandlerId { get; }
        public RandomId HandledObjectId { get; }
        public bool IsInitialized { get; }
        public Type HandledType { get; }
        public int Order { get; set; }
        public bool IsValid { get; }
        public SaveDataBase SaveData { get; }
        public void Accept(SaveDataBase data);
        public void ReleaseObject();
        //save
        public void WriteSaveData();
        public void ArrangeSaveDataForSerialization();
        //load
        public void Deserialize(string json);
        public void CreateObject();
        public void LoadPhase1();
        public void LoadPhase2();
    }
}
