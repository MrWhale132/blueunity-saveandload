
using Assets._Project.Scripts.SaveAndLoad;
using System;
using System.Collections.Generic;
using System.Linq;
using Theblueway.SaveAndLoad.Editor;
using UnityEditor;

namespace Packages.com.theblueway.saveandload.Editor.SaveAndLoad
{
    public class SaveHandlerTypeGenerationSettingsRegistry
    {
        public Dictionary<long, List<SaveHandlerTypeGenerationConfiguration>> _foundTypeGenConfigurationsByHandlerId = new();
        public Dictionary<long, SaveHandlerTypeGenerationSettings> _typeGenerationSettingsByHandlerId = new();
        public bool _isBuilt = false;


        public bool HasSettingsForHandledType(Type handledType, bool isStatic, out SaveHandlerTypeGenerationSettings settings)
        {
            BuildLookupIfNeeded();

            var handlerId = SaveAndLoadManager.Service.GetHandlerIdByHandledType(handledType, isStatic);

            if (!_typeGenerationSettingsByHandlerId.ContainsKey(handlerId))
            {
                if (isStatic)
                {
                    if (_foundTypeGenConfigurationsByHandlerId.TryGetValue(handlerId, out var configs))
                    {
                        var settings_ = SaveHandlerTypeGenerationSettings.From(configs);

                        _typeGenerationSettingsByHandlerId.Add(handlerId, settings_);
                    }
                }
                else
                {
                    List<SaveHandlerTypeGenerationConfiguration> configs = new();

                    Type current = handledType;

                    while (current != null)
                    {
                        var id = SaveAndLoadManager.Service.GetHandlerIdByHandledType(current, false);

                        if (_foundTypeGenConfigurationsByHandlerId.TryGetValue(id, out var configsForCurrentType))
                        {
                            configs.AddRange(configsForCurrentType);
                        }

                        current = current.BaseType;
                    }

                    if (configs.Count > 0)
                    {
                        var settings_ = SaveHandlerTypeGenerationSettings.From(configs);

                        _typeGenerationSettingsByHandlerId.Add(handlerId, settings_);
                    }
                }
            }

            return _typeGenerationSettingsByHandlerId.TryGetValue(handlerId, out settings);

        }


        public void BuildLookupIfNeeded()
        {
            if (_isBuilt) return;

            _foundTypeGenConfigurationsByHandlerId.Clear();
            _typeGenerationSettingsByHandlerId.Clear();


            var typeGenConfigSOs = GatherAllScriptableConfigs();

            var byHandlerId = new Dictionary<long, List<SaveHandlerTypeGenerationConfiguration>>();

            foreach (var so in typeGenConfigSOs)
            {
                void Add(SaveHandlerTypeGenerationConfiguration config, bool isStatic)
                {
                    var handlerId = SaveAndLoadManager.Service.GetHandlerIdByHandledType(config.ConfiguredType, isStatic: isStatic, out bool found);

                    if (found)
                    {
                        if (!byHandlerId.ContainsKey(handlerId))
                        {
                            byHandlerId.Add(handlerId, new List<SaveHandlerTypeGenerationConfiguration>());
                        }
                        byHandlerId[handlerId].Add(config);
                    }
                }

                Add(so._config, false);
                Add(so._config, true);
            }

            _foundTypeGenConfigurationsByHandlerId = byHandlerId;


            _isBuilt = true;
        }








        public void CacheInvalidate()
        {
            _isBuilt = false;
            _typeGenerationSettingsByHandlerId.Clear();
        }


        public IEnumerable<SaveHandlerTypeGenerationConfigSO> GatherAllScriptableConfigs()
        {
            return AssetDatabase.FindAssets("t:" + nameof(SaveHandlerTypeGenerationConfigSO))
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<SaveHandlerTypeGenerationConfigSO>(path))
                .Where(asset => asset != null);
        }
    }
}
