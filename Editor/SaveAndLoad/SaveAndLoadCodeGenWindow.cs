using Assets._Project.Scripts.Infrastructure;
using Assets._Project.Scripts.SaveAndLoad;
using Assets._Project.Scripts.SaveAndLoad.Editor;
using Assets._Project.Scripts.UtilScripts.CodeGen;
using Assets._Project.Scripts.UtilScripts.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Packages.com.theblueway.saveandload.Editor.SaveAndLoad;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Theblueway.CodeGen.Runtime;
using Theblueway.Core.Runtime.Extensions;
using Theblueway.Core.Runtime.Packages.com.blueutils.core.Runtime.Debugging.Logging;
using Theblueway.SaveAndLoad.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using static SaveHandlerAutoGenerator;
using Debug = UnityEngine.Debug;


public class SaveAndLoadCodeGenWindow : EditorWindow
{
    public static ConcurrentQueue<FileSystemEventArgsDto> _eventQueue = new();

    public SaveAndLoadCodeGenWindowState _state;
    public SaveAndLoadManager.EditorService _saveAndLoadService = new();
    public SaveAndLoadCodeGenWindow.Service _service = new();

    public SaveAndLoadCodeGenSettings _userSettings;


    public Vector2 _scrollPos;
    public List<FileSystemEventArgsDto> _changedFiles = new();
    public HashSet<int> _selectedIndices = new HashSet<int>(); // which entries are selected


    private SceneAsset _selectedSceneToScan;









    private void OnEnable()
    {
        LoadState();
    }
    private void OnDisable()
    {
        SaveState();
    }



    public void SaveState()
    {
        //Debug.Log("save state");
        _state._eventqueue = _eventQueue.ToList();
        _state._scrollPos = _scrollPos;
        _state._changedFiles = _changedFiles;
        _state._selectedIndices = _selectedIndices.ToList();
        _state._userSettings = _userSettings;
        _state._selectedFolderToScan = _selectedFolderToScan;
        EditorUtility.SetDirty(_state);
        EditorUtility.SetDirty(_userSettings);
        AssetDatabase.SaveAssets();
    }




    public void LoadState()
    {
        var guid = AssetDatabase.FindAssets($"{nameof(SaveAndLoadCodeGenWindow)}").First();
        var windowPath = AssetDatabase.GUIDToAssetPath(guid);
        var dir = Path.GetDirectoryName(windowPath);
        var statePath = Path.Combine(dir, $"{nameof(SaveAndLoadCodeGenWindowState)}.asset");

        _state = AssetDatabase.LoadAssetAtPath<SaveAndLoadCodeGenWindowState>(statePath);
        if (_state == null)
        {
            _state = CreateInstance<SaveAndLoadCodeGenWindowState>();
            AssetDatabase.CreateAsset(_state, statePath);
            AssetDatabase.SaveAssets();
        }
        _selectedFolderToScan = _state._selectedFolderToScan;
        _userSettings = _state._userSettings;
        _eventQueue = new ConcurrentQueue<FileSystemEventArgsDto>(_state._eventqueue);
        _scrollPos = _state._scrollPos;
        _changedFiles = _state._changedFiles;
        _selectedIndices = new HashSet<int>(_state._selectedIndices);
    }





    public static DateTime __lastCheckedDeltaTime;

    private void OnInspectorUpdate()
    {
        if (DateTime.Now - __lastCheckedDeltaTime < TimeSpan.FromSeconds(1)) return;
        __lastCheckedDeltaTime = DateTime.Now;

        bool changed = false;

        while (_eventQueue.TryDequeue(out var e))
        {
            // Now safely handle on Unity’s main thread
            if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                _changedFiles.RemoveAll(f => f.FullPath == e.FullPath);
                continue;
            }


            if (e.FullPath.Replace("\\", "/").Contains("/Editor/"))
            {
                continue;
            }

            bool duplicate = _changedFiles.Any(f => f.FullPath == e.FullPath && f.ChangeType == e.ChangeType);

            if (!duplicate)
            {
                changed = true;
                _changedFiles.Add(e);
            }
        }

