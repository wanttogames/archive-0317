using System;
using System.IO;
using System.Reflection;
using System.Collections;
using Archive0317;
using UnityEngine;

public static class CorridorRearCueSmokeTest
{
    public static IEnumerator CapturePreview()
    {
        var original=GameObject.Find("CorridorRearCues").GetComponent<CorridorRearCue>();var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();
        var position=player.transform.position;var rotation=player.transform.rotation;var view=player.ViewCamera.transform.localRotation;var cc=player.GetComponent<CharacterController>();bool collision=cc.enabled;bool enabled=original.enabled;
        original.enabled=false;var copy=UnityEngine.Object.Instantiate(original.gameObject);var cue=copy.GetComponent<CorridorRearCue>();cue.enabled=false;
        try
        {
            cc.enabled=false;cue.SendMessage("Start");typeof(CorridorRearCue).GetField("appearanceAt",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(cue,2);
            for(int pass=0;pass<2;pass++){player.transform.position=new Vector3(0,9.05f,1);player.transform.rotation=Quaternion.identity;player.ViewCamera.transform.localRotation=Quaternion.identity;cue.Evaluate(60);cue.Evaluate(60);for(int i=0;i<25;i++){player.transform.position+=Vector3.forward*.25f;cue.Evaluate(.1f);}}
            var figure=copy.transform.Find("DistantDoorwayFigure");player.ViewCamera.transform.LookAt(figure.position+Vector3.up);cue.Evaluate(.1f);Check(cue.FigureVisible,"Preview follows real second-cue appearance");
            Directory.CreateDirectory("Documentation/Verification");ScreenCapture.CaptureScreenshot("Documentation/Verification/CorridorRearFigure.png");float until=Time.time+.5f;while(Time.time<until)yield return null;
        }
        finally{UnityEngine.Object.Destroy(copy);player.transform.position=position;player.transform.rotation=rotation;player.ViewCamera.transform.localRotation=view;cc.enabled=collision;original.enabled=enabled;Physics.SyncTransforms();}
    }
    public static void Run()
    {
        var original=GameObject.Find("CorridorRearCues").GetComponent<CorridorRearCue>();
        var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();
        var position=player.transform.position;var rotation=player.transform.rotation;var view=player.ViewCamera.transform.localRotation;
        var cc=player.GetComponent<CharacterController>();bool collision=cc.enabled;var enabled=original.enabled;original.enabled=false;
        var copy=UnityEngine.Object.Instantiate(original.gameObject);var cue=copy.GetComponent<CorridorRearCue>();cue.enabled=false;
        try
        {
            cc.enabled=false;player.HUD.CloseCase();cue.SendMessage("Start");
            typeof(CorridorRearCue).GetField("appearanceAt",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(cue,2);
            for(int pass=1;pass<=3;pass++)
            {
                player.transform.position=new Vector3(0,9.05f,1);player.transform.rotation=Quaternion.identity;player.ViewCamera.transform.localRotation=Quaternion.identity;
                cue.Evaluate(60);cue.Evaluate(60);
                for(int i=0;i<25;i++){player.transform.position+=Vector3.forward*.25f;cue.Evaluate(.1f);}
                Check(cue.CueCount==pass,"Walking and cooldown produce cue "+pass);
                Check(cue.FigureVisible==(pass==2),"Figure occurs on selected second cue only: "+pass);
                if(pass==2)
                {
                    var figure=copy.transform.Find("DistantDoorwayFigure");player.ViewCamera.transform.LookAt(figure.position+Vector3.up);cue.Evaluate(.1f);
                    Check(cue.FigureVisible,"Turning back observes the distant figure");
                    player.ViewCamera.transform.Rotate(0,12,0);cue.Evaluate(.1f);Check(!cue.FigureVisible,"Camera movement removes figure after observation");
                }
                int count=cue.CueCount;cue.Evaluate(.1f);Check(cue.CueCount==count,"Stationary player cannot repeat cue");
            }
            player.transform.position=new Vector3(0,9.05f,1);cue.Evaluate(60);for(int i=0;i<25;i++){player.transform.position+=Vector3.forward*.25f;cue.Evaluate(.1f);}Check(cue.CueCount==3,"Maximum three cues, no recurring apparition");
            Directory.CreateDirectory("Documentation/Verification");File.WriteAllText("Documentation/Verification/CorridorRearCueSmokeTest.txt","PASS: three sparse rear spatial cues; second/third appearance selection (second tested); empty first and third cues; observation then camera-motion disappearance; no stationary repetition; maximum three cues. Actual Unity Play Mode component execution. Audio mix and subjective visual timing require human headphones playthrough.\n");
        }
        finally{UnityEngine.Object.Destroy(copy);player.transform.position=position;player.transform.rotation=rotation;player.ViewCamera.transform.localRotation=view;cc.enabled=collision;original.enabled=enabled;Physics.SyncTransforms();}
    }
    private static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
