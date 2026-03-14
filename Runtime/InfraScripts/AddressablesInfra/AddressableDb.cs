using Assets._Project.Scripts.UtilScripts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;
using Assets._Project.Scripts.UtilScripts.Misc;
using Assets._Project.Scripts.SaveAndLoad.SaveHandlerBases;
using Theblueway.Core.Runtime.Packages.com.blueutils.core.Runtime.Debugging.Logging;



#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using Assets._Project.Scripts.UtilScripts.Addressables;
#endif


namespace Assets._Project.Scripts.Infrastructure.AddressableInfra
{
    [CreateAssetMenu(fileName = "AddressableDb", menuName = "Scriptable Objects/Infra/Addressable Db")]
    public class AddressableDb : ScriptableSingleton<AddressableDb>
    {
        [Serializable]
        public class AddressableDTO
        {
            public RandomId id;
            public string unityId;
            public string address;
            public string assetPath;
            public string assetName; //its extended with asset type
            [NonSerialized]
            public bool isUnityBuiltinResource;
        }


        [Serializable]
        public class DataBase
        {
            public List<AddressableDTO> _addressables = new();

            //indexes
            [NonSerialized]
            public Dictionary<RandomId, AddressableDTO> _id;
            [NonSerialized]
            public Dictionary<string, AddressableDTO> _unityId;

            //document-based like columns
            public List<RandomId> _unityBuiltinResource;
            [NonSerialized]
            public HashSet<RandomId> _unityBuiltinResourceColumn;



            [Newtonsoft.Json.JsonIgnore]
            public bool Dirty { get; set; }


            public void BuildIndexesAndColumns()
            {
                if (_addressables == null)
                {
                    Debug.LogError("Addressables list is null. Cannot build indexes.");
                    return;
                }

                _id = new();
                _unityId = new();

                _unityBuiltinResourceColumn = new(_unityBuiltinResource);


                foreach (var entry in _addressables)
                {
                    if (!_id.ContainsKey(entry.id))
                    {
                        _id.Add(entry.id, entry);
                    }
                    else
                    {
                        Debug.LogError($"Duplicate ID found: {entry.id}. Skipping addition.");
                    }

                    if (!_unityId.ContainsKey(entry.unityId))
                    {
                        _unityId.Add(entry.unityId, entry);
                    }
                    else
                    {
                        Debug.LogError($"Duplicate unityId found: {entry.unityId}. SKipping addition.");
                    }

                    if (_unityBuiltinResourceColumn.Contains(entry.id))
                    {
                        entry.isUnityBuiltinResource = true;
                    }
                }
            }


            public void Add(AddressableDTO dto)
            {
                if (_id.ContainsKey(dto.id))
                {
                    Debug.LogError($"AddressableDb: AssetEntry with ID '{dto.id}' already exists. Skipping addition.");
                    return;
                }
                if (_unityId.TryGetValue(dto.unityId, out var existingAddressable))
                {
                    Debug.LogError($"Cant add AssetEntry entry with unityId '{dto.unityId}'\n" +
                             $"because an other AssetEntry had already been added with this unityId.\n" +
                             $"Already existing AssetEntry Id: {existingAddressable.id}.\n" +
                             $"Colliding AssetEntry Id: {dto.id}\n" +
                             $"Skipping addition.");
                    return;
                }

                if (dto.isUnityBuiltinResource)
                {
                    if (_unityBuiltinResourceColumn.Contains(dto.id))
                        Debug.LogError($"ERROR AssetDb: Add asset entry. Asset entry: {dto.id} tried to be added as unity built-in resource, " +
                            $"but the db already contains this id as such. Should not cause problmes, letting the entry continue to be added.");
                    else
                    {
                        _unityBuiltinResource.Add(dto.id);
                        _unityBuiltinResourceColumn.Add(dto.id);
                    }
                }

                _addressables.Add(dto);
                _id.Add(dto.id, dto);
                _unityId.Add(dto.unityId, dto);

                Dirty = true;

                Debug.Log($"DEBUG: Added AssetEntry with id: {dto.id}, name: {dto.assetName}, and unityId: {dto.unityId}'.");
            }


