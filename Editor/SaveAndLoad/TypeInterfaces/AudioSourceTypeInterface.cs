
using System;
using System.Collections.Generic;
using Theblueway.TypeBinding.Editor.TypeInterfaceScripts;
using UnityEngine;
using static Theblueway.CodeGen.Runtime.TypeUtils;

namespace Theblueway.SaveAndLoad.Runtime.TypeInterfaces
{
    [TypeInterface(111945637836243288, typeof(AudioSource))]
    public class AudioSourceTypeInterface : TypeInterface<AudioSource>
    {
        static TypeMember[] _members = new TypeMember[]
        {
#if UNITY_EDITOR
		    (0,gamepadSpeakerOutputType),
            (1,GetMethodSignature<Func<GamepadSpeakerOutputType, bool>>(GamepadSpeakerSupportsOutputType)),
            (2,GetMethodSignature<Func<int, bool>>(PlayOnGamepad)),
            (3,GetMethodSignature<Func<bool>>(DisableGamepadOutput)),
            (4,GetMethodSignature<Func<int, int, bool>>(SetGamepadSpeakerMixLevel)),
            (5,GetMethodSignature<Func<int, bool>>(SetGamepadSpeakerMixLevelDefault)),
            (6,GetMethodSignature<Func<int, bool, bool>>(SetGamepadSpeakerRestrictedAudio)),
#endif
        };
        public override IEnumerable<TypeMember> GetMembers()
        {
            return _members;
        }

#if UNITY_EDITOR
        static string gamepadSpeakerOutputType => nameof(AudioSource.GamepadSpeakerSupportsOutputType);
        static bool GamepadSpeakerSupportsOutputType(GamepadSpeakerOutputType outputType) { return default; } 
        static bool PlayOnGamepad(int slot) {  return default; }
        static bool DisableGamepadOutput() { return default;}
        static bool SetGamepadSpeakerMixLevel(int slot, int mixLevel) { return default;}
        static bool SetGamepadSpeakerMixLevelDefault(int slot) { return default;}
        static bool SetGamepadSpeakerRestrictedAudio(int slot, bool restricted) { return default; }
#endif
    }
}
