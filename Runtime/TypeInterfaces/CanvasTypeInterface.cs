
using System.Collections.Generic;
using Theblueway.TypeBinding.Runtime.TypeInterfaceScripts;
using UnityEngine;

namespace Theblueway.SaveAndLoad.Runtime.Packages.com.blueutils.saveandload.Runtime.TypeInterfaces
{
    [TypeInterface(213860844980714877, typeof(Canvas))]
    public class CanvasTypeInterface:TypeInterface<Canvas>
    {
        static TypeMember[] _members = new TypeMember[]
        {
            (0,preWillRenderCanvases),
            (1,willRenderCanvases),
        };
        public override IEnumerable<TypeMember> GetMembers()
        {
            return _members;
        }
        static string preWillRenderCanvases => nameof(Canvas.preWillRenderCanvases);
        static string willRenderCanvases => nameof(Canvas.willRenderCanvases);
    }
}
