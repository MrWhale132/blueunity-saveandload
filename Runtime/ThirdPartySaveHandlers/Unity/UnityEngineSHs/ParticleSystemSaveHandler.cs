using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.SaveAndLoad;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityEngine_
{
    [SaveHandler(521664889686445854, "ParticleSystem", typeof(UnityEngine.ParticleSystem), generationMode: SaveHandlerGenerationMode.Manual)]
    public class ParticleSystemSaveHandler : MonoSaveHandler<UnityEngine.ParticleSystem, ParticleSystemSaveData>
    {
        public override void WriteSaveData()
        {
            base.WriteSaveData();
            __saveData.time = __instance.time;
            __saveData.randomSeed = __instance.randomSeed;
            __saveData.useAutoRandomSeed = __instance.useAutoRandomSeed;
            __saveData.hideFlags = __instance.hideFlags;

            __saveData.isPlaying = __instance.isPlaying;
        }

        public override void LoadPhase1()
        {
            base.LoadPhase1();
            __instance.Stop();
            __instance.time = __saveData.time;
            __instance.randomSeed = __saveData.randomSeed;
            __instance.useAutoRandomSeed = __saveData.useAutoRandomSeed;
            __instance.hideFlags = __saveData.hideFlags;

            if (__saveData.isPlaying)
                __instance.Play();
        }

        static ParticleSystemSaveHandler()
        {
            Dictionary<string, long> methodToId = new()
            {
				/// methodToId map for <see cref="ParticleSystem"/>
				{$"SetParticles(UnityEngine.ParticleSystemModule UnityEngine.ParticleSystem+Particle[],mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 643837954574994590},
                {$"SetParticles(UnityEngine.ParticleSystemModule UnityEngine.ParticleSystem+Particle[],mscorlib System.Int32):mscorlib System.Void", 239986089513645880},
                {$"SetParticles(UnityEngine.ParticleSystemModule UnityEngine.ParticleSystem+Particle[]):mscorlib System.Void", 267484620941077560},
                {$"SetParticles(UnityEngine.CoreModule Unity.Collections.NativeArray<UnityEngine.ParticleSystemModule UnityEngine.ParticleSystem+Particle>,mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Void", 275775834093801332},
                {$"SetParticles(UnityEngine.CoreModule Unity.Collections.NativeArray<UnityEngine.ParticleSystemModule UnityEngine.ParticleSystem+Particle>,mscorlib System.Int32):mscorlib System.Void", 465426513743607561},
                {$"SetParticles(UnityEngine.CoreModule Unity.Collections.NativeArray<UnityEngine.ParticleSystemModule UnityEngine.ParticleSystem+Particle>):mscorlib System.Void", 548155062703974410},
                {$"GetParticles(UnityEngine.ParticleSystemModule UnityEngine.ParticleSystem+Particle[],mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Int32", 574926108138534978},
                {$"GetParticles(UnityEngine.ParticleSystemModule UnityEngine.ParticleSystem+Particle[],mscorlib System.Int32):mscorlib System.Int32", 410946371977929799},
                {$"GetParticles(UnityEngine.ParticleSystemModule UnityEngine.ParticleSystem+Particle[]):mscorlib System.Int32", 762105692426909509},
                {$"GetParticles(UnityEngine.CoreModule Unity.Collections.NativeArray<UnityEngine.ParticleSystemModule UnityEngine.ParticleSystem+Particle>,mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Int32", 953854512121973421},
                {$"GetParticles(UnityEngine.CoreModule Unity.Collections.NativeArray<UnityEngine.ParticleSystemModule UnityEngine.ParticleSystem+Particle>,mscorlib System.Int32):mscorlib System.Int32", 621534395407731984},
                {$"GetParticles(UnityEngine.CoreModule Unity.Collections.NativeArray<UnityEngine.ParticleSystemModule UnityEngine.ParticleSystem+Particle>):mscorlib System.Int32", 680868247044247798},
                {$"SetCustomParticleData(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector4>,UnityEngine.ParticleSystemModule UnityEngine.ParticleSystemCustomData):mscorlib System.Void", 831846800538908984},
                {$"GetCustomParticleData(mscorlib System.Collections.Generic.List<UnityEngine.CoreModule UnityEngine.Vector4>,UnityEngine.ParticleSystemModule UnityEngine.ParticleSystemCustomData):mscorlib System.Int32", 275228734016994304},
                {$"GetPlaybackState():UnityEngine.ParticleSystemModule UnityEngine.ParticleSystem+PlaybackState", 283395871804635361},
                {$"SetPlaybackState(UnityEngine.ParticleSystemModule UnityEngine.ParticleSystem+PlaybackState):mscorlib System.Void", 351301289454469470},
                {$"GetTrails():UnityEngine.ParticleSystemModule UnityEngine.ParticleSystem+Trails", 178945782048053204},
                {$"GetTrails(UnityEngine.ParticleSystemModule UnityEngine.ParticleSystem+Trails&):mscorlib System.Int32", 487111631744525528},
                {$"SetTrails(UnityEngine.ParticleSystemModule UnityEngine.ParticleSystem+Trails):mscorlib System.Void", 421651924149015408},
                {$"Simulate(mscorlib System.Single,mscorlib System.Boolean,mscorlib System.Boolean,mscorlib System.Boolean):mscorlib System.Void", 396731304752243487},
                {$"Simulate(mscorlib System.Single,mscorlib System.Boolean,mscorlib System.Boolean):mscorlib System.Void", 473519658262801723},
                {$"Simulate(mscorlib System.Single,mscorlib System.Boolean):mscorlib System.Void", 369035716180662703},
                {$"Simulate(mscorlib System.Single):mscorlib System.Void", 567608214194051012},
                {$"Play(mscorlib System.Boolean):mscorlib System.Void", 834780256859909825},
                {$"Play():mscorlib System.Void", 565072152203865097},
                {$"Pause(mscorlib System.Boolean):mscorlib System.Void", 978745145418423545},
                {$"Pause():mscorlib System.Void", 775163894399491284},
                {$"Stop(mscorlib System.Boolean,UnityEngine.ParticleSystemModule UnityEngine.ParticleSystemStopBehavior):mscorlib System.Void", 782093663488921931},
                {$"Stop(mscorlib System.Boolean):mscorlib System.Void", 370484084601624886},
                {$"Stop():mscorlib System.Void", 458250941637756991},
                {$"Clear(mscorlib System.Boolean):mscorlib System.Void", 429801896275314467},
                {$"Clear():mscorlib System.Void", 491954244785472345},
                {$"IsAlive(mscorlib System.Boolean):mscorlib System.Boolean", 755635476522228959},
                {$"IsAlive():mscorlib System.Boolean", 947455610179306870},
                {$"Emit(mscorlib System.Int32):mscorlib System.Void", 266658761246651230},
                {$"Emit(UnityEngine.ParticleSystemModule UnityEngine.ParticleSystem+EmitParams,mscorlib System.Int32):mscorlib System.Void", 829992420621545888},
                {$"TriggerSubEmitter(mscorlib System.Int32):mscorlib System.Void", 438969346451806604},
                {$"TriggerSubEmitter(mscorlib System.Int32,UnityEngine.ParticleSystemModule UnityEngine.ParticleSystem+Particle&):mscorlib System.Void", 117312803960077280},
                {$"TriggerSubEmitter(mscorlib System.Int32,mscorlib System.Collections.Generic.List<UnityEngine.ParticleSystemModule UnityEngine.ParticleSystem+Particle>):mscorlib System.Void", 296954507881583076},
                {$"AllocateAxisOfRotationAttribute():mscorlib System.Void", 291515648637451551},
                {$"AllocateMeshIndexAttribute():mscorlib System.Void", 815281592147600019},
                {$"AllocateCustomDataAttribute(UnityEngine.ParticleSystemModule UnityEngine.ParticleSystemCustomData):mscorlib System.Void", 968031696033993126},
            };
            Infra.Singleton.AddMethodSignatureToMethodIdMap(_typeReference, methodToId);
            Infra.Singleton.AddMethodIdToMethodMap(_typeReference, _idToMethod);
            Infra.Singleton.AddMethodIdToMethodInfoMap(_typeReference, _idToMethodInfo);
        }
        static Type _typeReference = typeof(UnityEngine.ParticleSystem);
        static Type _typeDefinition = typeof(UnityEngine.ParticleSystem);
        static Type[] _args = _typeReference.IsGenericType ? _typeReference.GetGenericArguments() : null;
        public static Func<object, Delegate> _idToMethod(long id)
        {
            Func<object, Delegate> method = id switch
            {
                643837954574994590 => new Func<object, Delegate>((instance) => new Action<ParticleSystem.Particle[], System.Int32, System.Int32>(((UnityEngine.ParticleSystem)instance).SetParticles)),
                239986089513645880 => new Func<object, Delegate>((instance) => new Action<ParticleSystem.Particle[], System.Int32>(((UnityEngine.ParticleSystem)instance).SetParticles)),
                267484620941077560 => new Func<object, Delegate>((instance) => new Action<ParticleSystem.Particle[]>(((UnityEngine.ParticleSystem)instance).SetParticles)),
                275775834093801332 => new Func<object, Delegate>((instance) => new Action<Unity.Collections.NativeArray<UnityEngine.ParticleSystem.Particle>, System.Int32, System.Int32>(((UnityEngine.ParticleSystem)instance).SetParticles)),
                465426513743607561 => new Func<object, Delegate>((instance) => new Action<Unity.Collections.NativeArray<UnityEngine.ParticleSystem.Particle>, System.Int32>(((UnityEngine.ParticleSystem)instance).SetParticles)),
                548155062703974410 => new Func<object, Delegate>((instance) => new Action<Unity.Collections.NativeArray<UnityEngine.ParticleSystem.Particle>>(((UnityEngine.ParticleSystem)instance).SetParticles)),
                574926108138534978 => new Func<object, Delegate>((instance) => new Func<ParticleSystem.Particle[], System.Int32, System.Int32, System.Int32>(((UnityEngine.ParticleSystem)instance).GetParticles)),
                410946371977929799 => new Func<object, Delegate>((instance) => new Func<ParticleSystem.Particle[], System.Int32, System.Int32>(((UnityEngine.ParticleSystem)instance).GetParticles)),
                762105692426909509 => new Func<object, Delegate>((instance) => new Func<ParticleSystem.Particle[], System.Int32>(((UnityEngine.ParticleSystem)instance).GetParticles)),
                953854512121973421 => new Func<object, Delegate>((instance) => new Func<Unity.Collections.NativeArray<UnityEngine.ParticleSystem.Particle>, System.Int32, System.Int32, System.Int32>(((UnityEngine.ParticleSystem)instance).GetParticles)),
                621534395407731984 => new Func<object, Delegate>((instance) => new Func<Unity.Collections.NativeArray<UnityEngine.ParticleSystem.Particle>, System.Int32, System.Int32>(((UnityEngine.ParticleSystem)instance).GetParticles)),
                680868247044247798 => new Func<object, Delegate>((instance) => new Func<Unity.Collections.NativeArray<UnityEngine.ParticleSystem.Particle>, System.Int32>(((UnityEngine.ParticleSystem)instance).GetParticles)),
                831846800538908984 => new Func<object, Delegate>((instance) => new Action<System.Collections.Generic.List<UnityEngine.Vector4>, UnityEngine.ParticleSystemCustomData>(((UnityEngine.ParticleSystem)instance).SetCustomParticleData)),
                275228734016994304 => new Func<object, Delegate>((instance) => new Func<System.Collections.Generic.List<UnityEngine.Vector4>, UnityEngine.ParticleSystemCustomData, System.Int32>(((UnityEngine.ParticleSystem)instance).GetCustomParticleData)),
                283395871804635361 => new Func<object, Delegate>((instance) => new Func<UnityEngine.ParticleSystem.PlaybackState>(((UnityEngine.ParticleSystem)instance).GetPlaybackState)),
                351301289454469470 => new Func<object, Delegate>((instance) => new Action<UnityEngine.ParticleSystem.PlaybackState>(((UnityEngine.ParticleSystem)instance).SetPlaybackState)),
                178945782048053204 => new Func<object, Delegate>((instance) => new Func<UnityEngine.ParticleSystem.Trails>(((UnityEngine.ParticleSystem)instance).GetTrails)),
                421651924149015408 => new Func<object, Delegate>((instance) => new Action<UnityEngine.ParticleSystem.Trails>(((UnityEngine.ParticleSystem)instance).SetTrails)),
                396731304752243487 => new Func<object, Delegate>((instance) => new Action<System.Single, System.Boolean, System.Boolean, System.Boolean>(((UnityEngine.ParticleSystem)instance).Simulate)),
                473519658262801723 => new Func<object, Delegate>((instance) => new Action<System.Single, System.Boolean, System.Boolean>(((UnityEngine.ParticleSystem)instance).Simulate)),
                369035716180662703 => new Func<object, Delegate>((instance) => new Action<System.Single, System.Boolean>(((UnityEngine.ParticleSystem)instance).Simulate)),
                567608214194051012 => new Func<object, Delegate>((instance) => new Action<System.Single>(((UnityEngine.ParticleSystem)instance).Simulate)),
                834780256859909825 => new Func<object, Delegate>((instance) => new Action<System.Boolean>(((UnityEngine.ParticleSystem)instance).Play)),
                565072152203865097 => new Func<object, Delegate>((instance) => new Action(((UnityEngine.ParticleSystem)instance).Play)),
                978745145418423545 => new Func<object, Delegate>((instance) => new Action<System.Boolean>(((UnityEngine.ParticleSystem)instance).Pause)),
                775163894399491284 => new Func<object, Delegate>((instance) => new Action(((UnityEngine.ParticleSystem)instance).Pause)),
                782093663488921931 => new Func<object, Delegate>((instance) => new Action<System.Boolean, UnityEngine.ParticleSystemStopBehavior>(((UnityEngine.ParticleSystem)instance).Stop)),
                370484084601624886 => new Func<object, Delegate>((instance) => new Action<System.Boolean>(((UnityEngine.ParticleSystem)instance).Stop)),
                458250941637756991 => new Func<object, Delegate>((instance) => new Action(((UnityEngine.ParticleSystem)instance).Stop)),
                429801896275314467 => new Func<object, Delegate>((instance) => new Action<System.Boolean>(((UnityEngine.ParticleSystem)instance).Clear)),
                491954244785472345 => new Func<object, Delegate>((instance) => new Action(((UnityEngine.ParticleSystem)instance).Clear)),
                755635476522228959 => new Func<object, Delegate>((instance) => new Func<System.Boolean, System.Boolean>(((UnityEngine.ParticleSystem)instance).IsAlive)),
                947455610179306870 => new Func<object, Delegate>((instance) => new Func<System.Boolean>(((UnityEngine.ParticleSystem)instance).IsAlive)),
                266658761246651230 => new Func<object, Delegate>((instance) => new Action<System.Int32>(((UnityEngine.ParticleSystem)instance).Emit)),
                829992420621545888 => new Func<object, Delegate>((instance) => new Action<UnityEngine.ParticleSystem.EmitParams, System.Int32>(((UnityEngine.ParticleSystem)instance).Emit)),
                438969346451806604 => new Func<object, Delegate>((instance) => new Action<System.Int32>(((UnityEngine.ParticleSystem)instance).TriggerSubEmitter)),
                296954507881583076 => new Func<object, Delegate>((instance) => new Action<System.Int32, System.Collections.Generic.List<UnityEngine.ParticleSystem.Particle>>(((UnityEngine.ParticleSystem)instance).TriggerSubEmitter)),
                291515648637451551 => new Func<object, Delegate>((instance) => new Action(((UnityEngine.ParticleSystem)instance).AllocateAxisOfRotationAttribute)),
                815281592147600019 => new Func<object, Delegate>((instance) => new Action(((UnityEngine.ParticleSystem)instance).AllocateMeshIndexAttribute)),
                968031696033993126 => new Func<object, Delegate>((instance) => new Action<UnityEngine.ParticleSystemCustomData>(((UnityEngine.ParticleSystem)instance).AllocateCustomDataAttribute)),
                _ => Infra.Singleton.GetIdToMethodMapForType(_typeReference.BaseType)(id),
            };
            return method;
        }
        public static MethodInfo _idToMethodInfo(long id)
        {
            MethodInfo methodDef = id switch
            {
                487111631744525528 => typeof(UnityEngine.ParticleSystem).GetMethod(nameof(UnityEngine.ParticleSystem.GetTrails), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(UnityEngine.ParticleSystem.Trails).MakeByRefType() }, null),
                117312803960077280 => typeof(UnityEngine.ParticleSystem).GetMethod(nameof(UnityEngine.ParticleSystem.TriggerSubEmitter), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(System.Int32), typeof(UnityEngine.ParticleSystem.Particle).MakeByRefType() }, null),
                _ => Infra.Singleton.GetMethodInfoIdToMethodMapForType(_typeReference.BaseType)(id),
            };
            return methodDef;
        }
    }


    public class ParticleSystemSaveData : MonoSaveDataBase
    {
        public System.Single time;
        public System.UInt32 randomSeed;
        public System.Boolean useAutoRandomSeed;
        public UnityEngine.HideFlags hideFlags;

        public bool isPlaying;
    }
}