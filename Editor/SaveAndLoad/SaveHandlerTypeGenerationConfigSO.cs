using Packages.com.theblueway.saveandload.Editor.SaveAndLoad;
using System;
using System.Collections.Generic;
using Theblueway.Core.Editor.EditorWindows;
using Theblueway.Core.Runtime.InspectorAttributes;
using Theblueway.TypeBinding.Editor.TypeInterfaceScripts;
using Theblueway.TypeBinding.Runtime.TypeInterfaceScripts;
using UnityEditor;
using UnityEngine;

namespace Theblueway.SaveAndLoad.Editor
{

    [CreateAssetMenu(fileName = "SaveHandlerTypeGenerationConfig", menuName = "Scriptable Objects/SaveAndLoad/SaveHandlerTypeGenerationConfig")]
    public class SaveHandlerTypeGenerationConfigSO : UnityEngine.ScriptableObject
    {
        [InlineDrawing]
        public SaveHandlerTypeGenerationConfiguration _config;

        //todo: requirement: move the savehandler codegen logic out from CodeGenWindow
        [HideInInspector]
        public bool _triggerSaveHandlerRegeneration;


        public void OnValidate()
        {
            if (_triggerSaveHandlerRegeneration)
            {
                _triggerSaveHandlerRegeneration = false;

            }


            _config.logContext = this;
        }
    }



    [Serializable]
    public class SaveHandlerTypeGenerationConfiguration
    {
        [TypeInterfaceSelector]
        public long _typeInterfaceId;

        [ReadOnly]
        public string _displayTypeName;
        [ReadOnly]
        public long __lastKnownTypeInterfaceId;
        [ReadOnly]
        public string __lastKnownTypeName;

        public int _loadOrder;


        public List<MemberConfig> _memberConfigs;

        [HideInInspector]
        public UnityEngine.Object logContext;


        public Type ConfiguredType => TypeInterface.GetHandledType(_typeInterfaceId);



        [Serializable]
        public class MemberConfig
        {
            public string memberName;

            [ReadOnly]
            public int _memberIndex = -1;

            public MemberInclusionMode inclusionMode = MemberInclusionMode.Include;
            public string directive;


#if UNITY_EDITOR
            //for editor drawing
            [HideInInspector] public long _typeInterfaceId;
#endif
        }
    }


    [CustomPropertyDrawer(typeof(SaveHandlerTypeGenerationConfiguration.MemberConfig))]
    public class MemberConfigPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            //return 60;
            float height = EditorGUI.GetPropertyHeight(property, false);

            if (!property.isExpanded) return height;

            var copy = property.Copy();
            var end = copy.GetEndProperty();
            copy.NextVisible(true); // enter children

            while (!SerializedProperty.EqualContents(copy, end))
            {
                bool includeChildren = copy.isExpanded && copy.hasVisibleChildren;
                height += EditorGUI.GetPropertyHeight(copy, includeChildren);
                height += EditorGUIUtility.standardVerticalSpacing;
                copy.NextVisible(false);
            }

            return height;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float y = position.y;

            float h = EditorGUI.GetPropertyHeight(property, false);

            Rect headerRect = new Rect(position.x, y, position.width, h);
            bool expanded = EditorGUI.PropertyField(headerRect, property, false);

            y += h + EditorGUIUtility.standardVerticalSpacing;

            if (!expanded)
                return;

            var copy = property.Copy();
            var end = copy.GetEndProperty();




            copy.NextVisible(true);

            while (!SerializedProperty.EqualContents(copy, end))
            {
                h = EditorGUI.GetPropertyHeight(copy, true);

                Rect r = new Rect(position.x, y, position.width, h);

                if (copy.name == nameof(SaveHandlerTypeGenerationConfiguration.MemberConfig.memberName))
                {
                    DrawMemberName(r, copy, label, memberConfig: property);
                }
                else
                    EditorGUI.PropertyField(r, copy, true);

                y += h + EditorGUIUtility.standardVerticalSpacing;
                copy.NextVisible(false);
            }
        }


        public void DrawMemberName(Rect position, SerializedProperty property, GUIContent label, SerializedProperty memberConfig)
        {
            var memberIndex = memberConfig.FindPropertyRelative(nameof(SaveHandlerTypeGenerationConfiguration.MemberConfig._memberIndex));
            long typeInterfaceId = memberConfig.FindPropertyRelative(nameof(SaveHandlerTypeGenerationConfiguration.MemberConfig._typeInterfaceId)).longValue;
            int memberIndexValue = memberIndex.intValue;


            var memberName = property.Copy();

            EditorGUI.BeginProperty(position, label, memberName);

            Rect fieldRect = EditorGUI.PrefixLabel(position, label);

            //Debug.Log(string.Join("\n", TypeInterface.GetTypeInterfaceInfos().Select(info => $"{info.Id} | {info.HandledType.Name}"))  );
            //Debug.Log(typeInterfaceId);
            //Debug.Log(TypeInterface.Exists(typeInterfaceId));
            if (TypeInterface.Exists(typeInterfaceId))
            {
                var typeMembers = TypeInterface.GetMembersOf(typeInterfaceId);
                var selected = TypeInterface.GetTypeMemberByIndex(typeInterfaceId, memberIndexValue);

                if(selected != null)
                {
                    memberName.stringValue = ToDisplayText(selected);
                }


                // Detect click BEFORE drawing anything that may consume the event
                if (Event.current.type == EventType.MouseDown && fieldRect.Contains(Event.current.mousePosition))
                {
                    DropDownWindow.Open(
                        options: typeMembers,
                        currentValue: selected,
                        newValue =>
                        {
                            memberIndex.intValue = newValue.Index;
                            memberName.stringValue = ToDisplayText(newValue);
                            memberName.serializedObject.ApplyModifiedProperties();
                        },
                        tostring: ToListElementDisplayText
                        );

                    Event.current.Use(); // swallow the click
                }
            }


            using (new EditorGUI.DisabledScope(true))
            {
                string text = "";
                if (memberName.stringValue == "")
                {
                    text = "click to select (unassigned)";
                }
                else
                {
                    text = memberName.stringValue;
                }

                EditorGUI.TextField(fieldRect, text, EditorStyles.popup);
            }

            EditorGUI.EndProperty();
        }

        public static string ToDisplayText(TypeMember member)
        {
            return $"{member.Name}";
        }
        public static string ToListElementDisplayText(TypeMember member)
        {
            return $"{member.Index} | {member.Name}";
        }
    }







    public enum MemberType
    {
        Field,
        Property,
        Method,
        Event,
    }
}