using System;
using System.IO;
using System.Linq;
using Archive0317;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class Case001Room403InteriorBuilder
{
    private static Transform root;
    private static CaseDefinition data;
    private static Font font;
    private static Material wall,wood,steel,paper,enamel,black,carpet;
    [MenuItem("Archive 03:17/Add CASE 001 Room 403 Interior")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
        var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(current.isDirty)EditorSceneManager.SaveScene(current);
        var scene=EditorSceneManager.OpenScene(Case001MotelBuilder.ScenePath);
        if(GameObject.Find("Room403Interior")!=null)return;
        data=AssetDatabase.LoadAssetAtPath<CaseDefinition>(Case001MotelBuilder.DefinitionPath);font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansCJKkr-Regular.otf");
        wall=Material("Assets/Materials/Motel/FadedWallpaper.mat");carpet=Material("Assets/Materials/Motel/DarkCarpet.mat");
        wood=Institutional("OfficeLaminate");steel=Institutional("PaintedSteel");paper=Institutional("YellowedPaper");enamel=Institutional("BeigeEnamel");black=Institutional("Bakelite");
        // Resolve existing palette by shader-independent asset names rather than creating duplicate materials.
        if(carpet==null)carpet=Material(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("Carpet t:Material").First()));
        if(wood==null)wood=Material(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("Wood t:Material").First()));
        if(paper==null)paper=Material(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("Paper t:Material").First()));
        if(black==null)black=steel;
        root=new GameObject("Room403Interior").transform;var player=Object.FindFirstObjectByType<FirstPersonPlayer>();
        var outer=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name=="RoomDoor403").gameObject;
        Object.DestroyImmediate(outer.GetComponent<InspectableSoundNote>());Object.DestroyImmediate(outer.transform.Find("Sealed403Backing").gameObject);
        var behind=GameObject.Find("Behind403Receiver").GetComponent<AudioSource>();var click=Sound("Room403Latch",new Vector3(-1.3f,10,4.5f),Wave("QuietLatch",.22f,0),.13f,4);
        var entrance=outer.AddComponent<InspectableCaseDoor>();entrance.Configure(data,outer.transform.Find("DoorHinge"),new[]{"LedgerInspected","CCTVContradictionFound","Room403Revealed"},"Room403DoorInspected","Room403Opened","Room403Entered","KeyEvidenceFound",behind,click);
        // The room is spatially isolated so its geometry cannot overlap the preserved 404 room.
        Box("Room403Floor",new Vector3(-20.1f,8.9f,4.5f),new Vector3(4.2f,.2f,4.4f),carpet);
        Box("Room403Ceiling",new Vector3(-20.1f,11.65f,4.5f),new Vector3(4.2f,.15f,4.4f),wall);
        Box("Room403West",new Vector3(-22.2f,10.3f,4.5f),new Vector3(.16f,2.6f,4.4f),wall);
        Box("Room403North",new Vector3(-20.1f,10.3f,6.7f),new Vector3(4.2f,2.6f,.16f),wall);
        foreach(float z in new[]{3.1f,5.9f})Box("Room403East",new Vector3(-18,10.3f,z),new Vector3(.16f,2.6f,1.65f),wall);
        Box("Room403Lintel",new Vector3(-18,11.3f,4.5f),new Vector3(.16f,.6f,1.1f),wall);
        Box("Room403SouthWest",new Vector3(-21.05f,10.3f,2.3f),new Vector3(2.3f,2.6f,.16f),wall);
        Box("Room403SouthEast",new Vector3(-18.35f,10.3f,2.3f),new Vector3(.7f,2.6f,.16f),wall);
        Box("BathroomLintel",new Vector3(-19.05f,11.3f,2.3f),new Vector3(.85f,.6f,.16f),wall);
        var bed=Box("Room403Bed",new Vector3(-21,9.23f,4.9f),new Vector3(1.7f,.46f,2.1f),wood);bed.AddComponent<InspectableNote>().Configure("이불 모서리가 매트리스 아래로 단단히 들어가 있다.");
        Box("Room403BedCover",new Vector3(-21,9.51f,4.9f),new Vector3(1.72f,.12f,2.08f),enamel,false);
        Box("Room403Pillow",new Vector3(-21,9.61f,5.6f),new Vector3(.75f,.12f,.45f),paper,false);
        Box("Room403Headboard",new Vector3(-21,9.69f,6.05f),new Vector3(1.8f,.9f,.08f),wood);
        Box("Room403Nightstand",new Vector3(-21.55f,9.3f,3.25f),new Vector3(.65f,.6f,.65f),wood);
        var telephone=Object.Instantiate(GameObject.Find("WiredTelephone"),root);telephone.name="Room403Telephone";telephone.transform.position=new Vector3(-21.55f,9.68f,3.25f);
        foreach(var note in telephone.GetComponentsInChildren<InspectableNote>())Object.DestroyImmediate(note);
        if(telephone.GetComponent<Collider>()==null){var col=telephone.AddComponent<BoxCollider>();col.size=new Vector3(.48f,.3f,.4f);col.center=new Vector3(0,.05f,0);}
        var humid=Sound("Room403FluorescentHum",new Vector3(-20,11.35f,4.6f),AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Atmosphere/FacilityHum.wav"),.032f,5,true);
        var pipes=Sound("Room403PipeTone",new Vector3(-19.2f,10,1.1f),AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Atmosphere/PipeAir.wav"),.018f,5,true);
        var bell=Sound("Room403PhoneBell",telephone.transform.position,Wave("RoomTelephone",4.3f,1),.22f,7);
        var receiver=Sound("Room403HandsetAudio",telephone.transform.position,Wave("ReceiverStatic",2,2),.13f,2.5f);
        telephone.AddComponent<InspectableEchoPhone>().Configure(data,new[]{"BagInspected","ReceiptInspected","PersonalNoteInspected","PhoneInspected"},bell,receiver,receiver.clip,new[]{humid,pipes});
        var desk=Box("Room403WritingTable",new Vector3(-19.5f,9.38f,6.23f),new Vector3(1.05f,.76f,.55f),wood);
        var chair=new GameObject("Room403Chair");chair.transform.SetParent(root);chair.transform.position=new Vector3(-19.5f,9,5.45f);
        Box("ChairSeat",new Vector3(-19.5f,9.43f,5.45f),new Vector3(.42f,.09f,.42f),wood,true,chair.transform);
        Box("ChairBack",new Vector3(-19.5f,9.7f,5.65f),new Vector3(.42f,.5f,.06f),wood,true,chair.transform);
        foreach(float x in new[]{-19.66f,-19.34f})foreach(float z in new[]{5.29f,5.61f})Box("ChairLeg",new Vector3(x,9.21f,z),new Vector3(.04f,.42f,.04f),steel,true,chair.transform);
        var bag=Box("Room403GuestBag",new Vector3(-20.65f,9.08f,2.85f),new Vector3(.8f,.16f,.5f),black);bag.AddComponent<InspectableNote>().Configure("안쪽 이름표: 최민수\n지갑, 자동차 키, 담배 한 갑. 주머니에는 접힌 영수증이 남아 있다.",data,"BagInspected");
        Box("BagWallet",new Vector3(-20.86f,9.2f,2.83f),new Vector3(.2f,.06f,.14f),wood,false);
        Box("BagCarKeys",new Vector3(-20.53f,9.2f,2.9f),new Vector3(.07f,.02f,.16f),steel,false);
        Box("BagCigarettes",new Vector3(-20.4f,9.2f,2.7f),new Vector3(.09f,.04f,.14f),paper,false);
        var receipt=Box("Room403Receipt",new Vector3(-19.48f,9.79f,6.2f),new Vector3(.3f,.025f,.4f),paper);
        var doc=receipt.AddComponent<InspectableDocument>();doc.Configure(data,"숙박 영수증",new[]{Receipt("404")},"ReceiptInspected",0,"ReceiptRoom","404");doc.ConfigureVariant("KeyEvidenceFound",new[]{Receipt("403")},"ReceiptChangedSeen","403");
        Label("ReceiptPrint","숙박 영수증\n객실 404",new Vector3(-19.48f,9.809f,6.2f),new Vector3(90,180,0),.009f).color=new Color(.12f,.11f,.1f);
        var memo=Box("Room403PersonalNote",new Vector3(-19.87f,9.79f,6.05f),new Vector3(.22f,.018f,.28f),paper);memo.AddComponent<InspectableDocument>().Configure(data,"수첩의 마지막 장",new[]{"10월 11일\n\n차는 골목에 두었다. 내일 아침 열쇠를 반납하고 내려가면 된다.\n전화기는 선이 꽂혀 있는데 연결음이 없다.\n\n사진 한 장이 종이 사이에 끼어 있다. 산길 옆에 세워 둔 흰 승용차."},"PersonalNoteInspected");
        Box("BagPhotograph",new Vector3(-20.72f,9.185f,2.71f),new Vector3(.15f,.012f,.1f),enamel,false);
        Box("Room403BlockedWindow",new Vector3(-22.1f,10.55f,4.5f),new Vector3(.05f,.9f,1.1f),black);
        Box("WindowBlind",new Vector3(-22.06f,10.55f,4.5f),new Vector3(.025f,1,1.2f),enamel,false);
        for(int i=0;i<10;i++)Box("BlindSlat",new Vector3(-22.04f,10.13f+i*.09f,4.5f),new Vector3(.025f,.022f,1.2f),wood,false);
        Box("Room403Mirror",new Vector3(-19.5f,10.65f,6.59f),new Vector3(.65f,.6f,.03f),steel).AddComponent<InspectableNote>().Configure("은빛 표면 가장자리가 검게 들떠 있다. 방 안의 불빛이 희미하게 번진다.",data,"MirrorInspected");
        Box("Room403Ashtray",new Vector3(-19.83f,9.79f,6.25f),new Vector3(.13f,.025f,.13f),steel,false);
        Box("Room403WasteBin",new Vector3(-18.35f,9.2f,2.9f),new Vector3(.25f,.4f,.25f),black);
        foreach(float x in new[]{-18.75f,-18.5f})Box("GuestSlipper",new Vector3(x,9.035f,3.25f),new Vector3(.12f,.055f,.3f),enamel,false);
        Box("CoatRackBar",new Vector3(-18.1f,10.8f,3.2f),new Vector3(.08f,.04f,.75f),steel,false);
        foreach(float z in new[]{3,3.25f,3.5f})Box("CoatHanger",new Vector3(-18.22f,10.57f,z),new Vector3(.3f,.08f,.025f),wood,false);
        Box("Room403Picture",new Vector3(-22.08f,10.75f,6.15f),new Vector3(.03f,.45f,.45f),wood,false);Box("PicturePrint",new Vector3(-22.055f,10.75f,6.15f),new Vector3(.015f,.37f,.37f),paper,false);
        // Add the same small print and TV shape to 404, without moving its evidence or colliders.
        Box("Room404Picture",new Vector3(-4.89f,10.65f,2.1f),new Vector3(.03f,.45f,.45f),wood,false);Box("Room404PicturePrint",new Vector3(-4.865f,10.65f,2.1f),new Vector3(.015f,.37f,.37f),paper,false);
        Box("Room404TVStand",new Vector3(-4.6f,9.45f,2.05f),new Vector3(.45f,.9f,.55f),wood);Box("Room404CRTTV",new Vector3(-4.5f,10.15f,2.05f),new Vector3(.45f,.42f,.55f),black).AddComponent<InspectableNote>().Configure("작은 TV의 화면에는 아무것도 비치지 않는다.");
        Bathroom();
        var roomLight=LightAt("Room403WarmFluorescent",new Vector3(-20,11.3f,4.5f),new Color(1,.79f,.55f),4.2f,5);
        Box("Room403LampHousing",new Vector3(-20,11.47f,4.5f),new Vector3(.85f,.06f,.19f),enamel,false);Box("Room403LampTube",new Vector3(-20,11.42f,4.5f),new Vector3(.76f,.03f,.065f),paper,false);
        var innerRoot=new GameObject("Room403ExitDoor");innerRoot.transform.SetParent(root);innerRoot.transform.position=new Vector3(-18,9,4.5f);
        var panel=new GameObject("DoorHinge");panel.transform.SetParent(innerRoot.transform);panel.transform.position=new Vector3(-18,9,4);
        Box("DoorLeaf",new Vector3(-18,10.05f,4.5f),new Vector3(.09f,2.1f,1),wood,true,panel.transform);
        Box("InsideHandle",new Vector3(-18.08f,10,4.85f),new Vector3(.1f,.04f,.13f),steel,false,panel.transform);
        var inner=innerRoot.AddComponent<InspectableCaseDoor>();inner.Configure(data,panel.transform,new[]{"Room403Entered"},"Room403DoorInspected","Room403ExitOpened","Room403Entered","KeyEvidenceFound",null,click);
        var entranceArea=Volume("Room403EntranceThreshold",new Vector3(-1.49f,9.85f,4.5f),new Vector3(.35f,1.8f,.7f));
        var entryPoint=Point("Room403EntryPoint",new Vector3(-18.5f,9.05f,4.5f));var exitPoint=Point("Room403CorridorReturn",new Vector3(-.6f,9.05f,4.5f));
        entranceArea.gameObject.AddComponent<CaseRoomThreshold>().Configure(data,player,entranceArea,entrance,entryPoint,"Room403Opened","Room403Completed","Room403Entered");
        Box("Room403DarkVestibule",new Vector3(-1.72f,10.05f,4.5f),new Vector3(.08f,2.1f,1),black,true,outer.transform);
        var exitArea=Volume("Room403ExitThreshold",new Vector3(-17.65f,9.85f,4.5f),new Vector3(.35f,1.8f,.7f));var leave=exitArea.gameObject.AddComponent<CaseRoomThreshold>();leave.Configure(data,player,exitArea,inner,exitPoint,"Room403Opened","Room403Completed","Room403Completed");leave.ConfigureCompletionCondition("KeyEvidenceFound");
        var roomArea=Volume("Room403Area",new Vector3(-20,10,4.5f),new Vector3(4.1f,2,4.3f));var bath=GameObject.Find("Room403BathArea").GetComponent<BoxCollider>();
        var before=Volume("Room403ExitPreview",new Vector3(-16.8f,10,4.5f),new Vector3(2.3f,2,8));
        Box("ExitPreviewFloor",new Vector3(-16.85f,8.9f,4.5f),new Vector3(2.3f,.2f,8),carpet);
        var previewWall=Box("ExitPreviewWallpaper",new Vector3(-15.65f,10.3f,4.5f),new Vector3(.15f,2.6f,8),wall);Box("ExitPreviewCeiling",new Vector3(-16.85f,11.65f,4.5f),new Vector3(2.3f,.15f,8),wall);
        LightAt("ExitPreviewLight",new Vector3(-16.8f,11.3f,4.5f),new Color(.62f,.79f,.7f),2.2f,5);
        var shelves=new GameObject("ArchiveShelfGlimpse");shelves.transform.SetParent(root);for(int i=0;i<3;i++){Box("ArchiveShelfBoard",new Vector3(-15.85f,9.4f+i*.58f,4.5f),new Vector3(.45f,.06f,2.3f),steel,false,shelves.transform);for(int j=0;j<6;j++)Box("ArchiveBinders",new Vector3(-15.95f,9.62f+i*.58f,3.65f+j*.3f),new Vector3(.24f,.37f,.12f),paper,false,shelves.transform);}shelves.SetActive(false);
        var tv=Television(player);
        var key=Box("Room403KeyEvidence",new Vector3(-20.03f,9.035f,4.03f),new Vector3(.24f,.055f,.17f),enamel);
        key.AddComponent<InspectableDocument>().Configure(data,"객실 키 태그",new[]{"앞면\n\n403\n\n번호 아래 플라스틱에 잔긁힘이 남아 있다.","뒷면\n\n404\n\n낡은 글자 위로 투명 테이프가 겹쳐 붙어 있다."},"KeyEvidenceFound",1,"KeyTag","403 / 404");
        Label("KeyTag403","403",key.transform.position+Vector3.up*.035f,new Vector3(90,180,0),.014f).transform.SetParent(key.transform,true);key.SetActive(false);
        var drip=Sound("BathroomSingleDrip",new Vector3(-19.2f,9.7f,1.5f),Wave("BathroomDrip",.6f,3),.1f,4);
        var roomEnvironment=root.gameObject.AddComponent<CaseEnvironmentState>();var missing=GameObject.Find("MissingRoom403Wall");var archiveMat=Institutional("Concrete");if(archiveMat==null)archiveMat=enamel;
        var receiptPrint=receipt.GetComponentsInChildren<TextMesh>().FirstOrDefault();if(receiptPrint==null)receiptPrint=GameObject.Find("ReceiptPrint").GetComponent<TextMesh>();
        roomEnvironment.Configure(data,player,new[]{
            new EnvironmentRule{requirements=new[]{"Room403Opened"},completionFlag="Room403InnerDoorOpened",changes=new[]{new EnvironmentChange{door=inner,doorOpen=true}}},
            new EnvironmentRule{requirements=new[]{"Room403Entered","PhoneEventAnswered","BathroomRevisited"},completionFlag="BathroomReturned",insideArea=bath,cue=drip},
            new EnvironmentRule{requirements=new[]{"BathroomReturned"},completionFlag="RoomAltered",insideArea=bath,hiddenFromView=chair.GetComponentInChildren<Renderer>(),changes=new[]{new EnvironmentChange{target=chair,changePosition=true,localPosition=chair.transform.localPosition+new Vector3(.35f,0,-.35f)},new EnvironmentChange{door=inner,doorOpen=false},new EnvironmentChange{door=entrance,doorOpen=false}}},
            new EnvironmentRule{requirements=new[]{"RoomAltered"},completionFlag="TelevisionPowerOn",outsideArea=bath,insideArea=roomArea,hiddenFromView=tv.GetComponent<Renderer>()},
            new EnvironmentRule{requirements=new[]{"TelevisionInspected","TVWatcherFinished"},completionFlag="KeyTagVisible",changes=new[]{new EnvironmentChange{target=key,changeActive=true,active=true}}},
            new EnvironmentRule{requirements=new[]{"KeyEvidenceFound"},completionFlag="ReceiptRewritten",changes=new[]{new EnvironmentChange{label=receiptPrint,text="숙박 영수증\n객실 403"}}},
            new EnvironmentRule{requirements=new[]{"ReceiptChangedSeen","KeyEvidenceFound","Room403ExitOpened"},completionFlag="ArchiveThresholdGlimpse",insideArea=roomArea,visibleFromView=previewWall.GetComponent<Renderer>(),restoreAfter=.8f,changes=new[]{new EnvironmentChange{surface=previewWall.GetComponent<Renderer>(),material=archiveMat},new EnvironmentChange{target=shelves,changeActive=true,active=true}},restoreChanges=new[]{new EnvironmentChange{surface=previewWall.GetComponent<Renderer>(),material=wall},new EnvironmentChange{target=shelves,changeActive=true,active=false}}},
            new EnvironmentRule{requirements=new[]{"Room403Completed"},completionFlag="Room403Vanished",changes=new[]{new EnvironmentChange{target=outer,changeActive=true,active=false},new EnvironmentChange{target=missing,changeActive=true,active=true},new EnvironmentChange{target=root.gameObject,changeActive=true,active=false}},observation="403호가 없다."}
        });
        // Completion restoration is evaluated last so the earlier reveal rule cannot leave a door behind.
        var anomaly=GameObject.Find("Room403Anomaly").GetComponent<CaseEnvironmentState>();var serialized=new SerializedObject(anomaly);var rules=serialized.FindProperty("rules");var last=rules.GetArrayElementAtIndex(3);var excluded=last.FindPropertyRelative("excludedFlags");excluded.arraySize=1;excluded.GetArrayElementAtIndex(0).stringValue="Room403Completed";serialized.ApplyModifiedPropertiesWithoutUndo();
        foreach(var label in root.GetComponentsInChildren<TextMesh>(true)){var depth=label.gameObject.AddComponent<WorldTextDepth>();depth.Configure(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/WorldTextDepth.shader"));}
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Room403TVWatcherBuilder.Apply();Debug.Log("Room 403 interior saved.");
    }
    private static string Receipt(string room)=>"은하장 / 숙박 영수증\n\n2002. 10. 11.\n성명: 최민수\n객실: "+room+"\n금액: 30,000원\n현금 수납\n\n하단에 프런트 도장이 흐리게 찍혀 있다.";
    private static GameObject Television(FirstPersonPlayer player)
    {
        Box("Room403TVStand",new Vector3(-18.55f,9.4f,5.8f),new Vector3(.6f,.8f,.65f),wood);
        var tv=Box("Room403CRTTV",new Vector3(-18.55f,10.05f,5.8f),new Vector3(.55f,.48f,.65f),black);
        var screen=Box("Room403TVScreen",new Vector3(-18.837f,10.08f,5.8f),new Vector3(.014f,.34f,.48f),black,false);
        // A camera views the current real doorway; no player body or guest model is rendered.
        var camera=new GameObject("Room403CurrentCorridorCamera").AddComponent<Camera>();camera.transform.SetParent(root);camera.transform.position=new Vector3(.85f,10.7f,6.1f);camera.transform.LookAt(new Vector3(-1.15f,10.25f,4.5f));camera.enabled=false;camera.fieldOfView=45;camera.nearClipPlane=.1f;camera.farClipPlane=12;camera.cullingMask=~(1<<30);camera.allowHDR=false;camera.allowMSAA=false;
        var stamp=Label("Room403TVTimestamp","03:17",new Vector3(-18.85f,9.965f,5.66f),new Vector3(0,90,0),.0065f);stamp.color=new Color(.65f,.72f,.66f);
        var unlit=new Material(Shader.Find("Universal Render Pipeline/Unlit"));unlit.SetColor("_BaseColor",new Color(.015f,.015f,.015f));AssetDatabase.CreateAsset(unlit,"Assets/Materials/Motel/Room403TVDisplay.mat");screen.GetComponent<Renderer>().sharedMaterial=unlit;
        var hum=Sound("Room403TVElectronics",tv.transform.position,AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Atmosphere/FacilityHum.wav"),.02f,3,true);hum.playOnAwake=false;
        tv.AddComponent<InspectableCaseTelevision>().Configure(data,screen.GetComponent<Renderer>(),camera,AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/CCTVImage.shader"),stamp,hum,"TelevisionPowerOn");return tv;
    }
    private static void Bathroom()
    {
        var tile=Material("Assets/Materials/Motel/OldTile.mat");if(tile==null)tile=enamel;
        Box("Room403BathFloor",new Vector3(-19.05f,8.9f,1.55f),new Vector3(1.6f,.2f,1.5f),tile);
        Box("Room403BathSouth",new Vector3(-19.05f,10.3f,.8f),new Vector3(1.6f,2.6f,.12f),tile);
        foreach(float x in new[]{-19.85f,-18.25f})Box("Room403BathSide",new Vector3(x,10.3f,1.55f),new Vector3(.12f,2.6f,1.5f),tile);
        Box("Room403BathCeiling",new Vector3(-19.05f,11.55f,1.55f),new Vector3(1.6f,.12f,1.5f),tile);
        var door=Box("Room403BathDoor",new Vector3(-19.05f,10.05f,2.23f),new Vector3(.8f,2.1f,.06f),enamel);var hinge=new GameObject("BathHinge");hinge.transform.SetParent(root);hinge.transform.position=new Vector3(-19.45f,9,2.23f);door.transform.SetParent(hinge.transform,true);hinge.AddComponent<InspectableDoor>().Configure(hinge.transform,data,"","욕실 문이 걸려 있다.");
        var sink=Box("BathroomWashbasin",new Vector3(-19.25f,9.65f,1.08f),new Vector3(.6f,.16f,.4f),enamel);sink.AddComponent<InspectableNote>().Configure("세면대 바닥에는 물때만 남아 있다.",data,"BathroomInspected");
        Box("BathroomFaucet",new Vector3(-19.25f,9.79f,.96f),new Vector3(.05f,.14f,.11f),steel,false);
        Box("BathroomMirror",new Vector3(-19.25f,10.35f,.89f),new Vector3(.52f,.65f,.025f),steel,false);
        Box("BathroomShowerPipe",new Vector3(-18.35f,10.13f,1.4f),new Vector3(.04f,1.6f,.04f),steel,false);Box("BathroomShowerHead",new Vector3(-18.45f,10.9f,1.4f),new Vector3(.2f,.055f,.12f),steel,false);
        Box("BathroomVent",new Vector3(-19.8f,11.05f,1.45f),new Vector3(.03f,.28f,.28f),black,false);
        LightAt("Room403BathroomLight",new Vector3(-19.05f,11.25f,1.55f),new Color(.75f,.85f,.69f),1.2f,2.5f);
        var bath=Volume("Room403BathArea",new Vector3(-19.05f,9.9f,1.55f),new Vector3(1.35f,1.8f,1.1f));var visit=bath.gameObject.AddComponent<CaseProgressTrigger>();visit.Configure(data,"BathroomVisited");visit.ConfigureRevisit("BathroomRevisited");
    }
    private static Material Material(string path)=>AssetDatabase.LoadAssetAtPath<Material>(path);
    private static Material Institutional(string name)=>Material("Assets/Materials/ArchiveRoom/Institutional/"+name+".mat");
    private static GameObject Box(string name,Vector3 pos,Vector3 scale,Material mat,bool solid=true,Transform parent=null){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent??root);go.transform.position=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;if(!solid)Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
    private static TextMesh Label(string name,string text,Vector3 pos,Vector3 rotation,float size){var go=new GameObject(name);go.transform.SetParent(root);go.transform.position=pos;go.transform.eulerAngles=rotation;var mesh=go.AddComponent<TextMesh>();mesh.font=font;mesh.text=text;mesh.fontSize=64;mesh.characterSize=size;mesh.anchor=TextAnchor.MiddleCenter;mesh.color=new Color(.77f,.74f,.65f);go.GetComponent<Renderer>().sharedMaterial=font.material;return mesh;}
    private static BoxCollider Volume(string name,Vector3 pos,Vector3 size){var go=Point(name,pos).gameObject;var collider=go.AddComponent<BoxCollider>();collider.isTrigger=true;collider.size=size;return collider;}
    private static Transform Point(string name,Vector3 pos){var go=new GameObject(name);go.transform.SetParent(root);go.transform.position=pos;return go.transform;}
    private static Light LightAt(string name,Vector3 pos,Color color,float intensity,float range){var light=Point(name,pos).gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=color;light.intensity=intensity;light.range=range;light.shadows=LightShadows.None;return light;}
    private static AudioSource Sound(string name,Vector3 pos,AudioClip clip,float volume,float range,bool loop=false){var audio=Point(name,pos).gameObject.AddComponent<AudioSource>();audio.clip=clip;audio.volume=volume;audio.spatialBlend=1;audio.minDistance=.8f;audio.maxDistance=range;audio.rolloffMode=AudioRolloffMode.Linear;audio.loop=loop;audio.playOnAwake=loop;return audio;}
    private static AudioClip Wave(string name,float seconds,int mode)
    {
        const int rate=22050;int length=(int)(seconds*rate);string path="Assets/Audio/Atmosphere/"+name+".wav";var rng=new System.Random(403);
        using(var w=new BinaryWriter(File.Open(path,FileMode.Create))){w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+length*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(length*2);
            for(int i=0;i<length;i++){float t=i/(float)rate;float v;if(mode==1){float s=t%1.5f;v=s<.65f?(Mathf.Sin(t*1240*2*Mathf.PI)+Mathf.Sin(t*1570*2*Mathf.PI))*.10f*Mathf.Min(s/.04f,1)*Mathf.Min((.65f-s)/.12f,1):0;}else if(mode==2)v=((float)rng.NextDouble()*2-1)*.08f;else if(mode==3)v=Mathf.Sin((1100*t-550*t*t)*2*Mathf.PI)*Mathf.Exp(-t*20)*.16f;else v=((float)rng.NextDouble()*2-1)*Mathf.Exp(-t*65)*.18f;w.Write((short)(v*short.MaxValue));}}
        AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
