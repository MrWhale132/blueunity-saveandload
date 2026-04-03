using Assets._Project.Scripts.UtilScripts;
using System;
using System.Collections.Generic;
using System.Linq;
using Theblueway.Core;
using Theblueway.Core.Editor.Extensions;
using UnityEditor;
using UnityEngine;

namespace Packages.com.theblueway.saveandload.Editor.SaveAndLoad
{
    //todo: why is this drawer here, and not in the Infra editor assembly?

    [CustomPropertyDrawer(typeof(RandomId))]
    public class RandomIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var idProp2 = property.FindPropertyRelative(RandomId.underlyingFieldName);

            // label first
            position = EditorGUI.PrefixLabel(position, label);

            // split into text + small button
            int buttonWidth = 30;
            var fieldRect = new Rect(position.x, position.y, position.width - 30f, position.height);

            float nextButtonPosX = fieldRect.xMax + 2;

            Rect GetNextButtonRect()
            {
                var current = new Rect(nextButtonPosX, position.y, 28f, position.height);
                nextButtonPosX -= buttonWidth;
                return current;
            }

            var copyButtonRect = new Rect(fieldRect.xMax + 2f, position.y, 28f, position.height);
            var pasteButtonRect = new Rect(fieldRect.xMax - 28f, position.y, 28f, position.height);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(fieldRect, idProp2, GUIContent.none);
            EditorGUI.EndDisabledGroup();


            if (Event.current.type == EventType.Layout)
            {
                if (property.IsDefined<AutoGenerateAttribute>())
                {
                    property.ForEachTarget(static prop =>
                    {
                        var idProp = prop.FindPropertyRelative(RandomId.underlyingFieldName);

                        if (idProp.longValue == default)
                        {
                            idProp.longValue = long.Parse(RandomId.New.ToString());
                        }
                    });
                }
            }


            //if (GUI.Button(btnRect, "⧉")) // unicode clipboard icon (does not work, only displayed as a square somewhy)
            if (GUI.Button(GetNextButtonRect(), "C")) // unicode clipboard icon
            {
                IEnumerable<long> values = property.serializedObject.targetObjects.Select(target =>
                {
                    var so = new SerializedObject(target);
                    var prop = so.FindProperty(property.propertyPath);
                    var idProp3 = prop.FindPropertyRelative(RandomId.underlyingFieldName);

                    return idProp3.longValue;
                });

                string list = string.Join(",", values);

                EditorGUIUtility.systemCopyBuffer = list;
            }


            if (property.HasAttribute<AllowEditAttribute>(out var editAttribute))
            {
                {

                    if (editAttribute.RandomIdEditMode.HasFlag(RandomIdEditMode.Paste))
                    {
                        Rect rect = GetNextButtonRect();

                        if (GUI.Button(rect, "P"))
                        {
                            string userInput = EditorGUIUtility.systemCopyBuffer;

                            var values = userInput.Split(',').Select(s => s.Trim());

                            bool matchingNumerosity = values.Count() == property.serializedObject.targetObjects.Length;

                            if (matchingNumerosity)
                            {
                                List<long> longValues = new();

                                foreach (var val in values)
                                {
                                    if (!long.TryParse(val, out long parsedVal))
                                    {
                                        Debug.LogError($"One of the {nameof(RandomId)} values in the clipboard is not a valid long. " +
                                            $"Invalid long: {val}");
                                        continue;
                                    }

                                    if (parsedVal < 100000000000000000)
                                    {
                                        Debug.LogError($"One of the {nameof(RandomId)} values in the clipboard is not in a valid format (must be at least 100000000000000000). " +
                                            $"Invalid value: {val}");

                                        continue;
                                    }

                                    longValues.Add(parsedVal);
                                }

                                bool allValid = longValues.Count == values.Count();

                                if (allValid)
                                {
                                    property.ForEachTarget((prop, i) =>
                                    {
                                        var idProp = prop.FindPropertyRelative(RandomId.underlyingFieldName);

                                        long val = longValues[i];

                                        idProp.longValue = val;
                                    });
                                }
                            }
                            else
                            {
                                Debug.LogError($"Number of values in clipboard ({values.Count()}) does not match number of selected objects ({property.serializedObject.targetObjects.Length}). Paste operation aborted.");
                            }
                        }
                    }


                    if (editAttribute.RandomIdEditMode.HasFlag(RandomIdEditMode.Generate))
                    {
                        Rect rect = GetNextButtonRect();

                        if (GUI.Button(rect, "G"))
                        {
                            property.ForEachTarget(prop =>
                            {
                                var idProp = prop.FindPropertyRelative(RandomId.underlyingFieldName);

                                //Debug.Log("here");
                                idProp.longValue = long.Parse(RandomId.New.ToString());
                            });
                        }
                    }
                }
            }
            //else Debug.Log("does not have");
        }
    }


    public static class SerializedPropertyExtensions
    {
        public static void ForEachTarget(this SerializedProperty property, Action<SerializedProperty> action)
        {
            foreach (var target in property.serializedObject.targetObjects)
            {
                var so = new SerializedObject(target);
                var prop = so.FindProperty(property.propertyPath);

                action(prop);

                so.ApplyModifiedProperties();
            }
        }
        public static void ForEachTarget(this SerializedProperty property, Action<SerializedProperty, int> action)
        {
            int i = 0;
            foreach (var target in property.serializedObject.targetObjects)
            {
                var so = new SerializedObject(target);
                var prop = so.FindProperty(property.propertyPath);

                action(prop, i);

                so.ApplyModifiedProperties();

                i++;
            }
        }
    }
}