        if (changed)
        {
            Repaint();
        }
    }




    public void EnsureTypeGenConfigs(bool forceRegenerate)
    {
        Session session = NewSession();

        var typeGenConfigs = session.TypeGenerationSettingsRegistry.GatherAllScriptableConfigs();

        IEnumerable<Type> configuredTypes = typeGenConfigs.Select(config => config._config.ConfiguredType).Where(t => t != null);

        session.TypeGenerationSettingsRegistry.CacheInvalidate();

        bool orig = session.UserSettings.ForceGenerateForUnchangedTypesToo;

        session.UserSettings.ForceGenerateForUnchangedTypesToo = true;

        CreateTypeReportsAndRunCodeGen(configuredTypes, session);

        session.UserSettings.ForceGenerateForUnchangedTypesToo = orig;

    }





    [MenuItem("Window/SaveAndLoad CodeGen")]
    public static void ShowWindow()
    {
        // Creates (or focuses) a new tabbed editor window
        GetWindow<SaveAndLoadCodeGenWindow>("SaveAndLoad CodeGen");
    }

    private void OnGUI()
    {
        CodeGenUtils.Config = ToCodeGenConfig(_userSettings);




        EditorGUILayout.LabelField("Changed Files", EditorStyles.boldLabel);


        EditorGUILayout.BeginHorizontal();
        // Scrollable list
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(200), GUILayout.Width(350));
        for (int i = 0; i < _changedFiles.Count; i++)
        {
            var entry = _changedFiles[i];

            bool selected = _selectedIndices.Contains(i);

            var style = new GUIStyle("Button")
            {
                alignment = TextAnchor.MiddleLeft
            };


            bool newSelected = GUILayout.Toggle(selected, Path.GetFileName(entry.FullPath), style, GUILayout.Width(250));


            if (newSelected != selected)
            {
                if (newSelected)
                    _selectedIndices.Add(i);
                else
                    _selectedIndices.Remove(i);
            }
        }
        EditorGUILayout.EndScrollView();


        EditorGUILayout.Space(100);

        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("CodeGen Settings", GUILayout.Width(120));

        _userSettings = (SaveAndLoadCodeGenSettings)EditorGUILayout.ObjectField(
            _userSettings,
            typeof(SaveAndLoadCodeGenSettings),
            false,
            options: GUILayout.Width(150));

        EditorGUILayout.EndHorizontal();


        EditorGUILayout.Space();


        EditorGUILayout.Space(10);



        if (GUILayout.Button("Clear All"))
        {
            ClearFileList();
        }
        if (GUILayout.Button("Discard Selection"))
        {
            DiscardSelected();
        }
        if (GUILayout.Button("Run CodeGen on Selected"))
        {
            var tasks = CreateCodeGenTasksFromSelected();

            CreateTypeReportsAndRunCodeGen(tasks);

            //turned off for easier dev testing
            //RemoveSelectedFiles();
        }



        EditorGUILayout.Space(40);

        _state._forceGenerateHandlersOfTypeGenConfigs = GUILayout.Toggle(_state._forceGenerateHandlersOfTypeGenConfigs,"Force regen handlers of typegen configs");

        if(GUILayout.Button("Ensure typegen configs"))
        {
            EnsureTypeGenConfigs(forceRegenerate:_state._forceGenerateHandlersOfTypeGenConfigs);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();


        //
        //scene selection section
        //


        EditorGUILayout.Space(30);

        EditorGUILayout.LabelField("Select a Scene", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();


        EditorGUILayout.LabelField("Scene", GUILayout.Width(60));

        _selectedSceneToScan = (SceneAsset)EditorGUILayout.ObjectField(
            _selectedSceneToScan,
            typeof(SceneAsset),
            false,
            options: GUILayout.Width(150));


        if (_selectedSceneToScan != null)
        {
            if (GUILayout.Button("Find Unhandled Types"))
            {
                _foundTypesInSelectedScene = FindUnhandledTypesInScene(_selectedSceneToScan);

                if (_foundTypesInSelectedScene.Count == 0)
                    Debug.Log("No unhandled types found.");
            }

            if (GUILayout.Button("Select All"))
            {
                _selectedTypesFoundInSelectedSceneTypesList = new HashSet<Type>(_foundTypesInSelectedScene);
            }

            if (GUILayout.Button("Discard All Selected"))
            {
                _selectedTypesFoundInSelectedSceneTypesList.Clear();
            }

            if (GUILayout.Button("Generate"))
            {
                CreateTypeReportsAndRunCodeGen(_selectedTypesFoundInSelectedSceneTypesList);
                _selectedTypesFoundInSelectedSceneTypesList.Clear();
            }
        }
        EditorGUILayout.EndHorizontal();



        if (_selectedSceneToScan != null && _foundTypesInSelectedScene != null && _foundTypesInSelectedScene.Count != 0)
        {
            _sceneTypesListScrollPos = EditorGUILayout.BeginScrollView(_sceneTypesListScrollPos, GUILayout.Height(200));

            {
                int i = 0;

                foreach (var type in _foundTypesInSelectedScene)
                {
                    var style = new GUIStyle("Button")
                    {
                        alignment = TextAnchor.MiddleLeft
                    };

                    bool selected = _selectedTypesFoundInSelectedSceneTypesList.Contains(type);

                    bool newSelected = GUILayout.Toggle(selected, type.Name, style, GUILayout.Width(250));


                    if (newSelected != selected)
                    {
                        if (newSelected)
                            _selectedTypesFoundInSelectedSceneTypesList.Add(type);
                        else
                            _selectedTypesFoundInSelectedSceneTypesList.Remove(type);
                    }

                    i++;
                }
            }
            EditorGUILayout.EndScrollView();
        }


        //
        //prefab folders section
        //




        EditorGUILayout.Space(30);

        EditorGUILayout.BeginHorizontal();


        if (_userSettings != null)
        {
            if (GUILayout.Button("Find Unhandled Types In Prefabs"))
            {
                _foundTypesInPrefabFolders = FindUnhandledTypesInPrefabFolders(_userSettings);

                if (_foundTypesInPrefabFolders.Count == 0)
                    Debug.Log("No unhandled types found.");
            }

            if (GUILayout.Button("Select All"))
            {
                _selectedTypesFoundInfPrefabFoldersTypesList = new HashSet<Type>(_foundTypesInPrefabFolders);
            }

            if (GUILayout.Button("Discard All Selected"))
            {
                _selectedTypesFoundInfPrefabFoldersTypesList.Clear();
            }

            if (GUILayout.Button("Generate"))
            {
                CreateTypeReportsAndRunCodeGen(_selectedTypesFoundInfPrefabFoldersTypesList);
                _selectedTypesFoundInfPrefabFoldersTypesList.Clear();
            }
        }
        EditorGUILayout.EndHorizontal();



        if (_userSettings != null && _foundTypesInPrefabFolders != null && _foundTypesInPrefabFolders.Count != 0)
        {
            _prefabFolderTypesListScrollPos = EditorGUILayout.BeginScrollView(_prefabFolderTypesListScrollPos, GUILayout.Height(200));

            if (_foundTypesInPrefabFolders != null)
            {
                int i = 0;

                foreach (var type in _foundTypesInPrefabFolders)
                {
                    var style = new GUIStyle("Button")
                    {
                        alignment = TextAnchor.MiddleLeft
                    };

                    bool selected = _selectedTypesFoundInfPrefabFoldersTypesList.Contains(type);

                    bool newSelected = GUILayout.Toggle(selected, type.Name, style, GUILayout.Width(250));


                    if (newSelected != selected)
                    {
                        if (newSelected)
                            _selectedTypesFoundInfPrefabFoldersTypesList.Add(type);
                        else
                            _selectedTypesFoundInfPrefabFoldersTypesList.Remove(type);
                    }

                    i++;
                }
            }
            EditorGUILayout.EndScrollView();
        }





        //
        //scan folder selection section
        //


        EditorGUILayout.Space(30);

        EditorGUILayout.LabelField("Input an Assets relative folder to scan for types. (Recursively)", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();


        EditorGUILayout.LabelField("Folder", GUILayout.Width(60));

        _selectedFolderToScan = EditorGUILayout.TextField(_selectedFolderToScan, options: GUILayout.Width(150));

        _selectedFolderToScan ??= "";


        bool exists = Directory.Exists(Path.Combine(Application.dataPath, _selectedFolderToScan));


        if (!exists)
        {
            EditorGUILayout.LabelField("Folder does not exist!", GUILayout.Width(150));
        }
        else
        {
            if (GUILayout.Button("Find Unhandled Types"))
            {
                _foundTypesInFolderToScan = FindUnhandledTypesInFolder(_selectedFolderToScan);

                if (_foundTypesInFolderToScan.Count == 0)
                    Debug.Log("No unhandled types found.");
            }

            if (GUILayout.Button("Select All"))
            {
                _selectedTypesFoundInfFolderToScanTypesList = new HashSet<Type>(_foundTypesInFolderToScan);
            }

            if (GUILayout.Button("Discard All Selected"))
            {
                _selectedTypesFoundInfFolderToScanTypesList.Clear();
            }

            if (GUILayout.Button("Generate"))
            {
                CreateTypeReportsAndRunCodeGen(_selectedTypesFoundInfFolderToScanTypesList);
                _selectedTypesFoundInfFolderToScanTypesList.Clear();
            }
        }
        EditorGUILayout.EndHorizontal();



        if (_foundTypesInFolderToScan != null && _foundTypesInFolderToScan.Count != 0)
        {
            _folderToScanTypesListScrollPos = EditorGUILayout.BeginScrollView(_folderToScanTypesListScrollPos, GUILayout.Height(200));

            {
                int i = 0;

                foreach (var type in _foundTypesInFolderToScan)
                {
                    var style = new GUIStyle("Button")
                    {
                        alignment = TextAnchor.MiddleLeft
                    };

                    bool selected = _selectedTypesFoundInfFolderToScanTypesList.Contains(type);

                    bool newSelected = GUILayout.Toggle(selected, type.Name, style, GUILayout.Width(250));


                    if (newSelected != selected)
                    {
                        if (newSelected)
                            _selectedTypesFoundInfFolderToScanTypesList.Add(type);
                        else
                            _selectedTypesFoundInfFolderToScanTypesList.Remove(type);
                    }

                    i++;
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }




    public HashSet<Type> _foundTypesInSelectedScene = new();
    public HashSet<Type> _selectedTypesFoundInSelectedSceneTypesList = new();
    public Vector2 _sceneTypesListScrollPos;


    public HashSet<Type> _foundTypesInPrefabFolders = new();
    public HashSet<Type> _selectedTypesFoundInfPrefabFoldersTypesList = new();
    public Vector2 _prefabFolderTypesListScrollPos;


    public HashSet<Type> _foundTypesInFolderToScan = new();
    public HashSet<Type> _selectedTypesFoundInfFolderToScanTypesList = new();
    public Vector2 _folderToScanTypesListScrollPos;
    public string _selectedFolderToScan;






    public HashSet<Type> FindUnhandledTypesInFolder(string assetsRelativeFolder)
    {
        if (assetsRelativeFolder is null) assetsRelativeFolder = "";

        string absPath = Path.Combine(Application.dataPath, assetsRelativeFolder);

        var csFiles = GetCsFiles(absPath);

        var foundTypes = new HashSet<Type>();

        foreach (var file in csFiles)
        {
            foundTypes.AddRange(FindTypesInFile(file));
        }


        var unhandledTypes = new HashSet<Type>();

        foreach (var type in foundTypes)
        {
            if (!_saveAndLoadService.IsTypeHandled_Editor(type)
                && !_userSettings.TypeExclusionSettings.ShouldExclude(type))
            {
                unhandledTypes.Add(type);
            }
        }

        return unhandledTypes;
    }


    IEnumerable<string> GetCsFiles(string root) => _service.GetCsFiles(root);


    public class Service
    {
        public IEnumerable<string> GetCsFiles(string root)
        {

            foreach (var file in Directory.EnumerateFiles(root, "*.cs"))
                yield return file;

            foreach (var dir in Directory.EnumerateDirectories(root))
            {
                var name = Path.GetFileName(dir);
                if (name.EndsWith("~")) continue; // skip ignored

                foreach (var file in Directory.EnumerateFiles(dir, "*.cs"))
                    yield return file;

                foreach (var subFile in GetCsFiles(dir))
                    yield return subFile;
            }
        }
    }







    public HashSet<Type> FindUnhandledTypesInScene(SceneAsset sceneAsset)
    {
        string path = AssetDatabase.GetAssetPath(sceneAsset);

        Scene existingScene = SceneManager.GetSceneByPath(path);
        bool wasLoaded = existingScene.isLoaded;

        // Load only if not already loaded
        Scene scene = wasLoaded
            ? existingScene
            : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);


        GameObject[] roots = scene.GetRootGameObjects();

        var types = GetAllComponentTypesFromGameObjects(roots);

        if (!wasLoaded)
            EditorSceneManager.CloseScene(scene, true);

        return types;
    }


    public HashSet<Type> GetAllComponentTypesFromGameObjects(IEnumerable<GameObject> gameObjects)
    {
        List<Component> allComponentsOfAllGameObjects = new();
        List<Component> components = new();


        foreach (var go in gameObjects)
        {
            go.GetComponentsInChildren(includeInactive: true, components);
            allComponentsOfAllGameObjects.AddRange(components);
        }

        var foundUnhandledTypes = allComponentsOfAllGameObjects
                            .Select(x => x.GetType())
                            .Where(t => !_saveAndLoadService.HasSaveHandlerForType_Editor(t)
                                        && !_userSettings.TypeExclusionSettings.ShouldExclude(t))
                            .ToHashSet();


        return foundUnhandledTypes;
    }




    public HashSet<Type> FindUnhandledTypesInPrefabFolders(SaveAndLoadCodeGenSettings settings)
    {
        IEnumerable<GameObject> prefabs = settings.PrefabFolderPaths.Select(path => GetPrefabsInDirectory(path)).SelectMany(x => x);

        return GetAllComponentTypesFromGameObjects(prefabs);
    }


    public List<GameObject> GetPrefabsInDirectory(string folderPath)
    {
        // Get all prefab GUIDs in the folder
        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { folderPath });

        var prefabs = new List<GameObject>();

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            prefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>(assetPath));
        }

        return prefabs;
    }








    private void DiscardSelected()
    {
        RemoveSelectedFiles();
    }





    public class CodeGenTask
    {
        public string filePath;
        public List<(Type rootType, List<Type> nestedTypes)> rootAndChildren = new();
    }


    public List<CodeGenTask> CreateCodeGenTasksFromSelected()
    {
        var filePaths = _changedFiles.Where((file, i) => _selectedIndices.Contains(i)).Select(f => f.FullPath);

        var tasks = CreateCodeGenTasks(filePaths);

        return tasks;
    }





    public IEnumerable<Type> FindTypesInFile(string filePath)
    {
        List<Type> typesInFile = new List<Type>();

        var inspectionReport = Roslyn.CodeGen.InspectCodeFile(filePath);


        var assemblyName = AssemblyResolver.ResolveAssembly(filePath);


        foreach (var typeReport in inspectionReport.TypeReports)
        {
            var namespaceName = typeReport.Namespace;
            var typeName = typeReport.TypeName;


            Type type = ResolveType(assemblyName, namespaceName, typeName);

            if (type == null)
            {
                Debug.LogError($"Could not resolve type {typeName} in namespace '{namespaceName}' from assembly '{assemblyName}'. Make sure the assembly is loaded.");
                continue;
            }

            if (!CodeGenUtils.Config.NonPublicToo && type.IsNested && !type.IsNestedPublic)
            {
                //skip non public nested types
                continue;
            }

            typesInFile.Add(type);
        }

        return typesInFile;
    }



    public List<CodeGenTask> CreateCodeGenTasks(IEnumerable<string> filePaths)
    {
        List<Type> typesInFiles = new List<Type>();

        foreach (var file in filePaths)
        {
            typesInFiles.AddRange(FindTypesInFile(file));
        }

        return CreateCodeGenTasks(typesInFiles);
    }


    public List<CodeGenTask> CreateCodeGenTasks(IEnumerable<Type> types)
    {
        var codegenTasks = new List<CodeGenTask>();

        Dictionary<string, Type> rootTypesByName = new();
        Dictionary<string, List<Type>> nestedTypesByRootTypeName = new();

        foreach (var type in types)
        {
            if (type.IsNested)
            {
                int rootTypeNameEndIndex = type.FullName.IndexOf("+");

                var rootTypeName = type.FullName.Substring(0, rootTypeNameEndIndex);

                if (!nestedTypesByRootTypeName.ContainsKey(rootTypeName))
                {
                    nestedTypesByRootTypeName[rootTypeName] = new List<Type>();
                }

                nestedTypesByRootTypeName[rootTypeName].Add(type);
            }
            else
            {
                //todo: this will fail with two types with same fullname in two different assembly
                //perhaps just use the type isntance instead of its name?
                rootTypesByName.Add(type.FullName, type);
            }
        }


        //todo: lookup the containing file location of the type
        //a groupby will aslo be needed
        var codegenTask = new CodeGenTask { filePath = null };

        foreach ((string name, Type rootType) in rootTypesByName)
        {
            var nestedTypesOfRootType = nestedTypesByRootTypeName.TryGetValue(name, out var nestedTypes) ? nestedTypes : new List<Type>();

            codegenTask.rootAndChildren.Add((rootType, nestedTypesOfRootType));
        }

        codegenTasks.Add(codegenTask);


        return codegenTasks;
    }





    private void CreateTypeReportsAndRunCodeGen(IEnumerable<CodeGenTask> codeGenTasks)
    {
        IEnumerable<Type> Flatten(IEnumerable<CodeGenTask> codegenTasks)
        {
            var result = new List<Type>();

            foreach (var task in codegenTasks)
            {
                foreach ((var rootType, var nestedTypes) in task.rootAndChildren)
                {
                    result.Add(rootType);

                    foreach (var nestedType in nestedTypes)
                        result.Add(nestedType);
                }
            }
            return result;
        }

        var flattened = Flatten(codeGenTasks);

        CreateTypeReportsAndRunCodeGen(flattened);
    }


    private void CreateTypeReportsAndRunCodeGen(IEnumerable<Type> typesToHandle, Session session = null)
    {
        session ??= NewSession();


        var allAsms = AppDomain.CurrentDomain.GetAssemblies();


        #region Dev Test/Debug

        //var refasms = typeof(Rigidbody).Assembly.GetReferencedAssemblies();

        //var refAsms2 = allAsms.Where(a => refasms.Any(r => r.FullName == a.FullName)).ToList();
        ////Debug.Log("ref1 count: " + refasms.Count());
        ////Debug.Log("ref2 count: " + refAsms2.Count());
        ////Debug.Log("type count: " + refAsms2.SelectMany(a => a.GetTypes()).Count());


        //var assemblies = refAsms2
        //        .Select(a =>
        //        {
        //            try { return (a.GetName().Name, a.GetTypes().Length); }
        //            catch (ReflectionTypeLoadException ex) { return ("excp", ex.Types.Where(t => t != null).Count()); }
        //        });

        //assemblies = assemblies.OrderByDescending(a => a.Item2);

        //var writer2 = File.CreateText("C:/temp/refassemblies.txt");

        //foreach (var asm in assemblies)
        //{
        //    writer2.WriteLine($"{asm.Item2}\t{asm.Item1}");
        //}
        //writer2.Close();
        //writer2.Dispose();



        //var unitycoremodulename = assemblies.ElementAt(2).Item1;

        //var module = refAsms2.FirstOrDefault(a => a.GetName().Name == unitycoremodulename);

        //writer2 = File.CreateText("C:/temp/coretypes.txt");

        //foreach (var type in module.GetTypes())
        //{
        //    writer2.WriteLine(type.FullName);
        //}
        //writer2.Close();
        //writer2.Dispose();



        //var depAsm = typeof(Animal).Assembly;

        //var dependantAsms = allAsms.Where(a => a.GetReferencedAssemblies().Any(r => r.FullName == depAsm.FullName)).OrderByDescending(a => a.GetTypes().Length).ToList();

        //writer2 = File.CreateText("C:/temp/dependants.txt");

        //foreach (var asm in dependantAsms)
        //{
        //    writer2.WriteLine(asm.GetTypes().Length + "\t" + asm.GetName().Name);
        //}
        //writer2.Close();
        //writer2.Dispose();


        #endregion


        //Debug.Log("running codegen... Task coun: " + typesToHandle.Count());

        Queue<Type> discoveryQueue = new Queue<Type>();
        Dictionary<Type, TypeReport> discoveredTypes = new();


        foreach (var type in typesToHandle)
        {
            if(type == null)
            {
                Debug.LogWarning("Found null reference in codegeneration logic. Please do not send null references for codegen.");
                continue;
            }

            discoveryQueue.Enqueue(type);
        }


        //todo: move these to type exlusion config
        HashSet<string> excludedNameSpaces = new()
        {
            typeof(Assets._Project.Scripts.SaveAndLoad.SaveAndLoadManager).Namespace,
            typeof(Newtonsoft.Json.JsonConvert).Namespace,
            typeof(System.Collections.IEnumerable).Namespace,//and maybe this one too
            typeof(System.Reflection.Emit.TypeBuilder).Namespace,//except this one
            //typeof(Assets._Project.Scripts.UtilScripts.DataStructures.NetworkSerializables.ListBytes<>).Namespace,
        };


        //this is not a type exclusion list, this is a checkIfOthersImplementThisType exclusion. So these types will still get their savehandlers
        HashSet<Type> visitedTypes = new()
        {
            //pre-exclude these types from type discovery because we dont want to add and iterate over all of the types that inherits from them
            typeof(object),
            typeof(System.ValueType),
            typeof(UnityEngine.Object),
            typeof(Component),
            typeof(Behaviour),
            typeof(MonoBehaviour),
            typeof(ScriptableObject),
            typeof(StateMachineBehaviour),
            typeof(UIBehaviour),
            typeof(AudioBehaviour),
            Type.GetType("UnityEngine.InputSystem.InputDevice, Unity.InputSystem"),
        };


        //debug purpose
        //bool found = false;


        HashSet<Type> typesFromChangedFiles = new(discoveryQueue);






        string SINGLEITERATION = "SINGLE_ITERATION";
        string STATICREFERENCEINSPECTION = "STATIC_REFERENCE_INSPECTION";
        string GET_CECIL_TYPE = "GET_CECIL_TYPE";

        Dictionary<string, List<TimeSpan>> benchmark = new()
        {
            {SINGLEITERATION, new ()},
            {STATICREFERENCEINSPECTION, new ()},
            {GET_CECIL_TYPE, new ()},
        };

        Stopwatch stopwatch = Stopwatch.StartNew();

        TimeSpan start;
        TimeSpan end;




        int maxIterations = 10000;
        int firstWarningAt = 500;
        int currentIteration = 0;


        //todo:
        //left to do: function pointers, pointer types, dynamic
        //
        while (discoveryQueue.Count > 0)
        {
            currentIteration++;

            if (currentIteration == firstWarningAt)
            {
                Debug.LogWarning("Type discovery is taking unusually long. " +
                    "It might be a bug causing infinite loop or you started the codegen for hundreds or thousands of types. " +
                    $"{currentIteration} iterations were done of the maximum {maxIterations}.");
            }
            if (currentIteration == maxIterations)
            {
                Debug.LogError("Max iterations reached. There is probably a bug causing infinite loop. Stopping.");
                break;
            }



            var type = discoveryQueue.Dequeue();


            var obsolete = type.GetCustomAttribute<ObsoleteAttribute>(false);

            if (obsolete != null && (obsolete.IsError || _userSettings.IgnoreAnyObsolete))
            {
                Debug.LogWarning("Skipping obsolete type: " + type.AssemblyQualifiedName);
                continue;
            }


            if (_userSettings.TypeExclusionSettings.ShouldExclude(type)) continue;


            if (_saveAndLoadService.HasSerializer_Editor(type)) continue;

            // if the codegen logic was ran for a type we can not change, for example Unity's or Microsoft's types,
            // there is no point to discover their dependencies again as they most probably didnt change since then.
            // even if they did, an option to force discovery to update their handles will be implemented
            //todo:
            bool skipUnchangedType = !_userSettings.ForceGenerateForUnchangedTypesToo && !_userSettings.GenerateExampleSaveHandlersForManuallyHandledTypesToo;
            if (skipUnchangedType)
            {
                bool notTheTypeThatWasChanged = !typesFromChangedFiles.Contains(type);
                if (notTheTypeThatWasChanged)
                {
                    bool alreadyHandled = _saveAndLoadService.IsTypeHandled_Editor(type);

                    if (alreadyHandled) continue;
                }
            }



            bool excluded = type.Namespace != null && excludedNameSpaces.Any(ns => type.Namespace.StartsWith(ns));
            if (excluded)
            {
                Debug.LogWarning("Skipping type from excluded namespace: " + type.AssemblyQualifiedName);
                continue;
            }





            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                if (!_userSettings.TypeDiscoverySettings.IgnoreGenericTypeArguments)
                    foreach (var argType in type.GenericTypeArguments)
                        discoveryQueue.Enqueue(argType);

                //keep in mind, after this, we work with the type def, not with the constructed type
                type = type.GetGenericTypeDefinition();


                //this is the first time we encounter this gen type def
                if (!_userSettings.TypeDiscoverySettings.IgnoreGenericTypeConstraints)
                    if (!discoveredTypes.ContainsKey(type))
                        foreach (var constraint in type.GetGenericArguments().SelectMany(a => a.GetGenericParameterConstraints()))
                            discoveryQueue.Enqueue(constraint);
            }



            if (discoveredTypes.ContainsKey(type)) continue;



            //todo: should we prepare for other element types too? ByRef and Pointer types
            if (type.IsArray)
            {
                discoveryQueue.Enqueue(type.GetElementType());
                continue;
            }




            bool classOrStructOrInterface = type.IsClass || type.IsStruct() || type.IsInterface;

            if (!classOrStructOrInterface || type == typeof(string)
                || type.IsGenericParameter
                || type.IsAssignableTo(typeof(Delegate)))
            {
                continue;
            }



            start = stopwatch.Elapsed;

            var cecilType = CodeGenUtils.GetCecilTypeDefinition(type);

            end = stopwatch.Elapsed;
            benchmark[GET_CECIL_TYPE].Add(end - start);


            if (cecilType == null)
            {
                Debug.LogWarning($"No Cecil typedef has found for type {type.CleanAssemblyQualifiedName()}.");
            }
            if (cecilType != null && !cecilType.IsCompileTimePublic())
            {
                Debug.LogWarning("Skipping non public type: " + type.AssemblyQualifiedName);
                continue;
            }



            //too slow: from 30ms to ~350ms per type
            //edit: idea: its actually easier if we just scan for static members in all types in all assemblies and if they have any, enqueue their types regardless.
            //start = stopwatch.Elapsed;

            //var staticReferences = CodeGenUtils.GetStaticlyReferencedTypes(cecilType);

            //foreach (var staticTypeDef in staticReferences.ResolvedTypes)
            //{
            //    var staticType = CodeGenUtils.ResolveType(staticTypeDef);
            //    discoveryQueue.Enqueue(staticType);
            //    //Debug.Log(staticType.FullName);
            //}

            //end = stopwatch.Elapsed;
            //benchmark[STATICREFERENCEINSPECTION].Add(end - start);






            string assemblyName = type.Assembly.GetName().Name;

            // we do not want to traverse microsoft's interfaces like IDisposable, IComparable, etc.
            // tough with this, there is no guarantee that every implementation will be covered
            //todo: config to switch this logic
            bool interfaceFromMicroSoft = type.IsInterface && assemblyName.StartsWith("mscorlib");

            bool checkTypesIfTheyInheritOrImplementThisType =
                                       !visitedTypes.Contains(type)
                                    && !type.IsAbstract && !type.IsSealed && !type.IsValueType && !type.IsStatic()
                                    && !interfaceFromMicroSoft;


            if (checkTypesIfTheyInheritOrImplementThisType && !_userSettings.TypeDiscoverySettings.IgnoreImplementOrInherit)
            {
                List<Assembly> dependantAsms;

                //these assemblies have too many dependant assemblies, iterating over their types too is too slow (minutes).
                if (assemblyName.StartsWith("Unity", StringComparison.OrdinalIgnoreCase)
                  || assemblyName.StartsWith("mscorlib"))
                {
                    dependantAsms = new List<Assembly>() { type.Assembly };
                }
                else
                    dependantAsms = allAsms.Where(a => a.GetReferencedAssemblies().Any(r => r.FullName == type.Assembly.FullName))
                                           .Append(type.Assembly).ToList();


                var allTypes = dependantAsms.SelectMany(a => a.GetTypes()).ToList();
                //Debug.Log(allTypes.Count + " types in dependant assemblies for " + type.FullName);



                int count = 0;
                foreach (var otherType in allTypes)
                {
                    var assignable = type.IsGenericType
                                    ? otherType.IsAssignableToGenericTypeDefinition(type)
                                    : otherType.IsAssignableTo(type);


                    if (assignable)
                    {
                        discoveryQueue.Enqueue(otherType);

                        count++;
                        if (otherType.IsGenericType)
                            visitedTypes.Add(otherType.GetGenericTypeDefinition());
                        else
                            visitedTypes.Add(otherType);
                    }
                }
                //if (type.IsInterface)
                //    Debug.Log($"Found {count} types implementing interface {type.FullName}");
            }




            //if anything fails after this, we still gain the perforamnce of not visiting this type again.
            //if we failed ones, no reason (so far) to check it again.
            discoveredTypes.Add(type, null);

            //Debug.Log(type.FullName);



            TypeReport CreateTypeReport(Type t, BindingFlags binding)
            {
                bool isStatic = binding.HasFlag(BindingFlags.Static);

                var fieldsReport = GetFieldInfos(type, binding, session);

                var properties = CodeGenUtils.GetSimpleProperties(type, binding).ToList();

                //todo: config
                var methods = type.GetUsableMethods(binding | BindingFlags.DeclaredOnly).ToList();

                List<EventInfo> events = type.GetEvents(binding).ToList();


                if (_userSettings.IgnoreAnyObsolete)
                {
                    //fields, properties: already skipped them
                    methods = methods.Where(m => !m.IsDefined(typeof(ObsoleteAttribute))).ToList();
                    events = events.Where(e => !e.IsDefined(typeof(ObsoleteAttribute))).ToList();
                }


                if (session.TypeGenerationSettingsRegistry.HasSettingsForHandledType(type, isStatic, out var settings))
                {
                    //fields: fieldReport already takes into account the settings
                    //todo: methods. methods are a different beast because of overloads with same name. See the design document for details

                    foreach (var prop in properties.ToList())
                    {
                        if (settings.HasInclusionModeFor(prop, out var inclusionMode))
                        {
                            if (inclusionMode is MemberInclusionMode.Exclude)
                            {
                                properties.Remove(prop);
                            }
                        }
                    }

                    foreach (var e in events.ToList())
                    {
                        if (settings.HasInclusionModeFor(e, out var inclusionMode))
                        {
                            if (inclusionMode is MemberInclusionMode.Exclude)
                            {
                                events.Remove(e);
                            }
                        }
                    }

                    foreach(var method in methods)
                    {
                        if (settings.HasInclusionModeFor(method, out var inclusionMode))
                        {
                            if (inclusionMode is MemberInclusionMode.Exclude)
                            {
                                methods.Remove(method);
                            }
                        }
                    }
                }

                var typeReport = new TypeReport
                {
                    ReportedType = type,
                    FieldsReport = fieldsReport,
                    Properties = properties,
                    Events = events,
                    Methods = methods
                };

                return typeReport;
            }




            var binding = BindingFlags.Public | BindingFlags.Static;

            if (_userSettings.GenerateSaveHandlersAsNestedClassesInsideHandledType && session.HasEditableSourceFile(type))
            {
                binding |= BindingFlags.NonPublic;
            }



            var typeReport = CreateTypeReport(type, binding);


            if (!type.IsStatic())
            {
                binding |= BindingFlags.Instance;
                binding &= ~BindingFlags.Static;
                var instanceReport = CreateTypeReport(type, binding);

                var staticReport = typeReport;


                bool hasNoTypeGenSettings = !session.TypeGenerationSettingsRegistry.HasSettingsForHandledType(type,isStatic:true, out var _);


                //doc:
                //we dont generate static handler if there is no static member of the handled type to reduce clutter and unnecessary types
                //however, it is possible that the typegen configs specified for the type exclude all members
                //there is a reason the user created a config for this type, so we interpet it as an enforcement
                if (hasNoTypeGenSettings 
                    && staticReport.FieldsReport.ValidFields.Count == 0
                    && staticReport.Properties.Count() == 0
                    && staticReport.Events.Count() == 0
                    && staticReport.Methods.Count == 0)
                {
                    //if there is no static members, dont generate a static handler
                    instanceReport.StaticReport = null;
                }
                else
                {
                    instanceReport.StaticReport = staticReport;
                }

                typeReport = instanceReport;
            }


            discoveredTypes[type] = typeReport;


            List<Type> GetDependencies(TypeReport typeReport)
            {
                var deps = new List<Type>();
                deps.AddRange(typeReport.FieldsReport.ValidFields.Select(f => f.FieldInfo.FieldType));
                deps.AddRange(typeReport.Properties.Select(p => p.PropertyType));
                deps.AddRange(typeReport.Events.Select(e => e.EventHandlerType));

                return deps;
            }



            if (!_userSettings.TypeDiscoverySettings.IgnoreDirectDependencies)
            {
                var dependencies = new List<Type>();

                dependencies.AddRange(GetDependencies(typeReport));

                if (!type.IsStatic() && typeReport.StaticReport != null)
                {
                    dependencies.AddRange(GetDependencies(typeReport.StaticReport));
                }



                foreach (var depType in dependencies)
                {
                    discoveryQueue.Enqueue(depType);
                }
            }


            if (type.BaseType != null && !_userSettings.TypeDiscoverySettings.IgnoreBaseType)
            {
                discoveryQueue.Enqueue(type.BaseType);
                visitedTypes.Add(type.BaseType.IsGenericType ? type.BaseType.GetGenericTypeDefinition() : type.BaseType);//so we dont check who is assignable to it
            }


            benchmark[SINGLEITERATION].Add(stopwatch.Elapsed);
            stopwatch.Restart();
            //if (!found && discoveryQueue.Any(t => t.FullName == "UnityEngine.Animation"))
            //{
            //    Debug.LogError(type.FullName + " added unwanted type");
            //    found = true;
            //}
        }

        //Debug.Log("discovery done. found " + discoveredTypes.Count + " types.");

        BlueDebug.Debug("Benchmark: Sum Total");
        foreach ((var key, var times) in benchmark)
        {
            TimeSpan total = new TimeSpan(times.Sum(t => t.Ticks));
            BlueDebug.Debug($"{key}: {total.TotalMilliseconds} ms over {times.Count} iterations. Avg: {total.TotalMilliseconds / times.Count} ms");
        }









        //dev test/debug
        //var names = discoveredTypes.Select(t => t.Key.FullName).OrderBy(name => name).ToList();

        //if (Directory.Exists("C:/temp") == false) Directory.CreateDirectory("C:/temp");
        //File.WriteAllText("C:/temp/discoveredTypes.txt", string.Join("\n", names));




        var codeGenerator = ScriptableObject.CreateInstance<SaveHandlerAutoGenerator>();


        List<(Type type, CodeGenerationResult generationResult)> typesAndTheirgenerationResults = new();

        foreach ((var type, var report) in discoveredTypes)
        {
            var generationResult = codeGenerator.GenerateSaveAndLoadCode(report, session);
            typesAndTheirgenerationResults.Add((type, generationResult));
        }


        Type d_type = null;
        try
        {
            AssetDatabase.StartAssetEditing();
            //Debug.LogWarning(typesAndTheirgenerationResults.Count);

            foreach ((var type, var generationResult) in typesAndTheirgenerationResults)
            {
                d_type = type;

                _saveAndLoadService.IsTypeManuallyHandled_Editor(type, out bool hasManualInstanceHandler, out bool hasManualStaticHandler);


                List<string> parts = new();

                if (!hasManualStaticHandler)
                {
                    if (generationResult.StaticHandlerInfo != null)
                        parts.Add(generationResult.StaticHandlerInfo.GeneratedTypeText);
                    if (generationResult.StaticSaveDataInfo != null)
                        parts.Add(generationResult.StaticSaveDataInfo.GeneratedTypeText);
                }
                if (!type.IsStatic() && !hasManualInstanceHandler)
                {
                    parts.Insert(0, generationResult.HandlerInfo.GeneratedTypeText);

                    if (generationResult.SaveDataInfo != null) //customsavedatas and special savehandlers, like UnityEvent derived types, dont have savedata
                        parts.Insert(1, generationResult.SaveDataInfo.GeneratedTypeText);
                }


                if (parts.Count == 0)
                {
                    //everything is manually handled, nothing to generate
                    Debug.Log($"Skipping generation for type {type.CleanAssemblyQualifiedName()} because it has manual handlers for all parts.");
                    continue;
                }


                string mergedFileContent = parts.StringJoin(Environment.NewLine + Environment.NewLine);


                //Debug.LogError(mergedFileContent);


                var namespaces = new List<string>();

                if (generationResult.HandlerInfo != null)
                    namespaces.AddRange(generationResult.HandlerInfo.UsingStatements);
                if (generationResult.SaveDataInfo != null)
                    namespaces.AddRange(generationResult.SaveDataInfo.UsingStatements);

                if (generationResult.StaticHandlerInfo != null)
                    namespaces.AddRange(generationResult.StaticHandlerInfo.UsingStatements);
                if (generationResult.StaticSaveDataInfo != null)
                    namespaces.AddRange(generationResult.StaticSaveDataInfo.UsingStatements);


                string nameSpace;

                if (type.Namespace != null)
                {
                    nameSpace = type.Namespace.Replace(".", "_.") + "_";
                }
                else
                {
                    nameSpace = "GlobalNamespace";
                }


                CsFileBuilder builder = new CsFileBuilder()
                {
                    GeneratedTypeText = mergedFileContent,
                    NameSpace = nameSpace,
                    UsingStatements = namespaces.ToHashSet(),
                };







                string outputPath;
                string fileContent;


                if (_userSettings.GenerateSaveHandlersAsNestedClassesInsideHandledType && session.HasEditableSourceFile(type, out var path))
                {
                    using var reader = File.OpenText(path);
                    string originalSource = reader.ReadToEnd();

                    var typeName = type.Name;

                    if (type.IsGenericType)
                    {
                        typeName = typeName.Substring(0, typeName.IndexOf('`'));
                        typeName += "{" + new string(',', type.GetGenericArguments().Length - 1) + "}";
                    }

                    string tag = $"/// auto-generated for <see cref=\"{typeName}\"/>";


                    int indentationLevel = 1; //it is nested so it starts at 1

                    var declType = type.DeclaringType;

                    while (declType != null) //check how deeply nested its containing type is
                    {
                        indentationLevel++;
                        declType = declType.DeclaringType;
                    }

                    if (type.Namespace != null)
                    {
                        indentationLevel++; //namespace level
                    }

                    var generatedTypeText = builder.BuildFile(asNestedType: true, offset: indentationLevel);

                    generatedTypeText = $"#region SaveAndLoad AutoGenerated" + Environment.NewLine +
                        tag + Environment.NewLine + Environment.NewLine +
                        generatedTypeText + Environment.NewLine +
                        "#endregion";


                    var originalLines = originalSource.Split(Environment.NewLine).ToList();


                    var originalUsings = new HashSet<string>();

                    int i = 0;

                    int firstUsingLineIndex = -1;
                    int lastCommentedLineIndex = -1;
                    int directiveLevel = 0;

                    while (i < originalLines.Count)
                    {
                        var line = originalLines[i];

                        if (line.StartsWith("//"))
                        { lastCommentedLineIndex = i; i++; continue; } //skip file header comments

                        if (line.StartsWith("#if")) directiveLevel++;
                        if (line.StartsWith("#endif")) directiveLevel--;
                        if (directiveLevel > 0) { i++; continue; } //skip preprocessor directives

                        if (line.StartsWith("using "))
                        {
                            if (firstUsingLineIndex == -1)
                                firstUsingLineIndex = i;

                            originalUsings.Add(line);
                        }
                        else if (line.Contains("{"))
                            break;
                        i++;
                    }

                    if (originalUsings.Count > 0)
                    {
                        foreach (var ns in builder.UsingStatements)
                        {
                            if (!originalUsings.Contains(ns))
                            {
                                originalLines.Insert(firstUsingLineIndex, ns);
                            }
                        }
                    }
                    else
                    {
                        //no usings found, insert after file header comments
                        int insertIndex = lastCommentedLineIndex + 1;

                        originalLines.InsertRange(insertIndex, builder.UsingStatements);
                    }

                    //not so original anymore...
                    originalSource = string.Join(Environment.NewLine, originalLines);



                    i = 0;

                    while (i < originalLines.Count)
                    {
                        var line = originalLines[i];
                        if (line.Contains(tag)) break;
                        i++;
                    }

                    bool hasOldGeneratedCode = i < originalLines.Count;

                    if (hasOldGeneratedCode)
                    {
                        //replace old generated code
                        //finding the tag that indicates the end of generated code. Which is #endregion in this case

                        int startIndex = i - 1; //step back one line to include the start of the region
                        int endIndex = i;

                        while (endIndex < originalLines.Count)
                        {
                            var line = originalLines[endIndex];
                            if (line.Contains("#endregion")) break;
                            endIndex++;
                        }

                        var beforeLines = originalLines.Take(startIndex);
                        var afterLines = originalLines.Skip(endIndex + 1);

                        fileContent = string.Join(Environment.NewLine, beforeLines) + Environment.NewLine
                            + generatedTypeText + Environment.NewLine
                            + string.Join(Environment.NewLine, afterLines);
                    }
                    else //insert code at the end of the class
                    {
                        void UpdateTargetTypesAsmDefFileWithSaveAndLoadPhase1()
                        {
                            if (session.HasAsmDefFile(path, out var asmdefPath))
                            {
                                var guidReferencesToAdd = new List<string>
                                {
                                    "GUID:6fde86d971e9f584ca799004c9e38439", //SaveAndLoad
                                    "GUID:11f9f0384cd82b14ea5d004a22a3f210", //BlueUtils.Core
                                };


                                {
                                    string json = File.ReadAllText(asmdefPath);

                                    JObject root;
                                    try
                                    {
                                        root = JObject.Parse(json);
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.LogError($"Invalid asmdef JSON: {asmdefPath}\n{e}");
                                        return;
                                    }

                                    // Ensure "references" array exists
                                    if (root["references"] == null || root["references"].Type != JTokenType.Array)
                                    {
                                        root["references"] = new JArray();
                                    }

                                    var referencesArray = (JArray)root["references"];
                                    var existing = new HashSet<string>();

                                    foreach (var token in referencesArray)
                                    {
                                        if (token.Type == JTokenType.String)
                                            existing.Add(token.Value<string>());
                                    }

                                    bool changed = false;

                                    foreach (var guid in guidReferencesToAdd)
                                    {
                                        if (string.IsNullOrWhiteSpace(guid))
                                            continue;

                                        string formatted = guid.StartsWith("GUID:", StringComparison.OrdinalIgnoreCase)
                                            ? guid
                                            : $"GUID:{guid}";

                                        if (!existing.Contains(formatted))
                                        {
                                            referencesArray.Add(formatted);
                                            changed = true;
                                        }
                                    }

                                    if (!changed)
                                        return;

                                    // Write back pretty-printed JSON
                                    File.WriteAllText(
                                        asmdefPath,
                                        root.ToString(Formatting.Indented)
                                    );

                                    //AssetDatabase.ImportAsset(asmdefPath);
                                }
                            }
                        }

                        UpdateTargetTypesAsmDefFileWithSaveAndLoadPhase1();


                        var combined = CodeGenUtils.InsertNestedTypeIntoTargetType(type, originalSource, generatedTypeText);

                        fileContent = combined;
                    }

                    outputPath = path;
                }
                else
                {
                    fileContent = builder.BuildFile();

                    string fileName = type.IsStatic() ?
                        generationResult.StaticHandlerInfo.FileName + ".cs" :
                        generationResult.HandlerInfo.FileName + ".cs";


                    string absDirPath;


                    Type handlerTypeToLookFor = null;

                    if (!hasManualInstanceHandler)
                    {
                        //it has no manual handler, but has it any? Same for static
                        if (_saveAndLoadService.IsTypeHandled_Editor(type, false, out var handlerType))
                        {
                            handlerTypeToLookFor = handlerType;
                        }
                    }
                    else if (!hasManualStaticHandler)
                    {
                        if (_saveAndLoadService.IsTypeHandled_Editor(type, true, out var handlerType))
                        {
                            handlerTypeToLookFor = handlerType;
                        }
                    }


                    path = "";//otherwise error: path is unassigned

                    bool hasExistingNotManualHandler = handlerTypeToLookFor != null;
                    bool canEditItsSourceFile = hasExistingNotManualHandler && session.HasEditableSourceFile(handlerTypeToLookFor, out path);
                    //todo: stronger validation for this logic. Examine if the source file contains only the expected handler type
                    bool fileContainsOnlyGeneratedCode = hasExistingNotManualHandler && Path.GetFileName(path) == fileName; //save to override as is

                    if (hasExistingNotManualHandler)
                    {
                        if (canEditItsSourceFile)
                        {
                            if (!fileContainsOnlyGeneratedCode)
                            {
                                Debug.LogError($"It was detected that the sourcefile of a handler may contains user code, not just generated code. " +
                                    $"This is not allowed, please dont do this. If the {nameof(SaveAndLoadCodeGenSettings.GenerateSaveHandlersAsNestedClassesInsideHandledType)} " +
                                    $"is turned off, meaning the generated handlers would go into their separate soruce file, then the codegen logic expects that that the sourcefiles " +
                                    $"of these handlers contains generated code only. " +
                                    $"A new file will be created instead. " +
                                    $"Type: {type.CleanAssemblyQualifiedName()} HandlerType:{handlerTypeToLookFor.CleanAssemblyQualifiedName()} " +
                                    $"Path: {path}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Type has a savehandler but its file was either not found or readonly. " +
                                $"Type: {type.CleanAssemblyQualifiedName()} HandlerType: {handlerTypeToLookFor.CleanAssemblyQualifiedName()} " +
                                $"Path: {path}");
                        }
                    }



                    if (hasExistingNotManualHandler && canEditItsSourceFile && fileContainsOnlyGeneratedCode)
                    {
                        //todo: check if the namespace part of the path still matches the current namespace of the type, if not, move the file
                        absDirPath = Path.GetDirectoryName(path);
                    }
                    else
                    {
                        var namespaceAsPath = type.Namespace != null ? type.Namespace.Replace(".", "/") : "";

                        string assemblysFolder = $"_Project/Scripts/Generated/TheBlueWay/SaveHandlers/" +
                                                 $"{type.Assembly.GetName().Name}";

                        //todo: configurable path
                        string relativeDirPath = assemblysFolder + "/" +
                                                 $"{(type.IsStruct() ? "DevTestCustomDatas" : "DevTest")}/" +
                                                 $"{namespaceAsPath}";


                        relativeDirPath = relativeDirPath.Replace("/", Path.DirectorySeparatorChar.ToString());


                        absDirPath = Path.Combine(Application.dataPath, relativeDirPath);

                        if (!Directory.Exists(absDirPath))
                        {
                            Directory.CreateDirectory(absDirPath);

                        }


                        if (type.Assembly.GetName().Name != "Assembly-CSharp")
                        {
                            List<string> referencedAssmeblies = new()
                                {
                                    type.Assembly.GetName().Name,
                                    "GUID:6fde86d971e9f584ca799004c9e38439", //SaveAndLoad
                                    "GUID:11f9f0384cd82b14ea5d004a22a3f210", //BlueUtils.Core
                                };

                            var list = AssemblyResolver.GetAsmdefReferences(type.Assembly);
                            referencedAssmeblies.AddRange(list);

                            string references = "\"" + string.Join("\"," + Environment.NewLine + "\"", referencedAssmeblies) + "\"";


                            string asmdefFileContent = $@"
{{
    ""name"": ""{type.Assembly.GetName().Name}.Generated"",
    ""rootNamespace"": ""GlobalNamespace"",
    ""references"": [
        {references}
    ],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": false,
    ""precompiledReferences"": [],
    ""autoReferenced"": true,
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}}
";


                            string asmDefFileName = type.Assembly.GetName().Name + ".Generated.asmdef";

                            var asmdefPath = Path.Combine(Application.dataPath, assemblysFolder, asmDefFileName);

                            File.WriteAllText(asmdefPath, asmdefFileContent);
                        }
                    }


                    outputPath = Path.Combine(absDirPath, fileName);


                    //outdated: need some work to get this working again
                    //if (hasManualInstanceHandler && hasManualStaticHandler)
                    //{
                    //    if (_userSettings.GenerateExampleSaveHandlersForManuallyHandledTypesToo)
                    //    {
                    //        relativeDirPath = _userSettings.InactiveSaveHandlersFolder;
                    //        //Debug.Log("here " + type.FullName ?? type.Name);
                    //    }
                    //    else
                    //        continue;
                    //}
                }



                using var writer = File.CreateText(outputPath);

                writer.Write(fileContent);

                var fileName2 = Path.GetFileNameWithoutExtension(outputPath);

                //todo: take into account if the generated code was nested under target type.
                //or wether we generated static, instance, or both
                Debug.Log($"SaveHandler file {fileName2} created at {outputPath}");
            }

        }
        catch (Exception ex)
        {
            Debug.LogError($"something bad happend at type: {d_type.CleanAssemblyQualifiedName()}. Exception: " + ex.ToString());
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }


        Debug.Log("CodeGen finished");

        //uncomment after finished testing
        //RemoveSelectedFiles();
    }





    public Session NewSession()
    {
        var session = new Session
        {
            UserSettings = _userSettings,
        };

        return session;
    }



    public class Session
    {
        SaveAndLoadCodeGenWindow.Service _service = new();

        public Dictionary<Type, string> _typeToSourceFilePath = new();


        public SaveAndLoadCodeGenSettings UserSettings { get; set; }
        public SaveHandlerTypeGenerationSettingsRegistry TypeGenerationSettingsRegistry { get; } = new();



        public bool HasAsmDefFile(string path, out string asmdefPath)
        {
            var dir = Path.GetDirectoryName(path);

            var files = Directory.GetFiles(dir, "*.asmdef", SearchOption.TopDirectoryOnly);

            if (files.Length > 0)
            {
                asmdefPath = files[0];
                return true;
            }

            asmdefPath = null;
            return false;
        }



        public bool HasEditableSourceFile(Type type) => HasEditableSourceFile(type, out _);

        public bool HasEditableSourceFile(Type type, out string path)
        {
            if (_typeToSourceFilePath.TryGetValue(type, out path))
            {
                return path != null;
            }

            path = GetSourceFilePath(type);

            return path != null;
        }


        //todo: the "Get" logic should be in a utility class, not here. The caching can stay
        public string GetSourceFilePath(Type type)
        {
            if (_typeToSourceFilePath.TryGetValue(type, out var path))
            {
                return path;
            }

            var asm = type.Assembly;

            var dirsToLookIn = new List<string>();

            if (asm.GetName().Name == "Assembly-CSharp")
            {
                dirsToLookIn.Add(Application.dataPath);
            }
            else
            {
                var asmdDef = AssemblyResolver.GetAsdmDefInfoInDirs(asm, AssemblyResolver.EditableSourceFilesDirs);

                if (asmdDef == null) return null;

                dirsToLookIn.AddRange(asmdDef.OwnedDirectories);
            }


            var name = type.QualifiedName();


            foreach (var dir in dirsToLookIn)
            {
                var csFiles = _service.GetCsFiles(dir);

                foreach (var csPath in csFiles)
                {
                    var inspectionReport = Roslyn.CodeGen.InspectCodeFile(csPath);


                    bool found = false;

                    foreach (var report in inspectionReport.TypeReports)
                    {

                        //debug
                        //if (report.TypeName.Contains("GenZSa") && type.Name.Contains("GenZSa"))
                        //{

                        //}

                        if (report.Namespace == type.Namespace && report.TypeName == name)
                        {
                            found = true;
                            break;
                        }
                    }

                    //debug
                    //if (csPath.Contains("Z.cs"))
                    //{

                    //}

                    if (found)
                    {
                        _typeToSourceFilePath[type] = csPath;
                        return csPath;
                    }
                }
            }
            Debug.LogError("didnt found source file for type: " + type.CleanAssemblyQualifiedName());
            _typeToSourceFilePath[type] = null;
            return null;
        }
    }





    public class FileReport
    {
        public string FilePath;
        public List<TypeReport> TypeReports = new();
        public List<TypeReport> DependencyTypeReports = new();
    }
    public class TypeReport
    {
        public TypeReport StaticReport;
        public Type ReportedType;
        public GetFieldInfosReport FieldsReport;
        public IEnumerable<PropertyInfo> Properties;
        public List<MethodInfo> Methods;
        public IEnumerable<EventInfo> Events;
    }

    [Flags]
    public enum FieldInfoCodeGenValidationCode
    {
        None = 0,
        Valid = 1,
        NonPublic = 2,
        ReadOnly = 4,
        //Required, c#11
        Const = 8,
        UnityEvent = 16,
        Obsolete = 32,
        CompilerGenerated = 64,
        IncludedByTypeGenSettings = 128,
        ExcludedByTypeGenSettings = 256,
    }
    public class GetFieldInfosReport
    {
        public List<FieldInfoReport> FieldInfoReports = new();

        public List<FieldInfoReport> ValidFields =>
            FieldInfoReports.Where(r => r.ValidationCode.HasFlag(FieldInfoCodeGenValidationCode.Valid)).ToList();

        public List<FieldInfoReport> InvalidFields =>
        FieldInfoReports.Where(r => !r.ValidationCode.HasFlag(FieldInfoCodeGenValidationCode.Valid)).ToList();
    }
    public class FieldInfoReport
    {
        public FieldInfo FieldInfo;
        public FieldInfoCodeGenValidationCode ValidationCode;
    }

    public GetFieldInfosReport GetFieldInfos(Type type, BindingFlags binding, Session session)
    {
        var report = new GetFieldInfosReport();

        var fieldReports = type.GetFields(binding)
                               .Select(f => new FieldInfoReport() { FieldInfo = f });


        bool publicOnly = !binding.HasFlag(BindingFlags.NonPublic);
        bool isStatic = binding.HasFlag(BindingFlags.Static);


        foreach (var fieldReport in fieldReports)
        {
            var field = fieldReport.FieldInfo;

            if (session.TypeGenerationSettingsRegistry.HasSettingsForHandledType(type, isStatic, out var settings))
            {
                if (settings.HasInclusionModeFor(field, out var inclusionMode))
                {
                    switch (inclusionMode)
                    {
                        case MemberInclusionMode.Include:
                            fieldReport.ValidationCode |= FieldInfoCodeGenValidationCode.IncludedByTypeGenSettings;
                            fieldReport.ValidationCode |= FieldInfoCodeGenValidationCode.Valid;
                            break;

                        case MemberInclusionMode.Exclude:
                            fieldReport.ValidationCode |= FieldInfoCodeGenValidationCode.ExcludedByTypeGenSettings;
                            break;
                    }
                }
            }
            if (publicOnly && !field.IsPublic) fieldReport.ValidationCode |= FieldInfoCodeGenValidationCode.NonPublic;
            if (field.IsInitOnly) fieldReport.ValidationCode |= FieldInfoCodeGenValidationCode.ReadOnly;
            if (field.IsLiteral && !field.IsInitOnly) fieldReport.ValidationCode |= FieldInfoCodeGenValidationCode.Const;
            if (CodeGenUtils.IsCompilerGenerated(field)) fieldReport.ValidationCode |= FieldInfoCodeGenValidationCode.CompilerGenerated;
            if (typeof(UnityEvent).IsAssignableFrom(field.FieldType)) fieldReport.ValidationCode |= FieldInfoCodeGenValidationCode.UnityEvent;

            var obsolete = field.GetCustomAttribute<ObsoleteAttribute>(false);
            if (obsolete != null && (obsolete.IsError || _userSettings.IgnoreAnyObsolete)) fieldReport.ValidationCode |= FieldInfoCodeGenValidationCode.Obsolete;

            if (fieldReport.ValidationCode == FieldInfoCodeGenValidationCode.None)
                fieldReport.ValidationCode |= FieldInfoCodeGenValidationCode.Valid;

            report.FieldInfoReports.Add(fieldReport);
        }

        return report;
    }


    //todo: test out this method
    public static bool IsAutoImplemented(PropertyInfo prop) =>
prop.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Any() ||
(prop.GetMethod?.IsDefined(typeof(CompilerGeneratedAttribute), false) ?? false) ||
        (prop.SetMethod?.IsDefined(typeof(CompilerGeneratedAttribute), false) ?? false);

    //tests
    public int _field;
    int _field2;
    public int Prop1 { get; set; }
    public int Prop2 { get => _field; set => _field = value; }
    public int Prop3 { get { return _field; } set => _field = value; }
    public int prop4 { get; }
    public int prop9 { set { } }
    public int prop5 { set => _field = value; }
    public int prop6 { get => _field2; set => _field = value; }
    public int prop7 { get { var a = _field2 * 3; return _field; } set => _field = value; }
    public int prop8 { get { var a = _field2 * 3; return _field; } set { var a = _field * 3; _field = value; } }




    public static Type ResolveType(string assemblyName, string namespaceName, string typeName)
    {
        return CodeGenUtils.ResolveType(assemblyName, namespaceName, typeName);
    }






    public void ClearFileList()
    {
        _changedFiles.Clear();
        _selectedIndices.Clear();
    }


    private void RemoveSelectedFiles()
    {
        // Remove in reverse order to avoid index shifting
        var indices = new List<int>(_selectedIndices);
        indices.Sort((a, b) => b.CompareTo(a));


        foreach (var index in indices)
            _changedFiles.RemoveAt(index);

        _selectedIndices.Clear();
    }


    // For testing you can add dummy files
    [MenuItem("Window/SaveAndLoad/Test")]
    public static void AddDummy()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        var clone = JsonConvert.DeserializeObject<Shader>("{}");
        clone = (Shader)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Shader));
        clone = (Shader)typeof(Shader).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null).Invoke(null);

        FieldInfo m_CachedPtr = typeof(UnityEngine.Object).GetField("m_CachedPtr", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo m_InstanceID = typeof(UnityEngine.Object).GetField("m_InstanceID", BindingFlags.NonPublic | BindingFlags.Instance);

        m_CachedPtr.SetValue(clone, m_CachedPtr.GetValue(shader));
        m_InstanceID.SetValue(clone, m_InstanceID.GetValue(shader));
        Debug.Log(clone is null);
        Debug.Log(clone == null);
        Debug.Log(clone.name);
        Debug.Log(shader == clone);
        return;
        //debug
        //var window = GetWindow<SaveAndLoadCodeGenWindow>();

        //window.CreateTypeReportsAndRunCodeGen(new List<Type> { type });
    }









    public CodeGenUtils.Configuration _codeGenConfig = CodeGenUtils.CreateDefaultConfig();

    public CodeGenUtils.Configuration ToCodeGenConfig(SaveAndLoadCodeGenSettings userSettings)
    {
        if (userSettings == null) return _codeGenConfig;

        _codeGenConfig.IgnoreAnyObsolete = userSettings.IgnoreAnyObsolete;
        _codeGenConfig.NonPublicToo = userSettings.GenerateSaveHandlersAsNestedClassesInsideHandledType;
        return _codeGenConfig;
    }





}


[Serializable]
public class FileSystemEventArgsDto
{
    public WatcherChangeTypes ChangeType;
    public string FullPath;
    public string Name;
}



public static class FileSystemEventArgsExtensions
{
    public static FileSystemEventArgsDto ToDto(this FileSystemEventArgs e)
    {
        return new FileSystemEventArgsDto
        {
            ChangeType = e.ChangeType,
            FullPath = e.FullPath,
            Name = e.Name
        };
    }

    //public static FileSystemEventArgs FromDto(this FileSystemEventArgsDto dto)
    //{
    //    return new FileSystemEventArgs(
    //        (WatcherChangeTypes)Enum.Parse(typeof(WatcherChangeTypes), dto.ChangeType),
    //        Path.GetDirectoryName(dto.FullPath),
    //        dto.Name);
    //}
}