            public void Remove(AddressableDTO dto)
            {
                if (_addressables.Contains(dto))
                {
                    _addressables.Remove(dto);
                    _id.Remove(dto.id);
                    _unityId.Remove(dto.unityId);

                    if (dto.isUnityBuiltinResource)
                    {
                        _unityBuiltinResource.Remove(dto.id);
                        _unityBuiltinResourceColumn.Remove(dto.id);
                    }

                    Dirty = true;
                }
                else
                {
                    Debug.LogError($"AddressableDb: Addressable with ID '{dto.id}' and asset name '{dto.assetName}' does not exist. Cannot remove." +
                        $"Skipping removal.");
                }
            }


            public bool Update(AddressableDTO request)
            {
                if (!_id.ContainsKey(request.id))
                {
                    Debug.LogError($"ERROR AssetDb: Can not Update entry {request.id} because it is not in the database.");
                    return false;
                }
                if (_unityId.TryGetValue(request.unityId, out var existingDto))
                {
                    if (existingDto.id != request.id)
                    {
                        Debug.LogError($"ERROR AssetDb: Can not Update entry with unityid {request.unityId} because an other entry also has this unityId " +
                            $"but with a different asset id.\n" +
                            $"Existing entry's asset id: {existingDto.id}\n" +
                            $"Update request entry's asset id: {request.id}");
                        return false;
                    }
                }

                var existing = _id[request.id];
                _CopyFromInto(from: request, into: existing);

                return true;
            }


            public void _CopyFromInto(AddressableDTO from, AddressableDTO into)
            {
                into.unityId = from.unityId;
                into.address = from.address;
                into.assetName = from.assetName;
                into.assetPath = from.assetPath;
                into.isUnityBuiltinResource = from.isUnityBuiltinResource;
            }


            public static void _LogDebug(string message)
            {
                BlueDebug.Debug("AssetDb: " + message);
            }
        }



        public static void LogError(string message, Object context = null)
        {

        }




        public List<Object> _unityBuiltInResources;

        public GameObject _defaults;



#if UNITY_EDITOR

        public AddressableDTO ToUpdateDTO(AddressableAssetEntry entry, AddressableDTO from)
        {
            return ToDTO(entry, from.id);
        }

        public AddressableDTO ToDTO(AddressableAssetEntry entry)
        {
            return ToDTO(entry, RandomId.New);
        }

        public AddressableDTO ToDTO(AddressableAssetEntry entry, RandomId id)
        {
            var unityId = GetUnityId(entry.TargetAsset, entry.MainAssetType);
            var assetName = _service.GetExtendedAssetName(entry.AssetName(), entry.MainAssetType);

            return new AddressableDTO
            {
                id = id,
                unityId = unityId,
                address = entry.address,
                assetName = assetName,
                assetPath = entry.AssetPath,
            };
        }


        public AddressableDTO ToDTOFromUnityBuiltinResource(Object unityBuiltinResource)
        {
            var unityId = GetUnityId(unityBuiltinResource);
            var name = _service.GetExtendedAssetName(unityBuiltinResource);

            return new AddressableDTO
            {
                id = RandomId.New,
                unityId = unityId,
                assetName = name,
                assetPath = null,
                isUnityBuiltinResource = true
            };
        }





        [Tooltip("If true, will reuse existing IDs for addressables with the same asset name. " +
            "Can be used when the path of assets changed but their names not.")]
        [Obsolete]
        [HideInInspector]
        public bool _reuseIds = true;


