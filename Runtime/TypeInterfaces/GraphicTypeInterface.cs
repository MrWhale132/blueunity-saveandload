
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
        public override Dictionary<int, TypeMember> Setup()
        {
            return new()
            {
                {0,runInEditMode },
                {1,CodeGenUtils.GetMethodSignature(OnRebuildRequested) },
            };
        }

        string runInEditMode => nameof(Graphic.runInEditMode);
        public void OnRebuildRequested()
        {
            Action _ = instance.OnRebuildRequested;
        }
    }
}
