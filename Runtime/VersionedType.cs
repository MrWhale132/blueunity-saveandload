
using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.SaveAndLoad;
using Assets._Project.Scripts.UtilScripts;
using Newtonsoft.Json;
using Theblueway.Core.Runtime.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Theblueway.SaveAndLoad.Packages.com.theblueway.saveandload.Runtime
{
    [Serializable]
    public class VersionedType
    {
        private VersionedType() { }
        public VersionedType(Type underlyingType)
        {
            this._underlyingType = underlyingType;
        }


        public static Dictionary<Type, RandomId> _registeredTypes = new();
        [JsonIgnore]
        public Type _underlyingType;

        public RandomId instanceId;
        //tyope or typedef, no generic args
        public string AssemblyQualifiedName;
        public Node root;

        public bool IsGeneric => root.IsGeneric;
        public bool IsArray => root.elementType == ElementType.Array || root.elementType == ElementType.SZArray;
        public int Version => root.version;


        public static RandomId From(Type type)
        {
            if (_registeredTypes.TryGetValue(type, out RandomId id))
            {
                return id;
            }
            else
            {
                var versionedType = new VersionedType(type);
                id = RandomId.Get();
                versionedType.instanceId = id;
                versionedType.AssemblyQualifiedName = type.CleanAssemblyQualifiedName();
                versionedType.root = GetTypeExpr(type);
                _registeredTypes.Add(type, id);
                VersionedTypeCache.Singleton.RegisteredTypes.Add(versionedType);
                return id;
            }
        }





        public static VersionedType GetById(RandomId instanceId)
        {
            return SaveAndLoadManager.Singleton.GetVersionedType(instanceId);
        }

        public static Type ResolveForCurrentHandledType(RandomId instanceId)
        {
            var instance = GetById(instanceId);
            var type = instance.ResolveForCurrentHandledType();
            return type;
        }


        public static Type ResolveForVersionedHandledType(RandomId instanceId, out bool isCurrentVersion)
        {
            var instance = GetById(instanceId);
            var type = instance.ResolveForVersionedHandledType();

            if (instance.root.HasId)
            {
                int currentVersion = SaveAndLoadManager.Singleton.GetCurrentVersionOfTypeById(instance.root.typeId);

                isCurrentVersion = instance.Version == currentVersion;
            }
            else
            {
                isCurrentVersion = true;
            }

            return type;
        }







        public Type ResolveForCurrentHandledType()
        {
            Type onHasId(Node parent)
            {
                Type type = SaveAndLoadManager.Singleton.GetHandledTypeByHandlerId(parent.typeId);
                if (type == null)
                {
                    string msg = $"Couldn't resolve type. The id that was saved with this type is not exists. \nid: {parent.typeId}, " +
                        $"type name in save data: {parent.AssemblyQualifiedName}";
                    Debug.LogError(msg);
                    throw new InvalidOperationException(msg);
                }
                else
                {
                    return type;
                }
            }

            return _ResolveType(root, onHasId);
        }


        public Type ResolveForVersionedHandledType()
        {
            Type onHasId(Node parent)
            {
                bool notFound = !SaveAndLoadManager.Singleton.HasSavedataTypeForVersionedId(parent.typeId, parent.version, out var type);
                if (notFound)
                {
                    Debug.LogWarning(JsonConvert.SerializeObject(SaveAndLoadManager.Singleton._coreService.__savedataTypesByVersionByHandlerId));
                    string msg = $"Couldn't resolve type. Did not find a suitable type for id: {parent.typeId}, version: {parent.version}.\n" +
                        $"Type name in save data: {parent.AssemblyQualifiedName}";
                    Debug.LogError(msg);
                    throw new InvalidOperationException(msg);
                }
                return type;
            }

            return _ResolveType(root, onHasId);
        }


        public Type _ResolveType(Node parent, Func<Node, Type> onHasId)
        {
            Type type;

            if (parent.HasId)
            {
                type = onHasId(parent);
            }
            else
            {
                type = VersionedTypeResolver.Resolve(parent.AssemblyQualifiedName);

                if (type == null)
                {
                    string msg = $"Couldn't resolve type. Didn't find the current version of type: {parent.AssemblyQualifiedName}";
                    Debug.LogError(msg);
                    throw new InvalidOperationException(msg);
                }
            }


            if (parent.IsGeneric)
            {
                Type[] args = new Type[parent.genericArguments.Length];


                for (int i = 0; i < args.Length; i++)
                {
                    Node child = parent.genericArguments[i];
                    Type arg = _ResolveType(child, onHasId);
                    args[i] = arg;
                }

                type = type.MakeGenericType(args);
            }


            switch (parent.elementType)
            {
                case ElementType.None:
                    break;
                case ElementType.ByRef:
                    type = type.MakeByRefType();
                    break;
                case ElementType.Pointer:
                    type = type.MakePointerType();
                    break;
                case ElementType.Array:
                    type = type.MakeArrayType(parent.arrayRank);
                    break;
                case ElementType.SZArray:
                    type = type.MakeArrayType();
                    break;
                default:
                    throw new Exception($"what is this? {parent.elementType}");
            }

            return type;
        }


        //duplicated logic id: jisdfdsf76isajhd3243
        static Node GetTypeExpr(Type type)
        {
            if (type.IsGenericParameter)
            {
                Debug.LogError($"[VersionedType] Can not get type expression of type that has generic parameter(s).\n" +
                    $"This method works with closed generic types only.\n" +
                    $"Type: {type.CleanAssemblyQualifiedName()}");
            }


            Node parent = new();

            if (type.HasElementType)
            {
                ElementType elementType = type.IsSZArray ? ElementType.SZArray : type.IsArray ? ElementType.Array
                              : type.IsByRef ? ElementType.ByRef : ElementType.Pointer;

                if (type.IsArray)
                    parent.arrayRank = type.GetArrayRank();

                parent.elementType = elementType;
                type = type.GetElementType();
            }

            if (type.IsGenericType)
            {
                var genericArgs = type.GetGenericArguments();

                parent.genericArguments = new Node[genericArgs.Length];

                for (int i = 0; i < genericArgs.Length; i++)
                {
                    Node child = GetTypeExpr(genericArgs[i]);
                    parent.genericArguments[i] = child;
                }

                type = type.GetGenericTypeDefinition();
            }


            parent.AssemblyQualifiedName = type.CleanAssemblyQualifiedName();

            if (SaveAndLoadManager.Singleton.HasTypeId(type, isStatic: false, out long typeId))
            {
                parent.typeId = typeId;
                parent.version = SaveAndLoadManager.Singleton.GetCurrentVersionOfTypeById(typeId);
            }
            else
            {
                parent.version = 1;
            }


            return parent;
        }
    }


    public class VersionedTypeCache
    {
        public static VersionedTypeCache Singleton { get; } = new();

        public List<VersionedType> RegisteredTypes { get; } = new();
    }


    [Serializable]
    public class Node
    {
        [JsonIgnore]
        public bool HasId => typeId != 0;
        [JsonIgnore]
        public bool IsGeneric => genericArguments != null;

        public long typeId;
        public int version;
        public string AssemblyQualifiedName;

        public ElementType elementType;
        public int arrayRank;

        public Node[] genericArguments;
    }

    public enum ElementType
    {
        None = 0,
        ByRef = 1,
        Pointer = 2,
        Array = 3,
        SZArray = 4,
    }
}
