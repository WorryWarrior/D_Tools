using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR

public class ModelProcessor : EditorWindow
{
    private const string WINDOW_TITLE = "Model Processor";
    private const string FIRST_LABEL_TEXT = "Target Folder";
    private const string USE_SELECTED_DIRECTORY_BUTTON_TEXT = "Use Selected Folder";
    private const string PROCESS_BUTTON_TEXT = "Process Model";

    private const string TOON_SHADER_NAME = "Toony Colors Pro 2/Hybrid Shader 2";

    static readonly Vector2 windowSize = new Vector2(350, 350);

    public Object go;
    public Object folder;
    string prefabName = "Enter name";
    string createAt = "";
    public CreatedPrefabType createdPrefabType = CreatedPrefabType.None;
    public bool isZeroSmoothness;
    public bool useToonShader = false;
    public bool hasTextures = false;
    public int additionalAnimations;
    public Object[] animations;

    private int selectedTabIndex = 0;
    private SelectedTabType selectedTab;

    private ModelProcessorSettings settings;
    //private Object prefabsFolder;
    //static ModelProcessor a;

    [MenuItem("Tools/Art/" + WINDOW_TITLE)]
    public static void ShowWindow()
    {
        ModelProcessor window = GetWindow<ModelProcessor>(false, WINDOW_TITLE, true);
        //a = window;
        window.minSize = windowSize;
        window.maxSize = windowSize;
    }

