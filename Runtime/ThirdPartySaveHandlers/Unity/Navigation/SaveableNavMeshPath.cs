
using Assets._Project.Scripts.SaveAndLoad;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Theblueway.SaveAndLoad.Packages.com.theblueway.saveandload.Runtime.ThirdPartySaveHandlers.Unity.Navigation
{
    public class SaveableNavMesh //todo: GC with help of infra. Or with weakref
    {
        public static Dictionary<NavMeshPath, SaveableNavMeshPath> _handledPaths = new();

        public static bool CalculatePath(Vector3 sourcePosition, Vector3 targetPosition, int areaMask, NavMeshPath path)
        {
            if (!_handledPaths.ContainsKey(path))
            {
                var saveablePath = new SaveableNavMeshPath();
                _handledPaths.Add(path, saveablePath);
            }

            var handler = _handledPaths[path];
            handler.sourcePosition = sourcePosition;
            handler.targetPosition = targetPosition;
            handler.areaMask = areaMask;

            return NavMesh.CalculatePath(sourcePosition, targetPosition, areaMask, path);
        }
    }

    public class SaveableNavMeshPath
    {
        public Vector3 sourcePosition;
        public Vector3 targetPosition;
        public int areaMask;
    }

    [SaveHandler(748707611468881488, "NavMeshPath", typeof(UnityEngine.AI.NavMeshPath), order: -4, dependsOn: new[] { typeof(NavMeshAgent) })]
    public class NavMeshPathSaveHandler : UnmanagedSaveHandler<NavMeshPath, NavMeshPathSaveData>
    {
        public override void Init(object instance)
        {
            base.Init(instance);

            if (SaveableNavMesh._handledPaths.ContainsKey(__instance))
            {
                var data = SaveableNavMesh._handledPaths[__instance];
                __saveData.sourcePosition = data.sourcePosition;
                __saveData.targetPosition = data.targetPosition;
                __saveData.areaMask = data.areaMask;
                __saveData._wasManuallyHandled = true;
            }
        }

        public override void WriteSaveData()
        {
            base.WriteSaveData();

            __saveData.status = __instance.status;
        }

        public override void _AssignInstance()
        {
            if (__saveData._wasManuallyHandled)
            {
                __instance = new NavMeshPath();

                SaveableNavMesh.CalculatePath(__saveData.sourcePosition, __saveData.targetPosition, __saveData.areaMask, __instance);
            }
            else
            {
                __instance = GetObjectById<NavMeshPath>(__saveData._ObjectId_);
            }
        }


        public override void ReleaseObject()
        {
            SaveableNavMesh._handledPaths.Remove(__instance);
            base.ReleaseObject();
        }
    }

    public class NavMeshPathSaveData : SaveDataBase
    {
        public Vector3 sourcePosition;
        public Vector3 targetPosition;
        public int areaMask;
        public NavMeshPathStatus status;
        public bool _wasManuallyHandled;
    }
}
