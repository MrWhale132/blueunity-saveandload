
using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.SaveAndLoad.SavableDelegates;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using Assets._Project.Scripts.UtilScripts.CodeGen;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace Assets._Project.Scripts.SaveAndLoad.ThirdPartySaveHandlers.Unity.UnityEvents
{
    //[SaveHandler(8324732879132, nameof(UnityEvent), typeof(UnityEvent))]
    public class UnityEventSaveHandlerBase<T>:UnmanagedSaveHandler<T, UnityEventSaveData> where T : UnityEventBase
    {
        public Type[] _typeArgs;

        public override void WriteSaveData()
        {
            base.WriteSaveData();

            if(__instance.GetPersistentEventCount() > 0)
            {
                Debug.LogError("UnityEventSaveHandler does not support persistent listeners. They will NOT be saved. " +
                    $"ObjectId: {HandledObjectId}");
            }

            IEnumerable<(MethodInfo, object)> methodsAndTargets = CodeGenUtils.GetRuntimeDelegatesFromUnityEvent(__instance, _typeArgs);

            __saveData.invocationList.Clear();
            foreach(var tuple in methodsAndTargets)
            {
                var saveInfo = Infra.Singleton.GetDelegateSaveInfo(tuple.Item1, tuple.Item2);
                __saveData.invocationList.Add(saveInfo);
            }
        }
    }


    public class UnityEventSaveData : SaveDataBase
    {
        public List<DelegateSaveInfo> invocationList = new();
    }





    [SaveHandler(324535435656, nameof(UnityEvent), typeof(UnityEvent))]
    public class UnityEventSaveHandler : UnityEventSaveHandlerBase<UnityEvent>
    {
        public override void Init(object instance)
        {
            base.Init(instance);

            _typeArgs = new Type[0];
        }
        public override void CreateObject()
        {
            base.CreateObject();

            _typeArgs = new Type[0];
        }


        public override void LoadPhase1()
        {
            base.LoadPhase1();

            foreach (var saveInfo in __saveData.invocationList)
            {
                var del = Infra.Singleton.GetDelegate<UnityAction>(saveInfo);
                __instance.AddListener(del);
            }
        }
    }


    [SaveHandler(756756345345435, "UnityEvent`1", typeof(UnityEvent<>))]
    public class UnityEventSaveHandler<T> : UnityEventSaveHandlerBase<UnityEvent<T>>
    {
         public override void Init(object instance)
        {
            base.Init(instance);

            _typeArgs = new Type[] {typeof(T)};
        }
        public override void CreateObject()
        {
            base.CreateObject();

            _typeArgs = new Type[] {typeof(T)};
        }


        public override void LoadPhase1()
        {
            base.LoadPhase1();

            foreach (var saveInfo in __saveData.invocationList)
            {
                var del = Infra.Singleton.GetDelegate<UnityAction<T>>(saveInfo);
                __instance.AddListener(del);
            }
        }
    }


    [SaveHandler(324234324234236, "UnityEvent`2", typeof(UnityEvent<,>))]
    public class UnityEventSaveHandler<T0,T1> : UnityEventSaveHandlerBase<UnityEvent<T0,T1>>
    {
        public override void Init(object instance)
        {
            base.Init(instance);

            _typeArgs = new Type[] { typeof(T0), typeof(T1) };
        }
        public override void CreateObject()
        {
            base.CreateObject();

            _typeArgs = new Type[] { typeof(T0), typeof(T1) };
        }


        public override void LoadPhase1()
        {
            base.LoadPhase1();

            foreach (var saveInfo in __saveData.invocationList)
            {
                var del = Infra.Singleton.GetDelegate<UnityAction<T0,T1>>(saveInfo);
                __instance.AddListener(del);
            }
        }
    }



    [SaveHandler(6574569573545345, "UnityEvent`3", typeof(UnityEvent<,,>))]
    public class UnityEventSaveHandler<T0, T1, T2> : UnityEventSaveHandlerBase<UnityEvent<T0, T1,T2>>
    {
        public override void Init(object instance)
        {
            base.Init(instance);

            _typeArgs = new Type[] { typeof(T0), typeof(T1),typeof(T2) };
        }
        public override void CreateObject()
        {
            base.CreateObject();

            _typeArgs = new Type[] { typeof(T0), typeof(T1) ,typeof(T2)};
        }


        public override void LoadPhase1()
        {
            base.LoadPhase1();

            foreach (var saveInfo in __saveData.invocationList)
            {
                var del = Infra.Singleton.GetDelegate<UnityAction<T0, T1,T2>>(saveInfo);
                __instance.AddListener(del);
            }
        }
    }


    [SaveHandler(345478965423324453, "UnityEvent`4", typeof(UnityEvent<,,,>))]
    public class UnityEventSaveHandler<T0, T1, T2,T3> : UnityEventSaveHandlerBase<UnityEvent<T0, T1, T2,T3>>
    {
        public override void Init(object instance)
        {
            base.Init(instance);

            _typeArgs = new Type[] { typeof(T0), typeof(T1), typeof(T2) ,typeof(T3)};
        }
        public override void CreateObject()
        {
            base.CreateObject();

            _typeArgs = new Type[] { typeof(T0), typeof(T1), typeof(T2) ,typeof(T3)};
        }


        public override void LoadPhase1()
        {
            base.LoadPhase1();

            foreach (var saveInfo in __saveData.invocationList)
            {
                var del = Infra.Singleton.GetDelegate<UnityAction<T0, T1, T2,T3>>(saveInfo);
                __instance.AddListener(del);
            }
        }
    }
}
