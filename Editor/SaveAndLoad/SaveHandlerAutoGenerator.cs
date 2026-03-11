using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.SaveAndLoad;
using Assets._Project.Scripts.SaveAndLoad.SavableDelegates;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using Assets._Project.Scripts.UtilScripts;
using Assets._Project.Scripts.UtilScripts.CodeGen;
using Assets._Project.Scripts.UtilScripts.Extensions;
using Packages.com.theblueway.saveandload.Editor.SaveAndLoad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Theblueway.CodeGen.Runtime;
using Theblueway.Core.Runtime.Extensions;
using Theblueway.SaveAndLoad.Editor;
using UnityEngine;
using UnityEngine.Events;
using static SaveAndLoadCodeGenWindow;




[CreateAssetMenu(fileName = "SaveHandlerAutoGenerator", menuName = "Scriptable Objects/SaveAndLoad/SaveHandlerAutoGenerator")]
public class SaveHandlerAutoGenerator : ScriptableObject
{
    [NonSerialized]
    public SaveAndLoadManager.EditorService _saveAndLoadService = new();






    public class CsFileBuilder
    {
        public string FileName;
        public string GeneratedTypeText;
        public string NameSpace;
        public HashSet<string> UsingStatements = new();


        public string BuildFile(bool asNestedType = false, int offset = 0)
        {
            var namespaces = asNestedType ? "" : UsingStatements.StringJoin(_NewLine);

            string file = "//auto-generated" + _NewLine;

            if (asNestedType)
            {
                file += GeneratedTypeText;
            }
            else
            {
                file += namespaces + _NewLine +
                        _NewLine +
                        $"namespace {NameSpace}" +
                        _NewLine +
                        "{" +
                        _NewLine +
                        GeneratedTypeText +
                        _NewLine +
                        "}" +
                        "";
            }

            var indented = Indent(file, offset);
            //Debug.Log(file);
            //Debug.Log(indented);
            return indented;
        }


        public string Indent(string file, int offset)
        {
            return CodeGenUtils.Indent(file, offset);
        }




        public string _CsFileTemplate =
        "//auto-generated" + _NewLine +
        AdditionalNameSpaces + _NewLine +
        _NewLine +
        $"namespace {FileNameSpace}" +
        _NewLine +
        "{" +
        _NewLine +
        NameSpaceContent +
        _NewLine +
        "}" +
        "";
    }


    public class CodeGenerationResult
    {
        public CsFileBuilder StaticHandlerInfo;
        public CsFileBuilder StaticSaveDataInfo;
        public CsFileBuilder HandlerInfo;
        public CsFileBuilder SaveDataInfo;
    }



    public CodeGenerationResult GenerateSaveAndLoadCode(TypeReport typeReport, Session session)
    {
        CodeGenerationResult result;

        if (typeReport.ReportedType.IsClass || typeReport.ReportedType.IsInterface)
        {
            result = GenerateSaveHandler(typeReport, session);
        }
        else
            result = GenerateCustomSaveData(typeReport, session);

        return result;
    }






    public CodeGenerationResult GenerateCustomSaveData(TypeReport typeReport, Session session)
    {
        var result = GenerateStaticSaveHandler(typeReport, session);

        if (typeReport.ReportedType.IsStatic())
            return result;


        string typeDef = TypeUtils.ToTypeDefinitionText(typeReport.ReportedType);


        string generationModeEnumText;

        if (session.TypeGenerationSettingsRegistry.HasSettingsForHandledType(typeReport.ReportedType, isStatic: false, out var settings))
        {
            generationModeEnumText = nameof(SaveHandlerGenerationMode) + "." + nameof(SaveHandlerGenerationMode.Configured);
        }
        else
        {
            generationModeEnumText = nameof(SaveHandlerGenerationMode) + "." + nameof(SaveHandlerGenerationMode.FullAutomata);
        }


        RandomId id = RandomId.Get();

        string attribute = $"[{nameof(CustomSaveDataAttribute)[..^"Attribute".Length]}(" +
            $"{id}, " +
            $"{nameof(CustomSaveDataAttribute.HandledType)} = typeof({typeDef}), " +
            $"{nameof(CustomSaveDataAttribute.GenerationMode)} = {generationModeEnumText}" +
            $")]";


        string template = _CustomSaveDataTemplate
            .Replace(CustomSaveDataAttributeText, attribute);

        var templates = new List<string>()
        {
            template,
        };


        string saveDataAccessor = "";
        string instanceAccessor = "instance.";


        var generatedTypes = GenerateCommonCode(typeReport, templates, saveDataAccessor, instanceAccessor, isStatic: false, session);

        var CustomSaveDataInfo = generatedTypes[0];

        string typeName = FlattenTypeNameIfNested(typeReport.ReportedType);

        string fileName = $"{typeName}CustomSaveData";

        CustomSaveDataInfo.FileName = fileName;

        result.HandlerInfo = CustomSaveDataInfo;

        return result;
    }




