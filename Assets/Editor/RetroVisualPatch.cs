using System;
using System.Linq;
using Archive0317;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

public static class RetroVisualPatch
{
    public const string ArchiveProfile="Assets/Settings/Retro/ArchiveRoom.asset";
    public const string MotelProfile="Assets/Settings/Retro/Motel.asset";
    [MenuItem("Archive 03:17/Apply PS1 VHS Visual Style")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before patching scenes.");
        if(!AssetDatabase.IsValidFolder("Assets/Settings/Retro"))AssetDatabase.CreateFolder("Assets/Settings","Retro");
        var cold=Profile(ArchiveProfile,new Color(.97f,1.01f,1.015f),.83f,.0024f);
        var warm=Profile(MotelProfile,new Color(1.035f,1.005f,.96f),.79f,.0032f);
        foreach(var path in new[]{"Assets/Settings/PC_Renderer.asset","Assets/Settings/Mobile_Renderer.asset"})
        {
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if(!renderer.rendererFeatures.Any(f=>f is RetroPixelationFeature))
            {var feature=ScriptableObject.CreateInstance<RetroPixelationFeature>();feature.name="Archive PS1 World";AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);}
            var serialized=new SerializedObject(renderer);var map=serialized.FindProperty("m_RendererFeatureMap");map.arraySize=renderer.rendererFeatures.Count;
            for(int i=0;i<map.arraySize;i++){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.rendererFeatures[i],out string guid,out long id);map.GetArrayElementAtIndex(i).longValue=id;}
            serialized.ApplyModifiedPropertiesWithoutUndo();renderer.SetDirty();EditorUtility.SetDirty(renderer);
        }
        ConfigureScene("Assets/Scenes/ArchiveRoom.unity",cold);
        ConfigureScene("Assets/Scenes/Cases/Case001_Motel.unity",warm);
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/ArchiveRoom.unity");
    }
    private static RetroVisualProfile Profile(string path,Color tint,float saturation,float grain)
    {
        var profile=AssetDatabase.LoadAssetAtPath<RetroVisualProfile>(path);
        if(profile==null){profile=ScriptableObject.CreateInstance<RetroVisualProfile>();AssetDatabase.CreateAsset(profile,path);}
        profile.shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/RetroWorld.shader");profile.pixelHeight=360;profile.pixelStrength=1;
        profile.ditherStrength=.22f;profile.grain=grain;profile.tint=tint;profile.saturation=saturation;EditorUtility.SetDirty(profile);return profile;
    }
    private static void ConfigureScene(string path,RetroVisualProfile profile)
    {
        var scene=EditorSceneManager.OpenScene(path);var player=Object.FindFirstObjectByType<FirstPersonPlayer>();var camera=player.ViewCamera;
        var style=camera.GetComponent<RetroCameraStyle>();if(style==null)style=camera.gameObject.AddComponent<RetroCameraStyle>();style.Configure(profile);EditorUtility.SetDirty(style);
        var data=camera.GetComponent<UniversalAdditionalCameraData>();data.antialiasing=AntialiasingMode.None;PrefabUtility.RecordPrefabInstancePropertyModifications(data);
        foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.shadows==LightShadows.Soft){light.shadows=LightShadows.Hard;PrefabUtility.RecordPrefabInstancePropertyModifications(light);}
        // Grain is in the integrated world pass; retain other existing Volume settings.
        foreach(var volume in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))if(volume.sharedProfile!=null && volume.sharedProfile.TryGet<FilmGrain>(out var grain))
        {grain.intensity.Override(0);EditorUtility.SetDirty(grain);EditorUtility.SetDirty(volume.sharedProfile);}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
}
