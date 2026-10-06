using System;
using System.IO;
using System.Linq;
using System.Text;
using Archive0317;
using UnityEditor;
using UnityEngine;

public static class ArchiveRoomSmokeTest
{
    [MenuItem("Archive 03:17/Run Play Mode Smoke Test")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Run this check in Play Mode.");
        var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();
        var hud=UnityEngine.Object.FindFirstObjectByType<ArchiveHUD>();
        var file=UnityEngine.Object.FindFirstObjectByType<CaseFile>();
        var controller=player.GetComponent<CharacterController>();
        Vector3 initial=player.transform.position;
        Quaternion rotation=player.transform.rotation;
        Quaternion cameraRotation=player.ViewCamera.transform.localRotation;
        var report=new StringBuilder("ArchiveRoom Play Mode smoke test\n");
        player.enabled=false;
        try
        {
            Teleport(controller,player,new Vector3(2,.05f,-3));
            for(int i=0;i<60;i++) player.Move(Vector2.zero,false,1f/60);
            Check(controller.isGrounded,"Gravity and floor grounding",report);
            Vector3 start=player.transform.position;
            for(int i=0;i<30;i++) player.Move(Vector2.up,false,1f/60);
            float walk=Vector3.Distance(new Vector3(start.x,0,start.z),new Vector3(player.transform.position.x,0,player.transform.position.z));
            Teleport(controller,player,start);
            for(int i=0;i<30;i++) player.Move(Vector2.up,true,1f/60);
            float run=Vector3.Distance(new Vector3(start.x,0,start.z),new Vector3(player.transform.position.x,0,player.transform.position.z));
            Check(walk>1.2f && walk<1.4f && run>walk*1.6f,"Walk / Shift speed: "+walk.ToString("F2")+"m / "+run.ToString("F2")+"m",report);
            Teleport(controller,player,new Vector3(3,.05f,0));
            for(int i=0;i<180;i++) player.Move(Vector2.right,true,1f/60);
            float eastBoundary=GameObject.Find("ArchiveRoom_Geometry/EastWall").transform.position.x;
            Check(player.transform.position.x<eastBoundary-.3f && player.transform.position.x>3.1f,"East wall collision",report);
            Teleport(controller,player,new Vector3(-1.8f,.05f,-2.5f));
            for(int i=0;i<90;i++) player.Move(Vector2.left,false,1f/60);
            Check(player.transform.position.x>-2.9f && player.transform.position.x<-2.3f,"Shelf collision and aisle clearance",report);
            Teleport(controller,player,new Vector3(0,.05f,-.8f));
            for(int i=0;i<120;i++) player.Move(Vector2.up,false,1f/60);
            Check(player.transform.position.z<.1f,"Desk collision",report);
            float yaw=player.transform.eulerAngles.y;
            player.ApplyLook(new Vector2(200,2000));
            Check(Mathf.Abs(Mathf.DeltaAngle(yaw,player.transform.eulerAngles.y))>10 && Mathf.Abs(Mathf.DeltaAngle(0,player.ViewCamera.transform.localEulerAngles.x))<=80.1f,"Mouse yaw and pitch clamp",report);
            Teleport(controller,player,new Vector3(.45f,.05f,-.55f)); player.transform.rotation=Quaternion.identity;
            player.ViewCamera.transform.LookAt(file.transform.position);
            Physics.SyncTransforms(); player.UpdateTarget();
            Check(player.CurrentTarget==file && hud.IsPromptVisible,"Raycast CASE 001 and E 조사 prompt",report);
            Check(player.TryInteract() && hud.IsCaseOpen && !hud.IsPromptVisible,"Case description opens and hides prompt",report);
            Check(hud.GetComponentsInChildren<UnityEngine.UI.Text>(true).Any(t=>t.name=="Description" && t.text==file.Description),"Case UI description content",report);
            Check(Cursor.lockState==CursorLockMode.None,"Case UI releases cursor",report);
            player.CloseCase(); Check(!hud.IsCaseOpen,"Case description closes",report);
            Teleport(controller,player,new Vector3(.45f,.05f,-3.3f));
            player.ViewCamera.transform.LookAt(file.transform.position); Physics.SyncTransforms(); player.UpdateTarget();
            Check(player.CurrentTarget==null && !hud.IsPromptVisible,"No prompt outside interaction distance",report);
            player.SetCapture(false); Check(Cursor.lockState==CursorLockMode.None && Cursor.visible,"Cursor unlock",report);
            // Cursor locking depends on Game View focus; record the requested state without asserting OS focus.
            player.SetCapture(true); report.AppendLine("Cursor lock requested: "+Cursor.lockState);
            Teleport(controller,player,new Vector3(0,.05f,-3.3f)); player.transform.rotation=Quaternion.identity; player.ViewCamera.transform.localRotation=Quaternion.identity;
            report.AppendLine("PASS: all component runtime checks. Physical keyboard/mouse and Game View focus require manual verification.");
        }
        catch(Exception ex) { report.AppendLine("FAIL: "+ex.Message); throw; }
        finally
        {
            player.ApplyLook(new Vector2(0,-888.8889f));
            Teleport(controller,player,initial); player.transform.rotation=rotation; player.ViewCamera.transform.localRotation=cameraRotation;
            hud.CloseCase(); player.enabled=true; player.SetCapture(false);
            Directory.CreateDirectory("Documentation/Verification"); File.WriteAllText("Documentation/Verification/PlayModeSmokeTest.txt",report.ToString()); Debug.Log(report.ToString());
        }
    }
    private static void Teleport(CharacterController controller,FirstPersonPlayer player,Vector3 position)
    { controller.enabled=false; player.transform.position=position; controller.enabled=true; Physics.SyncTransforms(); }
    private static void Check(bool condition,string name,StringBuilder report)
    { if(!condition) throw new InvalidOperationException(name); report.AppendLine("PASS: "+name); }
}
