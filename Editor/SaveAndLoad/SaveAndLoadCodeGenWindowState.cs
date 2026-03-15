
using System.Collections.Generic;
using UnityEngine;
using static SaveAndLoadCodeGenWindow;

namespace Assets._Project.Scripts.SaveAndLoad.Editor
{
    [CreateAssetMenu(fileName = "SaveAndLoadCodeGenWindowState", menuName = "Scriptable Objects/SaveAndLoad/CodeGenWindowState")]
    public class SaveAndLoadCodeGenWindowState :ScriptableObject
    {
        public Vector2 _scrollPos;
        public List<FileSystemEventArgsDto> _eventqueue = new();
        public List<FileSystemEventArgsDto> _changedFiles = new();
        public List<CodeGenTargetModel> _codegenTargets = new();
        public List<int> _selectedIndices = new ();
        public SaveAndLoadCodeGenSettings _userSettings;
        public string _selectedFolderToScan;
    }
}