    public CodeGenerationResult GenerateStaticSaveHandler(TypeReport typeReport, Session session)
    {
        var result = new CodeGenerationResult();


        bool isStatic = typeReport.ReportedType.IsStatic();

        var staticReport = isStatic ? typeReport : typeReport.StaticReport;

        if (staticReport == null)
        {
            return result;
        }


        Type staticType = staticReport.ReportedType;

        //Debug.Log(staticReport.FieldsReport.ValidFields.Count);
        string typeName = FlattenTypeNameIfNested(staticType);

        //todo
        string subtituteClassName = $"Static{typeName}Subtitute";




        //attribute

        string handledTypeText = subtituteClassName;

        if (staticType.IsGenericType)
        {
            if (session.UserSettings.GenerateSaveHandlersAsNestedClassesInsideHandledType)
            {
                handledTypeText = TypeUtils.ToTypeDefinitionText(staticType, withNameSpace: false) + "." + subtituteClassName;
            }
            else
            {
                int genParamCount = staticType.GetGenericArguments().Length;

                handledTypeText += "<" + new string(',', genParamCount - 1) + ">";
            }
        }

        string generationMode = $"{nameof(SaveHandlerGenerationMode)}.{SaveHandlerGenerationMode.FullAutomata}";

        string staticHandlerOfText = $"staticHandlerOf: typeof({TypeUtils.ToTypeDefinitionText(staticType, withNameSpace: true)})";

        var parameters = new List<string> {
        staticHandlerOfText
        };

        string additionalParamList = parameters.Count > 0 ?
            ", " + string.Join(", ", parameters) : "";


        string id = GetOrCreateSaveHandlerId(staticType, isStatic: true);


        string attribute = _SaveHandlerAttributeTemplate
            .Replace(SaveHandlerId, id)
            .Replace(HandledType, handledTypeText)
            .Replace(GenerationMode, generationMode)
            .Replace(AdditionalParamList, additionalParamList)
            ;



        var templates = new List<string>()
        {
            _StaticSaveHandlerAndSubtituteTemplate,
            _StaticSaveDataTemplate,
        };

        string saveDataAccessor = nameof(SaveHandlerGenericBase<int, SaveDataBase>.__saveData) + ".";
        string instanceAccessor = TypeUtils.ToTypeReferenceText(staticType, withNameSpace: true) + ".";


        var generatedTypes = GenerateCommonCode(staticReport, templates, saveDataAccessor, instanceAccessor, isStatic: true, session);

        var staticHandlerInfo = generatedTypes[0];
        var staticSaveDataInfo = generatedTypes[1];

        ///todo: this logic is duplicated from <see cref="GenerateCommonCode"/>
        staticHandlerInfo.FileName = $"Static{typeName}SaveHandler";
        staticSaveDataInfo.FileName = $"Static{typeName}SaveData";

        foreach (var builder in generatedTypes)
        {
            builder.GeneratedTypeText = builder.GeneratedTypeText
                .Replace(SaveHandlerAttribute, attribute)
                .Replace(BaseClassName, nameof(StaticSaveHandlerBase<StaticInfraSubtitute, StaticSaveDataBase>))
                .Replace(SaveDataBaseClassName, nameof(StaticSaveDataBase))
                ;
        }



        result.StaticHandlerInfo = staticHandlerInfo;
        result.StaticSaveDataInfo = staticSaveDataInfo;

        return result;
    }




    public string GetOrCreateSaveHandlerId(Type type, bool isStatic)
    {
        var attribute = _saveAndLoadService.GetSaveHandlerAttributeOfType_Editor(type, isStatic);

        if (attribute != null)
            return attribute.Id.ToString();

        //Debug.LogWarning($"Creating new SaveHandlerId for {(isStatic? "static":"")} {type.FullName} because it does not have SaveHandlerAttribute defined.");

        return RandomId.Get().ToString();
    }






    [NonSerialized]
    public string _UnityEventSaveHandlerTemplate =
        SaveHandlerAttribute + _NewLine +
        $"public class {GeneratedTypeName}SaveHandler : " +
        $"UnityEventSaveHandler{GenericParameterList} {GenericConstraints}" + _NewLine +
        "{ }" +
        "";




