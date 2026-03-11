
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using Assets._Project.Scripts.UtilScripts;
using System.Collections.Generic;
using Theblueway.Core.Runtime.Extensions;
using UnityEngine;
using Infrastructure = Assets._Project.Scripts.Infrastructure.Infra;


namespace Theblueway.SaveAndLoad.Packages.com.theblueway.saveandload.Runtime.InfraScripts
{
    public class GraphWalkingResult
    {
        public List<RandomId> memberIds;
        public List<RandomId> generatedIds;

        public List<RandomId> arrayMemberIds;
        public List<List<RandomId>> arrayElementMemberIdsPerArrayMembers;

        public GraphWalkingResult()
        {

        }
        public GraphWalkingResult(bool init)
        {
            if (init)
            {
                memberIds = new();
                generatedIds = new();
            }
        }
    }

    public class ObjectMemberGraphWalker
    {
        public class GraphWalkingContext
        {
            public List<RandomId> memberIds = new();
            public List<RandomId> generatedIds = new();

            public List<RandomId> arrayMemberIds = new();
            public List<List<RandomId>> arrayElementMemberIdsPerArrayMembers = new();
        }


        private readonly ObjectDescriptor _descriptor;

        public InitContext _saveHandlerInitContext;


        public ObjectMemberGraphWalker()
        {

        }

        public ObjectMemberGraphWalker(ObjectDescriptor descriptor)
        {
            _descriptor = descriptor;
        }


        public void Walk(object obj, InitContext initContext, out List<RandomId> memberIds, out List<RandomId> generatedIds)
        {
            var result = Walk(obj, initContext);

            memberIds = result.memberIds;
            generatedIds = result.generatedIds;
        }

        public GraphWalkingResult Walk(object obj, InitContext initContext)
        {
            _saveHandlerInitContext = initContext;
            var context = new GraphWalkingContext();
            Walk(ref obj, _descriptor, context);

            return new GraphWalkingResult
            {
                memberIds = context.memberIds,
                generatedIds = context.generatedIds,
                arrayMemberIds = context.arrayMemberIds,
                arrayElementMemberIdsPerArrayMembers = context.arrayElementMemberIdsPerArrayMembers,
            };
        }


        public void Walk(ref object obj, ObjectDescriptor descriptor, GraphWalkingContext context)
        {
            var type = obj.GetType();

            if (type == typeof(GameObject)) _Walk((GameObject)obj, descriptor, context);
            else
                Debug.LogError("Unsupported ObjectType in ObjectMemberGraphWalker: " + type.CleanAssemblyQualifiedName());
        }


        public void _Walk(GameObject instance, ObjectDescriptor descriptor, GraphWalkingContext context)
        {
            foreach (var setting in descriptor.membersSettings)
            {
                var elementIds = new List<RandomId>(); //helper for array members

                switch (setting.memberIndexV2)
                {
                    case GameObjectMembers.GameObject:
                        var id = Infrastructure.Singleton.Register(instance, rootObject: true, createSaveHandler: true, context: _saveHandlerInitContext);
                        context.memberIds.Add(setting.memberId);
                        context.generatedIds.Add(id);
                        break;

                    case GameObjectMembers.Components:

                        var components = instance.GetComponents<UnityEngine.Component>();

                        if (setting.allArrayElementMembers)
                        {
                            for (int i = 0; i < components.Length; i++)
                            {
                                var comp = components[i];
                                var compId = Infrastructure.Singleton.Register(comp, rootObject: true, createSaveHandler: true, context: _saveHandlerInitContext);
                                elementIds.Add(compId);
                            }

                            context.arrayMemberIds.Add(setting.memberId);
                            context.arrayElementMemberIdsPerArrayMembers.Add(elementIds);
                        }
                        else
                        {
                            foreach (var elementSettings in setting.arrayElementMembers)
                            {
                                var comp = components[elementSettings.arrayIndex];
                                var compId = Infrastructure.Singleton.Register(comp, rootObject: true, createSaveHandler: true, context: _saveHandlerInitContext);
                                context.memberIds.Add(elementSettings.memberId);
                                context.generatedIds.Add(compId);
                            }
                        }
                        break;

                    case GameObjectMembers.ImmediateChildren:

                        if (setting.allArrayElementMembers)
                        {
                            for (int i = 0; i < instance.transform.childCount; i++)
                            {
                                var child = instance.transform.GetChild(i);
                                var childId = Infrastructure.Singleton.Register(child.gameObject, rootObject: true, createSaveHandler: true, context: _saveHandlerInitContext);
                                elementIds.Add(childId);
                            }

                            context.arrayMemberIds.Add(setting.memberId);
                            context.arrayElementMemberIdsPerArrayMembers.Add(elementIds);
                        }
                        else
                        {
                            foreach (var elementSettings in setting.arrayElementMembers)
                            {
                                var child = instance.transform.GetChild(elementSettings.arrayIndex);
                                var childId = Infrastructure.Singleton.Register(child.gameObject, rootObject: true, createSaveHandler: true, context: _saveHandlerInitContext);
                                context.memberIds.Add(elementSettings.memberId);
                                context.generatedIds.Add(childId);

                                if (elementSettings.ShouldVisitMember)
                                    _Walk(child.gameObject, elementSettings.descriptor, context);
                            }
                        }
                        break;

                    case GameObjectMembers.AllChildrenAndComponentsRecursive:
                        var stack = new Stack<Transform>();
                        var compsBuffer = new List<Component>(16);


                        for (int i = instance.transform.childCount - 1; i >= 0; i--)
                            stack.Push(instance.transform.GetChild(i));


                        while (stack.Count > 0)
                        {
                            var current = stack.Pop();

                            // Register GameObject
                            var goId = Infrastructure.Singleton.Register(
                                current.gameObject, rootObject: true, createSaveHandler: true, context: _saveHandlerInitContext);
                            elementIds.Add(goId);

                            // Register components using a reusable buffer
                            compsBuffer.Clear();
                            current.GetComponents(compsBuffer);

                            for (int i = 0; i < compsBuffer.Count; i++)
                            {
                                var compId = Infrastructure.Singleton.Register(
                                    compsBuffer[i], rootObject: true, createSaveHandler: true, context: _saveHandlerInitContext);
                                elementIds.Add(compId);
                            }

                            // Push children
                            for (int i = current.childCount - 1; i >= 0; i--)
                                stack.Push(current.GetChild(i));
                        }

                        context.arrayMemberIds.Add(setting.memberId);
                        context.arrayElementMemberIdsPerArrayMembers.Add(elementIds);

                        break;

                    default:
                        Debug.LogError($"Unsupported memberIndex: {setting.memberIndexV2} with memberid: {setting.memberId} in {nameof(ObjectDescriptor)}: {_descriptor.name}");
                        break;
                }

                elementIds = null;
            }
        }




