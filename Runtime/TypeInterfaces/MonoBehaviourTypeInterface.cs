
using System.Collections.Generic;
using Theblueway.TypeBinding.Runtime.TypeInterfaceScripts;
using UnityEngine.UI;
using UnityEngine;
using static Theblueway.CodeGen.Runtime.TypeUtils;

namespace Theblueway.SaveAndLoad.Runtime.Packages.com.blueutils.saveandload.Runtime.TypeInterfaces
{
    [TypeInterface(843087938625977633, typeof(MonoBehaviour))]
    public class MonoBehaviourTypeInterface:TypeInterface<MonoBehaviour>
    {
        static TypeMember[] _members = new TypeMember[]
        {
            (0, runInEditMode),
        };

        public override IEnumerable<TypeMember> GetMembers()
        {
            return _members;
        }


#if UNITY_EDITOR
        static string runInEditMode => nameof(MonoBehaviour.runInEditMode);
#else
        string runInEditMode => "";
#endif

    }
}