    public CodeGenerationResult GenerateSaveHandler(TypeReport typeReport, Session session)
    {
        Type typeToHandle = typeReport.ReportedType;


        //todo: make this type of exceptional generation configurable from outside
        if (typeReport.ReportedType.IsAssignableTo(typeof(UnityEventBase)))
        {
            string generatedTypeName = FlattenTypeNameIfNested(typeToHandle);
            string genericConstraints = GetgenericConstraintsText(typeToHandle);
            //string baseTypeGenericParameterList = CodeGenUtils.GetGenericParameterListText(typeToHandle.BaseType);
            string attribute = GetAttributeText(typeToHandle);

            IEnumerable<string> typeArgNames = typeToHandle.BaseType.IsGenericType ?
                typeToHandle.BaseType.GetGenericArguments().Select(arg => TypeUtils.ToTypeReferenceText(arg, withNameSpace: true))
                : Enumerable.Empty<string>();

            var baseTypeGenericParameterList = typeToHandle.BaseType.IsGenericType ? "<" + string.Join(", ", typeArgNames) + ">" : "";


            var namespaces = new List<string>
            {
                "Assets._Project.Scripts.SaveAndLoad",
                "Assets._Project.Scripts.SaveAndLoad.ThirdPartySaveHandlers.Unity.UnityEvents",
            }
            .Select(ns => $"using {ns};").ToHashSet();


            var savehandler = new CsFileBuilder
            {
                UsingStatements = namespaces,
                FileName = generatedTypeName,
                GeneratedTypeText = _UnityEventSaveHandlerTemplate
                    .Replace(SaveHandlerAttribute, attribute)
                    .Replace(GeneratedTypeName, generatedTypeName)
                    .Replace(GenericParameterList, baseTypeGenericParameterList)
                    .Replace(GenericConstraints, genericConstraints)
                    ,
            };

            var result2 = new CodeGenerationResult
            {
                HandlerInfo = savehandler,
            };

            return result2;
        }



        var result = GenerateStaticSaveHandler(typeReport, session);

        if (typeReport.ReportedType.IsStatic())
            return result;




        string saveDataBaseClassName;
        string baseClass;

        if (typeof(UnityEngine.Object).IsAssignableFrom(typeToHandle))
        {
            //Gameobject is missing because it's not auto generated, it is handled manually
            if (typeof(Component).IsAssignableFrom(typeToHandle))
            {
                saveDataBaseClassName = nameof(MonoSaveDataBase);
                baseClass = nameof(MonoSaveHandler<Component, MonoSaveDataBase>);
            }
            else if (typeof(ScriptableObject).IsAssignableFrom(typeToHandle))
            {
                saveDataBaseClassName = nameof(SaveDataBase);
                baseClass = nameof(ScriptableSaveHandlerBase<ScriptableObject, SaveDataBase>);
            }
            else ///if <see cref="IsAsset(Type)"/>
            {
                saveDataBaseClassName = nameof(AssetSaveData);
                baseClass = nameof(AssetSaveHandlerBase<UnityEngine.Object, AssetSaveData>);
            }
        }
        else
        {
            saveDataBaseClassName = nameof(SaveDataBase);
            baseClass = nameof(UnmanagedSaveHandler<object, SaveDataBase>);
        }



        //attribute

        string GetAttributeText(Type typeToHandle, bool withoutId = false)
        {
            var additionalParamList = new List<string>();


            string generationModeEnumText;

            if (session.TypeGenerationSettingsRegistry.HasSettingsForHandledType(typeReport.ReportedType, isStatic: false, out var settings))
            {
                generationModeEnumText = nameof(SaveHandlerGenerationMode) + "." + nameof(SaveHandlerGenerationMode.Configured);

                if (settings.HasAttributeGenerationSettings(out var attributeSettings))
                {
                    if (attributeSettings.loadOrder.HasValue)
                    {
                        string loadOrderText = $"order: {attributeSettings.loadOrder}";
                        additionalParamList.Add(loadOrderText);
                    }
                }
            }
            else
            {
                generationModeEnumText = nameof(SaveHandlerGenerationMode) + "." + nameof(SaveHandlerGenerationMode.FullAutomata);
            }


            string additionalParamListTest = "";

            if (additionalParamList.Count > 0)
                additionalParamListTest = ", " + string.Join(", ", additionalParamList);


            var attribute = _SaveHandlerAttributeTemplate
                .Replace(HandledType, TypeUtils.ToTypeDefinitionText(typeToHandle, withNameSpace: true))
                .Replace(GenerationMode, generationModeEnumText)
                .Replace(AdditionalParamList, additionalParamListTest)
                ;

            if (!withoutId)
            {
                var id = RandomId.New.ToString();
                attribute = attribute.Replace(SaveHandlerId, id);
            }


            return attribute;
        }



        string id = GetOrCreateSaveHandlerId(typeToHandle, isStatic: false);


        var templates = new List<string>()
        {
            _SaveHandlerTemplate,
            _SaveDataTemplate,
        };


        var saveDataAccessor = nameof(SaveHandlerGenericBase<int, SaveDataBase>.__saveData) + ".";
        var instanceAccessor = nameof(SaveHandlerGenericBase<int, SaveDataBase>.__instance) + ".";


        var generatedTypes = GenerateCommonCode(typeReport, templates, saveDataAccessor, instanceAccessor, isStatic: false, session);

        var handlerInfo = generatedTypes[0];
        var saveDataInfo = generatedTypes[1];

        string typeName = FlattenTypeNameIfNested(typeToHandle);

        ///todo: this logic is duplicated from <see cref="GenerateCommonCode"/>
        handlerInfo.FileName = $"{typeName}SaveHandler";
        saveDataInfo.FileName = $"{typeName}SaveData";

        foreach (var builder in generatedTypes)
        {
            builder.GeneratedTypeText = builder.GeneratedTypeText
                .Replace(SaveHandlerAttribute, GetAttributeText(typeToHandle, withoutId:true))
                .Replace(BaseClassName, baseClass)
                .Replace(SaveDataBaseClassName, saveDataBaseClassName)
                .Replace(SaveHandlerId, id)
                ;
        }


        result.HandlerInfo = handlerInfo;
        result.SaveDataInfo = saveDataInfo;

        return result;
    }





