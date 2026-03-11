
using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using Theblueway.Core.Runtime.Extensions;
using System;
using System.Reflection;
using UnityEngine;

namespace Assets._Project.Scripts.SaveAndLoad.ThirdPartySaveHandlers.Dotnet.Misc
{
    [SaveHandler(id: 487945637836243288, "RuntimeType", handledType:null, RequiresManualAttributeCreation = true)]
    public class RuntimeTypeSaveHandler:UnmanagedSaveHandler<object, RuntimeTypeSaveData>
    {
        public override void Init(object instance)
        {
            base.Init(instance);

            Type type = instance as Type;

            if(type == null)
            {
                Debug.LogError("Instance is not a Type");
            }

            __saveData.AssemblyQualifiedName = type.CleanAssemblyQualifiedName();
        }

        public override void _AssignInstance()
        {
            Type type = VersionedTypeResolver.Resolve(__saveData.AssemblyQualifiedName);

            if(type == null)
            {
                Debug.LogError($"Type {__saveData.AssemblyQualifiedName} could not be resolved. Type might have been removed or renamed.");
            }

            __instance = type;
        }


        public static SaveHandlerAttribute ManualSaveHandlerAttributeCreation()
        {
            Type handledType = typeof(Type).GetType();//System.RuntimeType

            var attr = typeof(RuntimeTypeSaveHandler).GetCustomAttribute<SaveHandlerAttribute>();

            var manual = new SaveHandlerAttribute(attr.Id, handledType: handledType)
            {
                RequiresManualAttributeCreation = false
            };

            return manual;
        }
    }


    public class RuntimeTypeSaveData:SaveDataBase
    {
        public string AssemblyQualifiedName;
    }
}
