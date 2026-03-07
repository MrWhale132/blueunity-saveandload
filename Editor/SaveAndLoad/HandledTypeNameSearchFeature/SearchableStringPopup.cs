using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;


namespace Packages.com.theblueway.saveandload.Editor.SaveAndLoad.HandledTypeNameSearchFeature
{


    public class SearchableStringPopup : EditorWindow
    {
        private IEnumerable<string> allOptions;
        private IEnumerable<string> filteredOptions;
        private string search = "";
        private Vector2 scroll;
        private Action<string> onSelected;
        private string currentValue;

        public static void Open(
            IEnumerable<string> options,
            string currentValue,
            Action<string> onSelected)
        {
            var win = CreateInstance<SearchableStringPopup>();
            win.allOptions = options;
            win.filteredOptions = options;
            win.currentValue = currentValue;
            win.onSelected = onSelected;

            var mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            win.ShowAsDropDown(new Rect(mousePos, Vector2.zero), new Vector2(1000, 300));
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck())
            {
                filteredOptions = string.IsNullOrEmpty(search)
                    ? allOptions
                    : allOptions
                        .Where(o => o.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (var option in filteredOptions)
            {
                bool isCurrent = option == currentValue;
                GUIStyle style = isCurrent ? EditorStyles.boldLabel : EditorStyles.label;

                if (GUILayout.Button(option, style))
                {
                    onSelected?.Invoke(option);
                    Close();
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