        public void Refresh()
        {
            InitIfNeeded();


            //__db._unityBuiltinResource.Clear();
            //foreach ((var name, var asset) in _unityBuiltInResourcesByExtendedName)
            {
                //Debug.Log(asset.name);
                //var id = _unityBuiltInResourceExtendedNamesToObjectIdsMap[name];

                //__db._unityBuiltinResource.Add(id);
                //var unityId = GetUnityId(asset);

                //var dto = new AddressableDTO
                //{
                //    id = id,
                //    unityId = unityId,
                //    assetName = name,
                //};

                //__db.Add(dto);
            }
            //EditorUtility.SetDirty(this); 
            //AssetDatabase.Refresh();
            //return;



            //todo: remove the ones that are not used anymore?
            var assets = GetUnityBuiltinResources();
            //Debug.Log(assets.Count);
            foreach (var asset in assets)
            {
                var unityId = GetUnityId(asset);
                //Debug.Log(unityId);
                if (!__db._unityId.ContainsKey(unityId))
                {
                    var dto = ToDTOFromUnityBuiltinResource(asset);

                    __db.Add(dto);
                }
            }
            //EditorUtility.SetDirty(this);
            //AssetDatabase.Refresh();
            //return;


            var entries = AddressableUtils.GetAllAddressableEntries();

            {
                List<AddressableAssetEntry> subs = new List<AddressableAssetEntry>();
                foreach (var entry in entries)
                {
                    entry.GatherAllAssets(subs, false, true, true);
                }
                entries.AddRange(subs);
            }




            foreach (var entry in entries)
            {
                var unityId = GetUnityId(entry);
                string assetName = _service.GetExtendedAssetName(entry.AssetName(), entry.MainAssetType);

                if (string.IsNullOrEmpty(unityId))
                {
                    Debug.LogError($"Could not find unityId for addressable with asset name '{assetName}' and address '{entry.address}'. Skipping addition.");
                    continue;
                }


                if (__db._unityId.TryGetValue(unityId, out var dto))
                {
                    if (entry.address != dto.address)
                    {
                        string oldAddress = dto.address;

                        var updateRequest = ToUpdateDTO(entry, dto);

                        bool result = __db.Update(updateRequest);

                        if (result)
                        {
                            Debug.Log($"DEBUG: Updated addressable AssetEntry with id: {dto.id},\n" +
                                $"from old address: {oldAddress}\n" +
                                $"to new address: {dto.address}");
                        }
                    }
                    //Debug.LogError($"ERROR AssetDb: Can not add addressable: {entry.address} with unityId: {unityId}to assetdb " +
                    //    $"because an other AssetEntry is already registered with is unityId.\n" +
                    //    $"Other's ID: {dto.id}\n");
                    //the other dto might not be an addressable, it can be anything, so cant log more info about it
                }
                else
                {

                    var newDto = ToDTO(entry);

                    __db.Add(newDto);
                }
            }





            //foreach (var dbEntry in __db._addressables.ToList())
            //{
            //    if (!byAddress.ContainsKey(dbEntry.typedAddress))
            //    {
            //        Debug.Log($"DEBUG AddressableDb: Removing Addressable with asset name '{dbEntry.assetName}' and address '{dbEntry.typedAddress}' " +
            //            $"does not exist in the current addressables. Going to remove it from the database.");

            //        __db.Remove(dbEntry);
            //    }
            //}




            Debug.Log($"AddressableDb: Refresh completed.");

            if (__db.Dirty)
            {
                Debug.Log($"AddressableDb: Changes were detected. Going to save to disk.");
                SaveToDisk();
                __db.Dirty = false;
            }
            else
            {
                Debug.Log($"AddressableDb: No changes detected. No need to save to disk.");
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.Refresh();
        }


#endif

        new public static AddressableDb Singleton {
            get
            {
                var instance = ScriptableSingleton<AddressableDb>.Singleton;
                instance.InitIfNeeded();
                return instance;
            }
        }

        //[NonSerialized] //this is the way to drop the db
        public DataBase __db;

        public Service _service = new Service();






        [NonSerialized]
        public bool _didInit;


        public void InitIfNeeded()
        {
            if (_didInit) return;
            _didInit = true;

            __db.BuildIndexesAndColumns();

            CreateUnityBuiltInResourceLookUp();
            LoadInUnityBuiltInResourceObjectIds();
#if UNITY_EDITOR
            UpdateUnityBuiltInResourceObjectIdsIfNeeded();
#endif
        }





        public Dictionary<string, Object> _unityBuiltInResourcesByExtendedName = new();
        public Dictionary<RandomId, string> _unityBuiltInResourceObjectIdsToExtendedNameMap = new();
        public Dictionary<string, RandomId> _unityBuiltInResourceExtendedNamesToObjectIdsMap = new();



#if UNITY_EDITOR
        public void UpdateUnityBuiltInResourceObjectIdsIfNeeded()
        {
            bool updateNeeded = false;

            var knownAssetIdsByName = _unityBuiltInResourceObjectIdsToExtendedNameMap.Values.ToHashSet();

            foreach (string name in _unityBuiltInResourcesByExtendedName.Keys)
            {
                if (!knownAssetIdsByName.Contains(name))
                {
                    updateNeeded = true;
                    _unityBuiltInResourceObjectIdsToExtendedNameMap.Add(RandomId.Get(), name);
                }
            }


            List<RandomId> idsToRemove = new List<RandomId>();

            foreach ((var id, string name) in _unityBuiltInResourceObjectIdsToExtendedNameMap)
            {
                if (!_unityBuiltInResourcesByExtendedName.ContainsKey(name))
                {
                    updateNeeded = true;
                    idsToRemove.Add(id);
                    Debug.Log($"AddressableDb: Unity built-in resource with name '{name}' not found in the resources dictionary. Removing its ID entry.");
                }
            }

            foreach (var id in idsToRemove)
            {
                _unityBuiltInResourceObjectIdsToExtendedNameMap.Remove(id);
            }


            if (updateNeeded)
            {
                var path = Path.Combine(Application.streamingAssetsPath, "UnityBuiltInResourceObjectIds.json");

                if (!Directory.Exists(Application.streamingAssetsPath))
                {
                    Directory.CreateDirectory(Application.streamingAssetsPath);
                }

                using var fileStream = File.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);

                using var writer = new StreamWriter(fileStream);

                var json = JsonConvert.SerializeObject(_unityBuiltInResourceObjectIdsToExtendedNameMap.ToList(), Formatting.Indented);

                writer.Write(json);
            }

            if (updateNeeded)
                LoadInUnityBuiltInResourceObjectIds();
        }
#endif


