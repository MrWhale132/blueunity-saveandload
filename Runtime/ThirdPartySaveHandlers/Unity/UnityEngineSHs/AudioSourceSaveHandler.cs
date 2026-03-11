using UnityEngine;
using Assets._Project.Scripts.UtilScripts;
using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.SaveAndLoad;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using Assets._Project.Scripts.SaveAndLoad.SavableDelegates;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace DevTest
{
    //todo: it missies some properties
    [SaveHandler(560933255331122082, "AudioSource", typeof(UnityEngine.AudioSource), generationMode: SaveHandlerGenerationMode.Manual)]
    public class AudioSourceSaveHandler : MonoSaveHandler<UnityEngine.AudioSource, AudioSourceSaveData>
    {
        public override void WriteSaveData()
        {
            base.WriteSaveData();
            __saveData.volume = __instance.volume;
            //todo
            if (__instance.resource is not null)
            {
                __saveData.time = __instance.time;
                __saveData.timeSamples = __instance.timeSamples;
            }

            __saveData.resource = GetAssetId(__instance.resource);
            __saveData.outputAudioMixerGroup = GetAssetId(__instance.outputAudioMixerGroup);
#if UNITY_EDITOR
            __saveData.gamepadSpeakerOutputType = __instance.gamepadSpeakerOutputType;
#endif
            __saveData.loop = __instance.loop;
            __saveData.ignoreListenerVolume = __instance.ignoreListenerVolume;
            __saveData.playOnAwake = __instance.playOnAwake;
            __saveData.ignoreListenerPause = __instance.ignoreListenerPause;
            __saveData.velocityUpdateMode = __instance.velocityUpdateMode;
            __saveData.panStereo = __instance.panStereo;
            __saveData.spatialBlend = __instance.spatialBlend;
            __saveData.spatialize = __instance.spatialize;
            __saveData.spatializePostEffects = __instance.spatializePostEffects;
            __saveData.reverbZoneMix = __instance.reverbZoneMix;
            __saveData.bypassEffects = __instance.bypassEffects;
            __saveData.bypassListenerEffects = __instance.bypassListenerEffects;
            __saveData.bypassReverbZones = __instance.bypassReverbZones;
            __saveData.dopplerLevel = __instance.dopplerLevel;
            __saveData.spread = __instance.spread;
            __saveData.priority = __instance.priority;
            __saveData.mute = __instance.mute;
            __saveData.minDistance = __instance.minDistance;
            __saveData.maxDistance = __instance.maxDistance;
            __saveData.rolloffMode = __instance.rolloffMode;
            __saveData.enabled = __instance.enabled;
            __saveData.hideFlags = __instance.hideFlags;
            __saveData.isPlaying = __instance.isPlaying;
        }

        public override void LoadPhase1()
        {
            base.LoadPhase1();
            __instance.volume = __saveData.volume;

            //https://discussions.unity.com/t/addressables-audioclip-works-in-play-mode-not-in-build/868360
            __instance.resource = GetAssetById(__saveData.resource, __instance.resource);
            if (__instance.resource != null)
            {
                __instance.timeSamples = __saveData.timeSamples;
                __instance.time = __saveData.time;
            }
            __instance.outputAudioMixerGroup = GetAssetById(__saveData.outputAudioMixerGroup, __instance.outputAudioMixerGroup);
#if UNITY_EDITOR
            __instance.gamepadSpeakerOutputType = __saveData.gamepadSpeakerOutputType;
#endif
            __instance.loop = __saveData.loop;
            __instance.ignoreListenerVolume = __saveData.ignoreListenerVolume;
            __instance.playOnAwake = __saveData.playOnAwake;
            __instance.ignoreListenerPause = __saveData.ignoreListenerPause;
            __instance.velocityUpdateMode = __saveData.velocityUpdateMode;
            __instance.panStereo = __saveData.panStereo;
            __instance.spatialBlend = __saveData.spatialBlend;
            __instance.spatialize = __saveData.spatialize;
            __instance.spatializePostEffects = __saveData.spatializePostEffects;
            __instance.reverbZoneMix = __saveData.reverbZoneMix;
            __instance.bypassEffects = __saveData.bypassEffects;
            __instance.bypassListenerEffects = __saveData.bypassListenerEffects;
            __instance.bypassReverbZones = __saveData.bypassReverbZones;
            __instance.dopplerLevel = __saveData.dopplerLevel;
            __instance.spread = __saveData.spread;
            __instance.priority = __saveData.priority;
            __instance.mute = __saveData.mute;
            __instance.minDistance = __saveData.minDistance;
            __instance.maxDistance = __saveData.maxDistance;
            __instance.rolloffMode = __saveData.rolloffMode;
            __instance.enabled = __saveData.enabled;
            __instance.hideFlags = __saveData.hideFlags;
            if (__instance.enabled && __instance.gameObject.activeInHierarchy && __saveData.isPlaying)
                __instance.Play();
        }

        static AudioSourceSaveHandler()
        {
            Dictionary<string, long> methodToId = new()
            {
				/// methodToId map for <see cref="AudioSource"/>
				#if UNITY_EDITOR
				{$"PlayOnGamepad(mscorlib System.Int32):mscorlib System.Boolean", 713445441621775240},
				#endif
				#if UNITY_EDITOR
				{$"DisableGamepadOutput():mscorlib System.Boolean", 634204819967220497},
				#endif
				#if UNITY_EDITOR
				{$"SetGamepadSpeakerMixLevel(mscorlib System.Int32,mscorlib System.Int32):mscorlib System.Boolean", 166482135855520259},
				#endif
				#if UNITY_EDITOR
				{$"SetGamepadSpeakerMixLevelDefault(mscorlib System.Int32):mscorlib System.Boolean", 440853548439027718},
				#endif
				#if UNITY_EDITOR
				{$"SetGamepadSpeakerRestrictedAudio(mscorlib System.Int32,mscorlib System.Boolean):mscorlib System.Boolean", 514107806444707115},
				#endif
				{$"Play():mscorlib System.Void", 720284479672871183},
                {$"Play(mscorlib System.UInt64):mscorlib System.Void", 524295610463190208},
                {$"PlayDelayed(mscorlib System.Single):mscorlib System.Void", 813062812367252469},
                {$"PlayScheduled(mscorlib System.Double):mscorlib System.Void", 142457401558156236},
                {$"PlayOneShot(UnityEngine.AudioModule UnityEngine.AudioClip):mscorlib System.Void", 946247334211162116},
                {$"PlayOneShot(UnityEngine.AudioModule UnityEngine.AudioClip,mscorlib System.Single):mscorlib System.Void", 635565126310001542},
                {$"SetScheduledStartTime(mscorlib System.Double):mscorlib System.Void", 380412219948503050},
                {$"SetScheduledEndTime(mscorlib System.Double):mscorlib System.Void", 138011191420051083},
                {$"Stop():mscorlib System.Void", 622334617781604151},
                {$"Pause():mscorlib System.Void", 427821188898741043},
                {$"UnPause():mscorlib System.Void", 870339431972580066},
                {$"SetCustomCurve(UnityEngine.AudioModule UnityEngine.AudioSourceCurveType,UnityEngine.CoreModule UnityEngine.AnimationCurve):mscorlib System.Void", 779024682803065138},
                {$"GetCustomCurve(UnityEngine.AudioModule UnityEngine.AudioSourceCurveType):UnityEngine.CoreModule UnityEngine.AnimationCurve", 109749911620867536},
                {$"GetOutputData(mscorlib System.Single[],mscorlib System.Int32):mscorlib System.Void", 474675653791645206},
                {$"GetSpectrumData(mscorlib System.Single[],mscorlib System.Int32,UnityEngine.AudioModule UnityEngine.FFTWindow):mscorlib System.Void", 783010395103553976},
                {$"SetSpatializerFloat(mscorlib System.Int32,mscorlib System.Single):mscorlib System.Boolean", 343319522097566174},
                {$"GetSpatializerFloat(mscorlib System.Int32,mscorlib System.Single&):mscorlib System.Boolean", 345427954913877985},
                {$"GetAmbisonicDecoderFloat(mscorlib System.Int32,mscorlib System.Single&):mscorlib System.Boolean", 337449830864747862},
                {$"SetAmbisonicDecoderFloat(mscorlib System.Int32,mscorlib System.Single):mscorlib System.Boolean", 191545450822057084},
            };
            Infra.Singleton.AddMethodSignatureToMethodIdMap(_typeReference, methodToId);
            Infra.Singleton.AddMethodIdToMethodMap(_typeReference, _idToMethod);
            Infra.Singleton.AddMethodIdToMethodInfoMap(_typeReference, _idToMethodInfo);
        }
        static Type _typeReference = typeof(UnityEngine.AudioSource);
        static Type _typeDefinition = typeof(UnityEngine.AudioSource);
        static Type[] _args = _typeReference.IsGenericType ? _typeReference.GetGenericArguments() : null;
        public static Func<object, Delegate> _idToMethod(long id)
        {
            Func<object, Delegate> method = id switch
            {
#if UNITY_EDITOR
                713445441621775240 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Boolean>(((UnityEngine.AudioSource)instance).PlayOnGamepad)),
#endif
#if UNITY_EDITOR
                634204819967220497 => new Func<object, Delegate>((instance) => new Func<System.Boolean>(((UnityEngine.AudioSource)instance).DisableGamepadOutput)),
#endif
#if UNITY_EDITOR
                166482135855520259 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Int32, System.Boolean>(((UnityEngine.AudioSource)instance).SetGamepadSpeakerMixLevel)),
#endif
#if UNITY_EDITOR
                440853548439027718 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Boolean>(((UnityEngine.AudioSource)instance).SetGamepadSpeakerMixLevelDefault)),
#endif
#if UNITY_EDITOR
                514107806444707115 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Boolean, System.Boolean>(((UnityEngine.AudioSource)instance).SetGamepadSpeakerRestrictedAudio)),
