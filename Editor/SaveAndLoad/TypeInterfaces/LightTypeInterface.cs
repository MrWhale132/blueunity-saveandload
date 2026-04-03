
using System;
using System.Collections.Generic;
using Theblueway.TypeBinding.Editor.TypeInterfaceScripts;
using UnityEngine.Rendering;
using static Theblueway.CodeGen.Runtime.TypeUtils;

namespace Theblueway.SaveAndLoad.Runtime.Packages.com.blueutils.saveandload.Runtime.TypeInterfaces
{
    [TypeInterface(346879394327146699, typeof(UnityEngine.Light))]
    public class LightTypeInterface : TypeInterface<UnityEngine.Light>
    {
        static TypeMember[] _members = new TypeMember[]
            {
                #if UNITY_EDITOR
		    (0,GetMethodSignature<Action<LightEvent>>(RemoveCommandBuffers)),
                (1,GetMethodSignature<Action>(SetLightDirty)),
                (5,shadowRadius),
                (6,shadowAngle),
                (7,lightmapBakeType),  
	#endif
            };
        public override IEnumerable<TypeMember> GetMembers()
        {
            return _members;
        }
#if UNITY_EDITOR
        static string shadowRadius => nameof(UnityEngine.Light.shadowRadius);
        static string shadowAngle => nameof(UnityEngine.Light.shadowAngle);
        static string lightmapBakeType => nameof(UnityEngine.Light.lightmapBakeType);
        static void RemoveCommandBuffers(LightEvent evt) { }
        static void SetLightDirty() { } 
#endif
    }
}