    void OnGUI()
    {
        //Debug.Log(a.position);
        SerializedObject serializedObject = new UnityEditor.SerializedObject(this);
        serializedObject.Update();

        selectedTabIndex = GUILayout.Toolbar(selectedTabIndex, new string[] { "Tool", "Preferences"});
        selectedTab = (SelectedTabType)selectedTabIndex;

        Connect();

        switch (selectedTab)
        {
            case SelectedTabType.Tool:
                ToolMainBlock();
                break;
            case SelectedTabType.Preferences:
                PreferencesMainBlock();
                break;
        }

        if (GUI.changed)
        {
            //if (settings != null)
            //{
                EditorUtility.SetDirty(settings);
            //}
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void ToolMainBlock()
    {
        GUILayout.Space(10f);

        ShowAutoFolderButtons();

        if (folder != null && folder is DefaultAsset)
        {
            MainModel();

            if (go != null)
            {
                string goOldName = go.name;

                SettingsDraw();

                //SerializedProperty serializedPropertyAnimations = serializedObject.FindProperty("animations");
                //EditorGUILayout.PropertyField(serializedPropertyAnimations);

                string path = $"{createAt}/{prefabName}";

                bool isPrefabExist = AssetDatabase.IsValidFolder(path);
                if (!isPrefabExist)
                {
                    CreateButton(path, goOldName);

                    //RenameAnimations();
                }
                else
                {
                    //EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Asset already exists", EditorStyles.boldLabel);
                    //EditorGUILayout.Toggle(isPrefabExist);
                    //EditorGUILayout.EndHorizontal();

                    //RenameAnimations();
                }

            }
        }
    }

    private void PreferencesMainBlock()
    {
        GUILayout.Space(10f);

        if (GUILayout.Button("Reset Preferences"))
        {
            settings.ResetSettings();
            EditorUtility.SetDirty(settings);
        }

        GUILayout.Space(8f);

        settings.prefabNameTemplate = EditorGUILayout.TextField("Prefab Name Template", settings.prefabNameTemplate);
        settings.modelNameTemplate = EditorGUILayout.TextField("Model Name Template", settings.modelNameTemplate);

        settings.prefabFolder = EditorGUILayout.ObjectField("Prefab Folder", settings.prefabFolder, typeof(Object), true);
        settings.toonShader = (Shader)EditorGUILayout.ObjectField("Toon Shader", settings.toonShader, typeof(Shader), true);
    }

    public void ExtractMaterials(string assetPath, string destinationPath, string prefix)
    {
        HashSet<string> hashSet = new HashSet<string>();

        HashSet<string> testHashSet = new HashSet<string>();

        IEnumerable<Object> enumerable = from x in AssetDatabase.LoadAllAssetsAtPath(assetPath)
                                         where x.GetType() == typeof(Material)
                                         select x;

        int id = 0;
        foreach (Object item in enumerable)
        {
            //string path = System.IO.Path.Combine(destinationPath, $"{prefix}Material{id}") + ".mat";
            string path = System.IO.Path.Combine(destinationPath, item.name) + ".mat";
            id++;
            // string path = System.IO.Path.Combine(destinationPath, item.name) + ".mat";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            string value = AssetDatabase.ExtractAsset(item, path);
            if (string.IsNullOrEmpty(value))
            {
                hashSet.Add(path);
            }
        }

/*        id = 0;

        foreach (Object item in enumerable)
        {
            string path = System.IO.Path.Combine(destinationPath, $"Test_{prefix}Material{id}") + ".mat";
            id++;
            // string path = System.IO.Path.Combine(destinationPath, item.name) + ".mat";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            string value = AssetDatabase.ExtractAsset(item, path);
            if (string.IsNullOrEmpty(value))
            {
                testHashSet.Add(path);
            }
        }*/

        WriteAndImportAsset(assetPath);

        foreach (string item2 in hashSet)
        {
            WriteAndImportAsset(item2);
            Object asset = AssetDatabase.LoadAssetAtPath(item2, typeof(Material));

            if (isZeroSmoothness)
            {
                ((Material)asset).SetFloat("_Smoothness", 0f);
            }

            if (useToonShader && settings.toonShader != null)
            {
                Material mat = (Material)asset;
                mat.shader = settings.toonShader;

                WriteAndImportAsset(item2);
            }
        }

/*        Shader toonShader = Shader.Find(TOON_SHADER_NAME);

        foreach (string item2 in testHashSet)
        {
            
            Object asset = AssetDatabase.LoadAssetAtPath(item2, typeof(Material));

            Material mat = (Material)asset;
            mat.shader = toonShader;

            WriteAndImportAsset(item2);
        }*/

    }

    public void ExtractAnimations(string assetPath, string destinationPath, string defaultName = "", bool isOneF = true)
    {
        HashSet<string> hashSet = new HashSet<string>();
        IEnumerable<Object> enumerable = from x in AssetDatabase.LoadAllAssetsAtPath(assetPath)
                                         where x.GetType() == typeof(AnimationClip)
                                         select x;

        bool isOne = enumerable.Count() <= 2 && isOneF;
        int id = 0;
        foreach (Object item in enumerable)
        {
            if (!item.name.Contains("__preview__"))
            {
                AnimationClip ac = new AnimationClip();

                EditorUtility.CopySerialized(item, ac);
                if (string.IsNullOrEmpty(defaultName))
                {
                    if (isOne)
                    {
                        if (id == 0)
                            AssetDatabase.CreateAsset(ac, $"{destinationPath}/{prefabName}.anim");
                        else
                            AssetDatabase.CreateAsset(ac, $"{destinationPath}/{prefabName}{id}.anim");
                    }
                    else
                        AssetDatabase.CreateAsset(ac, $"{destinationPath}/{item.name.Replace('|', '_')}.anim");
                }
                else
                {
                    if (isOne)
                    {
                        if (id == 0)
                            AssetDatabase.CreateAsset(ac, $"{destinationPath}/{defaultName}.anim");
                        else
                            AssetDatabase.CreateAsset(ac, $"{destinationPath}/{item.name.Replace('|', '_')}.anim");
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(ac, $"{destinationPath}/{item.name.Replace('|', '_')}.anim");
                    }
                }
                id++;
            }
        }
    }

    public void ExtractTextures(string assetPath, string destinationPath)
    {
        HashSet<string> hashSet = new HashSet<string>();
        IEnumerable<Object> enumerable = from x in AssetDatabase.LoadAllAssetsAtPath(assetPath)
                                         where x.GetType() == typeof(Texture)
                                         select x;

        foreach (Object item in enumerable)
        {
            string path = System.IO.Path.Combine(destinationPath, item.name) + $".{AssetDatabase.GetAssetPath(item).Split('.')[1]}";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            string value = AssetDatabase.ExtractAsset(item, path);
            if (string.IsNullOrEmpty(value))
            {
                hashSet.Add(assetPath);
            }
        }

        foreach (string item2 in hashSet)
            WriteAndImportAsset(item2);
    }

    private static void WriteAndImportAsset(string assetPath)
    {
        AssetDatabase.WriteImportSettingsIfDirty(assetPath);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
    }

    private void ShowAutoFolderButtons()
    {
        GUILayout.Label(FIRST_LABEL_TEXT, EditorStyles.boldLabel);
        folder = EditorGUILayout.ObjectField(folder, typeof(Object), true);
        createAt = AssetDatabase.GetAssetPath(folder);

        if (GUILayout.Button(USE_SELECTED_DIRECTORY_BUTTON_TEXT))
        {
            Object autoObj = AssetDatabase.LoadAssetAtPath(GetCurrentlyOpenedDirectoryPath(), typeof(Object));

            if (autoObj != null)
            {
                folder = autoObj;
            }
        }
    }

    private void MainModel()
    {
        GUILayout.Space(20);
        GUILayout.Label("Object Main Model", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        go = EditorGUILayout.ObjectField(go, typeof(Object), true);

        if (EditorGUI.EndChangeCheck())
        {
            prefabName = go.name;
        }
    }

    private void SettingsDraw()
    {
        GUILayout.Space(20);
        GUILayout.Label("Base Settings", EditorStyles.boldLabel);
        prefabName = EditorGUILayout.TextField("Model Name", prefabName);

        createdPrefabType = (CreatedPrefabType)EditorGUILayout.EnumPopup("Created Prefab Type", createdPrefabType);

        if (createdPrefabType != CreatedPrefabType.None)
        {
            string prefabLocationPath = settings.prefabFolder == null ? AssetDatabase.GetAssetPath(folder)
                : AssetDatabase.GetAssetPath(settings.prefabFolder);

            GUILayout.Label($"Prefab will be created at {prefabLocationPath}");
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Is Zero Material Smoothness");
            isZeroSmoothness = EditorGUILayout.Toggle(isZeroSmoothness);
        }

        hasTextures = EditorGUILayout.Toggle("Has External Textures", hasTextures);

        useToonShader = EditorGUILayout.Toggle("Use Toon Shader", useToonShader);

        if (useToonShader && !TryFindToonShader())
        {
            EditorGUILayout.HelpBox("Toon Shader Not Found. This option will have no effect", MessageType.Warning, true);
        }

        //GUILayout.BeginHorizontal();
        //GUILayout.Label("Is Remove Animations From Model", EditorStyles.label);
        //GUILayout.EndHorizontal();

        // GUILayout.BeginHorizontal();

        //GUILayout.Label("Add Animation Models", EditorStyles.boldLabel);
    }

    private void CreateButton(string path, string goOldName)
    {
        if (GUILayout.Button(PROCESS_BUTTON_TEXT))
        {
            AssetDatabase.CreateFolder($"{createAt}", prefabName);
            string modelPath = AssetDatabase.GetAssetPath(go);
            string modelExtension = modelPath.Split('.')[1];
            Debug.Log($"Move from {modelPath} to {path}");
            //string movedModelPath = $"{path}/mdl_{prefabName}.{modelExtension}";
            string movedModelPath = $"{path}/{GetModelFormattedName(prefabName)}.{modelExtension}";
            AssetDatabase.MoveAsset(modelPath, movedModelPath);

            AssetDatabase.CreateFolder($"{path}", "Materials");
            ExtractMaterials(movedModelPath, $"{path}/Materials", prefabName);
            AssetDatabase.CreateFolder($"{path}", "Animations");
            ExtractAnimations(movedModelPath, $"{path}/Animations", goOldName);

            if (animations != null)
            {
                foreach (Object additionalAnim in animations)
                {
                    if (additionalAnim != null)
                    {
                        ExtractAnimations(AssetDatabase.GetAssetPath(additionalAnim), $"{path}/Animations", additionalAnim.name, false);
                    }
                }
            }

            try
            {
                AssetDatabase.CreateFolder($"{path}", "Textures");
                ExtractTextures(movedModelPath, $"{path}/Textures");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Texture bug: {e}");
            }

            CreatePrefab(path, createdPrefabType);


            TryDeleteEmptyDirectory($"{path}/Animations");

            if (!hasTextures)
            {
                TryDeleteEmptyDirectory($"{path}/Textures");
            }

            AssetDatabase.Refresh();
        }
    }

    private void CreatePrefab(string _path, CreatedPrefabType _createdPrefabType)
    {
        // string prefabLocationPath = settings.prefabFolder == null ? AssetDatabase.GetAssetPath(folder)
        //        : AssetDatabase.GetAssetPath(settings.prefabFolder);

        string prefabPath = settings.prefabFolder == null ? _path : AssetDatabase.GetAssetPath(settings.prefabFolder);
        switch (_createdPrefabType)
        {
            case CreatedPrefabType.None:
                break;
            case CreatedPrefabType.Instance:
                GameObject prefabInstance = Instantiate(go) as GameObject;
                PrefabUtility.SaveAsPrefabAsset(prefabInstance, $"{prefabPath}/{GetPrefabFormattedName(prefabName)}.prefab");

                DestroyImmediate(prefabInstance);
                break;
            case CreatedPrefabType.ModelReference:
                GameObject prefabRoot = new GameObject();
                GameObject prefabModelReference = PrefabUtility.InstantiatePrefab(go) as GameObject;
                prefabModelReference.transform.SetParent(prefabRoot.transform);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, $"{prefabPath}/{GetPrefabFormattedName(prefabName)}.prefab");
                DestroyImmediate(prefabRoot);
                break;
        }
    }

    private string GetPrefabFormattedName(string name)
    {
        string a = Regex.Replace(settings.prefabNameTemplate, @"[X]", "{0}");
        //Debug.Log("From " + settings.prefabNameTemplate + " to " + a);

        return string.Format(a, name);
    }

    private string GetModelFormattedName(string name)
    {
        string a = Regex.Replace(settings.modelNameTemplate, @"[X]", "{0}");
        //Debug.Log("From " + settings.prefabNameTemplate + " to " + a);

        return string.Format(a, name);
    }

    private bool TryFindToonShader()
    {
        if (settings.toonShader != null)
            return true;

        settings.toonShader = Shader.Find(TOON_SHADER_NAME);

        if (settings.toonShader == null)
            return false;
        
        return true;

    }

    private void RenameAnimations()
    {
        if (go != null && GUILayout.Button("Rename animations"))
        {
            string movedModelPath = AssetDatabase.GetAssetPath(go);
            // var movedModelPath = $"{path}/{prefabName}Model.fbx";
            ModelImporter importer = AssetImporter.GetAtPath(movedModelPath) as ModelImporter;
            // Debug.Log(importer.clipAnimations.Length);
            // Debug.Log(importer.defaultClipAnimations.Length);
            ModelImporterClipAnimation[] animas = importer.defaultClipAnimations;
            // animas.Foreach
            animas.ToList().ForEach(x => x.name = x.name.Replace("Armature|", ""));
            animas.ToList().ForEach(x => x.name = x.name.Replace("rig|", ""));
            animas.ToList().ForEach(x => x.name = x.name.Replace("metarig|", ""));
            animas.ToList().ForEach(x => x.name = x.name.Replace("Corset|", ""));
            importer.clipAnimations = animas;

            AssetDatabase.ImportAsset(movedModelPath);
        }
    }

    private bool TryDeleteEmptyDirectory(string directoryPath)
    {
        if (System.IO.Directory.GetFiles(directoryPath).Length == 0)
        {
            FileUtil.DeleteFileOrDirectory(directoryPath);
            FileUtil.DeleteFileOrDirectory($"{directoryPath}.meta");
            return true;
        }

        return false;
    }

    private string GetCurrentlyOpenedDirectoryPath()
    {
        System.Type projectWindowUtilType = typeof(ProjectWindowUtil);
        MethodInfo getActiveFolderPath = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
        object obj = getActiveFolderPath.Invoke(null, new object[0]);
        return obj.ToString();
    }

    private void Connect()
    {
        if (settings != null)
            return;

        settings = ConnectToSettings(settings, true);
    }

    private ModelProcessorSettings ConnectToSettings(ModelProcessorSettings settings, bool createIfMissing)
    {
        LocationData assetsLD = new LocationData(ModelProcessorUtils.AssetsPath + ModelProcessorUtils.PathSlash + "Resources");

        if (settings == null)
        {
            settings = ModelProcessorUtils.ConnectToSourceAsset<ModelProcessorSettings>(assetsLD.adbFilePath, false);
        }
        if (settings == null)
        {
            // Settings don't exist.
            if (!createIfMissing) 
                return null; // Stop here
            if (!Directory.Exists(assetsLD.dir)) AssetDatabase.CreateFolder(assetsLD.adbParentDir, "Resources");
            settings = ModelProcessorUtils.ConnectToSourceAsset<ModelProcessorSettings>(assetsLD.adbFilePath, true);
        }

        return settings;
    }

    private struct LocationData
    {
        public string dir; // without final slash
        public string filePath;
        public string adbFilePath;
        public string adbParentDir; // without final slash

        public LocationData(string srcDir) : this()
        {
            dir = srcDir;
            filePath = dir + ModelProcessorUtils.PathSlash + ModelProcessorSettings.ASSET_FULL_FILENAME;
            adbFilePath = ModelProcessorUtils.FullPathToADBPath(filePath);
            adbParentDir = ModelProcessorUtils.FullPathToADBPath(dir.Substring(0, dir.LastIndexOf(ModelProcessorUtils.PathSlash)));
        }
    }

    public enum CreatedPrefabType
    {
        None,
        Instance,
        ModelReference
    }

    private enum SelectedTabType
    {
        Tool = 0,
        Preferences = 1,

    }
}

#endif