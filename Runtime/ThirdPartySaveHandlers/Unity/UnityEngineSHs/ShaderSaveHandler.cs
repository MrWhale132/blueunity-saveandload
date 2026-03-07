using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.SaveAndLoad;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using Assets._Project.Scripts.UtilScripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityEngine_
{
    [SaveHandler(801744359158797818, "Shader", typeof(UnityEngine.Shader), order: -9, generationMode: SaveHandlerGenerationMode.Manual)]
    public class ShaderSaveHandler : AssetSaveHandlerBase<UnityEngine.Shader, ShaderSaveData>
    {
        public static Dictionary<string, RandomId> _registeredShadersByName = new();

        public static FieldInfo m_CachedPtr= typeof(UnityEngine.Object).GetField("m_CachedPtr", BindingFlags.NonPublic | BindingFlags.Instance);
#if UNITY_EDITOR
        public static FieldInfo m_InstanceID = typeof(UnityEngine.Object).GetField("m_InstanceID", BindingFlags.NonPublic | BindingFlags.Instance); 
#endif


        public override void Init(object instance)
        {
            base.Init(instance);

            //AddRegisteredShader(__instance.name, HandledObjectId, false);
        }

        public override void WriteSaveData()
        {
            base.WriteSaveData();
            __saveData.name = __instance.name;
            __saveData.maximumLOD = __instance.maximumLOD;
            __saveData.hideFlags = __instance.hideFlags;
        }

        public override void LoadPhase1()
        {
            base.LoadPhase1();
            //no need to set name here because it already has that name
            __instance.maximumLOD = __saveData.maximumLOD;
            __instance.hideFlags = __saveData.hideFlags;
        }

        public override void _AssignInstance()
        {
            //AddRegisteredShader(__saveData.name, HandledObjectId, true);
            var target = Shader.Find(__saveData.name);
            var clone = (Shader)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Shader));

            m_CachedPtr.SetValue(clone, m_CachedPtr.GetValue(target));
#if UNITY_EDITOR
            m_InstanceID.SetValue(clone, m_InstanceID.GetValue(target)); 
#endif

            //System.Runtime.Serialization.FormatterServices.PopulateObjectMembers //maybe?

            __instance = clone;
            //#if UNITY_EDITOR
            //            __instance = UnityEngine.Object.Instantiate(__instance);
            //#endif
        }


        public override void ReleaseObject()
        {
            _registeredShadersByName.Remove(__instance.name);

            base.ReleaseObject();
        }


        public static void AddRegisteredShader(string name, RandomId objectId, bool isLoading)
        {
            if (!_registeredShadersByName.ContainsKey(name))
                _registeredShadersByName.Add(name, objectId);
            else
            {
                string message = $"ShaderSaveHandler: It as detected that multiple c# managed object references the same engine side shader object. " +
                    $"Which is a problem because c# Shader object instances can not be created on command, thus the handler of this instance will " +
                    $"not be able recreate a Shader instance on loading back. This does not addect saving but do affect loading. " +
                    $"Loading this save will not be possbile and will throw error.\n" +
                    $"objectid: {objectId}, shader name: {name}";

#if !UNITY_EDITOR
                if (isLoading)
                {
                    throw new Theblueway.SaveAndLoad.Packages.com.theblueway.saveandload.Runtime.Exceptions.InstanceCreationException(message);
                }
                else
                    Debug.LogError(message);
#endif
            }
        }



        static ShaderSaveHandler()
        {
            Dictionary<string, long> methodToId = new()
            {
				/// methodToId map for <see cref="Shader"/>
				{$"GetDependency(mscorlib System.String):UnityEngine.CoreModule UnityEngine.Shader", 216031852506197163},
                {$"GetPassCountInSubshader(mscorlib System.Int32):mscorlib System.Int32", 463975320479908623},
                {$"FindPassTagValue(mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.ShaderTagId):UnityEngine.CoreModule UnityEngine.Rendering.ShaderTagId", 710181013836145966},
                {$"FindPassTagValue(mscorlib System.Int32,mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.ShaderTagId):UnityEngine.CoreModule UnityEngine.Rendering.ShaderTagId", 323070047369756344},
                {$"FindSubshaderTagValue(mscorlib System.Int32,UnityEngine.CoreModule UnityEngine.Rendering.ShaderTagId):UnityEngine.CoreModule UnityEngine.Rendering.ShaderTagId", 328186258423394130},
                {$"GetPropertyCount():mscorlib System.Int32", 164014474308079163},
                {$"FindPropertyIndex(mscorlib System.String):mscorlib System.Int32", 813781261496561637},
                {$"GetPropertyName(mscorlib System.Int32):mscorlib System.String", 921470185934323141},
                {$"GetPropertyNameId(mscorlib System.Int32):mscorlib System.Int32", 737275298884734213},
                {$"GetPropertyType(mscorlib System.Int32):UnityEngine.CoreModule UnityEngine.Rendering.ShaderPropertyType", 234250622606332090},
                {$"GetPropertyDescription(mscorlib System.Int32):mscorlib System.String", 964617230120010990},
                {$"GetPropertyFlags(mscorlib System.Int32):UnityEngine.CoreModule UnityEngine.Rendering.ShaderPropertyFlags", 647765269154264424},
                {$"GetPropertyAttributes(mscorlib System.Int32):mscorlib System.String[]", 861791059259534134},
                {$"GetPropertyDefaultFloatValue(mscorlib System.Int32):mscorlib System.Single", 320935818585569603},
                {$"GetPropertyDefaultVectorValue(mscorlib System.Int32):UnityEngine.CoreModule UnityEngine.Vector4", 984570500951003019},
                {$"GetPropertyRangeLimits(mscorlib System.Int32):UnityEngine.CoreModule UnityEngine.Vector2", 482698346469092125},
                {$"GetPropertyDefaultIntValue(mscorlib System.Int32):mscorlib System.Int32", 964042486658244922},
                {$"GetPropertyTextureDimension(mscorlib System.Int32):UnityEngine.CoreModule UnityEngine.Rendering.TextureDimension", 128622313095988714},
                {$"GetPropertyTextureDefaultName(mscorlib System.Int32):mscorlib System.String", 534011279812407019},
                {$"FindTextureStack(mscorlib System.Int32,mscorlib System.String&,mscorlib System.Int32&):mscorlib System.Boolean", 624163849066043048},
            };
            Infra.Singleton.AddMethodSignatureToMethodIdMap(_typeReference, methodToId);
            Infra.Singleton.AddMethodIdToMethodMap(_typeReference, _idToMethod);
            Infra.Singleton.AddMethodIdToMethodInfoMap(_typeReference, _idToMethodInfo);
        }
        static Type _typeReference = typeof(UnityEngine.Shader);
        static Type _typeDefinition = typeof(UnityEngine.Shader);
        static Type[] _args = _typeReference.IsGenericType ? _typeReference.GetGenericArguments() : null;
        public static Func<object, Delegate> _idToMethod(long id)
        {
            Func<object, Delegate> method = id switch
            {
                216031852506197163 => new Func<object, Delegate>((instance) => new Func<System.String, UnityEngine.Shader>(((UnityEngine.Shader)instance).GetDependency)),
                463975320479908623 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Int32>(((UnityEngine.Shader)instance).GetPassCountInSubshader)),
                710181013836145966 => new Func<object, Delegate>((instance) => new Func<System.Int32, UnityEngine.Rendering.ShaderTagId, UnityEngine.Rendering.ShaderTagId>(((UnityEngine.Shader)instance).FindPassTagValue)),
                323070047369756344 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Int32, UnityEngine.Rendering.ShaderTagId, UnityEngine.Rendering.ShaderTagId>(((UnityEngine.Shader)instance).FindPassTagValue)),
                328186258423394130 => new Func<object, Delegate>((instance) => new Func<System.Int32, UnityEngine.Rendering.ShaderTagId, UnityEngine.Rendering.ShaderTagId>(((UnityEngine.Shader)instance).FindSubshaderTagValue)),
                164014474308079163 => new Func<object, Delegate>((instance) => new Func<System.Int32>(((UnityEngine.Shader)instance).GetPropertyCount)),
                813781261496561637 => new Func<object, Delegate>((instance) => new Func<System.String, System.Int32>(((UnityEngine.Shader)instance).FindPropertyIndex)),
                921470185934323141 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.String>(((UnityEngine.Shader)instance).GetPropertyName)),
                737275298884734213 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Int32>(((UnityEngine.Shader)instance).GetPropertyNameId)),
                234250622606332090 => new Func<object, Delegate>((instance) => new Func<System.Int32, UnityEngine.Rendering.ShaderPropertyType>(((UnityEngine.Shader)instance).GetPropertyType)),
                964617230120010990 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.String>(((UnityEngine.Shader)instance).GetPropertyDescription)),
                647765269154264424 => new Func<object, Delegate>((instance) => new Func<System.Int32, UnityEngine.Rendering.ShaderPropertyFlags>(((UnityEngine.Shader)instance).GetPropertyFlags)),
                861791059259534134 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.String[]>(((UnityEngine.Shader)instance).GetPropertyAttributes)),
                320935818585569603 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Single>(((UnityEngine.Shader)instance).GetPropertyDefaultFloatValue)),
                984570500951003019 => new Func<object, Delegate>((instance) => new Func<System.Int32, UnityEngine.Vector4>(((UnityEngine.Shader)instance).GetPropertyDefaultVectorValue)),
                482698346469092125 => new Func<object, Delegate>((instance) => new Func<System.Int32, UnityEngine.Vector2>(((UnityEngine.Shader)instance).GetPropertyRangeLimits)),
                964042486658244922 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Int32>(((UnityEngine.Shader)instance).GetPropertyDefaultIntValue)),
                128622313095988714 => new Func<object, Delegate>((instance) => new Func<System.Int32, UnityEngine.Rendering.TextureDimension>(((UnityEngine.Shader)instance).GetPropertyTextureDimension)),
                534011279812407019 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.String>(((UnityEngine.Shader)instance).GetPropertyTextureDefaultName)),
                _ => Infra.Singleton.GetIdToMethodMapForType(_typeReference.BaseType)(id),
            };
            return method;
        }
        public static MethodInfo _idToMethodInfo(long id)
        {
            MethodInfo methodDef = id switch
            {
                624163849066043048 => typeof(UnityEngine.Shader).GetMethod(nameof(UnityEngine.Shader.FindTextureStack), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(System.Int32), typeof(System.String).MakeByRefType(), typeof(System.Int32).MakeByRefType() }, null),
                _ => Infra.Singleton.GetMethodInfoIdToMethodMapForType(_typeReference.BaseType)(id),
            };
            return methodDef;
        }
    }


    public class ShaderSaveData : AssetSaveData
    {
        public string name;
        public System.Int32 maximumLOD;
        public UnityEngine.HideFlags hideFlags;
    }
}