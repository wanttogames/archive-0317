using System;
using System.Collections;
using System.IO;
using System.Linq;
using Archive0317;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Room403TVWatcherSmokeTest
{
    private static string Frames;
    public static string CaptureFolder=>Frames;
    private static void Check(bool result,string message){if(!result)throw new InvalidOperationException(message);File.AppendAllText("Documentation/Verification/TVWatcher/Test.txt","PASS: "+message+"\n");}
    public static IEnumerator Run(FirstPersonPlayer player,InspectableCaseTelevision television,CaseEnvironmentState environment,CaseDefinition definition)
    {
        Directory.CreateDirectory("Documentation/Verification/TVWatcher");File.WriteAllText("Documentation/Verification/TVWatcher/Test.txt","Actual Play Mode TV watcher regression\n");
        Frames="Documentation/Verification/TVWatcher/Run_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+"/";Directory.CreateDirectory(Frames);
        var watcher=television.GetComponent<Room403TVWatcher>();var camera=television.RecordingCamera;var progress=CaseProgressStore.Get(definition);
        var key=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name=="Room403KeyEvidence").gameObject;
        Check(watcher.Stage==Room403TVWatcher.WatcherStage.Normal && !watcher.Figure.gameObject.activeSelf,"Initial TV room view contains no watcher");
        Check(((RenderTexture)television.Frame).width==320 && !camera.enabled,"320x180 manually rendered camera remains disabled");
        Check((player.ViewCamera.cullingMask&(1<<watcher.Figure.gameObject.layer))==0 && (camera.cullingMask&(1<<watcher.Figure.gameObject.layer))!=0,"Main view excludes actors while TV camera includes them");
        Check(watcher.Figure.GetComponentsInChildren<Collider>().Length==0 && GameObject.Find("Room403DoorwayShadow")==null,"TV-only silhouette has no collider and legacy real-room shadow is suppressed");
        // Simulate a resumed inspected-TV save where no observation occurs.
        watcher.Tick(41);environment.Evaluate();Check(progress.Has("TVWatcherFinished") && progress.Has("KeyTagVisible") && key.activeSelf,"40-second no-observation fallback releases existing key progression");
        watcher.SendMessage("Start");Check(watcher.Stage==Room403TVWatcher.WatcherStage.Finished,"Saved completion restores without replaying watcher");
        progress.flags.Remove("TVWatcherFinished");progress.flags.Remove("KeyTagVisible");progress.facts.RemoveAll(f=>f.key=="Room403TVWatcherSnapshot");key.SetActive(false);watcher.SendMessage("Start");
        player.ViewCamera.transform.LookAt(television.transform.position);
        float stop=Time.time+1;while(Time.time<stop)yield return null;
        Check(watcher.Stage==Room403TVWatcher.WatcherStage.Normal && watcher.LookingAtTV && Vector3.Distance(watcher.PlayerProxy.position,player.transform.position)<.2f,"Normal feed contains delayed player body at actual room position");
        SaveFrame(television,"Normal");
        foreach(var stage in new[]{Room403TVWatcher.WatcherStage.Doorway,Room403TVWatcher.WatcherStage.Approaching,Room403TVWatcher.WatcherStage.Behind})
        {
            while(watcher.StageAge<5.05f)yield return null;
            Check(!progress.Has("KeyTagVisible"),"Watching TV does not bypass observation sequence");
            player.ViewCamera.transform.LookAt(new Vector3(-22,10.1f,5.8f));stop=Time.time+.85f;while(Time.time<stop)yield return null;
            Check(watcher.Stage==stage && watcher.Figure.gameObject.activeSelf,"Looking away advances to "+stage);
            player.ViewCamera.transform.LookAt(television.transform.position);stop=Time.time+1;while(Time.time<stop)yield return null;
            var position=watcher.Figure.position;SaveFrame(television,stage.ToString());
            stop=Time.time+1;while(Time.time<stop)yield return null;
            Check(watcher.Figure.position==position && watcher.Stage==stage,"Figure never moves while TV is watched: "+stage);
            Check(!progress.Has("KeyTagVisible"),"Key does not appear before watcher completion: "+stage);
            var head=camera.WorldToViewportPoint(watcher.Figure.position+Vector3.up*1.65f);
            var feet=camera.WorldToViewportPoint(watcher.Figure.position);
            Check(head.z>0 && head.x>.03f && head.x<.97f && head.y>.03f && head.y<.97f && feet.y>.03f,"Figure stays framed from head to feet: "+stage);
            var capture=CaptureIntensities(stage.ToString());while(capture.MoveNext())yield return null;
            if(stage==Room403TVWatcher.WatcherStage.Approaching)
            {
                var beforeReload=watcher.Figure.position;var savedAge=watcher.StageAge;
                var unload=SceneManager.LoadSceneAsync("ArchiveRoom");while(!unload.isDone)yield return null;
                CaseProgressStore.ClearCache();
                var load=SceneManager.LoadSceneAsync("Case001_Motel");while(!load.isDone)yield return null;yield return null;
                player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();player.enabled=false;player.HUD.CloseCase();
                var controller=player.GetComponent<CharacterController>();controller.enabled=false;player.transform.position=new Vector3(-19.5f,9.05f,5.8f);controller.enabled=true;
                television=GameObject.Find("Room403CRTTV").GetComponent<InspectableCaseTelevision>();watcher=television.GetComponent<Room403TVWatcher>();camera=television.RecordingCamera;
                environment=GameObject.Find("Room403Interior").GetComponent<CaseEnvironmentState>();progress=CaseProgressStore.Get(definition);
                key=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name=="Room403KeyEvidence").gameObject;
                player.ViewCamera.transform.LookAt(television.transform.position);Physics.SyncTransforms();environment.Evaluate();
                Check(watcher.Stage==stage && Vector3.Distance(watcher.Figure.position,beforeReload)<.01f && watcher.StageAge>=savedAge,"Actual scene unload, cleared save cache and reload restore intermediate stage and pose");
                Check(!progress.Has("TVWatcherFinished") && !key.activeSelf,"Intermediate reload does not release key or finish sequence early");
                stop=Time.time+2.5f;while(Time.time<stop)yield return null;
                Check(television.FootageVisible && watcher.LookingAtTV,"Reloaded TV returns to live room feed and continues observation");
            }
        }
        var behind=watcher.Figure.position-player.transform.position;var forward=Vector3.ProjectOnPlane(player.ViewCamera.transform.forward,Vector3.up).normalized;
        Check(Vector3.Dot(behind,forward)<0 && behind.magnitude<1,"Final TV silhouette stands directly behind player's proxy");
        player.ViewCamera.transform.LookAt(watcher.Figure.position+Vector3.up);ScreenCapture.CaptureScreenshot(Frames+"RealRoomEmpty.png");
        stop=Time.time+1.05f;while(Time.time<stop)yield return null;
        var staticSource=GameObject.Find("SpatialOneShot_CRTStatic")?.GetComponent<AudioSource>();
        Check(staticSource!=null && staticSource.spatialBlend==1 && staticSource.volume<=.035f,"Final static has a quiet TV-position spatial source");
        stop=Time.time+.2f;while(Time.time<stop)yield return null;environment.Evaluate();
        Check(watcher.Stage==Room403TVWatcher.WatcherStage.Finished && !watcher.Figure.gameObject.activeSelf && key.activeSelf && progress.Has("KeyTagVisible"),"Looking back ends watcher and releases physical key");
        player.ViewCamera.transform.LookAt(television.transform.position);stop=Time.time+1;while(Time.time<stop)yield return null;SaveFrame(television,"Finished");
        watcher.SendMessage("Start");Check(watcher.Stage==Room403TVWatcher.WatcherStage.Finished,"Reloaded finished flags cannot restart sequence");
        File.AppendAllText("Documentation/Verification/TVWatcher/Test.txt","PASS\nPhysical sound/brightness, watcher silhouette readability and natural turning tempo require human playthrough.\n");
    }
    private static IEnumerator CaptureIntensities(string stage)
    {
        bool had=PlayerPrefs.HasKey(MainMenuController.VisualIntensityKey);float original=PlayerPrefs.GetFloat(MainMenuController.VisualIntensityKey);
        var gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));gameView.Show();gameView.Focus();
        var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();player.SetCapture(true);player.HUD.SetPrompt(false);
        try
        {
            foreach(float value in new[]{0f,.65f,1f})
            {
                PlayerPrefs.SetFloat(MainMenuController.VisualIntensityKey,value);
                var material=player.ViewCamera.GetComponent<RetroCameraStyle>().Prepare(player.ViewCamera);
                Check(Mathf.Abs(material.GetVector("_PixelGrid").z-.52f*value)<.001f,"VHS intensity drives world pixelation: "+value);
                float stop=Time.time+.25f;while(Time.time<stop){gameView.Repaint();EditorApplication.QueuePlayerLoopUpdate();yield return null;}
                var television=GameObject.Find("Room403CRTTV").GetComponent<InspectableCaseTelevision>();
                Check(GameObject.Find("Room403TVScreen").GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseMap")==television.Frame,"TV screen material references the current live frame: "+value);
                SaveFrame(television,stage+"_SourceVHS"+Mathf.RoundToInt(value*100));
                var target=player.ViewCamera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1920,1080,24);var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
                try{player.ViewCamera.targetTexture=rt;player.ViewCamera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(Frames+stage+"_CameraVHS"+Mathf.RoundToInt(value*100)+".png",image.EncodeToPNG());}
                finally{player.ViewCamera.targetTexture=target;RenderTexture.active=active;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);}
                ScreenCapture.CaptureScreenshot(Frames+stage+"_VHS"+Mathf.RoundToInt(value*100)+".png");
                stop=Time.time+.25f;while(Time.time<stop){gameView.Repaint();EditorApplication.QueuePlayerLoopUpdate();yield return null;}
            }
        }
        finally{if(had)PlayerPrefs.SetFloat(MainMenuController.VisualIntensityKey,original);else PlayerPrefs.DeleteKey(MainMenuController.VisualIntensityKey);}
    }
    private static void SaveFrame(InspectableCaseTelevision tv,string name)
    {var rt=(RenderTexture)tv.Frame;var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();RenderTexture.active=previous;File.WriteAllBytes(Frames+name+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);}
}
