using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using Assets._Project.Scripts.UtilScripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

///reason it is manual: same as <see cref="MeshRendererSaveHandler"/>
///todo: implement the same solution as for <see cref="MeshRendererSaveHandler"/>
//loading order
/*
1. Create mesh
2. Assign mesh data
3. Assign bindposes
4. Assign bone weights
5. Assign mesh to SMR
6. Assign bones[]
7. Assign rootBone
8. Restore localBounds   ← AFTER mesh + bones
*/

namespace Assets._Project.Scripts.SaveAndLoad.ThirdPartySaveHandlers.Unity.UnityEngineSHs
{
    [SaveHandler(876827195521580802, "SkinnedMeshRenderer", typeof(UnityEngine.SkinnedMeshRenderer))]
    public class SkinnedMeshRendererSaveHandler : MonoSaveHandler<UnityEngine.SkinnedMeshRenderer, SkinnedMeshRendererSaveData>
    {
        public override void WriteSaveData()
        {
            base.WriteSaveData();
            __saveData.sharedMaterials = GetObjectId(__instance.sharedMaterials);
            __saveData.materials = GetObjectId(__instance.materials);
            __saveData.quality = __instance.quality;
            __saveData.updateWhenOffscreen = __instance.updateWhenOffscreen;
            __saveData.forceMatrixRecalculationPerRender = __instance.forceMatrixRecalculationPerRender;
            __saveData.rootBone = GetObjectId(__instance.rootBone);
            __saveData.bones = GetObjectId(__instance.bones);
            __saveData.sharedMesh = GetAssetId(__instance.sharedMesh);
            __saveData.skinnedMotionVectors = __instance.skinnedMotionVectors;
            __saveData.vertexBufferTarget = __instance.vertexBufferTarget;
            __saveData.bounds.ReadFrom(__instance.bounds);
            __saveData.localBounds.ReadFrom(__instance.localBounds);
            __saveData.enabled = __instance.enabled;
            __saveData.shadowCastingMode = __instance.shadowCastingMode;
            __saveData.receiveShadows = __instance.receiveShadows;
            __saveData.forceRenderingOff = __instance.forceRenderingOff;
            __saveData.motionVectorGenerationMode = __instance.motionVectorGenerationMode;
            __saveData.lightProbeUsage = __instance.lightProbeUsage;
            __saveData.reflectionProbeUsage = __instance.reflectionProbeUsage;
            __saveData.renderingLayerMask = __instance.renderingLayerMask;
            __saveData.rendererPriority = __instance.rendererPriority;
            __saveData.rayTracingMode = __instance.rayTracingMode;
            __saveData.rayTracingAccelerationStructureBuildFlags = __instance.rayTracingAccelerationStructureBuildFlags;
            __saveData.rayTracingAccelerationStructureBuildFlagsOverride = __instance.rayTracingAccelerationStructureBuildFlagsOverride;
            __saveData.sortingLayerName = __instance.sortingLayerName;
            __saveData.sortingLayerID = __instance.sortingLayerID;
            __saveData.sortingOrder = __instance.sortingOrder;
            __saveData.allowOcclusionWhenDynamic = __instance.allowOcclusionWhenDynamic;
            __saveData.lightProbeProxyVolumeOverride = GetObjectId(__instance.lightProbeProxyVolumeOverride);
            __saveData.probeAnchor = GetObjectId(__instance.probeAnchor);
            __saveData.hideFlags = __instance.hideFlags;
        }



        public override void LoadPhase1()
        {
            base.LoadPhase1();

            __instance.sharedMaterials = GetObjectById<UnityEngine.Material[]>(__saveData.sharedMaterials);
            var materials = GetObjectById<UnityEngine.Material[]>(__saveData.materials);
            if (materials != null && materials.All(m => m != null))
                __instance.materials = GetObjectById<UnityEngine.Material[]>(__saveData.materials);
            __instance.quality = __saveData.quality;
            __instance.updateWhenOffscreen = __saveData.updateWhenOffscreen;
            __instance.forceMatrixRecalculationPerRender = __saveData.forceMatrixRecalculationPerRender;
            __instance.rootBone = GetObjectById<UnityEngine.Transform>(__saveData.rootBone);
            __instance.bones = GetObjectById<UnityEngine.Transform[]>(__saveData.bones);
            __instance.sharedMesh = GetAssetById(__saveData.sharedMesh, __instance.sharedMesh);
            __instance.skinnedMotionVectors = __saveData.skinnedMotionVectors;
            __instance.vertexBufferTarget = __saveData.vertexBufferTarget;
            //__instance.bounds = __saveData.bounds.WriteInto(__instance.bounds);
            __instance.localBounds = __saveData.localBounds.WriteInto(__instance.localBounds);
            __instance.enabled = __saveData.enabled;
            __instance.shadowCastingMode = __saveData.shadowCastingMode;
            __instance.receiveShadows = __saveData.receiveShadows;
            __instance.forceRenderingOff = __saveData.forceRenderingOff;
            __instance.motionVectorGenerationMode = __saveData.motionVectorGenerationMode;
            __instance.lightProbeUsage = __saveData.lightProbeUsage;
            __instance.reflectionProbeUsage = __saveData.reflectionProbeUsage;
            __instance.renderingLayerMask = __saveData.renderingLayerMask;
            __instance.rendererPriority = __saveData.rendererPriority;
            __instance.rayTracingMode = __saveData.rayTracingMode;
            __instance.rayTracingAccelerationStructureBuildFlags = __saveData.rayTracingAccelerationStructureBuildFlags;
            __instance.rayTracingAccelerationStructureBuildFlagsOverride = __saveData.rayTracingAccelerationStructureBuildFlagsOverride;
            __instance.sortingLayerName = __saveData.sortingLayerName;
            __instance.sortingLayerID = __saveData.sortingLayerID;
            __instance.sortingOrder = __saveData.sortingOrder;
            __instance.allowOcclusionWhenDynamic = __saveData.allowOcclusionWhenDynamic;
            __instance.lightProbeProxyVolumeOverride = GetObjectById<UnityEngine.GameObject>(__saveData.lightProbeProxyVolumeOverride);
            __instance.probeAnchor = GetObjectById<UnityEngine.Transform>(__saveData.probeAnchor);
            __instance.hideFlags = __saveData.hideFlags;

        }