        public void CreateUnityBuiltInResourceLookUp()
        {
            _unityBuiltInResourcesByExtendedName.Clear();

            var resources = GetAdditionalUnityAssets();
            resources.AddRange(_unityBuiltInResources);

            foreach (var obj in resources)
            {
                if (obj == null)
                {
                    Debug.LogWarning("AddressableDb: Null object found in Unity built-in resources list. Skipping.");
                    continue;
                };


                string extendedName = _service.GetExtendedAssetName(obj);

                if (_unityBuiltInResourcesByExtendedName.ContainsKey(extendedName))
                {
                    Debug.LogError($"AddressableDb: Duplicate Unity built-in resource extended name found: {extendedName}. Skipping addition.");
                    continue;
                }

                _unityBuiltInResourcesByExtendedName.Add(extendedName, obj);
            }
        }







        public void LoadInUnityBuiltInResourceObjectIds()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "UnityBuiltInResourceObjectIds.json");

            if (!File.Exists(path))
            {
                _unityBuiltInResourceObjectIdsToExtendedNameMap = new();
                _unityBuiltInResourceExtendedNamesToObjectIdsMap = new();
#if !UNITY_EDITOR
                Debug.LogError($"AddressableDb: Unity built-in resource object IDs file not found at {path}. " +
                    $"Continuing with an empty map.");
#endif
                return;
            }


            using var fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            using var reader = new StreamReader(fileStream);

            var json = reader.ReadToEnd();

            var keyvaluePairs = JsonConvert.DeserializeObject<List<KeyValuePair<RandomId, string>>>(json) ?? new();

