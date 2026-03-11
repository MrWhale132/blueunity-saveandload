
using System.Collections.Generic;
using Theblueway.TypeBinding.Runtime.TypeInterfaceScripts;

namespace Theblueway.SaveAndLoad.Runtime.Packages.com.blueutils.saveandload.Runtime.TypeInterfaces
{
    [TypeInterface(955885696226906640, typeof(UnityEngine.Renderer))]
    public class RendererTypeInterface:TypeInterface<UnityEngine.Renderer>
    {
        static TypeMember[] _members = new TypeMember[]
                {
                    (0,bounds),
                    (1,localBounds),
                };
        public override IEnumerable<TypeMember> GetMembers()
        {
            return _members;
        }
        static string bounds => nameof(UnityEngine.Renderer.bounds);
        static string localBounds => nameof(UnityEngine.Renderer.localBounds);
    }
}
