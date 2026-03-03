
using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.SaveAndLoad.SavableDelegates;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using Assets._Project.Scripts.SaveAndLoad;
using System;

namespace Theblueway.SaveAndLoad.Packages.com.theblueway.saveandload.Runtime.UtilsScripts.Adapters
{

    public class ActionAdapter<T>
    {
        public Action<T> Action { get; set; }


        public void Invoke(T arg)
        {
            Action?.Invoke(arg);
        }

        public static ActionAdapter<T> operator +(ActionAdapter<T> adapter, Action<T> del)
        {
            adapter.Action += del;
            return adapter;
        }
        public static ActionAdapter<T> operator -(ActionAdapter<T> adapter, Action<T> del)
        {
            adapter.Action -= del;
            return adapter;
        }
    }

    [SaveHandler(242423434529384, "ActionAdapter`1", typeof(ActionAdapter<>))]
    public class ActionAdapterSaveHandler<T> : UnmanagedSaveHandler<ActionAdapter<T>, ActionAdapterSaveData<T>>
    {
        public override void WriteSaveData()
        {
            base.WriteSaveData();

            __saveData.Action = GetInvocationList(__instance.Action);
        }

        public override void LoadPhase1()
        {
            base.LoadPhase1();

            __instance.Action = Infra.Singleton.GetDelegate<Action<T>>(__saveData.Action);
        }
    }

    public class ActionAdapterSaveData<T> : SaveDataBase
    {
        public InvocationList Action = new();
    }
}
