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
using Theblueway.Core.Runtime.Debugging.Logging;
using Theblueway.SaveAndLoad.Editor;
using Theblueway.Tools.Editor;
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
    //public List<FileSystemEventArgsDto> _changedFiles = new();
    public List<CodeGenTargetModel> _codegenTargets = new();
    public HashSet<int> _selectedIndices = new HashSet<int>(); // which entries are selected


    private SceneAsset _selectedSceneToScan;



    public bool IsSetUp => _state != null && _userSettings != null;




    private void OnEnable()
    {
        EnsureState();
    }
    private void OnDisable()
    {
        SaveState();
    }


    public void EnsureState()
    {
        var state = LoadState();
        if (state != null)
        {
            SetState(state);
            ValidateState();
        }
    }


    public void SaveState()
    {
        if (_state == null) return;

        _state._eventqueue = _eventQueue.ToList();
        _state._scrollPos = _scrollPos;
        _state._codegenTargets = _codegenTargets;
        _state._selectedIndices = _selectedIndices.ToList();
        _state._userSettings = _userSettings;
        
        _state._selectedFolderToScan = _selectedFolderToScan;

        if (_state != null)
            EditorUtility.SetDirty(_state);
        if (_userSettings != null)
            EditorUtility.SetDirty(_userSettings);

        AssetDatabase.SaveAssets();
    }




    public SaveAndLoadCodeGenWindowState LoadState()
    {
        var guids = AssetDatabase.FindAssets($"t:{nameof(SaveAndLoadCodeGenWindowState)}");

        if (guids.Length == 1)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var state = AssetDatabase.LoadAssetAtPath<SaveAndLoadCodeGenWindowState>(path);

            return state;
        }
        else return null;
    }

    public void SetState(SaveAndLoadCodeGenWindowState state)
    {
        _state = state;

        _selectedFolderToScan = state._selectedFolderToScan;
        _userSettings = state._userSettings;

        _eventQueue = new ConcurrentQueue<FileSystemEventArgsDto>(state._eventqueue);
        _scrollPos = state._scrollPos;
        _codegenTargets = state._codegenTargets;
        _selectedIndices = new HashSet<int>(state._selectedIndices);
    }



    public void ValidateState()
    {
        if (_codegenTargets.IsNotNullAndNotEmpty())
        {
            for (int i = _codegenTargets.Count - 1; i >= 0; i--)
            {
                var target = _codegenTargets[i];

                if (target.IsNotValid)
                {
                    _codegenTargets.RemoveAt(i);
                    _selectedIndices.Remove(i);
                }
            }
        }
    }










    public static DateTime __lastCheckedDeltaTime;

    private void OnInspectorUpdate()
    {
        if (DateTime.Now - __lastCheckedDeltaTime < TimeSpan.FromSeconds(1)) return;
        __lastCheckedDeltaTime = DateTime.Now;

        if (!IsSetUp) return;


        bool changed = false;

        while (_eventQueue.TryDequeue(out var e))
        {
            // Now safely handle on Unity’s main thread
            if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                continue;
            }


            if (e.FullPath.Replace("\\", "/").Contains("/Editor/"))
            {
                continue;
            }


            IEnumerable<Type> typesInFile = FindTypesInFile(e.FullPath);

            foreach (Type type in typesInFile)
            {
                CodeGenTargetModel codeGenTarget = CodeGenTargetModel.Create(type);

                var duplicate = _codegenTargets.Any(target => target.HasSameTargetAs(codeGenTarget));
                if (duplicate) continue;

                changed = true;
                _codegenTargets.Add(codeGenTarget);
            }
        }

        if (changed)
        {
            Repaint();
        }
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


        if (!IsSetUp)
        {
            GUILayout.Space(100);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();

            if (_state == null)
            {
                var guids = AssetDatabase.FindAssets($"t:{nameof(SaveAndLoadCodeGenWindowState)}");

                if (guids.Length > 0)
                {
                    if(guids.Length > 1)
                    {
                        Debug.Log($"There are multiple {nameof(SaveAndLoadCodeGenWindowState)} instances. There should be only one. " +
                            $"To stop this panel reappering leave only one instance.");
                    }


                    ObjectField(ref _state, "Window state", labelWidth: 120);

                    if (_state != null)
                    {
                        SetState(_state);
                        ValidateState();
                    }
                    else
                        GUILayout.Space(5);
                }


                if (GUILayout.Button("Create Default Window State", GUILayout.Width(200)))
                {
                    var path = Path.Combine("Assets", $"{nameof(SaveAndLoadCodeGenWindowState)}.asset");


                    if (!AssetDatabase.AssetPathExists(path))
                    {
                        _state = CreateInstance<SaveAndLoadCodeGenWindowState>();
                        AssetDatabase.CreateAsset(_state, path);
                        AssetDatabase.SaveAssets();

                        EditorGUIUtility.PingObject(_state);
                        Selection.activeObject = _state;
                    }
                    else
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<SaveAndLoadCodeGenWindowState>(path);

                        Debug.Log($"A {nameof(SaveAndLoadCodeGenWindowState)} instance is already exists at path {path}. Use that or move it.", asset);

                        EditorGUIUtility.PingObject(asset);
                        Selection.activeObject = asset;
                    }
                }
            }




            if (_userSettings == null)
            {
                GUILayout.Label("A codegen settings is required.");

                GUILayout.Space(5);

                ObjectField(ref _userSettings, "CodeGen Settings", labelWidth: 120);

                GUILayout.Space(5);

                if (GUILayout.Button("Create Default Settings", GUILayout.Width(200)))
                {
                    var path = Path.Combine("Assets", $"{nameof(SaveAndLoadCodeGenSettings)}.asset");

                    if (!AssetDatabase.AssetPathExists(path))
                    {
                        _userSettings = CreateInstance<SaveAndLoadCodeGenSettings>();
                        AssetDatabase.CreateAsset(_userSettings, path);
                        AssetDatabase.SaveAssets();

                        EditorGUIUtility.PingObject(_userSettings);
                        Selection.activeObject = _userSettings;
                    }
                    else
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<SaveAndLoadCodeGenSettings>(path);

                        Debug.Log($"A {nameof(SaveAndLoadCodeGenSettings)} instance is already exists at path {path}. Use that or move it.", asset);

                        EditorGUIUtility.PingObject(asset);
                        Selection.activeObject = asset;
                    }
                }
            }

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            return;
        }





        EditorGUILayout.LabelField("Changed Files", EditorStyles.boldLabel);


        EditorGUILayout.BeginHorizontal();
        // Scrollable list
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(200), GUILayout.Width(350));
        for (int i = 0; i < _codegenTargets.Count; i++)
        {
            var entry = _codegenTargets[i];

            if (entry.IsNotValid) continue;


            bool selected = _selectedIndices.Contains(i);

            var style = new GUIStyle("Button")
            {
                alignment = TextAnchor.MiddleLeft
            };


            bool newSelected = GUILayout.Toggle(selected, entry.Type.Name, style, GUILayout.Width(250));


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


        ObjectField(ref _userSettings, "CodeGen Settings", labelWidth: 120);


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
            var tasks = GetTypesFromSelected();

            CreateTypeReportsAndRunCodeGen(tasks);

            //turned off for easier dev testing
            //RemoveSelectedFiles();
        }



        EditorGUILayout.Space(40);


        if (GUILayout.Button("Ensure typegen configs"))
        {
            EnsureTypeGenConfigs();
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




    public static void ObjectField<T>(ref T obj, string label, int labelWidth, int objectFieldWidth = 150) where T : UnityEngine.Object
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));

        obj = (T)EditorGUILayout.ObjectField(
            obj,
            typeof(T),
            false,
            options: GUILayout.Width(objectFieldWidth));

        EditorGUILayout.EndHorizontal();
    }




    public void EnsureTypeGenConfigs()
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

        var unhandledTypes = new HashSet<Type>();

        foreach (var file in csFiles)
        {
            foreach (var type in FindTypesInFile(file))
            {
                if (!SaveAndLoadManager.Service.IsTypeHandled_Editor(type))
                {
                    unhandledTypes.Add(type);
                }
            }
        }

        return unhandledTypes;

        //var unhandledTypes = new HashSet<Type>();

        //foreach (var type in foundTypes)
        //{
        //    if (!_saveAndLoadService.IsTypeHandled_Editor(type)
        //        && !_userSettings.TypeExclusionSettings.ShouldExclude(type))
        //    {
        //        unhandledTypes.Add(type);
        //    }
        //}

        //return unhandledTypes;
    }


    IEnumerable<string> GetCsFiles(string root) => BlueTools.GetCsFiles(root);


    public class Service
    {
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
                            .Where(t => !SaveAndLoadManager.Service.IsTypeHandled_Editor(t)
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











    public IEnumerable<Type> GetTypesFromSelected()
    {
        foreach (var index in _selectedIndices)
        {
            var target = _codegenTargets[index];

            if (target.IsValid) yield return target.Type;
            else
            {
                BlueDebug.Debug($"codegen target is invalid");
            }
        }
    }





    public IEnumerable<Type> FindTypesInFile(string filePath)
    {
        List<Type> typesInFile = new List<Type>();

        var inspectionReport = Roslyn.CodeGen.InspectCodeFile(filePath);


        var assemblyName = AssemblyTools.ResolveAssembly(filePath);


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

            if (!_userSettings.GenerateSaveHandlersAsNestedClassesInsideHandledType && type.IsNested && !type.IsNestedPublic)
            {
                //skip non public nested types
                continue;
            }

            if (_userSettings != null && _userSettings.TypeExclusionSettings.ShouldExclude(type))
            {
                BlueDebug.Debug($"Type: {type.CleanAssemblyQualifiedName()} is excluded by type exlusion settings.");
                continue;
            }

            typesInFile.Add(type);
        }

        return typesInFile;
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
        HashSet<Type> discoveredTypes = new();

        Dictionary<(Type type, bool isStatic), TypeReport> typeReportsByLogicalTypes = new();




        foreach (var type in typesToHandle)
        {
            if (type == null)
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
                BlueDebug.Debug("Skipping obsolete type: " + type.AssemblyQualifiedName);
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
                    if (!discoveredTypes.Contains(type))
                        foreach (var constraint in type.GetGenericArguments().SelectMany(a => a.GetGenericParameterConstraints()))
                            discoveryQueue.Enqueue(constraint);
            }



            if (discoveredTypes.Contains(type)) continue;



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
            //if we failed once, no reason (so far) to check it again.
            discoveredTypes.Add(type);

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

                    foreach (var method in methods)
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
                    Methods = methods,
                    IsStatic = isStatic,
                };

                return typeReport;
            }




            var binding = BindingFlags.Public | BindingFlags.Static;

            if (_userSettings.GenerateSaveHandlersAsNestedClassesInsideHandledType && BlueTools.HasEditableSourceFile(type))
            {
                binding |= BindingFlags.NonPublic;
            }


            var staticReport = CreateTypeReport(type, binding);


            bool hasStaticTypeGenSettings = session.TypeGenerationSettingsRegistry.HasSettingsForHandledType(type, isStatic: true, out var _);

            if (hasStaticTypeGenSettings || staticReport.HasAnyMember)
            {
                typeReportsByLogicalTypes.Add((type, isStatic: true), staticReport);
            }


            if (!type.IsStatic())
            {
                binding |= BindingFlags.Instance;
                binding &= ~BindingFlags.Static;

                var instanceReport = CreateTypeReport(type, binding);

                typeReportsByLogicalTypes.Add((type, isStatic: false), instanceReport);


                //var staticReport = typeReport;

                //doc:
                //we dont generate static handler if there is no static member of the handled type to reduce clutter and unnecessary types
                //however, it is possible that the typegen configs specified for the type exclude all members
                //we assume there is a reason the user created a config for this type, so we interpet it as an enforcement
                //if (hasNoTypeGenSettings
                //    && staticReport.FieldsReport.ValidFields.Count == 0
                //    && staticReport.Properties.Count() == 0
                //    && staticReport.Events.Count() == 0
                //    && staticReport.Methods.Count == 0)
                //{
                //    //if there is no static members, do not generate a static handler
                //    instanceReport.StaticReport = null;
                //}
                //else
                //{
                //    instanceReport.StaticReport = staticReport;
                //}

                //typeReport = instanceReport;
            }


            //discoveredTypes[type] = typeReport;


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

                if (typeReportsByLogicalTypes.TryGetValue((type, isStatic: false), out var instanceReport))
                {
                    dependencies.AddRange(GetDependencies(instanceReport));
                }

                if (typeReportsByLogicalTypes.TryGetValue((type, isStatic: true), out var staticReport2))
                {
                    dependencies.AddRange(GetDependencies(instanceReport));
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





        List<(Type, TypeReport)> validatedTypes = new();

        {
            foreach ((var logicalType, var discoveryReport) in typeReportsByLogicalTypes)
            {
                if (Validate(logicalType.type, logicalType.isStatic, discoveryReport))
                {
                    validatedTypes.Add((logicalType.type, discoveryReport));
                }

                bool Validate(Type type, bool isStatic, TypeReport typeReport)
                {

                    if (_saveAndLoadService.IsTypeHandled_Editor(type, isStatic)
                     && !_saveAndLoadService.IsTypeManuallyHandled_Editor(type, isStatic))
                    {
                        var handlerType = _saveAndLoadService.GetSaveHandlerTypeFrom(type, isStatic);
                        if (handlerType == null) return true;


                        var existingMethodSignatureToIdMap = SaveAndLoadCodeInspection.GetMethodSignatureToMethodIdMap(type, isStatic: isStatic, handlerType);
                        //Debug.Log(typeReport.Methods.ToString());
                        HashSet<string> currentMethodSignatures = typeReport.Methods.Select(m => TypeUtils.GetMethodSignature(m)).ToHashSet();

                        Dictionary<string, long> invalidMethodSignatures = new();

                        foreach (var (signature, id) in existingMethodSignatureToIdMap)
                        {
                            if (!currentMethodSignatures.Contains(signature))
                            {
                                invalidMethodSignatures.Add(signature, id);
                            }
                        }


                        if (invalidMethodSignatures.Count > 0)
                        {
                            var handlerId = _saveAndLoadService.GetSaveHandlerAttributeOfType_Editor(type, isStatic).Id;

                            var invalidList = invalidMethodSignatures.Select(x => x.Key + " " + x.Value).StringJoin("\n");
                            var currentList = currentMethodSignatures.StringJoin("\n");

                            string staticText = isStatic ? "(static) " : "";

                            Debug.LogError($"SaveHandler with id {handlerId} for target type: {staticText}{type.FullName} has method signature entries that does no match any of the currently existing methods of target type. " +
                                $"This will cause runtime errors because the handler will not be able to find the methods in the type. " +
                                $"This can happen when you modify a method but does not update its methodsignature map. " +
                                $"You can fix this by manually updating the method signature map with the correct method signatures provided below. " +
                                $"Removing these entries and letting new ones be generated will result in that previous saves wont be able " +
                                $"to find these methods as the methodids they serialized will no longer exist on next load. " +
                                $"\n\nInvalid method signatures:\n{invalidList}\n\nCurrent method signatures:\n{currentList}\n");

                            return false;
                        }
                        else return true;
                    }
                    else return true;
                }

                //if (discoveryReport.IsStatic)
                //{
                //    if (Validate(logicalType, isStatic: true, discoveryReport))
                //    {
                //        validatedTypes.Add((logicalType, discoveryReport));
                //    }
                //}
                //else
                //{
                //    if (discoveryReport.HasStaticReport && !Validate(logicalType, isStatic: true, discoveryReport.StaticReport))
                //    {
                //        discoveryReport.StaticReport = null;
                //    }

                //    if (Validate(logicalType, isStatic: false, discoveryReport))
                //    {
                //        validatedTypes.Add((logicalType, discoveryReport));
                //    }
                //    else if (discoveryReport.HasStaticReport) validatedTypes.Add((logicalType, discoveryReport.StaticReport));
                //}
            }
        }







        //dev test/debug
        //var names = discoveredTypes.Select(t => t.Key.FullName).OrderBy(name => name).ToList();

        //if (Directory.Exists("C:/temp") == false) Directory.CreateDirectory("C:/temp");
        //File.WriteAllText("C:/temp/discoveredTypes.txt", string.Join("\n", names));




        var codeGenerator = ScriptableObject.CreateInstance<SaveHandlerAutoGenerator>();


        List<((Type type, bool isStatic), CodeGenerationResult generationResult)> typesAndTheirgenerationResults = new();

        foreach ((var type, var report) in validatedTypes)
        {
            var generationResult = codeGenerator.GenerateSaveAndLoadCode(report, session);
            typesAndTheirgenerationResults.Add(((type, report.IsStatic), generationResult));
        }


        Type d_type = null;
        try
        {
            AssetDatabase.StartAssetEditing();
            //Debug.LogWarning(typesAndTheirgenerationResults.Count);

            foreach ((var logicalType, var generationResult) in typesAndTheirgenerationResults)
            {
                Type type = logicalType.type;
                d_type = type;


                //_saveAndLoadService.IsTypeManuallyHandled_Editor(type, out bool hasManualInstanceHandler, out bool hasManualStaticHandler);
                var manuallyHandled = _saveAndLoadService.IsTypeManuallyHandled_Editor(type, logicalType.isStatic);


                List<string> parts = new();

                if (!manuallyHandled)
                {
                    if (logicalType.isStatic)
                    {
                        if (generationResult.StaticHandlerInfo != null)
                            parts.Add(generationResult.StaticHandlerInfo.GeneratedTypeText);
                        if (generationResult.StaticSaveDataInfo != null)
                            parts.Add(generationResult.StaticSaveDataInfo.GeneratedTypeText);
                    }
                    else
                    {
                        parts.Insert(0, generationResult.HandlerInfo.GeneratedTypeText);

                        if (generationResult.SaveDataInfo != null) //customsavedatas and special savehandlers, like UnityEvent derived types, dont have savedata
                            parts.Insert(1, generationResult.SaveDataInfo.GeneratedTypeText);
                    }
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


                if (_userSettings.GenerateSaveHandlersAsNestedClassesInsideHandledType && BlueTools.HasEditableSourceFile(type, out var path))
                {
                    using var reader = File.OpenText(path);
                    string originalSource = reader.ReadToEnd();

                    //var typeName = type.Name;

                    //if (type.IsGenericType)
                    //{
                    //    typeName = typeName.Substring(0, typeName.IndexOf('`'));
                    //    typeName += "{" + new string(',', type.GetGenericArguments().Length - 1) + "}";
                    //}

                    //string tag = $"/// auto-generated for <see cref=\"{typeName}\"/>";

                    var (beginTag, endTag) = SaveHandlerAutoGenerator.GenerateBeginAndEndTagsForGeneratedHandlerSection(type, generationResult.HasStaticHandler);


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

                    generatedTypeText =
                        //$"#region SaveAndLoad AutoGenerated" + Environment.NewLine +
                        beginTag + Environment.NewLine +
                        generatedTypeText + Environment.NewLine +
                        endTag;


                    var originalLines = originalSource.Split(Environment.NewLine).ToList();

                    var alteredLines = new List<string>(originalLines);

                    {

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
                                    alteredLines.Insert(firstUsingLineIndex, ns);
                                }
                            }
                        }
                        else
                        {
                            //no usings found, insert after file header comments
                            int insertIndex = lastCommentedLineIndex + 1;

                            alteredLines.InsertRange(insertIndex, builder.UsingStatements);
                        }
                    }





                    {
                        string alteredSource = string.Join(Environment.NewLine, alteredLines);

                        var (startLine, endLine) = CodeGenUtils.FindTypeDeclarationStartAndEndLineIndexInFile(type, alteredSource);

                        int i = endLine - 1;

                        while (i > startLine)
                        {
                            var line = alteredLines[i];
                            if (line.Contains("#region SaveAndLoad AutoGenerated")) break;
                            i--;
                        }


                        bool hasSaveAndLoadRegion = i > startLine;

                        if (hasSaveAndLoadRegion)
                        {
                            int regionStartIndex = i;
                            int regionEndIndex = i;


                            while (i < endLine)
                            {
                                var line = alteredLines[i];
                                if (line.Contains("#endregion")) break;
                                i++;
                            }

                            regionEndIndex = i;

                            int startIndex = regionStartIndex;
                            int endIndex = regionEndIndex;


                            i = regionStartIndex;

                            while (i < regionEndIndex)
                            {
                                var line = alteredLines[i];
                                if (line.Contains(beginTag)) break;
                                i++;
                            }

                            bool hasOldCode = i < regionEndIndex;

                            if (hasOldCode)
                            {
                                startIndex = i;

                                while (i < regionEndIndex)
                                {
                                    var line = alteredLines[i];
                                    if (line.Contains(endTag)) break;
                                    i++;
                                }

                                endIndex = i + 1;
                            }
                            else
                            {
                                //no old code, insert the generated code before the endregion tag
                                startIndex = regionEndIndex;
                                endIndex = regionEndIndex - 1;
                            }


                            var beforeLines = alteredLines.Take(startIndex);
                            var afterLines = alteredLines.Skip(endIndex);

                            var beforeSection = string.Join(Environment.NewLine, beforeLines);
                            var afterSection = string.Join(Environment.NewLine, afterLines);

                            fileContent = beforeSection + Environment.NewLine
                                + generatedTypeText + Environment.NewLine
                                + afterSection;
                        }
                        else //insert code at the end of the class
                        {
                            UpdateTargetTypesAsmDefFileWithSaveAndLoadReferences();

                            generatedTypeText = "#region SaveAndLoad AutoGenerated" + Environment.NewLine + Environment.NewLine +
                                                generatedTypeText + Environment.NewLine + Environment.NewLine +
                                                "#endregion";

                            var combined = CodeGenUtils.InsertNestedTypeIntoTargetType(type, alteredSource, generatedTypeText);

                            fileContent = combined;


                            void UpdateTargetTypesAsmDefFileWithSaveAndLoadReferences()
                            {
                                if (BlueTools.HasAsmDefFile(path, out var asmdefPath))
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
                        }

                        outputPath = path;
                    }
                }
                else
                {
                    fileContent = builder.BuildFile();

                    string fileName = logicalType.isStatic ?
                        generationResult.StaticHandlerInfo.FileName + ".cs" :
                        generationResult.HandlerInfo.FileName + ".cs";


                    string absDirPath;


                    Type handlerTypeToLookFor = null;

                    if (!manuallyHandled)
                    {
                        //it has no manual handler, but has it any? Same for static
                        if (_saveAndLoadService.IsTypeHandled_Editor(type, logicalType.isStatic, out var handlerType))
                        {
                            handlerTypeToLookFor = handlerType;
                        }
                    }


                    path = "";//otherwise error: path is unassigned

                    bool hasExistingNotManualHandler = handlerTypeToLookFor != null;
                    bool canEditItsSourceFile = hasExistingNotManualHandler && BlueTools.HasEditableSourceFile(handlerTypeToLookFor, out path);
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

                        string assemblyFolder = Path.Combine(_userSettings.FolderPathForGeneratedCode,
                                                    "TheBlueWay/SaveHandlers",
                                                 $"{type.Assembly.GetName().Name}");

                        //todo: configurable path
                        string relativeDirPath = assemblyFolder + "/" +
                                                 $"{(type.IsStruct() ? "CustomSaveDatas" : "SaveHandlers")}/" +
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

                            var list = AssemblyTools.GetAsmdefReferences(type.Assembly);
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

                            var asmdefPath = Path.Combine(Application.dataPath, assemblyFolder, asmDefFileName);

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

        public SaveAndLoadCodeGenSettings UserSettings { get; set; }
        public SaveHandlerTypeGenerationSettingsRegistry TypeGenerationSettingsRegistry { get; } = new();
    }





    public class FileReport
    {
        public string FilePath;
        public List<TypeReport> TypeReports = new();
        public List<TypeReport> DependencyTypeReports = new();
    }



    public class TypeReport
    {
        public Type ReportedType;
        public GetFieldInfosReport FieldsReport;
        public IEnumerable<PropertyInfo> Properties;
        public List<MethodInfo> Methods;
        public IEnumerable<EventInfo> Events;

        public bool IsStatic { get; set; }

        public bool HasNoMembers => FieldsReport.ValidFields.Count == 0
                                    && Properties.Count() == 0
                                    && Events.Count() == 0
                                    && Methods.Count == 0;

        public bool HasAnyMember => !HasNoMembers;
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





    private void DiscardSelected()
    {
        RemoveSelectedFiles();
    }



    public void ClearFileList()
    {
        _codegenTargets.Clear();
        _selectedIndices.Clear();
    }


    private void RemoveSelectedFiles()
    {
        // Remove in reverse order to avoid index shifting
        var indices = new List<int>(_selectedIndices);
        indices.Sort((a, b) => b.CompareTo(a));


        foreach (var index in indices)
            _codegenTargets.RemoveAt(index);

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




public enum CodeGenTargetIdentifier
{
    AssemblyQualifiedTypeName,
    SaveHandlerId,
}

[Flags]
public enum CodeGenTargetState : uint
{
    /// <summary>
    /// Not validated yet
    /// </summary>
    None = 0,
    Valid = 1,
    Invalid = 1u << 31,
    NotFound = Invalid | (1u << 1),
}


[Serializable]
public class CodeGenTargetModel
{
    //cant store this, it needs to be resolved every time because types can be moved/renamed/removed and the file path will not be updated accordingly, so we need to try to resolve it every time and if it fails, we mark it as not found. We can store the file path in editor only and use it for resolving, but it will not be reliable and will not work for types that are not defined in a file, like generated types, types from external assemblies, etc.
    //public string containingSourceFilePath;
    public CodeGenTargetIdentifier identifierType;

    public CodeGenTargetState _validityState;
    public string _assemblyQualifiedTypeName;
    public long _saveHandlerId;
    public bool _isStaticPart;

    public Type _resolvedTypeCache;

    public bool IsValid => GetValidityState() == CodeGenTargetState.Valid;
    public bool IsNotValid => !IsValid;

    public Type Type {
        get
        {
            ResolveIdentifierIfNeeded();
            return _resolvedTypeCache;
        }
        set
        {
            SetType(value);
        }
    }

    public static CodeGenTargetModel Create(Type type)
    {
        var view = new CodeGenTargetModel()
        {
        };

        view.SetType(type);
        return view;
    }


    public CodeGenTargetState GetValidityState()
    {
        ResolveIdentifierIfNeeded();

        var state = CodeGenTargetState.None;

        if (_resolvedTypeCache == null)
        {
            state |= CodeGenTargetState.NotFound;
        }

        if (state == CodeGenTargetState.None)
        {
            state = CodeGenTargetState.Valid;
        }

        return state;
    }


    public void ResolveIdentifierIfNeeded()
    {
        if (_resolvedTypeCache != null) return;


        if (identifierType == CodeGenTargetIdentifier.AssemblyQualifiedTypeName)
        {
            _resolvedTypeCache = Type.GetType(_assemblyQualifiedTypeName);
        }
        else if (identifierType == CodeGenTargetIdentifier.SaveHandlerId)
        {
            _resolvedTypeCache = SaveAndLoadManager.Service.GetHandledTypeByHandlerId(_saveHandlerId);
            _assemblyQualifiedTypeName = _resolvedTypeCache?.CleanAssemblyQualifiedName();
        }
    }

    public void SetType(Type type)
    {
        identifierType = CodeGenTargetIdentifier.AssemblyQualifiedTypeName;
        _assemblyQualifiedTypeName = type.CleanAssemblyQualifiedName();

        if (SaveAndLoadManager.Service.HasTypeId(type, isStatic: false, out _saveHandlerId))
        {
            identifierType = CodeGenTargetIdentifier.SaveHandlerId;
            _isStaticPart = false;
        }
        else if (SaveAndLoadManager.Service.HasTypeId(type, isStatic: true, out _saveHandlerId)) { }
        {
            identifierType = CodeGenTargetIdentifier.SaveHandlerId;
            _isStaticPart = true;
        }
    }


    public bool HasSameTargetAs(CodeGenTargetModel other)
    {
        if (other == null) return false;

        if (identifierType is CodeGenTargetIdentifier.AssemblyQualifiedTypeName)
        {
            return _assemblyQualifiedTypeName.Equals(other._assemblyQualifiedTypeName);
        }
        else if (identifierType is CodeGenTargetIdentifier.SaveHandlerId)
        {
            return _saveHandlerId == other._saveHandlerId;
        }
        else
        {
            Debug.LogError($"Unknown state for enum: {nameof(CodeGenTargetIdentifier)}");
            return false;
        }
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
