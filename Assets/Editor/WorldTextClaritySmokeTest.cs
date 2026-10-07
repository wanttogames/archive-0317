using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Archive0317;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

// Native Game View comparisons. All temporary PlayerPrefs changes are restored.
public static class WorldTextClaritySmokeTest
{
    private static IEnumerator sequence;
    private static bool hadIntensity,hadSave;
    private static float intensity;
    private static string save,key;
    private static bool background;
    public static string Status {get;private set;}="Not run";
    private const string Folder="Documentation/Verification/WorldText/Final/";
    public static void Begin()
    {
        if(!EditorApplication.isPlaying || sequence!=null)throw new InvalidOperationException("Enter Play Mode and finish the previous comparison first.");
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"Results.txt","Actual Play Mode / native Game View world text comparison\n");
        hadIntensity=PlayerPrefs.HasKey(MainMenuController.VisualIntensityKey);intensity=PlayerPrefs.GetFloat(MainMenuController.VisualIntensityKey);
        var definition=AssetDatabase.LoadAssetAtPath<CaseDefinition>(Case001MotelBuilder.DefinitionPath);key=CaseProgressStore.StorageKey(definition);hadSave=PlayerPrefs.HasKey(key);save=PlayerPrefs.GetString(key);
        PlayerPrefs.DeleteKey(key);CaseProgressStore.ClearCache();background=Application.runInBackground;Application.runInBackground=true;
        sequence=Run();Status="Running";EditorApplication.update+=Tick;
    }
    private static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        try{if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode stopped");if(!sequence.MoveNext())Finish("PASS");}
        catch(Exception ex){Finish("FAIL: "+ex.Message);Debug.LogError(ex);}
    }
    private static void Finish(string result)
    {
        EditorApplication.update-=Tick;sequence=null;Status=result;
        if(hadIntensity)PlayerPrefs.SetFloat(MainMenuController.VisualIntensityKey,intensity);else PlayerPrefs.DeleteKey(MainMenuController.VisualIntensityKey);
        if(hadSave)PlayerPrefs.SetString(key,save);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();CaseProgressStore.ClearCache();Application.runInBackground=background;
        File.AppendAllText(Folder+"Results.txt",result+"\n");
    }
    private static IEnumerator Run()
    {
        var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
        if(menu!=null && MainMenuController.IsMenuOpen){menu.StartCoroutine((IEnumerator)typeof(MainMenuController).GetMethod("CloseMenu",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(menu,null));while(MainMenuController.IsMenuOpen)yield return null;}
        var views=new[]{"ArchiveSign","DoorSign","EmergencyExit","CaseLabel","NoticeHeading"};
        VerifyTextGPU();
        foreach(var name in views){var routine=Compare(GameObject.Find(name).GetComponent<TextMesh>());while(routine.MoveNext())yield return null;}
        SceneManager.LoadScene("Case001_Motel");yield return null;
        double ready=EditorApplication.timeSinceStartup+.6;while(EditorApplication.timeSinceStartup<ready)yield return null;
        VerifyTextGPU();
        foreach(var number in new[]{"401","402","404","405"})
        {var t=UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(x=>x.text==number && x.name=="RoomNumber" && x.gameObject.activeInHierarchy);var routine=Compare(t);while(routine.MoveNext())yield return null;}
        // Inspect the revealed geometry separately; full CASE smoke verifies its real progression gate.
        var hidden=UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(x=>x.text=="403" && x.name=="RoomNumber" && !x.gameObject.activeInHierarchy && x.transform.position.y>8);
        var inactive=hidden.transform;while(inactive!=null){inactive.gameObject.SetActive(true);inactive=inactive.parent;}
        var reveal=Compare(hidden);while(reveal.MoveNext())yield return null;
        SceneManager.LoadScene("ArchiveRoom");yield return null;
    }
    private static void VerifyTextGPU()
    {
        var camera=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>().ViewCamera;var style=camera.GetComponent<RetroCameraStyle>();
        int mask=camera.cullingMask;var target=camera.targetTexture;var active=RenderTexture.active;var flags=camera.clearFlags;var color=camera.backgroundColor;
        var rt=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        var cameraData=camera.GetComponent<UniversalAdditionalCameraData>();bool post=cameraData.renderPostProcessing;cameraData.renderPostProcessing=false;
        var go=new GameObject("TemporaryLegibilityGPUProbe");go.layer=31;go.transform.position=camera.transform.position+camera.transform.forward*2;go.transform.rotation=camera.transform.rotation;
        var text=go.AddComponent<TextMesh>();text.font=UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).First(x=>x!=text && x.font!=null).font;
        text.text="403 EXIT";text.fontSize=128;text.characterSize=.025f;text.anchor=TextAnchor.MiddleCenter;text.color=Color.white;go.AddComponent<WorldTextDepth>();
        GameObject blocker=null;Material blockMaterial=null;
        float prior=PlayerPrefs.GetFloat(MainMenuController.VisualIntensityKey);bool enabled=style.enabled;
        try
        {
            camera.cullingMask=1<<31;camera.targetTexture=rt;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.02f,.02f);
            Color32[] baseline=null;
            foreach(float strength in new[]{0f,.65f,1f})
            {
                PlayerPrefs.SetFloat(MainMenuController.VisualIntensityKey,strength);camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();var pixels=image.GetPixels32();
                int bright=pixels.Count(p=>p.r>230 && p.g>230 && p.b>230);if(bright<100)throw new InvalidOperationException("Text GPU pass did not produce visible glyphs");
                if(baseline==null)baseline=pixels;
                else if(pixels.Where((p,i)=>baseline[i].r>230 && baseline[i].g>230 && baseline[i].b>230).Any(p=>p.r<228 || p.g<228 || p.b<228))throw new InvalidOperationException("Retro pass degraded full-resolution glyph cores");
            }
            blocker=GameObject.CreatePrimitive(PrimitiveType.Quad);blocker.layer=31;blocker.transform.position=camera.transform.position+camera.transform.forward;blocker.transform.rotation=camera.transform.rotation;blocker.transform.localScale=new Vector3(6,4,1);
            blockMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));blockMaterial.SetColor("_BaseColor",Color.black);blocker.GetComponent<Renderer>().sharedMaterial=blockMaterial;
            camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();if(image.GetPixels32().Any(p=>p.r>230 && p.g>230 && p.b>230))throw new InvalidOperationException("World text leaked through an opaque blocker");
            UnityEngine.Object.DestroyImmediate(blocker);blocker=null;style.enabled=false;
            camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();if(image.GetPixels32().Count(p=>p.r>230 && p.g>230 && p.b>230)<100)throw new InvalidOperationException("Non-retro recording camera lost world text");
            File.AppendAllText(Folder+"Results.txt","PASS: "+SceneManager.GetActiveScene().name+" actual GPU glyph cores preserved at VHS 0/.65/1, opaque depth occlusion retained, text visible without RetroCameraStyle\n");
        }
        finally
        {
            cameraData.renderPostProcessing=post;style.enabled=enabled;PlayerPrefs.SetFloat(MainMenuController.VisualIntensityKey,prior);camera.cullingMask=mask;camera.targetTexture=target;camera.clearFlags=flags;camera.backgroundColor=color;RenderTexture.active=active;
            if(blocker!=null)UnityEngine.Object.DestroyImmediate(blocker);if(blockMaterial!=null)UnityEngine.Object.DestroyImmediate(blockMaterial);UnityEngine.Object.DestroyImmediate(go);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
        }
    }
    private static IEnumerator Compare(TextMesh text)
    {
        var name=text.name;
        WorldTextDepth.ApplyLabels();var renderer=text.GetComponent<Renderer>();
        if(renderer.sharedMaterial.shader.name!="Archive0317/WorldTextDepth")throw new InvalidOperationException(name+" incorrect shader");
        var p=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();p.enabled=false;p.HUD.CloseCase();p.SetCapture(true);
        var camera=p.ViewCamera;var distance=name=="CaseLabel"?.7f:name=="ArchiveSign"?2.2f:1.7f;
        camera.transform.position=renderer.bounds.center-text.transform.forward*distance;
        if(SceneManager.GetActiveScene().name=="ArchiveRoom" && name!="CaseLabel")
        {var eye=camera.transform.position;eye.y=1.7f;camera.transform.position=eye;}
        camera.transform.LookAt(renderer.bounds.center);
        foreach(float strength in new[]{0f,.65f,1f})
        {
            PlayerPrefs.SetFloat(MainMenuController.VisualIntensityKey,strength);
            double until=EditorApplication.timeSinceStartup+.4;while(EditorApplication.timeSinceStartup<until)yield return null;
            var mat=camera.GetComponent<RetroCameraStyle>().Prepare(camera);var expected=camera.GetComponent<RetroCameraStyle>().Profile.pixelStrength*strength;
            if(!Mathf.Approximately(mat.GetVector("_PixelGrid").z,expected))throw new InvalidOperationException("VHS intensity link");
            string path=Folder+SceneManager.GetActiveScene().name+"_"+name+"_"+(name=="RoomNumber"?text.text+"_":"")+strength.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png";
            ScreenCapture.CaptureScreenshot(path);
            until=EditorApplication.timeSinceStartup+.8;while(EditorApplication.timeSinceStartup<until)yield return null;
            if(!File.Exists(path))throw new IOException("Native Game View capture missing: "+path);
            File.AppendAllText(Folder+"Results.txt","PASS: "+SceneManager.GetActiveScene().name+" / "+text.text.Replace("\n"," /")+" / VHS="+strength+" / correct shader, intensity and actual screenshot\n");
        }
    }
}