        public class MemberCollectionContext
        {
            public Dictionary<RandomId, object> membersById = new();
            public Dictionary<RandomId, List<object>> arrayElementMembersByArrayMemberId = new();
        }

        public class MemberCollectionResult
        {
            public Dictionary<RandomId, object> membersById;
            public Dictionary<RandomId, List<object>> arrayElementMembersByArrayMemberId;

            public MemberCollectionResult(bool init = false)
            {
                if(init)
                {
                    membersById = new();
                    arrayElementMembersByArrayMemberId = new();
                }
            }
        }



        public Dictionary<RandomId, object> CollectMembers(GameObject instance, ObjectDescriptor descriptor)
        {
            var result = CollectMembersV2(instance, descriptor);
            return result.membersById;
        }

        public MemberCollectionResult CollectMembersV2(GameObject instance, ObjectDescriptor descriptor)
        {
            var context = new MemberCollectionContext();
            _CollectMembers(instance, descriptor, context);

            var result = new MemberCollectionResult()
            {
                membersById = context.membersById,
                arrayElementMembersByArrayMemberId = context.arrayElementMembersByArrayMemberId,
            };
            return result;
        }


        public void _CollectMembers(GameObject instance, ObjectDescriptor descriptor, MemberCollectionContext context)
        {
            foreach (var settings in descriptor.membersSettings)
            {
                var arrayElements = new List<object>();

                switch (settings.memberIndexV2)
                {
                    case GameObjectMembers.GameObject: context.membersById.Add(settings.memberId, instance); break;

                    case GameObjectMembers.Components:

                        var components = instance.GetComponents<UnityEngine.Component>();

                        if (settings.allArrayElementMembers)
                        {
                            for (int i = 0; i < components.Length; i++)
                            {
                                var comp = components[i];
                                arrayElements.Add(comp);
                            }

                            context.arrayElementMembersByArrayMemberId.Add(settings.memberId, arrayElements);
                        }
                        else
                        {
                            foreach (var elementSettings in settings.arrayElementMembers)
                            {
                                var comp = components[elementSettings.arrayIndex];
                                context.membersById.Add(elementSettings.memberId, comp);
                            }
                        }
                        break;

                    case GameObjectMembers.ImmediateChildren:

                        if (settings.allArrayElementMembers)
                        {
                            for (int i = 0; i < instance.transform.childCount; i++)
                            {
                                var child = instance.transform.GetChild(i);
                                arrayElements.Add(child.gameObject);
                            }

                            context.arrayElementMembersByArrayMemberId.Add(settings.memberId, arrayElements);
                        }
                        else
                        {
                            foreach (var elementSettings in settings.arrayElementMembers)
                            {
                                var child = instance.transform.GetChild(elementSettings.arrayIndex);
                                context.membersById.Add(elementSettings.memberId, child.gameObject);

                                if (elementSettings.ShouldVisitMember)
                                {
                                    _CollectMembers(child.gameObject, elementSettings.descriptor, context);
                                }
                            }
                        }
                        break;

                    case GameObjectMembers.AllChildrenAndComponentsRecursive:

                        var stack = new Stack<Transform>();
                        var compsBuffer = new List<Component>(16);


                        for (int i = instance.transform.childCount - 1; i >= 0; i--)
                            stack.Push(instance.transform.GetChild(i));


                        while (stack.Count > 0)
                        {
                            var current = stack.Pop();

                            arrayElements.Add(current.gameObject);

                            // Register components using a reusable buffer
                            compsBuffer.Clear();
                            current.GetComponents(compsBuffer);

                            for (int i = 0; i < compsBuffer.Count; i++)
                            {
                                var comp = compsBuffer[i];
                                arrayElements.Add(comp);
                            }

                            // Push children
                            for (int i = current.childCount - 1; i >= 0; i--)
                                stack.Push(current.GetChild(i));
                        }

                        context.arrayElementMembersByArrayMemberId.Add(settings.memberId, arrayElements);

                        break;
                    default:
                        Debug.LogError($"Unsupported memberIndex: {settings.memberIndexV2} with memberid: {settings.memberId} in {nameof(ObjectDescriptor)}: {descriptor.name}");
                        break;
                }

                arrayElements = null;
            }
        }
    }
}
