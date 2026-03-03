
using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.SaveAndLoad;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using System.Collections.Generic;
using System.Reflection;
using System;
using UnityEngine.InputSystem;
using UnityEngine;

namespace Theblueway.Saveandload.Runtime.ThirdPartySaveHandlers.Unity.InputActionSH
{

    [SaveHandler(id: 487945637836243211, "FastKeyboard", handledType: null, RequiresManualAttributeCreation = true)]
    public class FastKeyboardSaveHandler : UnmanagedSaveHandler<Keyboard, InputDeviceSaveData>
    {
        public override void Init(object instance)
        {
            base.Init(instance);

            __saveData.name = __instance.name;
            __saveData.layout = __instance.layout;
            __saveData.deviceId = __instance.deviceId;
        }

        public override void _AssignInstance()
        {
            __instance = Keyboard.current;
        }

        public static SaveHandlerAttribute ManualSaveHandlerAttributeCreation()
        {
            Type handledType = System.Type.GetType("UnityEngine.InputSystem.FastKeyboard, Unity.InputSystem");

            return SaveHandlerBase.ManualSaveHandlerAttributeCreation(handledType, typeof(FastKeyboardSaveHandler));
        }
    }


    [SaveHandler(377884624243324234, nameof(InputDevice), typeof(InputDevice), order: -97, dependsOn: new[] { typeof(InputAction) })]
    public class InputDeviceSaveHandler : UnmanagedSaveHandler<InputDevice, InputDeviceSaveData>
    {
        public override void Init(object instance)
        {
            base.Init(instance);
            __saveData.name = __instance.name;
            __saveData.layout = __instance.layout;
            __saveData.deviceId = __instance.deviceId;
        }

        public override void _AssignInstance()
        {
            if(__saveData.layout == "Keyboard")
                __instance = Keyboard.current;
            else if(__saveData.layout == "Mouse")
                __instance = Mouse.current;
        }

        static InputDeviceSaveHandler()
        {
            Dictionary<string, long> methodToId = new()
            {
                {"ReadValueFromBufferAsObject(mscorlib System.Void*,mscorlib System.Int32):mscorlib System.Object", 161240758175876005},
                {"ReadValueFromStateAsObject(mscorlib System.Void*):mscorlib System.Object", 117584848157858406},
                {"ReadValueFromStateIntoBuffer(mscorlib System.Void*,mscorlib System.Void*,mscorlib System.Int32):mscorlib System.Void", 482272731264577173},
                {"CompareValue(mscorlib System.Void*,mscorlib System.Void*):mscorlib System.Boolean", 248749260021646061},
                {"MakeCurrent():mscorlib System.Void", 373574257368077515},
                {"ExecuteCommand<TCommand>(TCommand&):mscorlib System.Int64", 864201270052850580}
            };
            Infra.Singleton.AddMethodSignatureToMethodIdMap(_typeReference, methodToId);
            Infra.Singleton.AddMethodIdToMethodMap(_typeReference, _idToMethod);
            Infra.Singleton.AddMethodIdToMethodInfoMap(_typeReference, _idToMethodInfo);
        }
        static Type _typeReference = typeof(UnityEngine.InputSystem.InputDevice);
        static Type _typeDefinition = typeof(UnityEngine.InputSystem.InputDevice);
        static Type[] _args = _typeReference.IsGenericType ? _typeReference.GetGenericArguments() : null;
        public static Func<object, Delegate> _idToMethod(long id)
        {
            Func<object, Delegate> method = id switch
            {
                373574257368077515 => new Func<object, Delegate>((instance) => new Action(((UnityEngine.InputSystem.InputDevice)instance).MakeCurrent)),
                _ => Infra.Singleton.GetIdToMethodMapForType(_typeReference.BaseType)(id),
            };
            return method;
        }
        public static MethodInfo _idToMethodInfo(long id)
        {
            MethodInfo methodDef = id switch
            {
                161240758175876005 => typeof(UnityEngine.InputSystem.InputDevice).GetMethod(nameof(UnityEngine.InputSystem.InputDevice.ReadValueFromBufferAsObject), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { Type.GetType("System.Void").MakePointerType(), typeof(System.Int32) }, null),
                117584848157858406 => typeof(UnityEngine.InputSystem.InputDevice).GetMethod(nameof(UnityEngine.InputSystem.InputDevice.ReadValueFromStateAsObject), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { Type.GetType("System.Void").MakePointerType() }, null),
                482272731264577173 => typeof(UnityEngine.InputSystem.InputDevice).GetMethod(nameof(UnityEngine.InputSystem.InputDevice.ReadValueFromStateIntoBuffer), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { Type.GetType("System.Void").MakePointerType(), Type.GetType("System.Void").MakePointerType(), typeof(System.Int32) }, null),
                248749260021646061 => typeof(UnityEngine.InputSystem.InputDevice).GetMethod(nameof(UnityEngine.InputSystem.InputDevice.CompareValue), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { Type.GetType("System.Void").MakePointerType(), Type.GetType("System.Void").MakePointerType() }, null),
                864201270052850580 => typeof(UnityEngine.InputSystem.InputDevice).GetMethod(nameof(UnityEngine.InputSystem.InputDevice.ExecuteCommand), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { Type.MakeGenericMethodParameter(0).MakeByRefType() }, null),
                _ => Infra.Singleton.GetMethodInfoIdToMethodMapForType(_typeReference.BaseType)(id),
            };
            return methodDef;
        }
    }

    public class InputDeviceSaveData : SaveDataBase
    {
        public string name;
        public string layout;
        public int deviceId;
    }
}