            _unityBuiltInResourceObjectIdsToExtendedNameMap = keyvaluePairs.ToDictionary(kv => kv.Key, kv => kv.Value);
            _unityBuiltInResourceExtendedNamesToObjectIdsMap = keyvaluePairs.ToDictionary(kv => kv.Value, kv => kv.Key);
        }




        public List<Object> GetUnityBuiltinResources()
        {
            var result = new List<Object>();

            var additionals = GetAdditionalUnityAssets();

            result.AddRange(_unityBuiltInResources);
            result.AddRange(additionals);

            return result;
        }


        public List<Object> GetAdditionalUnityAssets()
        {
            var assets = new List<Object>();

            var image = _defaults.GetComponentInChildren<UnityEngine.UI.Image>(true);
            assets.Add(image.material);
            //assets.Add(image.material.shader);

            //lol: if the comp is on a child of the gameobject, then unity appends the (Instance) suffix to the name of the default PhysicsMaterial
            //var collider = _defaults.GetComponentInChildren<BoxCollider>(true);
            //assets.Add(collider.sharedMaterial);

            return assets;
        }


        public class Service
        {

#if UNITY_EDITOR
            public string GetTypedAddress(AddressableAssetEntry entry)
            {
                return $"{entry.address}.{entry.MainAssetType.Name}";
            }
#endif


            public string GetExtendedAssetName(Object asset)
            {
                return GetExtendedAssetName(asset.name, asset.GetType());
            }

            public string GetExtendedAssetName(string name, Type type)
            {
                string nameExtension = GetAssetNameExtension(type);
                return name + nameExtension;
            }


            public string GetAssetNameExtension(Object obj)
            {
                return GetAssetNameExtension(obj.GetType());
            }

            public string GetAssetNameExtension(Type type)
            {
                var typeName = type.Name;

                //todo: figure out something for this case
                if (type == typeof(UnityEngine.Audio.AudioMixerGroup))
                {
                    typeName = "AudioMixerGroupController";
                }

                return $" ({typeName})";
            }


#if UNITY_EDITOR
            public string GetExtendedAssetName(string assetPath)
            {
                var parts = assetPath.Split('@');
                string mainAssetPath = parts[0];

                Object asset = AssetDatabase.LoadAssetAtPath<Object>(mainAssetPath);

                if (parts.Length == 2)
                {
                    var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(mainAssetPath);
                    string subAssetNameAndType = parts[1];

                    int lastIndex = subAssetNameAndType.LastIndexOf('.');

                    var nameAndType = new string[]
                    {
                        subAssetNameAndType.Substring(0, lastIndex),
                        subAssetNameAndType.Substring(lastIndex + 1)
                    };

                    asset = subAssets.FirstOrDefault(sub => sub.name == nameAndType[0] && sub.GetType().Name == nameAndType[1]);
                }

                if (asset == null)
                {
                    Debug.LogError($"AddressableDb: Could not load asset at path: {assetPath}. Returning empty extended name.");
                    return string.Empty;
                }
                return GetExtendedAssetName(asset);
            }
#endif
        }




        public HashSet<RandomId> GetUnityBuiltinResourceIds()
        {
            return __db._unityBuiltinResourceColumn;
        }



        public void RegisterUnityBuiltinResources()
        {
            var ids = GetUnityBuiltinResourceIds();

            //Debug.Log(ids.Count);
            foreach (var id in ids)
            {
                var asset = _GetAssetById<Object>(id);

                var initContext = new AssetInitContext { instantiatedFromAssetId = id };
                Infra.S.Register(asset, ifHasntAlready: true, context: initContext, rootObject: false, createSaveHandler: true);
            }
        }










        public string _GetAddressById(RandomId id)
        {
            if (id.IsDefault)
            {
                //Debug.LogWarning("AddressableDb: Attempted to get address by default ID. Returning null.");
                return null;
            }


            if (__db._id.TryGetValue(id, out var dto))
            {
                return dto.address;
            }
            else
            {
                //Debug.LogWarning($"AddressableDb: No address found for ID {id}. Going to return a default value.");
                return null;
            }
        }


        public T _GetAssetById<T>(RandomId id) where T : UnityEngine.Object
        {
            //if (_unityBuiltInResourceObjectIdsToExtendedNameMap.TryGetValue(id, out var extendedName))
            if (__db._id.TryGetValue(id, out var dto) && dto.isUnityBuiltinResource)
            {
                var extendedName = dto.assetName;
                if (_unityBuiltInResourcesByExtendedName.TryGetValue(extendedName, out var obj))
                {
                    if (obj is T tObj)
                    {
                        return tObj;
                    }
                    else
                    {
                        Debug.LogError($"AddressableDb: Unity built-in resource with ID {id} and extended name '{extendedName}' is not of type {typeof(T).FullName}. It is of type {obj.GetType().FullName}. Returning null.");
                        return null;
                    }
                }
                else
                {
                    Debug.LogError($"AddressableDb: Unity built-in resource with ID {id} and extended name '{extendedName}' not found in the resources dictionary. Returning null.");
                    return null;
                }
            }




            string address = _GetAddressById(id);

            if (string.IsNullOrEmpty(address))
            {
                var asset = Infra.Singleton.GetObjectById<T>(id);

                if (asset != null)
                {
                    return asset;
                }
            }

            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError($"AddressableDb: Neither an address nor a registered object found for ID {id}. Cannot get asset.");
                return null;
            }


            var handle = Addressables.LoadAssetAsync<T>(address);
            _handles.Add(handle);

            return handle.WaitForCompletion();


            //handle.Completed += op =>
            //{
            //    if (handle.Status == AsyncOperationStatus.Failed)
            //    {
            //        Debug.LogError($"AddressableDb: Failed to load asset of type {typeof(T).FullName} at address '{address}' for ID {id}. exception: {handle.OperationException}");
            //    }
            //    if (handle.Status == AsyncOperationStatus.Succeeded)
            //    {
            //        return handle.Result;
            //    }
            //    else return default;
            //}
        }

        [HideInInspector]
        public List<object> _handles = new();


        public T GetAssetByIdOrFallback<T>(in T fallbackAsset, ref RandomId id) where T : UnityEngine.Object
        {
            if (id.IsDefault)
            {
                return fallbackAsset;
            }

            return _GetAssetById<T>(id);
        }




        public string GetTypedNameById(RandomId id)
        {
            if (__db._id.TryGetValue(id, out var dto))
            {
                return dto.assetName;
            }
            else
            {
                Debug.LogWarning($"AddressableDb: No asset name found for ID {id}. Going to return a default value.");
                return null;
            }
        }


        public string GetAssetNameById(RandomId id)
        {
            //if (id.IsDefault)
            //{
            //    Debug.LogWarning("AddressableDb: Attempted to get asset name by default ID. Returning null.");
            //    return null;
            //}

            if (__db._id.TryGetValue(id, out var dto))
            {
                string typedName = dto.assetName;

                string untypedName = GetNameFromTypedName(typedName);

                return untypedName;
            }
            else
            {
                Debug.LogWarning($"AddressableDb: No asset name found for ID {id}. Going to return a default value.");
                return null;
            }
        }




