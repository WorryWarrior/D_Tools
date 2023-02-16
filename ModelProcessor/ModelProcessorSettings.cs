using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelProcessorSettings : ScriptableObject
{
    public const string ASSET_NAME = "Model Processor Settings";
    public const string ASSET_FULL_FILENAME = ASSET_NAME + ".asset";

    public string prefabNameTemplate = "pfb_X";
    public string modelNameTemplate = "mdl_X";
    public Object prefabFolder = null;
    public Shader toonShader = null;

    public void ResetSettings()
    {
        prefabNameTemplate = "pfb_X";
        modelNameTemplate = "mdl_X";
        prefabFolder = null;
        toonShader = null;
    }
}