    public IReadOnlyList<CsFileBuilder> GenerateCommonCode(TypeReport typeReport, IEnumerable<string> csFileTemplates, string saveDataAccessor, string instanceAccessor, bool isStatic, Session session)
    {
        Type typeToHandle = typeReport.ReportedType;


        List<string> additionalNameSpaces = new()
        {
            typeToHandle.Namespace,
            typeof(RandomId).Namespace,
            typeof(Infra).Namespace,
            typeof(SaveAndLoadManager).Namespace,
            typeof(SaveHandlerBase).Namespace,
            typeof(InvocationList).Namespace,
            "System.Collections.Generic",
            "System",
            "System.Reflection",
        };





        Dictionary<MemberInfo, string> saveDataFields = new();
        Dictionary<MemberInfo, string> writeDataFields = new();
        Dictionary<MemberInfo, string> readDataFields = new();



        void GenerateDeclareWriteAndLoadField(Type type, MemberInfo memberInfo)
        {
            string saveDataField = "";
            string writeData = "";
            string readData = "";

            string fieldName = memberInfo.Name;

            bool isField = memberInfo is FieldInfo;

            var typeReference = TypeUtils.ToTypeReferenceText(type, withNameSpace: true);


            if (type.IsPrimitive || type.IsEnum || type == typeof(string) || _saveAndLoadService.HasSerializer_Editor(type)
                || type == typeof(LayerMask))// this madafaka's json converter doesnt work somewhy, so I made it an exception
            {
                saveDataField = $"public {typeReference} {fieldName};";

                writeData = $"{saveDataAccessor}{fieldName} = {instanceAccessor}{fieldName};";

                readData = $"{instanceAccessor}{fieldName} = {saveDataAccessor}{fieldName};";
            }
            else if (type.IsStruct())
            {
                if (_saveAndLoadService.HasCustomSaveData_Editor(type))
                {
                    //saveDataField = $"public {TypeUtils.ToTypeReferenceText(customSaveDataType, withNameSpace: true)} {fieldName} = new();";

                    Type customSaveDataType = typeof(CustomSaveData);

                    saveDataField = $"public {customSaveDataType.Namespace}.{nameof(CustomSaveData)}<{typeReference}> {fieldName} = " +
                        $"{nameof(CustomSaveData)}.{nameof(CustomSaveData.CreateFor)}<{typeReference}>();";

                    string readModifier = isField ? "in " : "";
                    string writeModifier = isField ? "ref " : "";


                    writeData = $"{saveDataAccessor}{fieldName}.{nameof(CustomSaveData<int>.ReadFrom)}({readModifier}{instanceAccessor}{fieldName}, {nameof(ISaveAndLoad.HandledObjectId)});";

                    if (isField)
                    {
                        readData = $"{saveDataAccessor}{fieldName}.{nameof(CustomSaveData<int>.WriteInto)}({writeModifier}{instanceAccessor}{fieldName});";
                    }
                    else
                    {
                        readData = $"{instanceAccessor}{fieldName} = {saveDataAccessor}{fieldName}.{nameof(CustomSaveData<int>.WriteInto)}({instanceAccessor}{fieldName});";
                    }
                }
            }
            else if (typeof(Delegate).IsAssignableFrom(type))
            {

                saveDataField = $"public {nameof(InvocationList)} {fieldName} = new();";

                writeData = $"{saveDataAccessor}{fieldName} = {nameof(SaveHandlerBase.GetInvocationList)}({instanceAccessor}{fieldName});";

                readData = $"{instanceAccessor}{fieldName} = {nameof(SaveHandlerBase.GetDelegate)}<{typeReference}>({saveDataAccessor}{fieldName});";
            }
            else if (type.IsGenericParameter)
            {
                saveDataField = $"public {nameof(Data)}<{type.Name}> {fieldName};";
                //writeData = $"{saveDataAccessor}{fieldName}.Value = {instanceAccessor}{fieldName};";
                //readData = $"{instanceAccessor}{fieldName} = {saveDataAccessor}{fieldName}.Value;";

                writeData = $"{saveDataAccessor}{fieldName}.{nameof(Data<int>.Set)}({instanceAccessor}{fieldName}, {nameof(ISaveAndLoad.HandledObjectId)});";
            }
            else //ref type: class, array
            {
                saveDataField = $"public {nameof(RandomId)} {fieldName};";

                //if (IsAsset(type))
                //{
                //    writeData = $"{saveDataAccessor}{fieldName} = {nameof(SaveHandlerBase.GetAssetId)}({instanceAccessor}{fieldName});";
                //    readData = $"{instanceAccessor}{fieldName} = {nameof(SaveHandlerBase.GetAssetById)}({saveDataAccessor}{fieldName}, {instanceAccessor}{fieldName});";
                //}
                //else
                {
                    writeData = $"{saveDataAccessor}{fieldName} = {nameof(SaveHandlerBase.GetObjectId)}({instanceAccessor}{fieldName});";
                    readData = $"{instanceAccessor}{fieldName} = {nameof(SaveHandlerBase.GetObjectById)}<{typeReference}>({saveDataAccessor}{fieldName});";
                }
            }

            saveDataFields.Add(memberInfo, saveDataField);
            writeDataFields.Add(memberInfo, writeData);
            readDataFields.Add(memberInfo, readData);
        }



        //fields and props

        foreach (FieldInfoReport fieldReport in typeReport.FieldsReport.ValidFields)
            GenerateDeclareWriteAndLoadField(fieldReport.FieldInfo.FieldType, fieldReport.FieldInfo);

        foreach (var property in typeReport.Properties)
            GenerateDeclareWriteAndLoadField(property.PropertyType, property);



        //events

        string targetTypeReference = TypeUtils.ToTypeReferenceText(typeToHandle, withNameSpace: true);
        string targetTypeDefinition = TypeUtils.ToTypeDefinitionText(typeToHandle, withNameSpace: true);


        foreach (var evt in typeReport.Events)
        {
            string fieldName = evt.Name;
            string eventTypeTypeReference = TypeUtils.ToTypeReferenceText(evt.EventHandlerType, withNameSpace: true);

            string saveDataField = $"public {nameof(InvocationList)} {fieldName} = new();";


            var getInvocationList = nameof(SaveHandlerGenericBase<int, SaveDataBase>.GetInvocationList);
            var getDelegate = nameof(SaveHandlerGenericBase<int, SaveDataBase>.GetDelegate);


            string writeData = $"{saveDataAccessor}{fieldName} = {getInvocationList}(nameof({targetTypeReference}.{evt.Name}));";

            string readData = $"var {fieldName}Del = {getDelegate}<{eventTypeTypeReference}>({saveDataAccessor}{fieldName});" + _NewLine;
            readData += $"if({fieldName}Del != null)" + _NewLine;
            readData += $"{instanceAccessor}{fieldName} += {fieldName}Del;";


            saveDataFields.Add(evt, saveDataField);
            writeDataFields.Add(evt, writeData);
            readDataFields.Add(evt, readData);
        }




        //method registration


        var dictEntries = new Dictionary<MethodInfo, string>();

        var idToMethodLookUpLines = new Dictionary<MethodInfo, string>();
        var idToGenMethodDefLookUpLines = new Dictionary<MethodInfo, string>();

        var existingMethodToIdMap = new Dictionary<string, long>();


        var typeName = typeToHandle.Name;

        if (typeToHandle.IsGenericType)
        {
            typeName = typeName.Substring(0, typeName.IndexOf('`'));
            typeName += "{" + new string(',', typeToHandle.GetGenericArguments().Length - 1) + "}";
        }


        string text2 = isStatic ? "static " : "";

        string tag = $"/// methodToId map for {text2}<see cref=\"{typeName}\"/>";



        var handlerType = _saveAndLoadService.GetSaveHandlerTypeFrom(typeToHandle, isStatic);

        if (handlerType != null)
        {
            var savehandlerFilePath = session.GetSourceFilePath(handlerType);

            var text = System.IO.File.ReadAllText(savehandlerFilePath);


            int tagStart = text.IndexOf(tag);

            //todo: only for backward comp, remove later
            if (tagStart != -1)
            {
                int dictionaryEntriesStart = tagStart + tag.Length + 1;

                int end = text.IndexOf("};", dictionaryEntriesStart);

                string section = text.Substring(dictionaryEntriesStart, end - dictionaryEntriesStart);

                var entries = section.Split(_NewLine, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < entries.Length - 1; i++)  //last line is the };
                {
                    var line = entries[i];

                    if (line.Contains("#if") || line.Contains("#endif")) continue;

                    int keyvalSep = line.IndexOfNth(',', -2);
                    var start = line.IndexOf('\"') + 1;
                    var length = keyvalSep - start - 1;
                    //debug
                    if (length < 0)
                    {
                        Debug.LogError(typeToHandle.CleanAssemblyQualifiedName() + " " + isStatic + "\n" + line);
                    }
                    var key = line.Substring(start, keyvalSep - start - 1);
                    var val = line.Substring(keyvalSep + 2, line.Length - keyvalSep - 4);

                    if (long.TryParse(val, out var existingId))
                    {
                        existingMethodToIdMap.Add(key, existingId);
                    }
                    else
                    {
                        Debug.LogError($"Failed to parse method id: {val} for method: {key} in existing SaveHandler: {handlerType.FullName} at path: {savehandlerFilePath}");
                    }
                }
            }
        }


        foreach (var method in typeReport.Methods)
        {
            string methodSignature = TypeUtils.GetMethodSignature(method, useNameOfOperator: false);

            string id;

            if (existingMethodToIdMap.TryGetValue(methodSignature, out var randomId))
            {
                id = randomId.ToString();
                //Debug.Log(id);
            }
            else
                id = RandomId.Get().ToString();


            string entry = $"{{$\"{methodSignature}\", {id}}},";

            dictEntries.Add(method, entry);


            Func<ParameterInfo, bool> canNotBeUsedAsGenericParameter = (p) => p.ParameterType.IsByRef || p.ParameterType.IsPointer || p.ParameterType.IsByRefLike;

            if (method.IsGenericMethod || method.GetParameters().Any(canNotBeUsedAsGenericParameter) || canNotBeUsedAsGenericParameter(method.ReturnParameter))
            {
                string line = $"{id} => {CodeGenUtils.GenerateGetMethodCode(method)},";
                idToGenMethodDefLookUpLines.Add(method, line);
            }
            else
            {
                string delegateType = "Action";

                try
                {
                    var argNames = method.GetParameters().Select(p => TypeUtils.ToTypeReferenceText(p.ParameterType, withNameSpace: true)).ToList();

                    if (method.ReturnType != typeof(void))
                    {
                        delegateType = "Func";

                        argNames.Add(TypeUtils.ToTypeReferenceText(method.ReturnType, withNameSpace: true));
                    }

                    string argListText = argNames.Count > 0 ?
                        "<" + string.Join(", ", argNames) + ">" : "";


                    string targetReference = isStatic ? targetTypeReference : $"(({targetTypeReference})instance)";
                    string line = $"{id} => new Func<object, Delegate>((instance) => new {delegateType}{argListText}({targetReference}.{method.Name})),";

                    idToMethodLookUpLines.Add(method, line);

                }
                catch
                {
                    Debug.Log(typeReport.ReportedType.FullName + " " + method.Name);
                    Debug.Log(method.IsGenericMethod);
                    foreach (var p in method.GetParameters())
                    {
                        Debug.Log(p.ParameterType.CleanAssemblyQualifiedName() + " " + canNotBeUsedAsGenericParameter(p));
                    }
                    Debug.Log(method.ReturnParameter.ParameterType.FullName + " " + canNotBeUsedAsGenericParameter(method.ReturnParameter));
                    throw;
                }
            }
        }


        //idToMethodLookUpLines.Add("", $"_ => {nameof(Infra)}.{nameof(Infra.Singleton)}.{nameof(Infra.Singleton.GetIdToMethodMapForType)}" +
        //                                $"(_typeReference.BaseType)(id),");

        //idToGenMethodDefLookUpLines.Add("", $"_ => {nameof(Infra)}.{nameof(Infra.Singleton)}.{nameof(Infra.Singleton.GetMethodInfoIdToMethodMapForType)}" +
                                                //$"(_typeReference.BaseType)(id),");





        string Wrap(string code, string directive)
        {
            string wrapped = $"#if {directive}{_NewLine}{code}{_NewLine}#endif";
            return wrapped;
        }


        if (session.TypeGenerationSettingsRegistry.HasSettingsForHandledType(typeToHandle, isStatic, out var settings))
        {
            foreach (var member in saveDataFields.Keys.ToList())
            {
                if (settings.HasDirective(member, out string directive))
                {
                    saveDataFields[member] = Wrap(saveDataFields[member], directive);
                    writeDataFields[member] = Wrap(writeDataFields[member], directive);
                    readDataFields[member] = Wrap(readDataFields[member], directive);
                }
            }


            foreach (var method in dictEntries.Keys.ToList())
            {
                if (settings.HasDirective(method, out string directive))
                {
                    dictEntries[method] = Wrap(dictEntries[method], directive);
                    if (idToMethodLookUpLines.ContainsKey(method))
                        idToMethodLookUpLines[method] = Wrap(idToMethodLookUpLines[method], directive);
                    if (idToGenMethodDefLookUpLines.ContainsKey(method))
                        idToGenMethodDefLookUpLines[method] = Wrap(idToGenMethodDefLookUpLines[method], directive);
                }

                //if (settings.HasInclusionModeFor(methodId, out var inclusionMode))
                //{
                //    if (inclusionMode is MemberInclusionMode.Exclude)
                //    {
                //        dictEntries.Remove(methodId);
                //        if (idToMethodLookUpLines.ContainsKey(methodId)) idToMethodLookUpLines.Remove(methodId);
                //        if (idToGenMethodDefLookUpLines.ContainsKey(methodId)) idToGenMethodDefLookUpLines.Remove(methodId);
                //    }
                //}
            }
        }



        string fieldList = string.Join(_NewLine, saveDataFields.Values);

        string writingFields = string.Join(_NewLine, writeDataFields.Values);

        string readingFields = string.Join(_NewLine, readDataFields.Values);



        string methodSignaturesToMethodIds = tag + _NewLine + string.Join(_NewLine, dictEntries.Values);

        if (dictEntries.Count == 0) methodSignaturesToMethodIds = tag;

        var helper1 = idToMethodLookUpLines.Values.ToList();
        helper1.Add($"_ => {nameof(Infra)}.{nameof(Infra.Singleton)}.{nameof(Infra.Singleton.GetIdToMethodMapForType)}" +
                                        $"(_typeReference.BaseType)(id),");

        var helper2 = idToGenMethodDefLookUpLines.Values.ToList();
        helper2.Add($"_ => {nameof(Infra)}.{nameof(Infra.Singleton)}.{nameof(Infra.Singleton.GetMethodInfoIdToMethodMapForType)}" +
                                                $"(_typeReference.BaseType)(id),");


        string idToMethodLookUp = string.Join(_NewLine, helper1);
        string idToGenMethodDefLookUp = string.Join(_NewLine, helper2);





        var genericParameterList = session.UserSettings.GenerateSaveHandlersAsNestedClassesInsideHandledType ?
                                    "" : CodeGenUtils.GetGenericParameterListText(typeToHandle);


        string genericConstraints = session.UserSettings.GenerateSaveHandlersAsNestedClassesInsideHandledType ?
                                    "" : GetgenericConstraintsText(typeToHandle);


        additionalNameSpaces = additionalNameSpaces.Where(ns => ns != null).Distinct()
            .Select(ns => $"using {ns};").ToList();



        IEnumerable<string> csFiles = csFileTemplates.Select(template =>
            template
            .Replace(GeneratedTypeName, FlattenTypeNameIfNested(typeToHandle))
            .Replace(TargetTypeReference, targetTypeReference)
            .Replace(TargetTypeDefinition, targetTypeDefinition)
            .Replace(GenericParameterList, genericParameterList)
            .Replace(GenericConstraints, genericConstraints)
            .Replace(FieldList, fieldList)
            .Replace(WritingFields, writingFields)
            .Replace(ReadingFields, readingFields)
            .Replace(MethodSignaturesToMethodIds, methodSignaturesToMethodIds)
            .Replace(IdToMethodLookUp, idToMethodLookUp)
            .Replace(IdToGenMethodDefLookUp, idToGenMethodDefLookUp)

        );

        var builders = csFiles.Select(csFile => new CsFileBuilder
        {
            UsingStatements = additionalNameSpaces.ToHashSet(),
            GeneratedTypeText = csFile,
        }).ToList();


        //Debug.LogWarning(typeToHandle.FullName);
        //foreach(var builder in builders)
        //{
        //    Debug.Log(builder.GeneratedTypeText);
        //}


        return builders;
    }