#if UNITY_EDITOR
        public AssetEntryInfo GetAssetEntryInfo(UnityEngine.Object asset)
        {
            var unityId = GetUnityId(asset);

            if (unityId == null)
            {
                if (asset != null)
                    BlueDebug.Debug($"AssetDb: No unityid found for unity object. name: {asset.name}");

                return null;
            }

            if (__db._unityId.TryGetValue(unityId, out var dto))
            {
                return GetAssetEntryInfo(dto);
            }
            else
            {
                BlueDebug.Debug($"AddressableDb: no entry for unityId: {unityId}.\n" +
                    $"name: {asset.name}",asset);
                return null;
            }
        }



        public AssetEntryInfo GetAssetEntryInfo(RandomId id)
        {
            if (__db._id.TryGetValue(id, out var dto))
            {
                return GetAssetEntryInfo(dto);
            }
            else
            {
                BlueDebug.Debug($"AddressableDb: no entry for asset id: {id}");
                return null;
            }
        }


        public AssetEntryInfo GetAssetEntryInfo(AddressableDTO dto)
        {
            var info = new AssetEntryInfo
            {
                assetId = dto.id,
                name = dto.assetName,
                assetPath = dto.assetPath,
                key = dto.address,
            };

            return info;
        }




#if UNITY_EDITOR
        public string GetUnityId(AddressableAssetEntry addressable)
        {
            return GetUnityId(addressable.TargetAsset, addressable.MainAssetType);
        }
#endif

        public string GetUnityId(Object unityObj)
        {
            return GetUnityId(unityObj, unityObj == null ? null : unityObj.GetType());
        }


        public string GetUnityId(Object unityObj, Type acutalAssetType)
        {
            if (unityObj == null)
            {
                BlueDebug.Debug("AddressableDb: object is null");
                return null;
            }

            string path = AssetDatabase.GetAssetPath(unityObj);

            if (string.IsNullOrEmpty(path))
            {

                if (unityObj.name == "Default UI Material")
                {
                    return _service.GetExtendedAssetName(unityObj);
                }

                BlueDebug.Debug($"AddressableDb: did not find asset path. Name: {unityObj.name}");

                return null;
            }

            string guid = AssetDatabase.AssetPathToGUID(path);

            if (string.IsNullOrEmpty(guid))
            {
                //Debug.Log($"[DEBUG] AddressableDb: did not find guid. Name: {unityObj.name}");
                guid = path; //idk if this is a good idea or not, time will tell
            }

            ///unity uses these special guids for internal reserved resources like Cube/Plain/Sphere etc... mesh
            ///the problem is he assigns the same guid for multiple resources
            ///for example all primitive meshes has the same guid
            //System.IO.File.WriteAllText("unityguids_34823748",
            //    UnityEngine.JsonUtility.ToJson(
            //        AssetDatabase.GetAllAssetPaths()
            //        .Select(path => AssetDatabase.GUIDFromAssetPath(path) + ": " + path)));
            if (guid.StartsWith("0000000"))
            {
                string extension = _service.GetExtendedAssetName(unityObj.name, acutalAssetType);
                guid += "/" + extension;
                return guid;
            }


            if (!AssetDatabase.IsSubAsset(unityObj))
            {
                return guid;
            }
            else
            {
                var extension = _service.GetExtendedAssetName(unityObj.name, acutalAssetType);
                return $"{guid}/{extension}";
            }
        }
