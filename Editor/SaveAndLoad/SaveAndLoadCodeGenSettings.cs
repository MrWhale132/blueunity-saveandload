using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.SaveAndLoad;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using Assets._Project.Scripts.UtilScripts.CodeGen;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Theblueway.CodeGen.Runtime;
using Theblueway.Core.Runtime.Extensions;
using Theblueway.Core.Runtime.Packages.com.blueutils.core.Runtime.ScriptResources;
using UnityEngine;

[CreateAssetMenu(fileName = "SaveAndLoadCodeGenSettings", menuName = "Scriptable Objects/SaveAndLoad/SaveAndLoadCodeGenSettings")]
public class SaveAndLoadCodeGenSettings : ScriptableObject
{
    [Tooltip("Use this to finalize and set changes. "+StringResources.ActsLikeAButton)]
    public bool _triggerSet;

    private void OnValidate()
    {
        if (_triggerSet)
        {
            _triggerSet = false;

            Set();
            Debug.Log($"{nameof(SaveAndLoadCodeGenSettings)} is set.");
        }
    }


    private void OnEnable()
    {
        Set();
    }


    public void Set()
    {
        TypeExclusionSettings = new()
        {
            DirectlyExcludedTypes = new HashSet<Type>
            {
                //typeof(Assets._Project.Scripts.UtilScripts.DataStructures.SerilaizeableKeyValuePair<,>),
            },
            IsAssignableToTypes = new List<Type>() {
                typeof(ISaveAndLoad),
                typeof(SaveDataBase),
                typeof(CustomSaveData),
                typeof(JsonConverter),
                typeof(StaticSubtitute),
            },
            AssemblyExclusionRules = new List<AssemblyExclusionRule>()
            {
                AssemblyExclusionRule.From(assembly => assembly.GetName().Name == "Assembly-CSharp-Editor"),
                AssemblyExclusionRule.From(assembly => assembly.FullName.StartsWith("UnityEditor.")),
                AssemblyExclusionRule.From(assembly => assembly.FullName.StartsWith("nunit.")),
            },
            TypeExclusionRules = new List<TypeExclusionRule>()
            {
                TypeExclusionRule.From(t => t.IsStruct() && t.IsGenericType),
                TypeExclusionRule.From(t => t.Namespace?.StartsWith(typeof(Infra).Namespace) ?? false),
                TypeExclusionRule.From(t =>
                {
                    var obsolete = t.GetCustomAttribute<ObsoleteAttribute>(false);

                    if (obsolete != null && (obsolete.IsError|| IgnoreAnyObsolete))
                        return true;

                    return false;
                }),
                TypeExclusionRule.From(t => t.Assembly.GetName().Name == "SaveAndLoad"),
            }
        };
    }

    [HideInInspector]
    public bool GenerateExampleSaveHandlersForManuallyHandledTypesToo;

    [HideInInspector]
    public ExclusionSettings TypeExclusionSettings;

    public bool IgnoreAnyObsolete;

    [Tooltip("Only for types you have access to.")]
    public bool GenerateSaveHandlersAsNestedClassesInsideHandledType;

    public bool ForceGenerateForUnchangedTypesToo;

    [Tooltip("This is where all generated code will be placed if generated as non-nested. The folder will be created if it doesn't exist.")]
    public string FolderPathForGeneratedCode = "Assets/_Project/Scripts/Generated";

    public TypeDiscoverySettings TypeDiscoverySettings;

    [Tooltip("Paths should start from Assets folder, e.g.: Assets/path/to/my/dir")]
    public List<string> PrefabFolderPaths;
}



[Serializable]
public class TypeDiscoverySettings
{
    public bool IgnoreAllDependencyTypes;

    public bool IgnoreGenericTypeArguments => IgnoreAllDependencyTypes;
    public bool IgnoreGenericTypeConstraints => IgnoreAllDependencyTypes;
    public bool IgnoreImplementOrInherit => IgnoreAllDependencyTypes;
    public bool IgnoreDirectDependencies => IgnoreAllDependencyTypes;
    public bool IgnoreBaseType => IgnoreAllDependencyTypes;
}


[Serializable]
public class ExclusionSettings
{
    public HashSet<Type> DirectlyExcludedTypes;
    public IEnumerable<Type> IsAssignableToTypes;
    public IEnumerable<AssemblyExclusionRule> AssemblyExclusionRules;
    public IEnumerable<TypeExclusionRule> TypeExclusionRules;


    public bool ShouldExclude(Type type)
    {
        if (type == null) return false;

        if (TypeExclusionRules != null && TypeExclusionRules.Any(rule => rule.ShouldExclude(type))) return true;

        if (DirectlyExcludedTypes.Contains(type.IsGenericType ? type.GetGenericTypeDefinition() : type)) return true;

        if (IsAssignableToTypes.Any(t => t.IsGenericTypeDefinition ? type.IsAssignableToGenericTypeDefinition(t) : type.IsAssignableTo(t)))
            return true;

        if (AssemblyExclusionRules != null && AssemblyExclusionRules.Any(rule => rule.ShouldExclude(type.Assembly))) return true;

        return false;
    }
}


public class TypeExclusionRule
{
    public Predicate<Type> Filter;

    public static TypeExclusionRule From(Predicate<Type> filter) => new TypeExclusionRule() { Filter = filter };

    public bool ShouldExclude(Type assembly) => Filter(assembly);
}
//ideas for future:
public class NameSpaceExclusionRule { }
public class AssemblyExclusionRule
{
    public Predicate<Assembly> Filter;

    public static AssemblyExclusionRule From(Predicate<Assembly> filter) => new AssemblyExclusionRule() { Filter = filter };

    public bool ShouldExclude(Assembly assembly) => Filter(assembly);
}