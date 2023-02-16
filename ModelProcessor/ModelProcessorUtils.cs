using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class ModelProcessorUtils
{
#if UNITY_EDITOR
    public static string ProjectPath { get; private set; } // Without final slash
    public static string AssetsPath { get; private set; } // Without final slash
    public static string PathSlash { get; private set; }
    public static string PathSlashToReplace { get; private set; }

    static ModelProcessorUtils()
    {
        bool useWindowsSlashes = Application.platform == RuntimePlatform.WindowsEditor;
        PathSlash = useWindowsSlashes ? "\\" : "/";
        PathSlashToReplace = useWindowsSlashes ? "/" : "\\";

        ProjectPath = Application.dataPath;
        ProjectPath = ProjectPath.Substring(0, ProjectPath.LastIndexOf("/"));
        ProjectPath = ProjectPath.Replace(PathSlashToReplace, PathSlash);

        AssetsPath = ProjectPath + PathSlash + "Assets";
    }

    public static T ConnectToSourceAsset<T>(string adbFilePath, bool createIfMissing = false) where T : ScriptableObject
    {
        if (!AssetExists(adbFilePath))
        {
            if (createIfMissing) CreateScriptableAsset<T>(adbFilePath);
            else return null;
        }
        T source = (T)AssetDatabase.LoadAssetAtPath(adbFilePath, typeof(T));
        if (source == null)
        {
            CreateScriptableAsset<T>(adbFilePath);
            source = (T)AssetDatabase.LoadAssetAtPath(adbFilePath, typeof(T));
        }
        return source;
    }

    public static bool AssetExists(string adbPath)
    {
        string fullPath = ADBPathToFullPath(adbPath);
        return File.Exists(fullPath) || Directory.Exists(fullPath);
    }

    static void CreateScriptableAsset<T>(string adbFilePath) where T : ScriptableObject
    {
        T data = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(data, adbFilePath);
    }

    public static string FullPathToADBPath(string fullPath)
    {
        string adbPath = fullPath.Substring(ProjectPath.Length + 1);
        return adbPath.Replace("\\", "/");
    }

    public static string ADBPathToFullPath(string adbPath)
    {
        adbPath = adbPath.Replace(PathSlashToReplace, PathSlash);
        return ProjectPath + PathSlash + adbPath;
    }
#endif
}
