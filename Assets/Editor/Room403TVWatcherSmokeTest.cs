using System;
using System.Collections;
using System.IO;
using System.Linq;
using Archive0317;
using UnityEditor;
using UnityEngine;

public static class Room403TVWatcherSmokeTest
{
    private static void Check(bool result,string message){if(!result)throw new InvalidOperationException(message);File.AppendAllText("Documentation/Verification/TVWatcher/Test.txt","PASS: "+message+"\n");}
    public static IEnumerator Run(FirstPersonPlayer player,InspectableCaseTelevision television,CaseEnvironmentState environment,CaseDefinition definition)
    {
        Directory.CreateDirectory("Documentation/Verification/TVWatcher");File.WriteAllText("Documentation/Verification/TVWatcher/Test.txt","Actual Play Mode TV watcher regression\n");
        var watcher=television.GetComponent<Room403TVWatcher>();var camera=television.RecordingCamera;var progress=CaseProgressStore.Get(definition);
        var key=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name=="Room403KeyEvidence").gameObject;
        Check(watcher.Stage==Room403TVWatcher.WatcherStage.Normal && !watcher.Figure.gameObject.activeSelf,"Initial TV room view contains no watcher");
        Check(((RenderTexture)television.Frame).width==320 && !camera.enabled,"320x180 manually rendered camera remains disabled");
        Check((player.ViewCamera.cullingMask&(1<<watcher.Figure.gameObject.layer))==0 && (camera.cullingMask&(1<<watcher.Figure.gameObject.layer))!=0,"Main view excludes actors while TV camera includes them");
        Check(watcher.Figure.GetComponentsInChildren<Collider>().Length==0 && GameObject.Find("Room403DoorwayShadow")==null,"TV-only silhouette has no collider and legacy real-room shadow is suppressed");
        // Simulate a resumed inspected-TV save where no observation occurs.
        watcher.Tick(41);environment.Evaluate();Check(progress.Has("TVWatcherFinished") && progress.Has("KeyTagVisible") && key.activeSelf,"40-second no-observation fallback releases existing key progression");
        watcher.SendMessage("Start");Check(watcher.Stage==Room403TVWatcher.WatcherStage.Finished,"Saved completion restores without replaying watcher");
        progress.flags.Remove("TVWatcherFinished");progress.flags.Remove("KeyTagVisible");key.SetActive(false);watcher.SendMessage("Start");
        player.ViewCamera.transform.LookAt(television.transform.position);
        float stop=Time.time+1;while(Time.time<stop)yield return null;
        Check(watcher.Stage==Room403TVWatcher.WatcherStage.Normal && watcher.LookingAtTV && Vector3.Distance(watcher.PlayerProxy.position,player.transform.position)<.2f,"Normal feed contains delayed player body at actual room position");
        SaveFrame(television,"Normal");
        foreach(var stage in new[]{Room403TVWatcher.WatcherStage.Doorway,Room403TVWatcher.WatcherStage.Approaching,Room403TVWatcher.WatcherStage.Behind})
        {
            player.ViewCamera.transform.LookAt(new Vector3(-22,10.1f,5.8f));stop=Time.time+6.25f;while(Time.time<stop)yield return null;
            Check(watcher.Stage==stage && watcher.Figure.gameObject.activeSelf,"Looking away advances to "+stage);
            player.ViewCamera.transform.LookAt(television.transform.position);stop=Time.time+1;while(Time.time<stop)yield return null;
            var position=watcher.Figure.position;SaveFrame(television,stage.ToString());
            stop=Time.time+1;while(Time.time<stop)yield return null;
            Check(watcher.Figure.position==position && watcher.Stage==stage,"Figure never moves while TV is watched: "+stage);
            Check(!progress.Has("KeyTagVisible"),"Key does not appear before watcher completion: "+stage);
        }
        var behind=watcher.Figure.position-player.transform.position;var forward=Vector3.ProjectOnPlane(player.ViewCamera.transform.forward,Vector3.up).normalized;
        Check(Vector3.Dot(behind,forward)<0 && behind.magnitude<1,"Final TV silhouette stands directly behind player's proxy");
        player.ViewCamera.transform.LookAt(watcher.Figure.position+Vector3.up);ScreenCapture.CaptureScreenshot("Documentation/Verification/TVWatcher/RealRoomEmpty.png");
        stop=Time.time+1.05f;while(Time.time<stop)yield return null;
        var staticSource=GameObject.Find("SpatialOneShot_CRTStatic")?.GetComponent<AudioSource>();
        Check(staticSource!=null && staticSource.spatialBlend==1 && staticSource.volume<=.035f,"Final static has a quiet TV-position spatial source");
        stop=Time.time+.2f;while(Time.time<stop)yield return null;environment.Evaluate();
        Check(watcher.Stage==Room403TVWatcher.WatcherStage.Finished && !watcher.Figure.gameObject.activeSelf && key.activeSelf && progress.Has("KeyTagVisible"),"Looking back ends watcher and releases physical key");
        player.ViewCamera.transform.LookAt(television.transform.position);stop=Time.time+1;while(Time.time<stop)yield return null;SaveFrame(television,"Finished");
        watcher.SendMessage("Start");Check(watcher.Stage==Room403TVWatcher.WatcherStage.Finished,"Reloaded finished flags cannot restart sequence");
        File.AppendAllText("Documentation/Verification/TVWatcher/Test.txt","PASS\nPhysical sound/brightness, watcher silhouette readability and natural turning tempo require human playthrough.\n");
    }
    private static void SaveFrame(InspectableCaseTelevision tv,string name)
    {var rt=(RenderTexture)tv.Frame;var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();RenderTexture.active=previous;File.WriteAllBytes("Documentation/Verification/TVWatcher/"+name+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);}
}
