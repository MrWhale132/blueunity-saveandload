//auto-generated
using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using Assets._Project.Scripts.UtilScripts;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.AI;
namespace Assets._Project.Scripts.SaveAndLoad.ThirdPartySaveHandlers.UnitySH.Navigation
{
    [SaveHandler(625698679967909860, "NavMeshAgent", typeof(UnityEngine.AI.NavMeshAgent), order: -5, dependsOn: new[] { typeof(NavMeshData) })] ///<see cref="NavMeshSurfaceSaveHandler"/>
    public class NavMeshAgentSaveHandler : MonoSaveHandler<NavMeshAgent, NavMeshAgentSaveData>
    {
        public override void WriteSaveData()
        {
            base.WriteSaveData();
            __saveData.destination = __instance.destination;
            __saveData.stoppingDistance = __instance.stoppingDistance;
            __saveData.velocity = __instance.velocity;
            __saveData.nextPosition = __instance.nextPosition;
            __saveData.baseOffset = __instance.baseOffset;
            __saveData.autoTraverseOffMeshLink = __instance.autoTraverseOffMeshLink;
            __saveData.autoBraking = __instance.autoBraking;
            __saveData.autoRepath = __instance.autoRepath;
            __saveData.isStopped = __instance.isStopped;
            __saveData.agentTypeID = __instance.agentTypeID;
            __saveData.areaMask = __instance.areaMask;
            __saveData.speed = __instance.speed;
            __saveData.angularSpeed = __instance.angularSpeed;
            __saveData.acceleration = __instance.acceleration;
            __saveData.updatePosition = __instance.updatePosition;
            __saveData.updateRotation = __instance.updateRotation;
            __saveData.updateUpAxis = __instance.updateUpAxis;
            __saveData.radius = __instance.radius;
            __saveData.height = __instance.height;
            __saveData.obstacleAvoidanceType = __instance.obstacleAvoidanceType;
            __saveData.avoidancePriority = __instance.avoidancePriority;
            __saveData.path = GetObjectId(__instance.path);
        }
        public override void LoadPhase1()
        {
            base.LoadPhase1();
            __instance.destination = __saveData.destination;
            __instance.stoppingDistance = __saveData.stoppingDistance;
            __instance.velocity = __saveData.velocity;
            __instance.nextPosition = __saveData.nextPosition;
            __instance.baseOffset = __saveData.baseOffset;
            __instance.autoTraverseOffMeshLink = __saveData.autoTraverseOffMeshLink;
            __instance.autoBraking = __saveData.autoBraking;
            __instance.autoRepath = __saveData.autoRepath;
            __instance.isStopped = __saveData.isStopped;
            __instance.agentTypeID = __saveData.agentTypeID;
            __instance.areaMask = __saveData.areaMask;
            __instance.speed = __saveData.speed;
            __instance.angularSpeed = __saveData.angularSpeed;
            __instance.acceleration = __saveData.acceleration;
            __instance.updatePosition = __saveData.updatePosition;
            __instance.updateRotation = __saveData.updateRotation;
            __instance.updateUpAxis = __saveData.updateUpAxis;
            __instance.radius = __saveData.radius;
            __instance.height = __saveData.height;
            __instance.obstacleAvoidanceType = __saveData.obstacleAvoidanceType;
            __instance.avoidancePriority = __saveData.avoidancePriority;

            __instance.SetDestination(__saveData.destination);
            __instance.isStopped = true;

            Infra.Singleton.RegisterReference(__instance.path, __saveData.path,rootObject:false);
        }
        static NavMeshAgentSaveHandler()
        {
            Dictionary<string, long> methodToId = new()
            {
                {"SetDestination(UnityEngine.CoreModule UnityEngine.Vector3):mscorlib System.Boolean", 977713436024511799},
                {"ActivateCurrentOffMeshLink(mscorlib System.Boolean):mscorlib System.Void", 275243250476841484},
                {"CompleteOffMeshLink():mscorlib System.Void", 480225164434621068},
                {"Warp(UnityEngine.CoreModule UnityEngine.Vector3):mscorlib System.Boolean", 572446206918313620},
                {"Move(UnityEngine.CoreModule UnityEngine.Vector3):mscorlib System.Void", 895908881684588300},
                {"ResetPath():mscorlib System.Void", 303491915602090116},
                {"SetPath(UnityEngine.AIModule UnityEngine.AI.NavMeshPath):mscorlib System.Boolean", 347455462143900332},
                {"FindClosestEdge(UnityEngine.AIModule UnityEngine.AI.NavMeshHit&):mscorlib System.Boolean", 730408227958436105},
                {"Raycast(UnityEngine.CoreModule UnityEngine.Vector3,UnityEngine.AIModule UnityEngine.AI.NavMeshHit&):mscorlib System.Boolean", 485702976270175747},
                {"CalculatePath(UnityEngine.CoreModule UnityEngine.Vector3,UnityEngine.AIModule UnityEngine.AI.NavMeshPath):mscorlib System.Boolean", 783812851470679595},
                {"SamplePathPosition(mscorlib System.Int32,mscorlib System.Single,UnityEngine.AIModule UnityEngine.AI.NavMeshHit&):mscorlib System.Boolean", 783743105778269942},
                {"SetAreaCost(mscorlib System.Int32,mscorlib System.Single):mscorlib System.Void", 246294159559167905},
                {"GetAreaCost(mscorlib System.Int32):mscorlib System.Single", 493071708310173635}
            };
            Infra.Singleton.__methodIdsByMethodSignaturePerType.Add(_typeReference, methodToId);
            Infra.Singleton.__methodGetterFactoryPerType.Add(_typeReference, _idToMethod);
            Infra.Singleton.__methodInfoGettersPerType.Add(_typeReference, _idToMethodInfo);
        }
        static Type _typeReference = typeof(NavMeshAgent);
        static Type _typeDefinition = typeof(UnityEngine.AI.NavMeshAgent);
        static Type[] _args = _typeReference.IsGenericType ? _typeReference.GetGenericArguments() : null;
        public static Func<object, Delegate> _idToMethod(long id)
        {
            Func<object, Delegate> method = id switch
            {
                977713436024511799 => new Func<object, Delegate>((instance) => new Func<UnityEngine.Vector3, System.Boolean>(((NavMeshAgent)instance).SetDestination)),
                275243250476841484 => new Func<object, Delegate>((instance) => new Action<System.Boolean>(((NavMeshAgent)instance).ActivateCurrentOffMeshLink)),
                480225164434621068 => new Func<object, Delegate>((instance) => new Action(((NavMeshAgent)instance).CompleteOffMeshLink)),
                572446206918313620 => new Func<object, Delegate>((instance) => new Func<UnityEngine.Vector3, System.Boolean>(((NavMeshAgent)instance).Warp)),
                895908881684588300 => new Func<object, Delegate>((instance) => new Action<UnityEngine.Vector3>(((NavMeshAgent)instance).Move)),
                303491915602090116 => new Func<object, Delegate>((instance) => new Action(((NavMeshAgent)instance).ResetPath)),
                347455462143900332 => new Func<object, Delegate>((instance) => new Func<UnityEngine.AI.NavMeshPath, System.Boolean>(((NavMeshAgent)instance).SetPath)),
                783812851470679595 => new Func<object, Delegate>((instance) => new Func<UnityEngine.Vector3, UnityEngine.AI.NavMeshPath, System.Boolean>(((NavMeshAgent)instance).CalculatePath)),
                246294159559167905 => new Func<object, Delegate>((instance) => new Action<System.Int32, System.Single>(((NavMeshAgent)instance).SetAreaCost)),
                493071708310173635 => new Func<object, Delegate>((instance) => new Func<System.Int32, System.Single>(((NavMeshAgent)instance).GetAreaCost)),
                _ => Infra.Singleton.GetIdToMethodMapForType(_typeReference.BaseType)(id),
            };
            return method;
        }
        public static MethodInfo _idToMethodInfo(long id)
        {
            MethodInfo methodDef = id switch
            {
                730408227958436105 => typeof(UnityEngine.AI.NavMeshAgent).GetMethod(nameof(NavMeshAgent.FindClosestEdge), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(UnityEngine.AI.NavMeshHit).MakeByRefType() }, null),
                485702976270175747 => typeof(UnityEngine.AI.NavMeshAgent).GetMethod(nameof(NavMeshAgent.Raycast), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(UnityEngine.Vector3), typeof(UnityEngine.AI.NavMeshHit).MakeByRefType() }, null),
                783743105778269942 => typeof(UnityEngine.AI.NavMeshAgent).GetMethod(nameof(NavMeshAgent.SamplePathPosition), BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(System.Int32), typeof(System.Single), typeof(UnityEngine.AI.NavMeshHit).MakeByRefType() }, null),
                _ => Infra.Singleton.GetMethodInfoIdToMethodMapForType(_typeReference.BaseType)(id),
            };
            return methodDef;
        }
    }
    public class NavMeshAgentSaveData : MonoSaveDataBase
    {
        public RandomId path;
        public UnityEngine.Vector3 destination;
        public System.Single stoppingDistance;
        public UnityEngine.Vector3 velocity;
        public UnityEngine.Vector3 nextPosition;
        public System.Single baseOffset;
        public System.Boolean autoTraverseOffMeshLink;
        public System.Boolean autoBraking;
        public System.Boolean autoRepath;
        public System.Boolean isStopped;
        public System.Int32 agentTypeID;
        public System.Int32 areaMask;
        public System.Single speed;
        public System.Single angularSpeed;
        public System.Single acceleration;
        public System.Boolean updatePosition;
        public System.Boolean updateRotation;
        public System.Boolean updateUpAxis;
        public System.Single radius;
        public System.Single height;
        public UnityEngine.AI.ObstacleAvoidanceType obstacleAvoidanceType;
        public System.Int32 avoidancePriority;
    }
}