using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CeskeDrahy;

public static class RailProject
{
    [MenuItem("SuperCigos/Prepare game scene")]
    public static void Prepare()
    {
        EnsureMaterials();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("České dráhy / game").AddComponent<RailGame>();
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/OlomoucHlavniNadrazi.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/OlomoucHlavniNadrazi.unity",true)};
        ConfigurePlayer();
        var so=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        // Input System only (1): keyboard + gamepad through RailInput; the legacy Input Manager is deprecated.
        var input=so.FindProperty("activeInputHandler");if(input!=null){input.intValue=1;so.ApplyModifiedPropertiesWithoutUndo();}
        foreach(string file in System.IO.Directory.GetFiles("Assets/Resources/Textures"))
        {
            string path=file.Replace('\\','/');
            if(path.EndsWith(".meta"))continue;
            var ti=AssetImporter.GetAtPath(path) as TextureImporter;if(ti!=null){ti.maxTextureSize=1024;ti.textureCompression=TextureImporterCompression.Compressed;if(path.Contains("Normal"))ti.textureType=TextureImporterType.NormalMap;ti.SaveAndReimport();}
        }
        AssetDatabase.SaveAssets();Debug.Log("RAILWAY_SCENE_READY");
    }
    /// <summary>Player settings shared by every platform build.</summary>
    public static void ConfigurePlayer()
    {
        PlayerSettings.productName="SuperCigos";PlayerSettings.companyName="Railway Platformer";
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.allowFullscreenSwitch=true;
        PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
        // The only scene has fog disabled, so automatic stripping would drop the fog the levels enable at runtime.
        var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        graphics.FindProperty("m_FogStripping").intValue=1;
        graphics.FindProperty("m_FogKeepLinear").boolValue=true;
        graphics.FindProperty("m_FogKeepExp").boolValue=false;
        graphics.FindProperty("m_FogKeepExp2").boolValue=false;
        graphics.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
    }
    [MenuItem("SuperCigos/Build macOS")]
    public static void BuildMac()
    {
        EnsureMaterials();ConfigurePlayer();
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/OlomoucHlavniNadrazi.unity"},locationPathName="Builds/SuperCigos.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.None});
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Build failed: "+report.summary.result);
        Debug.Log("RAILWAY_BUILD_OK "+report.summary.totalSize);
    }
    [MenuItem("SuperCigos/Build Windows")]
    public static void BuildWindows()
    {
        EnsureMaterials();ConfigurePlayer();
        // IL2CPP cannot cross-compile Windows players from macOS; Mono runs everywhere.
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        const string folder="Builds/Windows/SuperCigos";
        if(System.IO.Directory.Exists(folder))System.IO.Directory.Delete(folder,true);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/OlomoucHlavniNadrazi.unity"},locationPathName=folder+"/SuperCigos.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Build failed: "+report.summary.result);
        foreach(string junk in System.IO.Directory.GetDirectories(folder,"*DontShip*"))System.IO.Directory.Delete(junk,true);
        const string zip="Builds/SuperCigos-Windows.zip";
        if(System.IO.File.Exists(zip))System.IO.File.Delete(zip);
        System.IO.Compression.ZipFile.CreateFromDirectory(folder,zip,System.IO.Compression.CompressionLevel.Optimal,true);
        Debug.Log("RAILWAY_WINDOWS_BUILD_OK "+report.summary.totalSize+" warnings="+report.summary.totalWarnings);
    }
    public static void EnsureMaterials()
    {
        // Serialized template materials keep the runtime shaders and their instancing variants in player builds.
        Template("Assets/Resources/Surface.mat","Surface","Railway/Surface");
        Template("Assets/Resources/Foliage.mat","Foliage","Railway/Foliage");
        Template("Assets/Resources/Water.mat","Water","Railway/Water");
        if(AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/SurfaceNormal.mat"))AssetDatabase.DeleteAsset("Assets/Resources/SurfaceNormal.mat");
        AssetDatabase.SaveAssets();
    }
    static void Template(string path,string name,string shaderName)
    {
        var shader=Shader.Find(shaderName);
        if(!shader)throw new System.InvalidOperationException("Missing shader "+shaderName);
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(shader){name=name,enableInstancing=true};AssetDatabase.CreateAsset(material,path);}
        else if(material.shader!=shader){material.shader=shader;material.enableInstancing=true;EditorUtility.SetDirty(material);}
    }
}