        static SkinnedMeshRendererSaveHandler()
        {
            Dictionary<string, long> methodToId = new()
            {
                {"GetBlendShapeWeight(mscorlib System.Int32):mscorlib System.Single", 259530597884777207},
                {"SetBlendShapeWeight(mscorlib System.Int32,mscorlib System.Single):mscorlib System.Void", 853684846493091714},
                {"BakeMesh(UnityEngine.CoreModule UnityEngine.Mesh):mscorlib System.Void", 923522931075338294},
                {"BakeMesh(UnityEngine.CoreModule UnityEngine.Mesh,mscorlib System.Boolean):mscorlib System.Void", 956579310378206338},
                {"GetVertexBuffer():UnityEngine.CoreModule UnityEngine.GraphicsBuffer", 433065287106580776},
                {"GetPreviousVertexBuffer():UnityEngine.CoreModule UnityEngine.GraphicsBuffer", 735410265276800986}
            };
            Infra.Singleton.AddMethodSignatureToMethodIdMap(_typeReference, methodToId);
            Infra.Singleton.AddMethodIdToMethodMap(_typeReference, _idToMethod);
            Infra.Singleton.AddMethodIdToMethodInfoMap(_typeReference, _idToMethodInfo);
        }
        static Type _typeReference = typeof(UnityEngine.SkinnedMeshRenderer);
        static Type _typeDefinition = typeof(UnityEngine.SkinnedMeshRenderer);
        static Type[] _args = _typeReference.IsGenericType ? _typeReference.GetGenericArguments() : null;
        public static Func<object, Delegate> _idToMethod(long id)
        {
            Func<object, Delegate> method = id switch
            {
                259530597884777207 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Single>(((UnityEngine.SkinnedMeshRenderer)instance).GetBlendShapeWeight)),
                853684846493091714 => new Func<object, Delegate>((instance) => new Action<System.Int32, System.Single>(((UnityEngine.SkinnedMeshRenderer)instance).SetBlendShapeWeight)),
                923522931075338294 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Mesh>(((UnityEngine.SkinnedMeshRenderer)instance).BakeMesh)),
                956579310378206338 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Mesh, System.Boolean>(((UnityEngine.SkinnedMeshRenderer)instance).BakeMesh)),
                433065287106580776 => new Func<object, Delegate>((instance) => new Func<UnityEngine.GraphicsBuffer>(((UnityEngine.SkinnedMeshRenderer)instance).GetVertexBuffer)),
                735410265276800986 => new Func<object, Delegate>((instance) => new Func<UnityEngine.GraphicsBuffer>(((UnityEngine.SkinnedMeshRenderer)instance).GetPreviousVertexBuffer)),
                _ => Infra.Singleton.GetIdToMethodMapForType(_typeReference.BaseType)(id),
            };
            return method;
        }
        public static MethodInfo _idToMethodInfo(long id)
        {
            MethodInfo methodDef = id switch
            {
                _ => Infra.Singleton.GetMethodInfoIdToMethodMapForType(_typeReference.BaseType)(id),
            };
            return methodDef;
        }
    }


    public class SkinnedMeshRendererSaveData : MonoSaveDataBase
    {
        public UnityEngine.SkinQuality quality;
        public System.Boolean updateWhenOffscreen;
        public System.Boolean forceMatrixRecalculationPerRender;
        public RandomId sharedMaterials;
        public RandomId materials;
        public RandomId rootBone;
        public RandomId bones;
        public RandomId sharedMesh;
        public System.Boolean skinnedMotionVectors;
        public UnityEngine.GraphicsBuffer.Target vertexBufferTarget;
        public CustomSaveData<Bounds> bounds = CustomSaveData.CreateFor<Bounds>();
        public CustomSaveData<Bounds> localBounds = CustomSaveData.CreateFor<Bounds>();
        public System.Boolean enabled;
        public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode;
        public System.Boolean receiveShadows;
        public System.Boolean forceRenderingOff;
        public UnityEngine.MotionVectorGenerationMode motionVectorGenerationMode;
        public UnityEngine.Rendering.LightProbeUsage lightProbeUsage;
        public UnityEngine.Rendering.ReflectionProbeUsage reflectionProbeUsage;
        public System.UInt32 renderingLayerMask;
        public System.Int32 rendererPriority;
        public UnityEngine.Experimental.Rendering.RayTracingMode rayTracingMode;
        public UnityEngine.Rendering.RayTracingAccelerationStructureBuildFlags rayTracingAccelerationStructureBuildFlags;
        public System.Boolean rayTracingAccelerationStructureBuildFlagsOverride;
        public System.String sortingLayerName;
        public System.Int32 sortingLayerID;
        public System.Int32 sortingOrder;
        public System.Boolean allowOcclusionWhenDynamic;
        public RandomId lightProbeProxyVolumeOverride;
        public RandomId probeAnchor;
        public UnityEngine.HideFlags hideFlags;
    }
}