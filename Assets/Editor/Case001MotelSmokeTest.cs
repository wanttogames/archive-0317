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
    private static string capturePrefix;
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
        capturePrefix="TVWatcher/Suite_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+"/";Directory.CreateDirectory("Documentation/Verification/"+capturePrefix);
        report = new StringBuilder("CASE 001 Play Mode smoke test\n"); Status = "Running"; deadline = EditorApplication.timeSinceStartup + 180;
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
        if(success && CaseProgressStore.Get(definition).Has("Case001Completed"))File.WriteAllText("Temp/Case001CompletedTestSave.json",PlayerPrefs.GetString(key));
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
        System.IO.File.WriteAllText("Documentation/Verification/RetroVisualSmokeTest.txt","PS1 / VHS visual smoke test\n");RetroVisualSmokeTest.ValidateCurrentScene();
        var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>(); player.enabled = false;
        var file = UnityEngine.Object.FindFirstObjectByType<CaseFile>();
        Target(player, new Vector3(.45f,.05f,-.55f), file.transform.position);
        Check(player.TryInteract() && player.HUD.IsCaseOpen, "Official CASE file Raycast opens UI");
        Check(Body(player).Contains("투숙 객실: 404호"), "Official record says 404");
        var archiveCapture=RetroVisualSmokeTest.Capture("Retro_ArchiveDocument");while(archiveCapture.MoveNext())yield return null;
        var start = Button(player,"StartField"); Check(start.gameObject.activeInHierarchy, "Start field button visible"); start.onClick.Invoke();
        Check(SceneTransitionManager.IsTransitioning, "Fade transition starts through UI listener");
        while (SceneTransitionManager.IsTransitioning) yield return null;
        Check(SceneManager.GetActiveScene().name == "Case001_Motel", "ArchiveRoom to motel scene load and fade complete");
        RetroVisualSmokeTest.ValidateCurrentScene();
        player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>(); player.enabled = false;
        Check(UnityEngine.Object.FindObjectsByType<FirstPersonPlayer>(FindObjectsSortMode.None).Length==1 && UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1 && UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1, "One shared player, AudioListener and EventSystem");
        Check(CaseProgressStore.Get(definition).Has("CaseStarted"), "CaseStarted stored");
        var environment=GameObject.Find("Room403Anomaly").GetComponent<CaseEnvironmentState>();
        var anomaly=GameObject.Find("Room403Anomaly");
        var door403=anomaly.GetComponentsInChildren<Transform>(true).First(t=>t.name=="RoomDoor403").gameObject;
        var missingWall=GameObject.Find("MissingRoom403Wall");
        Check(!door403.activeSelf && missingWall.activeSelf,"403 initially absent and its wall solid");
        var initialNumbers=UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Where(t=>t.name=="RoomNumber").Select(t=>t.text).OrderBy(t=>t).ToArray();
        Check(initialNumbers.SequenceEqual(new[]{"401","402","404","405"}),"Initial actual corridor numbers: 401 / 402 / 404 / 405");
        CorridorRearCueSmokeTest.Run();Check(true,"Sparse rear sound / distant figure / camera-motion disappearance regression");
        var rearPreview=CorridorRearCueSmokeTest.CapturePreview();while(rearPreview.MoveNext())yield return null;
        Teleport(player,new Vector3(0,.05f,-3.7f)); player.transform.rotation=Quaternion.identity;
        player.ViewCamera.transform.localRotation=Quaternion.identity;var motelCapture=RetroVisualSmokeTest.Capture("Retro_MotelGameView");while(motelCapture.MoveNext())yield return null;
        Steps(player,Vector2.zero,60); Check(player.GetComponent<CharacterController>().isGrounded,"Motel floor grounding");
        var initial=player.transform.position; Steps(player,Vector2.up,30); float walk=player.transform.position.z-initial.z;
        Teleport(player,initial); Steps(player,Vector2.up,30,true); float sprint=player.transform.position.z-initial.z;
        Check(walk>1.2f && sprint>walk*1.6f,"WASD movement / Shift speed");
        Teleport(player,new Vector3(2.5f,.05f,-2)); Steps(player,Vector2.right,90,true); Check(player.transform.position.x<3.15f,"Lobby wall collision");
        foreach(int floor in new[]{2,3})
        {
            float y=(floor-1)*3;
            foreach(float x in new[]{-1.4f,0,1.4f})foreach(float h in new[]{1.3f,1.7f,2.7f})
                Check(Physics.Raycast(new Vector3(x,y+h,9.9f),Vector3.back,out var wallHit,1f) && wallHit.collider.name=="StairwellFrontWall_"+floor,"Intermediate-floor front wall coverage "+floor+" / "+x+" / "+h);
            Teleport(player,new Vector3(.8f,y+.05f,9.98f));Steps(player,Vector2.down,90,true);
            Check(player.transform.position.z>=9.7f,"CharacterController cannot walk through intermediate stair front wall "+floor);
        }
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
        var spareKey=GameObject.Find("Spare404Key");Check(spareKey!=null && spareKey.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),"Physical 404 spare key visible at front cabinet");
        var spareView=CaptureKeyWorld(player,"Spare404",new Vector3(-1.4f,1.8f,1.95f),spareKey.transform.position);while(spareView.MoveNext())yield return null;
        var keyAudio=InteractionSoundscape.Ensure();int sparePlays=keyAudio.SpareKeyPlays;
        Target(player,new Vector3(-2,.05f,1.1f),spareKey.transform.position); Check(player.CurrentTarget==spareKey.GetComponent<InspectableNote>() && player.TryInteract() && CaseProgressStore.Get(definition).Has("Room404KeyTaken"),"Physical spare key Raycast investigation grants 404 key");
        Check(keyAudio.SpareKeyPlays==sparePlays+1 && !spareKey.GetComponent<InspectableNote>().PlayInspectSound,"First spare pickup plays dedicated key ring without generic tap");
        var spareSource=GameObject.Find("SpatialOneShot_SpareKeyRing").GetComponent<AudioSource>();Check(spareSource.isPlaying && spareSource.spatialBlend==1 && spareSource.volume<=.1f,"Spare key actual low-volume 3D AudioSource plays");
        spareKey.GetComponent<CaseEnvironmentState>().Evaluate();Check(!spareKey.activeSelf,"Acquired spare key disappears from cabinet");
        Target(player,new Vector3(-2,.05f,1.1f),GameObject.Find("RoomKeyCabinet").transform.position);Check(player.TryInteract() && keyAudio.SpareKeyPlays==sparePlays+1,"Cabinet reread cannot repeat spare acquisition audio");
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
        var progressAfterOpen=CaseProgressStore.Get(definition);
        progressAfterOpen.flags.Remove("Room404PlateSeen");progressAfterOpen.flags.Remove("MissingRoomNoticed");
        var openPlate=door.transform.Find("DoorHinge/NumberPlate");
        Target(player,new Vector3(0,9.05f,openPlate.position.z),openPlate.position);
        Check(player.CurrentTarget==openPlate.GetComponent<InspectableNote>() && player.TryInteract() && progressAfterOpen.Has("Room404PlateSeen"),"404 numberplate remains Raycast inspectable after opening the door first");
        environment.Evaluate();Check(progressAfterOpen.Has("MissingRoomNoticed"),"Corridor-number objective can complete after 404 door opens");
        Target(player,new Vector3(0,9.05f,roomZ),door.transform.position+Vector3.up*1.05f);
        player.transform.rotation=Quaternion.identity; Steps(player,Vector2.left,42);
        float entered=Time.fixedTime+.08f; while(Time.fixedTime<entered)yield return null;
        Check(CaseProgressStore.Get(definition).Has("Room404Entered"),"404 entry trigger through opened doorway");
        var bed404Capture=CaptureBed(player,"Bed404Final","BedCover",new Vector3(-2.15f,10.5f,2.5f));while(bed404Capture.MoveNext())yield return null;
        Target(player,new Vector3(-1.8f,9.05f,roomZ-.4f),GameObject.Find("Room404Paper").transform.position); Check(player.TryInteract() && player.HUD.IsCaseOpen,"404 paper Raycast / document UI");
        Check(CaseProgressStore.Get(definition).Has("Room404PaperInspected"),"404 paper evidence stored"); player.CloseCase();
        Check(!CaseProgressStore.Get(definition).Has("RoomNumberMismatchFound"),"Player chooses to compare evidence");
        player.HUD.ShowNotebook(); Check(Body(player).Contains("404호") && Body(player).Contains("403호"),"Notebook shows official and field records");
        player.HUD.SelectNotebookTab(1);
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
        var cctvCapture=RetroVisualSmokeTest.Capture(capturePrefix+"Retro_CCTVGameView");while(cctvCapture.MoveNext())yield return null;
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
        Target(player,new Vector3(0,9.05f,4.5f),new Vector3(-1.13f,10.05f,4.5f));Check(player.CurrentTarget is InspectableCaseDoor && player.HUD.IsPromptVisible,"Revealed 403 uses shared E investigation prompt");
        Check(player.TryInteract() && CaseProgressStore.Get(definition).Has("Room403DoorInspected"),"Locked 403 investigation ends mid-case progression");
        Check(player.HUD.GetComponentsInChildren<Text>(true).Any(t=>t.name=="Observation" && t.text=="문이 잠겨 있다."),"403 response remains minimal");
        float heard=Time.time+1.7f;while(Time.time<heard)yield return null;
        Check(door403.GetComponent<InspectableCaseDoor>().CuePlayed,"Delayed faint receiver sound after door inspection");
        player.transform.rotation=Quaternion.identity;Steps(player,Vector2.left,120);Check(player.transform.position.x>-.9f,"403 leaf/backing collision prevents interior access");
        CaseProgressStore.ClearCache();Check(CaseProgressStore.Get(definition).Has("Room403Revealed"),"Anomaly flags persist across progress-cache reload");
        player.CloseCase(); player.transform.rotation=Quaternion.identity; initial=player.transform.position; Steps(player,Vector2.up,10); Check(Vector3.Distance(initial,player.transform.position)>.2f,"Exploration continues after endpoint");
        player.SetCapture(false); Check(Cursor.visible && Cursor.lockState==CursorLockMode.None,"Cursor unlock after documents");
        var reload=SceneManager.LoadSceneAsync("Case001_Motel");while(!reload.isDone)yield return null;yield return null;
        Check(GameObject.Find("RoomDoor403")!=null && GameObject.Find("MissingRoom403Wall")==null,"Saved spatial state restored on scene reload");
        Check(!GameObject.Find("Missing403Telephone").GetComponent<AudioSource>().isPlaying,"One-shot lead does not replay when saved scene is reloaded");
        var interior=RunInterior();while(interior.MoveNext())yield return null;
        var finale=RunFinale();while(finale.MoveNext())yield return null;
        report.AppendLine("Physical keyboard/mouse, pointer clicks, monitor brightness, sound balance and investigation pacing require human playthrough. Automated checks invoke component runtime paths and real UI listeners.");
    }
    private static IEnumerator RunInterior()
    {
        var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();player.enabled=false;
        var progress=CaseProgressStore.Get(definition);var entrance=GameObject.Find("RoomDoor403").GetComponent<InspectableCaseDoor>();
        Target(player,new Vector3(0,9.05f,4.5f),new Vector3(-1.13f,10.05f,4.5f));
        progress.flags.Remove("CCTVContradictionFound");player.TryInteract();Check(!entrance.IsOpen,"403 unlock rejects incomplete evidence conditions");CaseProgressStore.Mark(definition,"CCTVContradictionFound");
        Check(player.TryInteract(),"Second investigation begins quiet delayed opening");
        float stop=Time.time+.7f;while(Time.time<stop)yield return null;Check(!entrance.IsOpen,"Handle remains locked during initial one-second pause");
        stop=Time.time+3;while(Time.time<stop)yield return null;Check(entrance.IsOpen && progress.Has("Room403Opened"),"403 slowly opens after latch cue");
        player.transform.rotation=Quaternion.identity;Steps(player,Vector2.left,45);yield return null;
        Check(player.transform.position.x<-18 && progress.Has("Room403Entered"),"Walking across threshold enters isolated 403 without changing player/camera");
        var environment=GameObject.Find("Room403Interior").GetComponent<CaseEnvironmentState>();environment.Evaluate();
        Check(GameObject.Find("Room403ExitDoor").GetComponent<InspectableCaseDoor>().IsOpen,"Inner doorway starts open");
        var bed403Capture=CaptureBed(player,"Bed403Final","Room403BedCover",new Vector3(-19.4f,10.5f,3.5f));while(bed403Capture.MoveNext())yield return null;
        player.transform.rotation=Quaternion.identity;Steps(player,Vector2.right,20);stop=Time.time+.25f;while(Time.time<stop)yield return null;Check(player.transform.position.x>-.9f && !progress.Has("Room403Completed"),"Leaving before evidence safely returns to corridor without completing case");
        stop=Time.time+1.1f;while(Time.time<stop)yield return null;player.transform.rotation=Quaternion.identity;Steps(player,Vector2.left,30);yield return null;Check(player.transform.position.x<-18,"Unfinished 403 can be re-entered without a duplicate player");
        Target(player,new Vector3(-19.05f,9.05f,3.15f),GameObject.Find("Room403BathDoor").transform.position);Check(player.TryInteract(),"First bathroom door inspection opens normally");stop=Time.time+.6f;while(Time.time<stop)yield return null;player.transform.rotation=Quaternion.identity;Steps(player,Vector2.down,40);stop=Time.time+.25f;while(Time.time<stop)yield return null;
        Check(progress.Has("BathroomVisited") && !progress.Has("RoomAltered") && !progress.Has("BathroomReturned"),"First bathroom visit is quiet and unchanged");
        Target(player,new Vector3(-19.05f,9.05f,1.85f),GameObject.Find("BathroomWashbasin").transform.position);Check(player.TryInteract() && progress.Has("BathroomInspected"),"Bathroom basin is inspectable without a strong event");
        Teleport(player,new Vector3(-19.05f,9.05f,3.15f));stop=Time.time+.25f;while(Time.time<stop)yield return null;
        var phone=GameObject.Find("Room403Telephone").GetComponent<InspectableEchoPhone>();
        Target(player,new Vector3(-20.65f,9.05f,3.3f),phone.transform.position);Check(player.TryInteract() && progress.Has("PhoneInspected") && !phone.Ringing,"Initial phone inspection hears silence");
        Target(player,new Vector3(-20.5f,9.05f,3.7f),GameObject.Find("Room403GuestBag").transform.position);Check(player.TryInteract() && progress.Has("BagInspected"),"Guest bag Raycast and possessions inspection");
        Target(player,new Vector3(-19.5f,9.05f,5.15f),GameObject.Find("Room403Receipt").transform.position);Check(player.TryInteract() && player.HUD.IsCaseOpen && Body(player).Contains("객실: 404") && progress.Has("ReceiptInspected"),"Receipt initially records 404 in existing document UI");player.CloseCase();
        Target(player,new Vector3(-19.7f,9.05f,5.15f),GameObject.Find("Room403PersonalNote").transform.position);Check(player.TryInteract() && progress.Has("PersonalNoteInspected"),"Personal notebook is reachable via shared Raycast");player.CloseCase();
        yield return null;yield return null;stop=Time.time+.4f;while(Time.time<stop)yield return null;
        Check(phone.Ringing && progress.Has("PhoneEventTriggered"),"Three quiet telephone rings after core clues");
        Target(player,new Vector3(-20.65f,9.05f,3.3f),phone.transform.position);Check(player.TryInteract() && phone.Answering,"Player answers using E interaction");
        stop=Time.time+2.2f;while(Time.time<stop)yield return null;Check(phone.EchoPlayed && GameObject.Find("Room403HandsetAudio").GetComponent<AudioSource>().isPlaying,"Receiver reproduces configured room ambience after static");
        stop=Time.time+4.1f;while(Time.time<stop)yield return null;Check(!phone.Answering && progress.Has("PhoneEventAnswered"),"Six-second call ends automatically without voice");
        Target(player,new Vector3(-19.05f,9.05f,3.15f),new Vector3(-19.05f,10,1.55f));Check(GameObject.Find("BathHinge").GetComponent<InspectableDoor>().IsOpen,"Bathroom door remains open for revisit");
        player.transform.rotation=Quaternion.identity;Steps(player,Vector2.down,40);stop=Time.time+.25f;while(Time.time<stop)yield return null;environment.Evaluate();
        Check(progress.Has("BathroomReturned") && progress.Has("RoomAltered"),"Entering bathroom after phone changes chair and closes door out of view");
        Check(!GameObject.Find("Room403ExitDoor").GetComponent<InspectableCaseDoor>().IsOpen,"Exit closed while evidence remains unfound");
        Check(GameObject.Find("BathroomSingleDrip").GetComponent<AudioSource>().isPlaying,"Bathroom emits small drip on return");
        Teleport(player,new Vector3(-19.05f,9.05f,3.2f));player.ViewCamera.transform.LookAt(new Vector3(-22,10,3.2f));environment.Evaluate();yield return null;
        Check(progress.Has("TelevisionPowerOn"),"TV powers on outside bathroom only when out of view");
        stop=Time.time+2.4f;while(Time.time<stop)yield return null;var television=GameObject.Find("Room403CRTTV").GetComponent<InspectableCaseTelevision>();
        Check(television.FootageVisible && progress.Has("TelevisionEventTriggered") && GameObject.Find("Room403TVTimestamp").GetComponent<TextMesh>().text=="03:17","CRT static resolves into present room surveillance with 03:17");
        var rt=(RenderTexture)television.Frame;var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();RenderTexture.active=prior;var values=image.GetPixels();Check(values.Max(c=>c.r)-values.Min(c=>c.r)>.08f,"Room TV RenderTexture contains actual nonblank corridor imagery");UnityEngine.Object.DestroyImmediate(image);
        Target(player,new Vector3(-19.5f,9.05f,5.8f),television.transform.position);Check(player.TryInteract() && progress.Has("TelevisionInspected"),"TV inspection uses original interaction UI");environment.Evaluate();
        var watcherTest=Room403TVWatcherSmokeTest.Run(player,television,environment,definition);while(watcherTest.MoveNext())yield return null;Check(true,"TV-only watcher, no movement while seen, delayed proxy, save restore and key fallback regression");
        player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();player.enabled=false;environment=GameObject.Find("Room403Interior").GetComponent<CaseEnvironmentState>();progress=CaseProgressStore.Get(definition);
        ScreenCapture.CaptureScreenshot(Room403TVWatcherSmokeTest.CaptureFolder+"CRTGameView.png");stop=Time.time+.25f;while(Time.time<stop)yield return null;
        Check(GameObject.Find("Room403KeyEvidence")!=null,"Key tag becomes discoverable beside bed after TV");
        var keyEvidence=GameObject.Find("Room403KeyEvidence");Check(keyEvidence.transform.Find("EvidenceKeyVisual").gameObject.activeInHierarchy,"Physical key, ring and tag appear after TV");
        var keyWorld=CaptureKeyWorld(player,"EvidenceWorld",new Vector3(-19.7f,9.85f,3.6f),keyEvidence.transform.position);while(keyWorld.MoveNext())yield return null;
        var keyAudio=InteractionSoundscape.Ensure();int evidencePlays=keyAudio.EvidenceKeyPlays,rearPlays=keyAudio.RearKeyPlays;
        Target(player,new Vector3(-19.3f,9.05f,4.03f),keyEvidence.transform.position);Check(player.TryInteract() && Body(player).Contains("403") && !progress.Has("KeyEvidenceFound"),"Key tag front inspected without prematurely recording reverse evidence");Check(keyAudio.EvidenceKeyPlays==evidencePlays && keyAudio.RearKeyPlays==rearPlays,"Front 403 page does not trigger acquisition or rear cue");
        var keyFrame=player.HUD.GetComponentsInChildren<RawImage>(true).First(t=>t.name=="CCTVFrame");var keyDoc=keyEvidence.GetComponent<InspectableDocument>();Check(keyFrame.gameObject.activeInHierarchy && keyFrame.texture==keyDoc.Image(0),"403 front close-up image visible in document viewer");
        var keyFront=CaptureKeyUI("Front403");while(keyFront.MoveNext())yield return null;
        player.HUD.NextPage();Check(Body(player).Contains("404") && progress.Has("KeyEvidenceFound"),"Reverse side 404 sets KeyEvidenceFound");Check(keyFrame.texture==keyDoc.Image(1) && keyDoc.Image(0)!=keyDoc.Image(1),"404 reverse has a separate close-up image");
        Check(keyAudio.EvidenceKeyPlays==evidencePlays+1 && keyAudio.RearKeyPlays==rearPlays,"Evidence sound starts exactly when reverse-page flag is recorded; rear cue waits");
        var heavySource=GameObject.Find("SpatialOneShot_EvidenceKeyRing").GetComponent<AudioSource>();Check(heavySource.isPlaying && heavySource.spatialBlend==1 && heavySource.volume<=.13f,"Heavy key actual low-volume 3D AudioSource plays");
        Check(!keyAudio.GetComponents<AudioSource>().Any(s=>s.isPlaying),"Document UI source is quiet during key acquisition");
        var rearOrigin=player.ViewCamera.transform.position;var rearForward=Vector3.ProjectOnPlane(player.ViewCamera.transform.forward,Vector3.up).normalized;
        var keyBack=CaptureKeyUI("Back404");while(keyBack.MoveNext())yield return null;
        float rearWait=Time.unscaledTime+.25f;while(Time.unscaledTime<rearWait)yield return null;
        Check(keyAudio.RearKeyPlays==rearPlays+1 && keyAudio.LastRearDelay>=.3f && keyAudio.LastRearDelay<=.55f && Vector3.Dot(keyAudio.LastKeyRearPosition-rearOrigin,rearForward)<0,"Delayed small latch plays behind player after approximately 0.4 seconds");
        player.HUD.PreviousPage();player.HUD.NextPage();Check(keyAudio.EvidenceKeyPlays==evidencePlays+1,"Page cycling cannot replay evidence acquisition");
        player.CloseCase();environment.Evaluate();
        Target(player,new Vector3(-19.3f,9.05f,4.03f),keyEvidence.transform.position);Check(player.TryInteract(),"Recorded key can be reopened");player.HUD.NextPage();Check(keyAudio.EvidenceKeyPlays==evidencePlays+1 && keyAudio.RearKeyPlays==rearPlays+1,"Recorded key reread cannot replay heavy metal or rear cue");player.CloseCase();
        Target(player,new Vector3(-19.5f,9.05f,5.15f),GameObject.Find("Room403Receipt").transform.position);Check(player.TryInteract() && Body(player).Contains("객실: 403") && progress.Has("ReceiptChangedSeen") && progress.Fact("ReceiptRoom")=="403","Reinspection silently changes receipt to 403");player.CloseCase();
        var inner=GameObject.Find("Room403ExitDoor").GetComponent<InspectableCaseDoor>();Target(player,new Vector3(-18.8f,9.05f,4.5f),new Vector3(-18,10.05f,4.5f));Check(player.TryInteract(),"Evidence permits reopening the exit");player.ViewCamera.transform.LookAt(new Vector3(-22,10,4.5f));stop=Time.time+3.5f;while(Time.time<stop)yield return null;Check(inner.IsOpen,"Exit slowly reopens after evidence");
        Target(player,new Vector3(-18.7f,9.05f,4.5f),GameObject.Find("ExitPreviewWallpaper").GetComponent<Renderer>().bounds.center);environment.Evaluate();
        Check(progress.Has("ArchiveThresholdGlimpse") && GameObject.Find("ArchiveShelfGlimpse")!=null,"One quiet archive shelf glimpse through doorway");stop=Time.time+.95f;while(Time.time<stop)yield return null;Check(GameObject.Find("ArchiveShelfGlimpse")==null,"Archive glimpse restores motel within one second");
        player.transform.rotation=Quaternion.identity;Steps(player,Vector2.right,28);yield return null;
        Check(player.transform.position.x>-.9f && progress.Has("Room403Completed"),"Walking out returns to real corridor and completes interior segment");environment.Evaluate();
        Check(GameObject.Find("RoomDoor403")==null && GameObject.Find("MissingRoom403Wall")!=null,"403 vanishes after safe corridor exit");
        Check(GameObject.Find("Room403Interior")==null,"Interior and its audiovisual sources disabled after exit");
        player.transform.rotation=Quaternion.identity;Steps(player,Vector2.left,60);Check(player.transform.position.x>-.9f,"Restored wall prevents re-entry without trapping player");
        CaseProgressStore.ClearCache();Check(CaseProgressStore.Get(definition).Has("Room403Completed"),"Interior endpoint survives cache reload");
        var reload=SceneManager.LoadSceneAsync("Case001_Motel");while(!reload.isDone)yield return null;yield return null;
        Check(GameObject.Find("RoomDoor403")==null && GameObject.Find("MissingRoom403Wall")!=null && GameObject.Find("Room403Interior")==null,"Completed scene reload retains original 401/402/404/405 layout without replaying room");
    }
    private static void Check(bool condition,string label){if(!condition)throw new InvalidOperationException(label);report.AppendLine("PASS: "+label);}
    private static IEnumerator RunFinale()
    {
        var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();player.enabled=false;var progress=CaseProgressStore.Get(definition);
        var environment=GameObject.Find("MotelClosure").GetComponent<CaseEnvironmentState>();environment.Evaluate();
        Check(GameObject.Find("Final404KeyHolder")!=null && GameObject.Find("Plate404Scratch")!=null,"Final 404 keyholder and subtle plate scratch appear after 403 completion");
        var door=GameObject.Find("RoomDoor404").GetComponent<InspectableDoor>();Target(player,new Vector3(0,9.05f,3.4f),new Vector3(-1.13f,10.05f,3.4f));Check(player.TryInteract(),"404 can be reopened for final investigation");float until=Time.time+.5f;while(Time.time<until)yield return null;
        Target(player,new Vector3(-2,9.05f,4.1f),GameObject.Find("Final404KeyHolder").transform.position);Check(player.TryInteract() && player.HUD.IsCaseOpen && Body(player).Contains("403") && progress.Has("FinalKeyInspected"),"Supporting 403 key in 404 reachable with original document UI");player.CloseCase();
        var ledger=GameObject.Find("GuestLedger").GetComponent<InspectableDocument>();Target(player,new Vector3(-1.6f,.05f,-1.85f),ledger.transform.position);Check(player.TryInteract(),"Final ledger investigation through shared Raycast");player.HUD.NextPage();Check(Body(player).Contains("객실: 404") && progress.Fact("LedgerRoom")=="404" && progress.Has("FinalLedgerInspected"),"Final ledger silently rewrites 403 to 404");player.CloseCase();
        var cctv=GameObject.Find("CRTMonitor").GetComponent<InspectableCCTV>();Target(player,new Vector3(-4.55f,.05f,1.45f),new Vector3(-4.8f,1.4f,2.75f));Check(player.TryInteract() && cctv.Unavailable && Body(player).Contains("DATA ERROR") && !player.HUD.GetComponentsInChildren<RawImage>(true).First(t=>t.name=="CCTVFrame").gameObject.activeSelf,"Old CCTV footage becomes inaccessible rather than replaying");player.CloseCase();
        var exit=GameObject.Find("EntranceGlassDoor").GetComponent<InspectableCaseExit>();progress.flags.Remove("KeyEvidenceFound");Check(!exit.Ready && !exit.ReturnToArchive(),"Exit rejects missing key evidence");CaseProgressStore.Mark(definition,"KeyEvidenceFound");
        Target(player,new Vector3(0,.05f,-3.65f),exit.transform.position);Check(player.TryInteract() && player.HUD.IsCaseOpen && Button(player,"ReturnToArchive").gameObject.activeInHierarchy && Button(player,"ContinueInvestigation").gameObject.activeInHierarchy,"Exit offers return or continue in existing document card");
        Button(player,"ContinueInvestigation").onClick.Invoke();Check(!player.HUD.IsCaseOpen && SceneManager.GetActiveScene().name=="Case001_Motel","Continue leaves player free in motel");
        player.TryInteract();Button(player,"ReturnToArchive").onClick.Invoke();Check(SceneTransitionManager.IsTransitioning,"Return uses shared fade transition");while(SceneTransitionManager.IsTransitioning || SceneManager.GetActiveScene().name!="ArchiveRoom")yield return null;yield return null;
        player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();player.enabled=false;progress=CaseProgressStore.Get(definition);
        Check(progress.Has("ReturnedToArchive") && GameObject.Find("FieldInvestigationMemo")!=null,"Archive return reflects completed field investigation");
        Check(UnityEngine.Object.FindObjectsByType<FirstPersonPlayer>(FindObjectsSortMode.None).Length==1 && UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1,"Return has one player and one AudioListener");
        var file=UnityEngine.Object.FindFirstObjectByType<CaseFile>();var resolution=file.GetComponent<CaseReport>();Target(player,new Vector3(.45f,.05f,-.55f),file.transform.position);Check(player.TryInteract() && Body(player).Contains("수집 증거") && Body(player).Contains("양면 키") && !Button(player,"StartField").gameObject.activeInHierarchy,"Returned case file opens report with actual evidence and no field replay");
        var evidence=EvidenceCollection.Collect(definition);Check(evidence.Count==7 && progress.evidenceIds.Count==7,"Seven genuinely found key evidence IDs are collected and saved");
        var reportBody=player.HUD.GetComponentsInChildren<Text>(true).First(t=>t.name=="Description");Canvas.ForceUpdateCanvases();Check(reportBody.preferredHeight<=reportBody.rectTransform.rect.height,"Every discovered evidence line fits the editable report without clipping");
        // A report for a case with only one observed flag must not list other evidence.
        var partial=ScriptableObject.CreateInstance<CaseDefinition>();partial.Configure("smoke_evidence_partial","Test","","","",new string[0]);partial.ConfigureEvidence(definition.Evidence);var partialKey=CaseProgressStore.StorageKey(partial);bool hadPartial=PlayerPrefs.HasKey(partialKey);string oldPartial=PlayerPrefs.GetString(partialKey);
        try{PlayerPrefs.DeleteKey(partialKey);CaseProgressStore.ClearCache();CaseProgressStore.Mark(partial,"LedgerInspected");Check(EvidenceCollection.Collect(partial).Count==1,"Evidence filtering excludes undiscovered objects");}finally{if(hadPartial)PlayerPrefs.SetString(partialKey,oldPartial);else PlayerPrefs.DeleteKey(partialKey);PlayerPrefs.Save();UnityEngine.Object.DestroyImmediate(partial);CaseProgressStore.ClearCache();}
        progress=CaseProgressStore.Get(definition);string beforeVerdict=JsonUtility.ToJson(progress);var storageKey=CaseProgressStore.StorageKey(definition);
        var choiceNames=new[]{"VerdictHuman","VerdictRecord","VerdictUnexplained","VerdictDeferred"};
        for(int choice=0;choice<4;choice++)
        {
            PlayerPrefs.SetString(storageKey,beforeVerdict);CaseProgressStore.ClearCache();player.HUD.ShowReport(resolution);Check(!Button(player,"ConfirmVerdict").gameObject.activeSelf,"Verdict requires explicit selection before confirmation");
            Button(player,choiceNames[choice]).onClick.Invoke();Check(Button(player,"ConfirmVerdict").gameObject.activeInHierarchy && !CaseProgressStore.Get(definition).Has("Case001Completed"),"Selecting classification does not prematurely complete case");
            Button(player,"ConfirmVerdict").onClick.Invoke();progress=CaseProgressStore.Get(definition);
            Check(player.HUD.IsCaseOpen && !Button(player,"VerdictDeferred").gameObject.activeInHierarchy && progress.Has("Case001Completed") && progress.verdict==((CaseVerdict)choice).ToString() && progress.Fact("Case001Verdict")==progress.verdict,"All four verdicts accepted and saved: "+((CaseVerdict)choice));
            Button(player,"ContinueInvestigation").onClick.Invoke();Check(!player.HUD.IsCaseOpen,"Completion report returns to archive through its real UI button");
        }
        var archiveEnvironment=GameObject.Find("ArchiveClosure").GetComponent<CaseEnvironmentState>();var unknown=UnityEngine.Object.FindObjectsByType<InspectableDocument>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(d=>d.name=="CASE 00");
        Target(player,new Vector3(2.62f,.05f,2.5f),unknown.transform.position);archiveEnvironment.Evaluate();until=Time.time+4.3f;while(Time.time<until)yield return null;Check(!unknown.gameObject.activeSelf,"CASE 00 does not appear while its empty slot is being watched");
        player.ViewCamera.transform.LookAt(new Vector3(0,1.4f,-3));archiveEnvironment.Evaluate();Check(unknown.gameObject.activeSelf && progress.Has("Case00Activated"),"CASE 00 appears silently outside view after leaving report area");
        Target(player,new Vector3(2.62f,.05f,2.5f),unknown.transform.position);Check(player.TryInteract() && Body(player).Contains("기록 담당자 0317") && progress.Has("Case00Inspected"),"CASE 00 discovered by Raycast with fictional investigator code");player.HUD.NextPage();var image=player.HUD.GetComponentsInChildren<RawImage>(true).First(t=>t.name=="CCTVFrame");Check(image.gameObject.activeInHierarchy && image.texture!=null,"CASE 00 archival photograph appears in reused document viewer");player.HUD.NextPage();Check(Body(player)=="이후 기록 없음" && !image.gameObject.activeSelf,"CASE 00 ends at later record absent and no further progression");player.CloseCase();
        CaseProgressStore.ClearCache();progress=CaseProgressStore.Get(definition);Check(progress.Has("Case001Completed") && progress.Has("Case00Activated") && progress.evidenceIds.Count==7 && progress.verdict=="Deferred","Completion, verdict, evidence and CASE 00 survive cache reset from PlayerPrefs JSON");
        var reload=SceneManager.LoadSceneAsync("ArchiveRoom");while(!reload.isDone)yield return null;yield return null;player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();player.enabled=false;
        Check(GameObject.Find("CASE 00")!=null && GameObject.Find("FieldInvestigationMemo")!=null,"Reload restores archive file state without replaying delayed appearance");
        file=UnityEngine.Object.FindFirstObjectByType<CaseFile>();Target(player,new Vector3(.45f,.05f,-.55f),file.transform.position);Check(player.TryInteract() && Body(player).Contains("판단 보류") && !Button(player,"VerdictDeferred").gameObject.activeInHierarchy && !Button(player,"StartField").gameObject.activeInHierarchy,"Completed report remains readable with stored verdict and no new classification/replay");player.CloseCase();
        Check(!SceneTransitionManager.Begin(definition),"Completed case cannot accidentally restart through shared start function");
        var motel=SceneManager.LoadSceneAsync("Case001_Motel");while(!motel.isDone)yield return null;while(SceneManager.GetActiveScene().name!="ArchiveRoom" || SceneTransitionManager.IsTransitioning)yield return null;yield return null;
        Check(SceneManager.GetActiveScene().name=="ArchiveRoom" && GameObject.Find("CASE 00")!=null,"Launching completed motel save redirects safely to ArchiveRoom");
        player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();player.enabled=false;Teleport(player,new Vector3(2,.05f,-3));player.transform.rotation=Quaternion.identity;var start=player.transform.position;Steps(player,Vector2.up,20);Check(Vector3.Distance(start,player.transform.position)>.5f,"Player continues moving after episode completion");
    }
    private static IEnumerator CaptureBed(FirstPersonPlayer player,string name,string cover,Vector3 position)
    {
        var camera=player.ViewCamera;var localPosition=camera.transform.localPosition;var localRotation=camera.transform.localRotation;
        try{camera.transform.position=position;camera.transform.LookAt(GameObject.Find(cover).transform.position);Directory.CreateDirectory("Documentation/Verification/StairBedding");ScreenCapture.CaptureScreenshot("Documentation/Verification/StairBedding/"+name+".png");float until=Time.time+.4f;while(Time.time<until)yield return null;}
        finally{camera.transform.localPosition=localPosition;camera.transform.localRotation=localRotation;}
    }
    private static IEnumerator CaptureKeyUI(string name)
    {Directory.CreateDirectory("Documentation/Verification/MotelKeys");ScreenCapture.CaptureScreenshot("Documentation/Verification/MotelKeys/"+name+".png");float stop=Time.time+.4f;while(Time.time<stop)yield return null;}
    private static IEnumerator CaptureKeyWorld(FirstPersonPlayer player,string name,Vector3 position,Vector3 target)
    {var camera=player.ViewCamera;var localPosition=camera.transform.localPosition;var rotation=camera.transform.localRotation;try{camera.transform.position=position;camera.transform.LookAt(target);player.UpdateTarget();var capture=CaptureKeyUI(name);while(capture.MoveNext())yield return null;}finally{camera.transform.localPosition=localPosition;camera.transform.localRotation=rotation;}}
    private static void Teleport(FirstPersonPlayer player,Vector3 position){var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=position;cc.enabled=true;Physics.SyncTransforms();}
    private static void Steps(FirstPersonPlayer player,Vector2 input,int count,bool run=false){for(int i=0;i<count;i++)player.Move(input,run,1f/60);}
    private static void Target(FirstPersonPlayer player,Vector3 position,Vector3 look){player.HUD.CloseCase();Teleport(player,position);player.transform.rotation=Quaternion.identity;player.ViewCamera.transform.LookAt(look);Physics.SyncTransforms();player.UpdateTarget();}
    private static string Body(FirstPersonPlayer player)=>player.HUD.GetComponentsInChildren<Text>(true).First(t=>t.name==(player.HUD.HasActiveDocument?"DocumentText":"Description")).text;
    private static Button Button(FirstPersonPlayer player,string name)=>player.HUD.GetComponentsInChildren<Button>(true).First(t=>t.name==name);
}
