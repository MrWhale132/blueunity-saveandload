using Assets._Project.Scripts.UtilScripts;
using UnityEditor;
using UnityEngine;
using Theblueway.Core.Editor.Extensions;
using Theblueway.Core;

namespace Packages.com.theblueway.saveandload.Editor.SaveAndLoad
{
    //todo: why is this drawer here, and not in the Infra editor assembly?

    [CustomPropertyDrawer(typeof(RandomId))]
    public class RandomIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var idProp = property.FindPropertyRelative("_id");

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
            EditorGUI.PropertyField(fieldRect, idProp, GUIContent.none);
            EditorGUI.EndDisabledGroup();


            if (idProp.longValue == default && property.IsDefined<AutoGenerateAttribute>())
            {
                idProp.longValue = long.Parse(RandomId.New.ToString());
            }




            //if (GUI.Button(btnRect, "⧉")) // unicode clipboard icon (does not work, only displayed as a square somewhy)
            if (GUI.Button(GetNextButtonRect(), "C")) // unicode clipboard icon
            {
                EditorGUIUtility.systemCopyBuffer = idProp.longValue.ToString();
            }


            if (property.HasAttribute<AllowEditAttribute>(out var editAttribute))
            {
                if (editAttribute.RandomIdEditMode.HasFlag(RandomIdEditMode.Paste))
                    if (GUI.Button(GetNextButtonRect(), "P"))
                    {
                        if (long.TryParse(EditorGUIUtility.systemCopyBuffer, out long val))
                        {
                            if (val >= 100000000000000000)
                            {
                                idProp.longValue = val;
                            }
                            else
                            {
                                Debug.LogWarning("RandomId long value must be at least 100000000000000000.");
                            }
                        }
                        else
                            Debug.LogWarning("Clipboard does not contain a valid RandomId long value.");
                    }


                if (editAttribute.RandomIdEditMode.HasFlag(RandomIdEditMode.Generate))
                    if (GUI.Button(GetNextButtonRect(), "G"))
                    {
                        idProp.longValue = long.Parse(RandomId.New.ToString());
                    }
            }
        }
    }

}