    public string FlattenTypeNameIfNested(Type type)
    {
        return CodeGenUtils.ToFlatName(type);
    }

    public string GetgenericConstraintsText(Type type)
    {
        string genericConstraints = type.IsGenericType ?
            _NewLine + CodeGenUtils.GetGenericParameterConstraintsText(type)
            : "";

        return genericConstraints;
    }



    //todo: assetdb should decide what is asset and what is not. quick fix for now
    public bool IsAsset(Type type)
    {
        return typeof(UnityEngine.Object).IsAssignableFrom(type)
                && !typeof(UnityEngine.Component).IsAssignableFrom(type)
                && !typeof(UnityEngine.GameObject).IsAssignableFrom(type)
                && !typeof(UnityEngine.ScriptableObject).IsAssignableFrom(type);
    }


    class MyClass
    {
        public string name;
    }
    [SaveHandler(34343, nameof(MyClass), typeof(MyClass))]
    class MyClassSaveHandler : UnmanagedSaveHandler<MyClass, MyClassSaveData>
    {
        public override void WriteSaveData()
        {
            base.WriteSaveData();

            __saveData.name = __instance.name;
        }
    }
    class MyClassSaveData : SaveDataBase
    {
        public string name;
    }




    public const string AdditionalNameSpaces = "<AdditionalNameSpaces>";
    public const string SaveHandlerClass = "<SaveHandlerClass>";
    public const string SaveDataClass = "<SaveDataClass>";
    public const string FileNameSpace = "<FileNameSpace>";
    public const string NameSpaceContent = "<NameSpaceContent>";






