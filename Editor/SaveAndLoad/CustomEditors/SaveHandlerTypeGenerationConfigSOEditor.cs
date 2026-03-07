using Packages.com.theblueway.saveandload.Editor.SaveAndLoad;
using System.Collections.Generic;
using Theblueway.Core.Runtime.Extensions;
using Theblueway.Core.Runtime.TypeInterfaceScripts;
using Theblueway.Core.Editor.Extensions;
using UnityEditor;
using UnityEngine;

namespace Theblueway.SaveAndLoad.Editor.SaveAndLoad.CustomEditors
{

    [CustomEditor(typeof(SaveHandlerTypeGenerationConfigSO))]
    [CanEditMultipleObjects]
    public class SaveHandlerTypeGenerationConfigSOEditor : UnityEditor.Editor
    {
        public static HashSet<int> _refreshed = new();

        public SerializedProperty _membersList;

        private void OnEnable()
        {
            _membersList = serializedObject.FindProperty(nameof(SaveHandlerTypeGenerationConfigSO._config)).FindPropertyRelative(nameof(SaveHandlerTypeGenerationConfiguration._memberConfigs));
        }


        public override void OnInspectorGUI()
        {
            foreach (var t in targets)
            {
                if (!_refreshed.Contains(t.GetInstanceID()))
                {
                    _refreshed.Add(t.GetInstanceID());
                    var so = (SaveHandlerTypeGenerationConfigSO)t;
                    RefreshDisplayValues(so._config);
                }
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

            if (config._memberConfigs is not null and not { Count: 0 })
            {
                //var members = TypeInterface.GetMembersOf(config._typeInterfaceId);

                foreach (var member in config._memberConfigs)
                {
                    member._typeInterfaceId = config._typeInterfaceId;
                    //member._members = members;
                }
            }
        }
    }
}