#endif
                720284479672871183 => new Func<object, Delegate>((instance) => new Action(((UnityEngine.AudioSource)instance).Play)),
                524295610463190208 => new Func<object, Delegate>((instance) => new Action<System.UInt64>(((UnityEngine.AudioSource)instance).Play)),
                813062812367252469 => new Func<object, Delegate>((instance) => new Action<System.Single>(((UnityEngine.AudioSource)instance).PlayDelayed)),
                142457401558156236 => new Func<object, Delegate>((instance) => new Action<System.Double>(((UnityEngine.AudioSource)instance).PlayScheduled)),
                946247334211162116 => new Func<object, Delegate>((instance) => new Action<UnityEngine.AudioClip>(((UnityEngine.AudioSource)instance).PlayOneShot)),
                635565126310001542 => new Func<object, Delegate>((instance) => new Action<UnityEngine.AudioClip, System.Single>(((UnityEngine.AudioSource)instance).PlayOneShot)),
                380412219948503050 => new Func<object, Delegate>((instance) => new Action<System.Double>(((UnityEngine.AudioSource)instance).SetScheduledStartTime)),
                138011191420051083 => new Func<object, Delegate>((instance) => new Action<System.Double>(((UnityEngine.AudioSource)instance).SetScheduledEndTime)),
                622334617781604151 => new Func<object, Delegate>((instance) => new Action(((UnityEngine.AudioSource)instance).Stop)),
                427821188898741043 => new Func<object, Delegate>((instance) => new Action(((UnityEngine.AudioSource)instance).Pause)),
                870339431972580066 => new Func<object, Delegate>((instance) => new Action(((UnityEngine.AudioSource)instance).UnPause)),
                779024682803065138 => new Func<object, Delegate>((instance) => new Action<UnityEngine.AudioSourceCurveType, UnityEngine.AnimationCurve>(((UnityEngine.AudioSource)instance).SetCustomCurve)),
                109749911620867536 => new Func<object, Delegate>((instance) => new Func<UnityEngine.AudioSourceCurveType, UnityEngine.AnimationCurve>(((UnityEngine.AudioSource)instance).GetCustomCurve)),
                474675653791645206 => new Func<object, Delegate>((instance) => new Action<System.Single[], System.Int32>(((UnityEngine.AudioSource)instance).GetOutputData)),
                783010395103553976 => new Func<object, Delegate>((instance) => new Action<System.Single[], System.Int32, UnityEngine.FFTWindow>(((UnityEngine.AudioSource)instance).GetSpectrumData)),
                343319522097566174 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Single, System.Boolean>(((UnityEngine.AudioSource)instance).SetSpatializerFloat)),
                191545450822057084 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Single, System.Boolean>(((UnityEngine.AudioSource)instance).SetAmbisonicDecoderFloat)),
                _ => Infra.Singleton.GetIdToMethodMapForType(_typeReference.BaseType)(id),
            };
            return method;
        }
        public static MethodInfo _idToMethodInfo(long id)
        {
            MethodInfo methodDef = id switch
            {
                345427954913877985 => typeof(UnityEngine.AudioSource).GetMethod(nameof(UnityEngine.AudioSource.GetSpatializerFloat), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(System.Int32), typeof(System.Single).MakeByRefType() }, null),
                337449830864747862 => typeof(UnityEngine.AudioSource).GetMethod(nameof(UnityEngine.AudioSource.GetAmbisonicDecoderFloat), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(System.Int32), typeof(System.Single).MakeByRefType() }, null),
                _ => Infra.Singleton.GetMethodInfoIdToMethodMapForType(_typeReference.BaseType)(id),
            };
            return methodDef;
        }
    }


    public class AudioSourceSaveData : MonoSaveDataBase
    {
        public System.Single volume;
        public System.Single time;
        public System.Int32 timeSamples;
        public RandomId resource;
        public RandomId outputAudioMixerGroup;
#if UNITY_EDITOR
        public UnityEngine.GamepadSpeakerOutputType gamepadSpeakerOutputType;
#endif
        public System.Boolean isPlaying;
        public System.Boolean loop;
        public System.Boolean ignoreListenerVolume;
        public System.Boolean playOnAwake;
        public System.Boolean ignoreListenerPause;
        public UnityEngine.AudioVelocityUpdateMode velocityUpdateMode;
        public System.Single panStereo;
        public System.Single spatialBlend;
        public System.Boolean spatialize;
        public System.Boolean spatializePostEffects;
        public System.Single reverbZoneMix;
        public System.Boolean bypassEffects;
        public System.Boolean bypassListenerEffects;
        public System.Boolean bypassReverbZones;
        public System.Single dopplerLevel;
        public System.Single spread;
        public System.Int32 priority;
        public System.Boolean mute;
        public System.Single minDistance;
        public System.Single maxDistance;
        public UnityEngine.AudioRolloffMode rolloffMode;
        public System.Boolean enabled;
        public UnityEngine.HideFlags hideFlags;
    }
}