    public const string SaveHandlerAttribute = "<SaveHandlerAttribute>";
    public const string SaveHandlerClassName = "<SaveHandlerClassName>";
    public const string BaseClassName = "<BaseClassName>";
    public const string GenericParameterList = "<GenericParameterList>";
    public const string GenericConstraints = "<GenericConstraints>";
    public const string WritingFields = "<WritingFields>";
    public const string ReadingFields = "<ReadingFields>";
    public const string MethodSignaturesToMethodIds = "<MethodSignaturesToMethodIds>";
    public const string IdToMethodLookUp = "<IdToMethodLookUp>";
    public const string IdToGenMethodDefLookUp = "<IdToGenMethodDefLookUp>";


    [NonSerialized]
    public string _SaveHandlerTemplate =
        SaveHandlerAttribute + _NewLine +
        $"public class {GeneratedTypeName}SaveHandler{GenericParameterList} : " +
        $"{BaseClassName}<{TargetTypeReference}, {GeneratedTypeName}SaveData{GenericParameterList}> {GenericConstraints}" + _NewLine +
        "{" + _NewLine +
        $"public override void WriteSaveData()" + _NewLine +
        "{" + _NewLine +
        "base.WriteSaveData();" +
        _NewLine +
        WritingFields + _NewLine +
        "}" + _NewLine +
        _NewLine +
        $"public override void {nameof(ISaveAndLoad.LoadPhase1)}()" + _NewLine +
        "{" + _NewLine +
        $"base.{nameof(ISaveAndLoad.LoadPhase1)}();" +
        _NewLine +
        ReadingFields + _NewLine +
        "}" + _NewLine +
        _NewLine +
        $"static {GeneratedTypeName}SaveHandler()" + _NewLine +
        "{" + _NewLine +
        "Dictionary<string, long> methodToId = new()" + _NewLine +
        "{" + _NewLine +
        MethodSignaturesToMethodIds + _NewLine +
        "};" +
        _NewLine +
        nameof(Infra) + "." + nameof(Infra.Singleton) + "." + nameof(Infra.Singleton.AddMethodSignatureToMethodIdMap) + "(_typeReference, methodToId);" + _NewLine +
        nameof(Infra) + "." + nameof(Infra.Singleton) + "." + nameof(Infra.Singleton.AddMethodIdToMethodMap) + "(_typeReference, _idToMethod);" + _NewLine +
        nameof(Infra) + "." + nameof(Infra.Singleton) + "." + nameof(Infra.Singleton.AddMethodIdToMethodInfoMap) + "(_typeReference, _idToMethodInfo);" + _NewLine +
        "}" +
        _NewLine +
        $"static Type _typeReference = typeof({TargetTypeReference});" + _NewLine +
        $"static Type _typeDefinition = typeof({TargetTypeDefinition});" + _NewLine +
        "static Type[] _args = _typeReference.IsGenericType ? _typeReference.GetGenericArguments() : null;" +
        _NewLine +
        "public static Func<object, Delegate> _idToMethod(long id)" + _NewLine +
        "{" + _NewLine +
        "Func<object, Delegate> method = id switch" + _NewLine +
        "{" + _NewLine +
        IdToMethodLookUp + _NewLine +
        "};" + _NewLine +
        "return method;" + _NewLine +
        "}" +
        _NewLine +
        "public static MethodInfo _idToMethodInfo(long id)" + _NewLine +
        "{" + _NewLine +
        "MethodInfo methodDef = id switch" + _NewLine +
        "{" + _NewLine +
        IdToGenMethodDefLookUp + _NewLine +
        "};" + _NewLine +
        "return methodDef;" + _NewLine +
        "}" + _NewLine +
        "}" + _NewLine +
        "";





