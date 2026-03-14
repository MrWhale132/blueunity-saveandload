#nullable enable
using System;
using System.Collections.Generic;
using Theblueway.Tools.Editor;
using UnityEngine;
using Theblueway.Core.Runtime.Extensions;
using Theblueway.Core.Runtime.Packages.com.blueutils.core.Runtime.Debugging.Logging;

namespace Theblueway.SaveAndLoad.Editor
{
    public static class SaveAndLoadCodeInspection
    {
        public static Dictionary<string, long> GetMethodSignatureToMethodIdMap(Type typeToHandle, bool isStatic, Type handlerType)
        {
            if(handlerType == null)
            {
                return new();
            }


            string tag = SaveHandlerAutoGenerator.GenerateMethodSignatureToIdMapTag(typeToHandle, isStatic);


            var savehandlerFilePath = BlueTools.GetSourceFilePath(handlerType);

            var text = System.IO.File.ReadAllText(savehandlerFilePath);


            int tagStart = text.IndexOf(tag);

            if (tagStart == -1)
            {
                Debug.LogError($"Failed to find method signature to id map tag in existing SaveHandler: {handlerType.CleanAssemblyQualifiedName()} at path: {savehandlerFilePath} for type: {typeToHandle.CleanAssemblyQualifiedName()} isStatic: {isStatic}");
                BlueDebug.Debug($"tag={tag}\n\nsourcefile text=\n{text}");
                return new();
            }


            var existingMethodToIdMap = new Dictionary<string, long>();


            int dictionaryEntriesStart = tagStart + tag.Length + 1;

            int end = text.IndexOf("};", dictionaryEntriesStart);

            string section = text.Substring(dictionaryEntriesStart, end - dictionaryEntriesStart);

            var entries = section.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < entries.Length - 1; i++)  //last line is the closing parenthesis of the dictionarly };
            {
                var line = entries[i];

                if (line.Contains("#if") || line.Contains("#endif")) continue;

                int keyvalSep = line.IndexOfNth(',', -2);
                var start = line.IndexOf('\"') + 1;
                var length = keyvalSep - start - 1;
                //debug
                if (length < 0)
                {
                    Debug.LogError(typeToHandle.CleanAssemblyQualifiedName() + " " + isStatic + "\n" + line);
                }
                var methodSignature = line.Substring(start, keyvalSep - start - 1);
                var val = line.Substring(keyvalSep + 2, line.Length - keyvalSep - 4);

                if (long.TryParse(val, out var existingId))
                {
                    existingMethodToIdMap.Add(methodSignature, existingId);
                }
                else
                {
                    Debug.LogError($"Failed to parse method id: {val} for method: {methodSignature} in existing SaveHandler: {handlerType.FullName} at path: {savehandlerFilePath}");
                }
            }


            return existingMethodToIdMap;
        }
    }
}
