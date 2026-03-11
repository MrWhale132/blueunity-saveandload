using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.SaveAndLoad.SavableDelegates;
using Assets._Project.Scripts.UtilScripts;
using System;
using Theblueway.Core.Runtime.Extensions;
using Theblueway.SaveAndLoad.Packages.com.theblueway.saveandload.Runtime;
using UnityEngine;
using UnityEngine.Scripting;

namespace Assets._Project.Scripts.SaveAndLoad
{
    //T should only be a struct
    public abstract class CustomSaveData<TStruct> : CustomSaveData ///where T : struct, cant because of <see cref="Data{T}"/>
    {
        public RandomId _referencedBy;
        //todo: temporary solution as the codegenerator used the same read and write member list for classes and structs too,
        //and when a class type writes into a customsavedata is passes its HandledObjectId.
        public RandomId HandledObjectId => _referencedBy; 


        public abstract void ReadFrom(in TStruct instance);
        public abstract void WriteInto(ref TStruct instance);

        public void ReadFrom(TStruct instance)
        {
            ReadFrom(in instance);
        }

        public TStruct WriteInto(TStruct instance)
        {
            WriteInto(ref instance);
            return instance;
        }

        public void ReadFrom(TStruct instance, RandomId referencedBy)
        {
            ReadFrom(in instance, referencedBy);
        }
        public void ReadFrom(in TStruct instance, RandomId referencedBy)
        {
            _referencedBy = referencedBy;
            ReadFrom(in instance);
            _referencedBy = RandomId.Default;
        }


        public override void SlowReadFrom<T>(in T instance)
        {
            if (instance is TStruct @struct)
            {
                ReadFrom(in @struct);
            }
            else
            {
                Debug.LogError($"Cant read from raw object of type {instance.GetType().CleanAssemblyQualifiedName()} " +
                    $"into CustomSaveData of type {typeof(TStruct).CleanAssemblyQualifiedName()}");
            }
        }

        public override void SlowWriteInto<T>(ref T instance)
        {
            if (instance is TStruct @struct)
            {
                WriteInto(ref @struct);
                var copy = (T)(object)@struct;
                instance = copy;
            }
            else
            {
                Debug.LogError($"Cant write to raw object of type {instance.GetType().CleanAssemblyQualifiedName()} " +
                    $"from CustomSaveData of type {typeof(TStruct).CleanAssemblyQualifiedName()}");
            }
        }


        public override Type GetHandledType()
        {
            return typeof(TStruct);
        }



        public RandomId GetObjectId(object obj)
        {
            //temp solution for backward compatibility
            return Infra.Singleton.GetObjectId(obj, _referencedBy.IsDefault ? Infra.GlobalReferencing : _referencedBy);
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


        public T GetDelegate<T>(InvocationList list) where T : Delegate
        {
            return Infra.Singleton.GetDelegate<T>(list);
        }
    }

    [Preserve]
    public abstract class CustomSaveData
    {
        public RandomId versionedType;

        public CustomSaveData() { }

        public abstract void SlowReadFrom<T>(in T instance);
        public abstract void SlowWriteInto<T>(ref T instance);
        public abstract Type GetHandledType();



        public CustomSaveData MigrateIfNeeded(MigrationContext context, out bool didMigrate)
        {
            Type handledType = GetHandledType();
            int appVersionOfThisHandler = SaveAndLoadManager.Singleton.GetCustomSaveDataAppVersionByHandledType(handledType);

            CustomSaveData migrated = null;
            
            if (appVersionOfThisHandler == context.CurrentStep.appVersion)
            {
                didMigrate = true;
                migrated = Migrate(context);
                return migrated;
            }
            else
            {
                didMigrate = false;
                return migrated;
            }
        }

        protected virtual CustomSaveData Migrate(MigrationContext context)
        {
            Debug.LogError("Do not call this base method");
            return null;
        }

        public static CustomSaveData<T> CreateFor<T>()
        {
            var instance = SaveAndLoadManager.Singleton.CreateCustomSaveDataInstanceFor<T>();

            if (typeof(T).IsClass)
            {
                return instance;
            }

            instance.versionedType = VersionedType.From(typeof(T));
            return instance;
        }
    }
}
