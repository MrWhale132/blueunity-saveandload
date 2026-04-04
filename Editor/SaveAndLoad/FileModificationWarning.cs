using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Assets._Project.Scripts.UtilScripts.Extensions;


[InitializeOnLoad]
public static class CodegenWatcher
{
    private static List<FileSystemWatcher> watchers;

    static CodegenWatcher()
    {
        EnsureWatcher();
        AppDomain.CurrentDomain.DomainUnload += (_, __) => DisposeWatcher();
    }

    private static void EnsureWatcher()
    {
        if (watchers != null) return;

        List<string> paths = new List<string>
        {
            Path.Combine(Application.dataPath),
            Path.Combine(Application.dataPath,"..","Packages"),
        };

        watchers = new();


        foreach (var path in paths)
        {
            if (!Directory.Exists(path)) return;

            var watcher = new FileSystemWatcher(path, "*.cs")
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
            };

            watcher.Changed += (s, e) => SaveAndLoadCodeGenWindow._eventQueue.Enqueue(e.ToDto()); ;
            watcher.Created += (s, e) => SaveAndLoadCodeGenWindow._eventQueue.Enqueue(e.ToDto()); ;
            watcher.Deleted += (s, e) => SaveAndLoadCodeGenWindow._eventQueue.Enqueue(e.ToDto()); ;

            watchers.Add(watcher);
        }

        //Debug.Log("[CodegenWatcher] Watching " + path);
    }



    private static void DisposeWatcher()
    {
        if (watchers.IsNotNullAndNotEmpty())
        {
            foreach (var watcher in watchers)
            {
                if (watcher != null)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                    //Debug.Log("[CodegenWatcher] Disposed");
                }
            }

            watchers = null;
        }
    }
}