    public const string SaveHandlerId = "<SaveHandlerId>";
    public const string HandledType = "<HandledType>";
    //public const string IsStatic = "<IsStatic>";
    public const string GenerationMode = "<GenerationMode>";
    public const string AdditionalParamList = "<AdditionalParamList>";

    [NonSerialized]
    public string _SaveHandlerAttributeTemplate = $"[SaveHandler({SaveHandlerId}, handledType: typeof({HandledType}), generationMode: {GenerationMode}{AdditionalParamList})]";






    public const string SaveDataClassName = "<SaveDataClassName>";
    public const string SaveDataBaseClassName = "<SaveDataBaseClassName>";
    public const string FieldList = "<FieldList>";

    [NonSerialized]
    public string _SaveDataTemplate =
        $"[{nameof(SaveDataAttribute)[..^"Attribute".Length]}({SaveHandlerId})]" + _NewLine +
        $"public class {GeneratedTypeName}SaveData{GenericParameterList} : {SaveDataBaseClassName} {GenericConstraints}" +
        _NewLine +
        "{" +
        _NewLine +
        $"{FieldList}" +
        _NewLine +
        "}" + _NewLine +
        "";






    [NonSerialized]
    public string _StaticSaveHandlerAndSubtituteTemplate =
        $"public class Static{GeneratedTypeName}Subtitute{GenericParameterList} : StaticSubtitute {GenericConstraints}" + _NewLine +
        "{" + _NewLine +
        $"public override Type SubtitutedType => typeof({TargetTypeReference});" + _NewLine +
        "}" +
        _NewLine +
        _NewLine +
        SaveHandlerAttribute + _NewLine +
        $"public class Static{GeneratedTypeName}SaveHandler{GenericParameterList} : " +
        $"StaticSaveHandlerBase<Static{GeneratedTypeName}Subtitute{GenericParameterList}, Static{GeneratedTypeName}SaveData{GenericParameterList}> {GenericConstraints}" + _NewLine +
        "{" + _NewLine +
        $"public override void WriteSaveData()" + _NewLine +
        "{" + _NewLine +
        "base.WriteSaveData();" +
        _NewLine +
        WritingFields + _NewLine +
        "}" + _NewLine +
        _NewLine +
        $"public override void {nameof(ISaveAndLoad.LoadPhase1)}()" + _NewLine +
        "{" + _NewLine +
        $"base.{nameof(ISaveAndLoad.LoadPhase1)}();" +
        _NewLine +
        ReadingFields + _NewLine +
        "}" + _NewLine +
        $"static Static{GeneratedTypeName}SaveHandler()" + _NewLine +
        "{" + _NewLine +
        "Dictionary<string, long> methodToId = new()" + _NewLine +
        "{" + _NewLine +
        MethodSignaturesToMethodIds + _NewLine +
        "};" +
        _NewLine +
        nameof(Infra) + "." + nameof(Infra.Singleton) + "." + nameof(Infra.Singleton.AddMethodSignatureToMethodIdMap) + "(_typeReference, methodToId);" + _NewLine +
        nameof(Infra) + "." + nameof(Infra.Singleton) + "." + nameof(Infra.Singleton.AddMethodIdToMethodMap) + "(_typeReference, _idToMethod);" + _NewLine +
        nameof(Infra) + "." + nameof(Infra.Singleton) + "." + nameof(Infra.Singleton.AddMethodIdToMethodInfoMap) + "(_typeReference, _idToMethodInfo);" + _NewLine +
        "}" +
        _NewLine +
        $"static Type _typeReference = typeof({TargetTypeReference});" + _NewLine +
        $"static Type _typeDefinition = typeof({TargetTypeDefinition});" + _NewLine +
        "static Type[] _args = _typeReference.IsGenericType ? _typeReference.GetGenericArguments() : null;" +
        _NewLine +
        "public static Func<object, Delegate> _idToMethod(long id)" + _NewLine +
        "{" + _NewLine +
        "Func<object, Delegate> method = id switch" + _NewLine +
        "{" + _NewLine +
        IdToMethodLookUp + _NewLine +
        "};" + _NewLine +
        "return method;" + _NewLine +
        "}" +
        _NewLine +
        "public static MethodInfo _idToMethodInfo(long id)" + _NewLine +
        "{" + _NewLine +
        "MethodInfo methodDef = id switch" + _NewLine +
        "{" + _NewLine +
        IdToGenMethodDefLookUp + _NewLine +
        "};" + _NewLine +
        "return methodDef;" + _NewLine +
        "}" + _NewLine +
        "}" + _NewLine +
        "";




