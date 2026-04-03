
using System.Collections.Generic;
using Theblueway.TypeBinding.Editor.TypeInterfaceScripts;
using UnityEngine;

namespace Theblueway.SaveAndLoad.Runtime.Packages.com.blueutils.saveandload.Runtime.TypeInterfaces
{
    [TypeInterface(746022504230144684, typeof(RectTransform))]
    public class RectTransformTypeInterface:TypeInterface<RectTransform>
    {
        static TypeMember[] _members = new TypeMember[]
                {
                    (0,reapplyDrivenProperties),
                };
        public override IEnumerable<TypeMember> GetMembers()
        {
            return _members;
        }
        static string reapplyDrivenProperties => nameof(RectTransform.reapplyDrivenProperties);
    }
}