#endif







        public string GetNameFromTypedName(string typedName)
        {
            string untypedName = typedName.Substring(0, typedName.LastIndexOf(" (", StringComparison.Ordinal));
            return untypedName;
        }

        //this code is duplicated, if you change this, change the duplicates too. ojfnds89735nsfkdjdfk
        public bool IsAssetType(Type type)
        {
            return typeof(UnityEngine.Object).IsAssignableFrom(type)
               && !typeof(UnityEngine.Component).IsAssignableFrom(type)
               && !typeof(UnityEngine.GameObject).IsAssignableFrom(type)
               && !typeof(UnityEngine.ScriptableObject).IsAssignableFrom(type);
        }






        //[ReadOnly]
        public int __majorVersion;
        //[ReadOnly]
        public int __minorVersion;
        //[ReadOnly]
        public int __patchVersion;

        public string _dbVersionsPathRelativeToAssetsFolder;
        public string _saveNameBase;



        public void SaveToDisk()
        {
            int major = __majorVersion;
            int minor = __minorVersion;
            int patch = __patchVersion;

            if (__db.Dirty)
            {
                minor++;
                patch = 0;
            }
            else patch++;

            string version = $"v{major}.{minor}.{patch}";


            var metadata = new AddressableDataBaseMetaData
            {
                version = version,
                creationDate = DateTime.UtcNow
            };
            var data = new AddressableDataBasePersistenData
            {
                metaData = metadata,
                db = __db
            };

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);


            string dirPath = Path.Combine(Application.dataPath, _dbVersionsPathRelativeToAssetsFolder);

            string fileName = $"{_saveNameBase}_{version}.json";

            string path = Path.Combine(dirPath, fileName);


            try
            {
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                if (File.Exists(path))
                {
                    Debug.LogError($"AddressableDb: An addressable db save file already exists at {path}." +
                        $"The save process is canceled. Do something with that file then comeback and trigger the flow again with some change, for example by adding some dummy asset.");
                    return;
                }


                File.WriteAllText(path, json);
                Debug.Log($"AddressableDb: Data saved to disk at {path}");

                __majorVersion = major;
                __minorVersion = minor;
                __patchVersion = patch;

                __db.Dirty = false; // Reset dirty flag after saving
            }
            catch (Exception e)
            {
                Debug.LogError($"AddressableDb: Failed to save data to disk. Error: {e.Message}");
            }
        }


        public class AddressableDataBasePersistenData
        {
            public AddressableDataBaseMetaData metaData;
            public DataBase db;
        }

        public class AddressableDataBaseMetaData
        {
            public string version;
            public DateTime creationDate;
        }
    }




    [Serializable]
    public class AssetEntryInfo
    {
        public RandomId assetId;
        //[ReadOnly] //cant not use because it makes the text hard to read and cant nobe selected
        public string name;
        public string assetPath;
        //[ReadOnly]
        [Tooltip("The key is what will be used to load the asset from whatever provider it is configured to")]
        public string key;
    }





    [Serializable]
    public class AssetEntryReference
    {
        public Object asset;

        [HideInInspector]
        public Object _lastKnownAsset;

        public AssetEntryInfo entryInfo;
        public RandomId assetId => entryInfo.assetId;
        public bool isValid => entryInfo != null;


#if UNITY_EDITOR
        public void UpdateReferenceIfNeeded()
        {
            if (_lastKnownAsset != asset)
            {
                entryInfo = AddressableDb.Singleton.GetAssetEntryInfo(asset);

                if (entryInfo is null)
                {
                    Debug.LogError("AssetEntryRefernce: Could not update asset reference because no asset entry found for the assigned asset.");
                }

                _lastKnownAsset = asset;
            }
        }
#endif
    }







    public static class AddressableExtensions
    {
#if UNITY_EDITOR
        public static string AssetName(this AddressableAssetEntry entry)
        {
            if (string.IsNullOrEmpty(entry.AssetPath))
            {
                var start = entry.address.IndexOf('[') + 1;
                var end = entry.address.IndexOf(']');
                var name = entry.address.Substring(start, end - start);
                return name;
            }
            return Path.GetFileNameWithoutExtension(entry.address);
        }
#endif
    }
}
