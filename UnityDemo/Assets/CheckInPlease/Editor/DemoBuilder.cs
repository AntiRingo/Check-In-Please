using System;
using System.IO;
using CheckInPlease;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DemoBuilder
{
    [MenuItem("Check In Please/Create Demo Scene")]
    public static void CreateScene()
    {
        Directory.CreateDirectory("Assets/CheckInPlease/Scenes");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("Check in, please — playable demo");root.AddComponent<DemoGame>();
        EditorSceneManager.SaveScene(scene,"Assets/CheckInPlease/Scenes/Hotel.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/CheckInPlease/Scenes/Hotel.unity",true)};
        PlayerSettings.companyName="Northern District";PlayerSettings.productName="Check In Please";
        PlayerSettings.bundleVersion="0.2.0";PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
        PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone,ApiCompatibilityLevel.NET_Standard);
        var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var input=settings.FindProperty("activeInputHandler");if(input!=null){input.intValue=0;settings.ApplyModifiedProperties();}
        AssetDatabase.SaveAssets();
    }
    [MenuItem("Check In Please/Build Windows Demo")]
    public static void BuildMenu()
    {
        if(Application.dataPath.Contains(","))
        {
            AssetDatabase.SaveAssets();
            string script=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Build-Demo.ps1"));
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("powershell.exe","-NoProfile -File \""+script+"\"")
            {UseShellExecute=false,CreateNoWindow=true});
            Debug.Log("Windows build started via Build-Demo.ps1. Progress: ../Builds/unity-build.log");
            return;
        }
        Build();
    }
    public static void Build()
    {
        try
        {
            ConfigureArt();CreateScene();var output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/Windows"));Directory.CreateDirectory(output);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{"Assets/CheckInPlease/Scenes/Hotel.unity"},locationPathName=Path.Combine(output,"CheckInPlease.exe"),
                target=BuildTarget.StandaloneWindows64,options=BuildOptions.None });
            Debug.Log("BUILD_RESULT "+report.summary.result+" bytes="+report.summary.totalSize);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            File.WriteAllText(Path.Combine(output,"build-success.json"),"{\"version\":\"0.2.0\",\"unity\":\""+Application.unityVersion+"\",\"bytes\":"+report.summary.totalSize+"}");
            if(Application.isBatchMode)EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
    static void ConfigureArt()
    {
        foreach(string file in Directory.GetFiles("Assets/CheckInPlease/Resources/Art","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;
            importer.npotScale=TextureImporterNPOTScale.None;importer.filterMode=FilterMode.Bilinear;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
    }
}
