
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Assets._Project.Scripts.Infrastructure
{
    public class VersionedTypeResolver
    {
        public static Dictionary<string, Type> _resolvedTypeCacheByName;
        public static Dictionary<string, Assembly> _assemblyLookUpByName;

        static VersionedTypeResolver()
        {
            _resolvedTypeCacheByName = new Dictionary<string, Type>();
            _assemblyLookUpByName = new Dictionary<string, Assembly>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!_assemblyLookUpByName.ContainsKey(assembly.GetName().Name))
                {
                    _assemblyLookUpByName[assembly.GetName().Name] = assembly;
                }
            }
        }


        public static Type Resolve(string assemblyQualifiedName)
        {
            if (string.IsNullOrEmpty(assemblyQualifiedName))
            {
                Debug.LogError("Cannot resolve type from null or empty assembly qualified name.");
                return null;
            }

            if (_resolvedTypeCacheByName.ContainsKey(assemblyQualifiedName))
                return _resolvedTypeCacheByName[assemblyQualifiedName];

            //int assemblyNameIndexFromBackward = assemblyQualifiedName.Count(c => c == '=') +1;

            //todo: extract all the assembly and type names and map them to their current versions' name

            //string assemblyName = assemblyQualifiedName.Split(',')[^assemblyNameIndexFromBackward].Trim();

            //var assembly = _assemblyLookUpByName[assemblyName];

            //Type type = assembly.GetType(assemblyQualifiedName);

            Type type = Type.GetType(assemblyQualifiedName);

            return type;
        }
    }
}
