using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using CeskeDrahy;

public static class RailProject
{
    [MenuItem("SuperCigo/Prepare game scene")]
    public static void Prepare()
    {
        EnsureMaterials();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("České dráhy / game").AddComponent<RailGame>();
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene,Scene);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
        ConfigurePlayer();
        var so=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        // Input System only (1): keyboard + gamepad through RailInput; the legacy Input Manager is deprecated.
        var input=so.FindProperty("activeInputHandler");if(input!=null){input.intValue=1;so.ApplyModifiedPropertiesWithoutUndo();}
        // Texture import settings come from RailAssetRules (AssetPostprocessor).
        AssetDatabase.SaveAssets();Debug.Log("RAILWAY_SCENE_READY");
    }
    public const string Version="1.1.0";
    const string Scene="Assets/Scenes/OlomoucHlavniNadrazi.unity";
    /// <summary>Player settings shared by every platform build.</summary>
    public static void ConfigurePlayer()
    {
        PlayerSettings.productName="SuperCigo";PlayerSettings.companyName="Railway Platformer";
        PlayerSettings.bundleVersion=Version;PlayerSettings.macOS.buildNumber=Version;
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.allowFullscreenSwitch=true;
        PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
        PlayerSettings.SplashScreen.show=false;PlayerSettings.SplashScreen.showUnityLogo=false;
        var icon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Icon/SuperCigo_1024.png");
        if(icon)PlayerSettings.SetIcons(NamedBuildTarget.Unknown,new[]{icon},IconKind.Any);
        // IL2CPP cannot cross-compile the Windows player from macOS; Mono runs everywhere and builds universal Mac apps.
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        PlayerSettings.meshDeformation=MeshDeformation.GPU;
        // The only scene has fog disabled, so automatic stripping would drop the fog the levels enable at runtime.
        var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        graphics.FindProperty("m_FogStripping").intValue=1;
        graphics.FindProperty("m_FogKeepLinear").boolValue=true;
        graphics.FindProperty("m_FogKeepExp").boolValue=false;
        graphics.FindProperty("m_FogKeepExp2").boolValue=false;
        graphics.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
    }
    /// <summary>
    /// Universal (Intel + Apple silicon) app, ad-hoc signed, zipped with ditto without resource forks:
    /// Builds/macOS/SuperCigo.app and Builds/SuperCigo-macOS.zip.
    /// </summary>
    [MenuItem("SuperCigo/Build macOS")]
    public static void BuildMac()
    {
        EnsureMaterials();ConfigurePlayer();
        UnityEditor.OSXStandalone.UserBuildSettings.architecture=OSArchitecture.x64ARM64;
        const string folder="Builds/macOS",app=folder+"/SuperCigo.app",zip="Builds/SuperCigo-macOS.zip";
        if(Directory.Exists(folder))Directory.Delete(folder,true);
        Build(app,BuildTarget.StandaloneOSX);
        foreach(string junk in Directory.GetDirectories(folder,"*DoNotShip*"))Directory.Delete(junk,true);
        foreach(string junk in Directory.GetDirectories(folder,"*DontShip*"))Directory.Delete(junk,true);
        // Every executable and library carries both architectures.
        int binaries=0;
        foreach(string file in Directory.GetFiles(app,"*",SearchOption.AllDirectories))
        {
            if(!IsMachO(file))continue;
            string archs=Run("lipo","-archs \""+file+"\"");
            if(!archs.Contains("x86_64")||!archs.Contains("arm64"))throw new System.Exception("Not universal: "+file+" ("+archs.Trim()+")");
            binaries++;
        }
        Run("xattr","-cr \""+app+"\"");
        Run("codesign","--force --deep --sign - \""+app+"\"");
        Run("codesign","--verify --deep --strict \""+app+"\"");
        if(File.Exists(zip))File.Delete(zip);
        Run("ditto","-c -k --norsrc --keepParent \""+app+"\" \""+zip+"\"");
        using(var archive=ZipFile.OpenRead(zip))
            foreach(var entry in archive.Entries)
                if(entry.FullName.Contains("__MACOSX")||entry.Name.StartsWith("._"))throw new System.Exception("Resource fork in zip: "+entry.FullName);
        Debug.Log("RAILWAY_MAC_BUILD_OK universal binaries="+binaries+" zip="+new FileInfo(zip).Length);
    }
    /// <summary>Windows x64 player in Builds/Windows/SuperCigo and Builds/SuperCigo-Windows.zip (the folder inside the zip).</summary>
    [MenuItem("SuperCigo/Build Windows")]
    public static void BuildWindows()
    {
        EnsureMaterials();ConfigurePlayer();
        const string folder="Builds/Windows/SuperCigo",exe=folder+"/SuperCigo.exe",zip="Builds/SuperCigo-Windows.zip";
        if(Directory.Exists(folder))Directory.Delete(folder,true);
        Build(exe,BuildTarget.StandaloneWindows64);
        foreach(string junk in Directory.GetDirectories(folder,"*DontShip*"))Directory.Delete(junk,true);
        foreach(string junk in Directory.GetDirectories(folder,"*DoNotShip*"))Directory.Delete(junk,true);
        // PE header: "MZ", then "PE\0\0" at e_lfanew followed by the machine type 0x8664 (x86-64).
        var bytes=File.ReadAllBytes(exe);int pe=System.BitConverter.ToInt32(bytes,0x3C);
        if(bytes[0]!='M'||bytes[1]!='Z'||bytes[pe]!='P'||bytes[pe+1]!='E'||System.BitConverter.ToUInt16(bytes,pe+4)!=0x8664)throw new System.Exception("SuperCigo.exe is not an x86-64 PE");
        if(File.Exists(zip))File.Delete(zip);
        ZipFile.CreateFromDirectory(folder,zip,System.IO.Compression.CompressionLevel.Optimal,true);
        Debug.Log("RAILWAY_WINDOWS_BUILD_OK zip="+new FileInfo(zip).Length);
    }
    /// <summary>Both platforms plus Builds/SHA256SUMS.txt for the release page.</summary>
    [MenuItem("SuperCigo/Build release")]
    public static void BuildRelease()
    {
        BuildMac();BuildWindows();
        var sums=new System.Text.StringBuilder();
        foreach(string zip in new[]{"Builds/SuperCigo-macOS.zip","Builds/SuperCigo-Windows.zip"})
            using(var sha=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead(zip))
                sums.Append(System.BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant()).Append("  ").Append(Path.GetFileName(zip)).Append('\n');
        File.WriteAllText("Builds/SHA256SUMS.txt",sums.ToString());
        Debug.Log("RAILWAY_RELEASE_OK "+Version+"\n"+sums);
    }
    static void Build(string path,BuildTarget target)
    {
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName=path,target=target,options=BuildOptions.CompressWithLz4HC});
        foreach(var step in report.steps)foreach(var message in step.messages)
            if(message.type==LogType.Warning||message.type==LogType.Error)Debug.Log("RAILWAY_BUILD_MESSAGE "+message.type+": "+message.content);
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Build failed: "+report.summary.result);
        Debug.Log("RAILWAY_BUILD "+target+" size="+report.summary.totalSize+" warnings="+report.summary.totalWarnings);
    }
    static bool IsMachO(string file)
    {
        using(var stream=File.OpenRead(file))
        {
            if(stream.Length<4)return false;
            var magic=new byte[4];stream.Read(magic,0,4);uint value=System.BitConverter.ToUInt32(magic,0);
            return value==0xBEBAFECA||value==0xFEEDFACF||value==0xCFFAEDFE||value==0xCAFEBABE;
        }
    }
    static string Run(string file,string arguments)
    {
        var info=new System.Diagnostics.ProcessStartInfo(file,arguments){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
        using(var process=System.Diagnostics.Process.Start(info))
        {
            // Drain stderr in the background: reading both pipes one after the other can deadlock on a full buffer.
            var errors=process.StandardError.ReadToEndAsync();
            string output=process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            output+=errors.Result;
            if(process.ExitCode!=0)throw new System.Exception(file+" "+arguments+" failed: "+output);
            return output;
        }
    }
    public static void EnsureMaterials()
    {
        // Serialized template materials keep the runtime shaders and their instancing variants in player builds.
        Template("Assets/Resources/Surface.mat","Surface","Railway/Surface");
        Template("Assets/Resources/SurfaceCutout.mat","SurfaceCutout","Railway/SurfaceCutout");
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
