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
        var environment=UnityEngine.Object.FindFirstObjectByType<CaseEnvironmentState>();
        var anomaly=GameObject.Find("Room403Anomaly");
        var door403=anomaly.GetComponentsInChildren<Transform>(true).First(t=>t.name=="RoomDoor403").gameObject;
        var missingWall=GameObject.Find("MissingRoom403Wall");
        Check(!door403.activeSelf && missingWall.activeSelf,"403 initially absent and its wall solid");
        var initialNumbers=UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Where(t=>t.name=="RoomNumber").Select(t=>t.text).OrderBy(t=>t).ToArray();
        Check(initialNumbers.SequenceEqual(new[]{"401","402","404","405"}),"Initial actual corridor numbers: 401 / 402 / 404 / 405");
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
        Check(player.TryInteract() && Body(player).Contains("입구 카메라"),"Initial CCTV entry recording remains available"); player.CloseCase();
        Check(CaseProgressStore.Get(definition).Has("CCTVInspected"),"CCTVInspected stored");
        Check(!CaseProgressStore.Get(definition).Has("CCTVContradictionFound"),"CCTV contradiction not unlocked prematurely");
        int hints=0; foreach(var document in UnityEngine.Object.FindObjectsByType<InspectableDocument>(FindObjectsSortMode.None)) for(int i=0;i<document.PageCount;i++) hints+=document.Page(i).Split(new[]{"03:17"},StringSplitOptions.None).Length-1;
        var cctv=UnityEngine.Object.FindFirstObjectByType<InspectableCCTV>();
        Check(hints==0 && cctv.Timestamp.Split(new[]{"03:17"},StringSplitOptions.None).Length==2,"Timestamp occurs once on CCTV display, no repeated document timestamp");
        var door=GameObject.Find("RoomDoor404").GetComponent<InspectableDoor>();
        float roomZ=door.transform.position.z;
        Target(player,new Vector3(0,9.05f,roomZ),door.transform.position+Vector3.up*1.05f);
        Check(player.TryInteract() && !door.IsOpen,"404 remains locked without key");
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
        foreach(string number in new[]{"402","404"})
        {
            var plate=GameObject.Find("RoomDoor"+number).transform.Find("DoorHinge/NumberPlate");
            Target(player,new Vector3(0,9.05f,plate.position.z),plate.position);
            Check(player.TryInteract() && CaseProgressStore.Get(definition).Has("Room"+number+"PlateSeen"),number+" numberplate Raycast observation");
        }
        environment.Evaluate();Check(CaseProgressStore.Get(definition).Has("MissingRoomNoticed"),"Both plates and ledger permit only a short observation");
        Target(player,new Vector3(0,9.05f,roomZ),door.transform.position+Vector3.up*1.05f); Check(player.TryInteract(),"404 door Raycast with key");
        while(!door.IsOpen)yield return null;
        player.transform.rotation=Quaternion.identity; Steps(player,Vector2.left,42);
        float entered=Time.fixedTime+.08f; while(Time.fixedTime<entered)yield return null;
        Check(CaseProgressStore.Get(definition).Has("Room404Entered"),"404 entry trigger through opened doorway");
        Target(player,new Vector3(-1.8f,9.05f,roomZ-.4f),GameObject.Find("Room404Paper").transform.position); Check(player.TryInteract() && player.HUD.IsCaseOpen,"404 paper Raycast / document UI");
        Check(CaseProgressStore.Get(definition).Has("Room404PaperInspected"),"404 paper evidence stored"); player.CloseCase();
        Check(!CaseProgressStore.Get(definition).Has("RoomNumberMismatchFound"),"Player chooses to compare evidence");
        player.HUD.ShowNotebook(); Check(Body(player).Contains("404호") && Body(player).Contains("403호"),"Notebook shows official and field records");
        Check(Button(player,"CompareRecords").gameObject.activeInHierarchy,"Compare button requires investigation"); Button(player,"CompareRecords").onClick.Invoke();
        Check(CaseProgressStore.Get(definition).Has("RoomNumberMismatchFound") && player.HUD.GetComponentsInChildren<Text>(true).Any(t=>t.name=="Observation" && t.text=="기록이 일치하지 않는다." && t.gameObject.activeSelf),"Slice endpoint short notification");
        CaseProgressStore.ClearCache(); Check(CaseProgressStore.Get(definition).Has("RoomNumberMismatchFound"),"Per-case JSON progress survives cache reload");
        player.CloseCase();environment.Evaluate();Check(!CaseProgressStore.Get(definition).Has("CorridorAltered"),"Corridor stays unchanged while player is inside 404");
        // Exercise the alternate route where the player notices the plates but skips the notebook button.
        CaseProgressStore.Get(definition).flags.Remove("RoomNumberMismatchFound");
        Target(player,new Vector3(-1.85f,9.05f,roomZ),new Vector3(-4,10.5f,roomZ));Steps(player,Vector2.right,44);
        environment.Evaluate();Check(CaseProgressStore.Get(definition).Has("CorridorAltered"),"Exiting investigated 404 changes corridor out of sight");
        Check(CaseProgressStore.Get(definition).Has("RoomNumberMismatchFound"),"Observed spatial mismatch progresses even without formal notebook comparison");
        Check(!UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.name=="FourthFloorLight" && l.transform.position.z>7).enabled,"One fourth-floor fluorescent now off");
        Check(!door403.activeSelf,"Corridor alteration does not prematurely reveal 403");
        Target(player,new Vector3(-4.55f,.05f,1.45f),new Vector3(-4.8f,1.4f,2.75f));
        Check(player.TryInteract() && player.HUD.IsCaseOpen && cctv.IsReady,"Repeat CCTV inspection opens recorded corridor view");
        var frame=player.HUD.GetComponentsInChildren<RawImage>(true).First(t=>t.name=="CCTVFrame");
        var stamp=player.HUD.GetComponentsInChildren<Text>(true).First(t=>t.name=="CCTVTimestamp");
        Check(frame.gameObject.activeInHierarchy && frame.texture==cctv.Frame && stamp.gameObject.activeInHierarchy && stamp.text.Contains("03:17"),"RenderTexture and single CCTV timestamp in existing document UI");
        Check(cctv.RecordingCamera.cullingMask==(1<<30) && GameObject.Find("RecordedNumber403").GetComponent<TextMesh>().text=="403","Recorded 403 exists only in isolated CCTV layer");
        Check(CaseProgressStore.Get(definition).Has("CCTVContradictionFound"),"CCTVContradictionFound stored after footage inspection");
        var texture=(RenderTexture)cctv.Frame;var previous=RenderTexture.active;RenderTexture.active=texture;
        var pixels=new Texture2D(texture.width,texture.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);pixels.Apply();RenderTexture.active=previous;
        var colours=pixels.GetPixels();Check(colours.Max(c=>c.r)-colours.Min(c=>c.r)>.15f && colours.All(c=>Mathf.Abs(c.r-c.g)<.02f),"CCTV image is nonblank and monochrome");UnityEngine.Object.DestroyImmediate(pixels);
        player.CloseCase();environment.Evaluate();Check(!CaseProgressStore.Get(definition).Has("PhoneLeadPlayed"),"Telephone lead waits until returning upstairs");
        Target(player,new Vector3(0,9.05f,8.7f),missingWall.GetComponent<Renderer>().bounds.center);environment.Evaluate();
        Check(CaseProgressStore.Get(definition).Has("PhoneLeadPlayed") && GameObject.Find("Missing403Telephone").GetComponent<AudioSource>().isPlaying,"Spatial telephone lead plays on corridor return");
        float ringing=Time.time+1.0f;while(Time.time<ringing)yield return null;
        Check(!door403.activeSelf,"403 cannot appear while its wall is being watched");
        player.ViewCamera.transform.LookAt(new Vector3(0,10.6f,12));environment.Evaluate();
        Check(door403.activeSelf && !missingWall.activeSelf && CaseProgressStore.Get(definition).Has("Room403Revealed"),"403 quietly replaces wall only after looking away");
        Target(player,new Vector3(0,9.05f,4.5f),new Vector3(-1.13f,10.05f,4.5f));Check(player.CurrentTarget is InspectableSoundNote && player.HUD.IsPromptVisible,"Revealed 403 uses shared E investigation prompt");
        Check(player.TryInteract() && CaseProgressStore.Get(definition).Has("Room403DoorInspected"),"Locked 403 investigation ends mid-case progression");
        Check(player.HUD.GetComponentsInChildren<Text>(true).Any(t=>t.name=="Observation" && t.text=="문이 잠겨 있다."),"403 response remains minimal");
        float heard=Time.time+1.7f;while(Time.time<heard)yield return null;
        Check(door403.GetComponent<InspectableSoundNote>().CuePlayed,"Delayed faint receiver sound after door inspection");
        player.transform.rotation=Quaternion.identity;Steps(player,Vector2.left,120);Check(player.transform.position.x>-.9f,"403 leaf/backing collision prevents interior access");
        CaseProgressStore.ClearCache();Check(CaseProgressStore.Get(definition).Has("Room403Revealed"),"Anomaly flags persist across progress-cache reload");
        player.CloseCase(); player.transform.rotation=Quaternion.identity; initial=player.transform.position; Steps(player,Vector2.up,10); Check(Vector3.Distance(initial,player.transform.position)>.2f,"Exploration continues after endpoint");
        player.SetCapture(false); Check(Cursor.visible && Cursor.lockState==CursorLockMode.None,"Cursor unlock after documents");
        var reload=SceneManager.LoadSceneAsync("Case001_Motel");while(!reload.isDone)yield return null;yield return null;
        Check(GameObject.Find("RoomDoor403")!=null && GameObject.Find("MissingRoom403Wall")==null,"Saved spatial state restored on scene reload");
        Check(!GameObject.Find("Missing403Telephone").GetComponent<AudioSource>().isPlaying,"One-shot lead does not replay when saved scene is reloaded");
        report.AppendLine("Physical keyboard/mouse, pointer clicks, monitor brightness, sound balance and investigation pacing require human playthrough. Automated checks invoke component runtime paths and real UI listeners.");
    }
    private static void Check(bool condition,string label){if(!condition)throw new InvalidOperationException(label);report.AppendLine("PASS: "+label);}
    private static void Teleport(FirstPersonPlayer player,Vector3 position){var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=position;cc.enabled=true;Physics.SyncTransforms();}
    private static void Steps(FirstPersonPlayer player,Vector2 input,int count,bool run=false){for(int i=0;i<count;i++)player.Move(input,run,1f/60);}
    private static void Target(FirstPersonPlayer player,Vector3 position,Vector3 look){player.HUD.CloseCase();Teleport(player,position);player.transform.rotation=Quaternion.identity;player.ViewCamera.transform.LookAt(look);Physics.SyncTransforms();player.UpdateTarget();}
    private static string Body(FirstPersonPlayer player)=>player.HUD.GetComponentsInChildren<Text>(true).First(t=>t.name=="Description").text;
    private static Button Button(FirstPersonPlayer player,string name)=>player.HUD.GetComponentsInChildren<Button>(true).First(t=>t.name==name);
}
