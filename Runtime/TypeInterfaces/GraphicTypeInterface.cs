#pragma warning disable 8321 // Unused local function
using System;
using System.Collections.Generic;
using Theblueway.TypeBinding.Runtime.TypeInterfaceScripts;
using UnityEngine.UI;
using static Theblueway.CodeGen.Runtime.TypeUtils;

namespace Theblueway.SaveAndLoad.Runtime.TypeInterfaces
{
    [TypeInterface(280351672890743277, typeof(Graphic))]
    public class GraphicTypeInterface : TypeInterface<Graphic>
    {
        static TypeMember[] _members = new TypeMember[]
        {
            (0, GetMethodSignature<Action>(OnRebuildRequested)),
        };

        public override IEnumerable<TypeMember> GetMembers()
        {
            return _members;
        }

        static void OnRebuildRequested()
        {
#if UNITY_EDITOR
            Action _ = instance.OnRebuildRequested;
#endif
        }
    }
}
