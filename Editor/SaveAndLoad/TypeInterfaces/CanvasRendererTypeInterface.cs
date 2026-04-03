
using System.Collections.Generic;
using Theblueway.TypeBinding.Editor.TypeInterfaceScripts;
using UnityEngine;

namespace Theblueway.SaveAndLoad.Runtime.Packages.com.blueutils.saveandload.Runtime.TypeInterfaces
{
    [TypeInterface(754042177704482358, typeof(CanvasRenderer))]
    public class CanvasRendererTypeInterface:TypeInterface<CanvasRenderer>
    {
        static TypeMember[] _members = new TypeMember[]
        {
            (0,onRequestRebuild),
        };
        public override IEnumerable<TypeMember> GetMembers()
        {
            return _members;
        }
        static string onRequestRebuild => nameof(CanvasRenderer.onRequestRebuild);
    }
}
