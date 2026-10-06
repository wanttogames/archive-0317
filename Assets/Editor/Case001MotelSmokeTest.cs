using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using Archive0317;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Case001MotelSmokeTest
{
    private static IEnumerator sequence;
    private static StringBuilder report;
    private static CaseDefinition definition;
    private static bool hadSave;
    private static string saved;
    private static double deadline;
    private static bool previousBackground;
    public static string Status { get; private set; } = "Not run";
    [MenuItem("Archive 03:17/Run CASE 001 Play Mode Smoke Test")]
    public static void Begin()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "ArchiveRoom") throw new InvalidOperationException("Start in ArchiveRoom Play Mode.");
        if (sequence != null) throw new InvalidOperationException("Test already running.");
        definition = AssetDatabase.LoadAssetAtPath<CaseDefinition>(Case001MotelBuilder.DefinitionPath);
        var key = CaseProgressStore.StorageKey(definition); hadSave = PlayerPrefs.HasKey(key); saved = PlayerPrefs.GetString(key);
        PlayerPrefs.DeleteKey(key); CaseProgressStore.ClearCache();
        previousBackground=Application.runInBackground; Application.runInBackground=true;
        report = new StringBuilder("CASE 001 Play Mode smoke test\n"); Status = "Running"; deadline = EditorApplication.timeSinceStartup + 90;
        sequence = Run(); EditorApplication.update += Tick;
    }
    private static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > deadline) throw new InvalidOperationException("Play Mode stopped or test timed out.");
            if (!sequence.MoveNext()) Finish(true, "");
        }
        catch (Exception ex) { Finish(false, ex.Message); }
    }
    private static void Finish(bool success, string failure)
    {
        EditorApplication.update -= Tick; sequence = null;
        Status = success ? "PASS" : "FAIL: " + failure; report.AppendLine(Status);
        var key = CaseProgressStore.StorageKey(definition);
        if (hadSave) PlayerPrefs.SetString(key, saved); else PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save(); CaseProgressStore.ClearCache();
        Application.runInBackground=previousBackground;
        var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();
        if (player != null) { player.HUD.CloseCase(); player.enabled = true; player.SetCapture(false); }
        Directory.CreateDirectory("Documentation/Verification"); File.WriteAllText("Documentation/Verification/Case001SmokeTest.txt", report.ToString());
        if (success) Debug.Log(report.ToString()); else Debug.LogError(report.ToString());
    }
    private static IEnumerator Run()
    {
        ArchiveRoomSmokeTest.Run(); Check(true, "Existing ArchiveRoom runtime smoke test");
        var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>(); player.enabled = false;
        var file = UnityEngine.Object.FindFirstObjectByType<CaseFile>();
        Target(player, new Vector3(.45f,.05f,-.55f), file.transform.position);
        Check(player.TryInteract() && player.HUD.IsCaseOpen, "Official CASE file Raycast opens UI");
        Check(Body(player).Contains("투숙 객실: 404호"), "Official record says 404");
        var start = Button(player,"StartField"); Check(start.gameObject.activeInHierarchy, "Start field button visible"); start.onClick.Invoke();
        Check(SceneTransitionManager.IsTransitioning, "Fade transition starts through UI listener");
        while (SceneTransitionManager.IsTransitioning) yield return null;
        Check(SceneManager.GetActiveScene().name == "Case001_Motel", "ArchiveRoom to motel scene load and fade complete");
        player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>(); player.enabled = false;
        Check(UnityEngine.Object.FindObjectsByType<FirstPersonPlayer>(FindObjectsSortMode.None).Length==1 && UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1 && UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1, "One shared player, AudioListener and EventSystem");
        Check(CaseProgressStore.Get(definition).Has("CaseStarted"), "CaseStarted stored");
        Teleport(player,new Vector3(0,.05f,-3.7f)); player.transform.rotation=Quaternion.identity;
        Steps(player,Vector2.zero,60); Check(player.GetComponent<CharacterController>().isGrounded,"Motel floor grounding");
        var initial=player.transform.position; Steps(player,Vector2.up,30); float walk=player.transform.position.z-initial.z;
        Teleport(player,initial); Steps(player,Vector2.up,30,true); float sprint=player.transform.position.z-initial.z;
        Check(walk>1.2f && sprint>walk*1.6f,"WASD movement / Shift speed");
        Teleport(player,new Vector3(2.5f,.05f,-2)); Steps(player,Vector2.right,90,true); Check(player.transform.position.x<3.15f,"Lobby wall collision");
        player.ApplyLook(new Vector2(200,2000)); Check(Mathf.Abs(Mathf.DeltaAngle(0,player.ViewCamera.transform.localEulerAngles.x))<=80.1f,"Mouse look pitch clamp");
        Target(player,new Vector3(-1.6f,.05f,-2),GameObject.Find("GuestLedger").transform.position);
        Check(player.CurrentTarget is InspectableDocument && player.HUD.IsPromptVisible,"Ledger Raycast / E prompt");
        Check(player.TryInteract() && !Body(player).Contains("객실: 403"),"Ledger first page does not reveal room number");
        Check(!CaseProgressStore.Get(definition).Has("LedgerInspected"),"Evidence requires detail page");
        Button(player,"NextPage").onClick.Invoke(); Check(Body(player).Contains("객실: 403") && CaseProgressStore.Get(definition).Fact("LedgerRoom")=="403","Ledger detail UI records 403"); player.CloseCase();
        Check(!CaseProgressStore.CompareRooms(definition),"No premature mismatch completion");
        Target(player,new Vector3(-4.55f,.05f,1.45f),new Vector3(-4.8f,1.4f,2.75f));
        Check(player.TryInteract() && Body(player).Contains("03:17"),"CCTV investigation and one timestamp hint"); player.CloseCase();
        Check(CaseProgressStore.Get(definition).Has("CCTVInspected"),"CCTVInspected stored");
        int hints=0; foreach(var document in UnityEngine.Object.FindObjectsByType<InspectableDocument>(FindObjectsSortMode.None)) for(int i=0;i<document.PageCount;i++) hints+=document.Page(i).Split(new[]{"03:17"},StringSplitOptions.None).Length-1;
        Check(hints==1,"03:17 appears once in motel evidence");
        Target(player,new Vector3(0,9.05f,1.2f),new Vector3(-1.13f,10.05f,1.2f));
        var door=GameObject.Find("RoomDoor404").GetComponent<InspectableDoor>(); Check(player.TryInteract() && !door.IsOpen,"404 remains locked without key");
        Target(player,new Vector3(-2,.05f,1.1f),GameObject.Find("RoomKeyCabinet").transform.position); Check(player.TryInteract() && CaseProgressStore.Get(definition).Has("Room404KeyTaken"),"Front key cabinet investigation");
        Teleport(player,new Vector3(-.88f,.05f,10)); player.transform.rotation=Quaternion.identity;
        for(int level=0;level<3;level++)
        {
            Steps(player,Vector2.up,85); Check(Mathf.Abs(player.transform.position.y-(level*3+1.5f))<.15f,"Stair flight "+(level*2+1)+" ascent: "+player.transform.position);
            Steps(player,Vector2.right,41); Steps(player,Vector2.down,85);
            Check(Mathf.Abs(player.transform.position.y-(level+1)*3)<.15f,"Stair flight "+(level*2+2)+" ascent: "+player.transform.position);
            float settled=Time.fixedTime+.08f;
            while(Time.fixedTime<settled)yield return null;
            if(level<2)Steps(player,Vector2.left,41);
        }
        Check(CaseProgressStore.Get(definition).Has("DistantLatchPlayed"),"Single quiet landing cue trigger");
        // Walk from the final landing into the fourth-floor corridor.
        Steps(player,Vector2.left,20); Steps(player,Vector2.down,170); Check(player.transform.position.z<3,"Fourth-floor corridor reachable from stairs");
        Target(player,new Vector3(0,9.05f,1.2f),new Vector3(-1.13f,10.05f,1.2f)); Check(player.TryInteract(),"404 door Raycast with key");
        while(!door.IsOpen)yield return null;
        player.transform.rotation=Quaternion.identity; Steps(player,Vector2.left,42);
        float entered=Time.fixedTime+.08f; while(Time.fixedTime<entered)yield return null;
        Check(CaseProgressStore.Get(definition).Has("Room404Entered"),"404 entry trigger through opened doorway");
        Target(player,new Vector3(-1.8f,9.05f,.8f),GameObject.Find("Room404Paper").transform.position); Check(player.TryInteract() && player.HUD.IsCaseOpen,"404 paper Raycast / document UI");
        Check(CaseProgressStore.Get(definition).Has("Room404PaperInspected"),"404 paper evidence stored"); player.CloseCase();
        Check(!CaseProgressStore.Get(definition).Has("RoomNumberMismatchFound"),"Player chooses to compare evidence");
        player.HUD.ShowNotebook(); Check(Body(player).Contains("404호") && Body(player).Contains("403호"),"Notebook shows official and field records");
        Check(Button(player,"CompareRecords").gameObject.activeInHierarchy,"Compare button requires investigation"); Button(player,"CompareRecords").onClick.Invoke();
        Check(CaseProgressStore.Get(definition).Has("RoomNumberMismatchFound") && player.HUD.GetComponentsInChildren<Text>(true).Any(t=>t.name=="Observation" && t.text=="기록이 일치하지 않는다." && t.gameObject.activeSelf),"Slice endpoint short notification");
        CaseProgressStore.ClearCache(); Check(CaseProgressStore.Get(definition).Has("RoomNumberMismatchFound"),"Per-case JSON progress survives cache reload");
        player.CloseCase(); player.transform.rotation=Quaternion.identity; initial=player.transform.position; Steps(player,Vector2.up,10); Check(Vector3.Distance(initial,player.transform.position)>.2f,"Exploration continues after endpoint");
        player.SetCapture(false); Check(Cursor.visible && Cursor.lockState==CursorLockMode.None,"Cursor unlock after documents");
        report.AppendLine("Physical keyboard/mouse, monitor brightness, sound balance and 5–10 minute pacing require human playthrough. Input bindings verified by component runtime path.");
    }
    private static void Check(bool condition,string label){if(!condition)throw new InvalidOperationException(label);report.AppendLine("PASS: "+label);}
    private static void Teleport(FirstPersonPlayer player,Vector3 position){var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=position;cc.enabled=true;Physics.SyncTransforms();}
    private static void Steps(FirstPersonPlayer player,Vector2 input,int count,bool run=false){for(int i=0;i<count;i++)player.Move(input,run,1f/60);}
    private static void Target(FirstPersonPlayer player,Vector3 position,Vector3 look){player.HUD.CloseCase();Teleport(player,position);player.transform.rotation=Quaternion.identity;player.ViewCamera.transform.LookAt(look);Physics.SyncTransforms();player.UpdateTarget();}
    private static string Body(FirstPersonPlayer player)=>player.HUD.GetComponentsInChildren<Text>(true).First(t=>t.name=="Description").text;
    private static Button Button(FirstPersonPlayer player,string name)=>player.HUD.GetComponentsInChildren<Button>(true).First(t=>t.name==name);
}