    [NonSerialized]
    public string _StaticSaveDataTemplate =
        $"public class Static{GeneratedTypeName}SaveData{GenericParameterList} : StaticSaveDataBase {GenericConstraints}" + _NewLine +
        "{" + _NewLine +
        $"{FieldList}" +
        _NewLine +
        "}" + _NewLine +
        "";





    [NonSerialized]
    public string _CustomSaveDataTemplate =
        CustomSaveDataAttributeText + _NewLine +
        $"public class {GeneratedTypeName}SaveData{GenericParameterList} : " +
        $"CustomSaveData<{TargetTypeReference}> {GenericConstraints}" +
        _NewLine +
        "{" +
        _NewLine +
        $"{FieldList}" +
        _NewLine +
        $"public override void {nameof(CustomSaveData<int>.ReadFrom)}(in {TargetTypeReference} instance)" +
        _NewLine +
        "{" +
        _NewLine +
        $"{WritingFields}" +
        _NewLine +
        "}" +
        _NewLine +
        $"public override void {nameof(CustomSaveData<int>.WriteInto)}(ref {TargetTypeReference} instance)" +
        _NewLine +
        "{" +
        _NewLine +
        $"{ReadingFields}" +
        _NewLine +
        "}" +
        _NewLine +
        //$"public static implicit operator {TargetTypeReference}({CustomSaveDataClassDefinition} saveData)" +
        //"{" +
        //"}" +
        "}" +
        "";


    public const string CustomSaveDataAttributeText = "<CustomSaveDataAttributeText>";
    public const string CustomSaveDataClassDefinition = "<CustomSaveDataClassDefinition>";
    public const string CustomSaveDataBaseClassDefinition = "<CustomSaveDataBaseClassDefinition>";
    public const string TargetTypeReference = "<TargetTypeReference>";
    public const string TargetTypeDefinition = "<TargetTypeDefinition>";
    public const string GeneratedTypeName = "<GeneratedTypeName>";
    public const string FieldName = "<FieldName>";



    public static string _NewLine = Environment.NewLine;
}


//todo: move to util package
public static class UtilExtenstions
{
    public static int IndexOfNth(this string str, char of, int nth)
    {
        return str.IndexOfNth(of, nth, nth < 0 ? str.Length - 1 : 0);
    }
    public static int IndexOfNth(this string str, char of, int nth, int startFrom)
    {
        int step = 1;
        int count = nth;
        int i = startFrom;

        if (nth < 0)
        {
            step = -1;
            count = -nth - 1;
        }

        while (i >= 0 && i < str.Length)
        {
            if (str[i] == of)
            {
                if (count == 0)
                    return i;
                count--;
            }
            i += step;
        }
        return -1;
    }
}