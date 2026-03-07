
using Assets._Project.Scripts.UtilScripts.CodeGen;
using System;
using System.Collections.Generic;
using Theblueway.Core.Runtime.TypeInterfaceScripts;
using UnityEngine.UI;

namespace Theblueway.SaveAndLoad.Runtime.TypeInterfaces
{
    [TypeInterface(280351672890743277, typeof(Graphic))]
    public class GraphicTypeInterface : TypeInterface<Graphic>
    {
        public override IEnumerable<TypeMember> GetMembers()
        {
            yield return (0, runInEditMode);
            yield return (1, CodeGenUtils.GetMethodSignature(OnRebuildRequested));
            yield return (2, "test1");
            yield return (3, "test2");
            yield return (4, "test3");
        }


#if UNITY_EDITOR
        string runInEditMode => nameof(Graphic.runInEditMode);
#else
        string runInEditMode => "";
#endif
        public void OnRebuildRequested()
        {
#if UNITY_EDITOR
            Action _ = instance.OnRebuildRequested;
#endif
        }
    }
}
