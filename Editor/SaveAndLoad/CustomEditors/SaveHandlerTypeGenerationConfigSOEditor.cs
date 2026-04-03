using System.Collections.Generic;
using Theblueway.Core.Runtime.Extensions;
using Theblueway.TypeBinding.Editor.TypeInterfaceScripts;
using UnityEditor;
using UnityEngine;

namespace Theblueway.SaveAndLoad.Editor.SaveAndLoad.CustomEditors
{

    [CustomEditor(typeof(SaveHandlerTypeGenerationConfigSO))]
    [CanEditMultipleObjects]
    public class SaveHandlerTypeGenerationConfigSOEditor : UnityEditor.Editor
    {
        public static HashSet<int> _refreshed = new();


        public override void OnInspectorGUI()
        {
            foreach (var t in targets)
            {
                var so = (SaveHandlerTypeGenerationConfigSO)t;

                bool changedId = so._config._typeInterfaceId != so._config.__lastKnownTypeInterfaceId;

                if (!_refreshed.Contains(t.GetInstanceID()) || changedId)
                {
                    _refreshed.Add(t.GetInstanceID());
                    RefreshDisplayValues(so._config);

                    if (changedId) so._config._memberConfigs.Clear();
                }

                RefreshMemberConfigs(so._config);
            }


            serializedObject.Update();

            var prop = serializedObject.GetIterator();

            if (prop.NextVisible(true))
            {
                do
                {
                    EditorGUI.BeginProperty(Rect.zero, GUIContent.none, prop);
                    EditorGUILayout.PropertyField(prop, true);
                    EditorGUI.EndProperty();
                }
                while (prop.NextVisible(false));
                //Debug.Log("-----------");
            }

            serializedObject.ApplyModifiedProperties();
        }


        public static void RefreshDisplayValues(SaveHandlerTypeGenerationConfiguration config)
        {
            TypeInterfaceInfo info = TypeInterface.GetTypeInterfaceInfo(config._typeInterfaceId);

            if (info == null) return;

            string displayName = $"{info.HandledType.Name} | {info.HandledType.Assembly.GetName().Name} | {info.HandledType.Namespace}";
            config._displayTypeName = displayName;

            config.__lastKnownTypeInterfaceId = config._typeInterfaceId;
            config.__lastKnownTypeName = info.HandledType.CleanAssemblyQualifiedName();
        }

        public static void RefreshMemberConfigs(SaveHandlerTypeGenerationConfiguration config)
        {
            if (config._memberConfigs is not null and not { Count: 0 })
            {
                foreach (var member in config._memberConfigs)
                {
                    member._typeInterfaceId = config._typeInterfaceId;
                }
            }
        }
    }
}

