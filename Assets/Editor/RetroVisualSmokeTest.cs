using System;
using System.IO;
using System.Linq;
using Archive0317;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class RetroVisualSmokeTest
{
    public static System.Collections.IEnumerator Capture(string name)
    {ScreenCapture.CaptureScreenshot("Documentation/Verification/"+name+".png");float until=Time.time+.18f;while(Time.time<until)yield return null;}
    public static void ValidateCurrentScene()
    {
        var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();var camera=player.ViewCamera;var style=camera.GetComponent<RetroCameraStyle>();
        Check(style!=null && style.Profile!=null,"Player camera has a serialized visual profile");
        Check(style.Profile.pixelHeight==360 && style.Profile.pixelStrength==1,"640x360-equivalent pixel grid selected");
        var expected=SceneManager.GetActiveScene().name=="ArchiveRoom"?RetroVisualPatch.ArchiveProfile:RetroVisualPatch.MotelProfile;
        Check(AssetDatabase.GetAssetPath(style.Profile)==expected,"Scene-specific cold/warm profile");
        Check(player.HUD.GetComponentInParent<Canvas>().renderMode==RenderMode.ScreenSpaceOverlay,"HUD and document text bypass world post-processing");
        Check(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c!=camera).All(c=>c.GetComponent<RetroCameraStyle>()==null),"CCTV and CRT capture cameras exclude gameplay effects");
        foreach(var path in new[]{"Assets/Settings/PC_Renderer.asset","Assets/Settings/Mobile_Renderer.asset"})
            Check(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path).rendererFeatures.Count(f=>f is RetroPixelationFeature && f.isActive)==1,"Exactly one integrated world pass: "+path);
        foreach(var path in new[]{"Assets/Shaders/RetroWorld.shader","Assets/Shaders/CCTVImage.shader"})
        {var shader=AssetDatabase.LoadAssetAtPath<Shader>(path);Check(shader!=null && shader.isSupported && !ShaderUtil.ShaderHasError(shader),"Shader compiled and supported: "+path);}
        Check(style.Profile.grain<=.004f && style.Profile.ditherStrength<=.3f,"Restrained grain and shadow-safe dither settings");
        VerifyPixelBlocks(camera);
    }
    private static void VerifyPixelBlocks(Camera camera)
    {
        var priorTarget=camera.targetTexture;var priorMask=camera.cullingMask;var priorActive=RenderTexture.active;
        var texture=new Texture2D(128,128,TextureFormat.RGB24,false){filterMode=FilterMode.Point};var random=new System.Random(317);var colors=new Color32[128*128];
        for(int i=0;i<colors.Length;i++)colors[i]=new Color32((byte)random.Next(32,220),(byte)random.Next(32,220),(byte)random.Next(32,220),255);
        texture.SetPixels32(colors);texture.Apply();var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetTexture("_BaseMap",texture);
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="TemporaryRetroGPUProbe";quad.layer=31;
        quad.transform.position=camera.transform.position+camera.transform.forward*2;quad.transform.rotation=camera.transform.rotation;quad.transform.localScale=new Vector3(6,4,1);quad.GetComponent<Renderer>().sharedMaterial=material;
        var rt=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try
        {
            camera.cullingMask=1<<31;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();var pixels=image.GetPixels32();int matched=0,total=0;
            for(int y=160;y<560;y+=2)for(int x=200;x<1080;x+=2){var a=pixels[y*1280+x];var b=pixels[y*1280+x+1];var c=pixels[(y+1)*1280+x];if(Difference(a,b)<3 && Difference(a,c)<3)matched++;total++;}
            Check(matched/(float)total>.98f,"Actual RenderGraph output forms uniform 2x2 pixel blocks at 1280x720");
            Check(pixels.Max(c=>c.r)-pixels.Min(c=>c.r)>30,"Actual GPU output is nonblank");
        }
        finally{camera.targetTexture=priorTarget;camera.cullingMask=priorMask;RenderTexture.active=priorActive;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(quad);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(texture);}
    }
    private static int Difference(Color32 a,Color32 b)=>Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b);
    private static void Check(bool success,string label)
    {if(!success)throw new InvalidOperationException(label);File.AppendAllText("Documentation/Verification/RetroVisualSmokeTest.txt","PASS: "+SceneManager.GetActiveScene().name+" / "+label+"\n");}
}
