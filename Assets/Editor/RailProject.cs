using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CeskeDrahy;

public static class RailProject
{
    [MenuItem("České dráhy/Prepare game scene")]
    public static void Prepare()
    {
        EnsureMaterials();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("České dráhy / game").AddComponent<RailGame>();
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/OlomoucHlavniNadrazi.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/OlomoucHlavniNadrazi.unity",true)};
        PlayerSettings.productName="České dráhy — Poslední spoj";PlayerSettings.companyName="Railway Platformer";
        PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
        PlayerSettings.colorSpace=ColorSpace.Linear;
        var so=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var input=so.FindProperty("activeInputHandler");if(input!=null){input.intValue=0;so.ApplyModifiedPropertiesWithoutUndo();}
        foreach(string path in new[]{"Assets/Resources/Textures/RailBallast.png","Assets/Resources/Textures/RailBallastNormal.png"})
        {var ti=AssetImporter.GetAtPath(path) as TextureImporter;if(ti!=null){ti.maxTextureSize=1024;ti.textureCompression=TextureImporterCompression.Compressed;if(path.Contains("Normal"))ti.textureType=TextureImporterType.NormalMap;ti.SaveAndReimport();}}
        AssetDatabase.SaveAssets();Debug.Log("RAILWAY_SCENE_READY");
    }
    [MenuItem("České dráhy/Build macOS")]
    public static void BuildMac()
    {
        EnsureMaterials();
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/OlomoucHlavniNadrazi.unity"},locationPathName="Builds/CeskeDrahy.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.None});
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Build failed: "+report.summary.result);
        Debug.Log("RAILWAY_BUILD_OK "+report.summary.totalSize);
    }
    public static void EnsureMaterials()
    {
        // Serialized references preserve runtime-created Standard materials in player builds.
        if(!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Surface.mat"))
        {
            var material=new Material(Shader.Find("Standard")){name="Surface",enableInstancing=true};
            AssetDatabase.CreateAsset(material,"Assets/Resources/Surface.mat");
        }
        if(!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/SurfaceNormal.mat"))
        {
            var normal=new Material(Shader.Find("Standard")){name="Surface Normal",enableInstancing=true};
            normal.SetTexture("_BumpMap",Resources.Load<Texture2D>("Textures/RailBallastNormal"));normal.EnableKeyword("_NORMALMAP");
            AssetDatabase.CreateAsset(normal,"Assets/Resources/SurfaceNormal.mat");
        }
        AssetDatabase.SaveAssets();
    }
}
