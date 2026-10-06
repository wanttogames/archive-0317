using System;
using System.IO;
using System.Linq;
using Archive0317;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using UnityEngine.UI;

public static class Case001MotelBuilder
{
    public const string ScenePath = "Assets/Scenes/Cases/Case001_Motel.unity";
    public const string DefinitionPath = "Assets/Cases/Case001.asset";
    private static Font font;
    private static CaseDefinition definition;
    private static Material wallpaper, carpet, wood, steel, paper, black, enamel, tube, screen, tile;
    private static Transform environment;

    [MenuItem("Archive 03:17/Build CASE 001 Motel Slice")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        if (File.Exists(ScenePath)) { Debug.Log("CASE 001 scene already exists; keeping the existing scene."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        foreach (var folder in new[] { "Assets/Cases", "Assets/Scenes/Cases", "Assets/Materials/Motel", "Assets/Textures/Motel", "Assets/Audio/Atmosphere" }) Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansCJKkr-Regular.otf");
        definition = ScriptableObject.CreateInstance<CaseDefinition>();
        definition.Configure("case001", "CASE 001 — 404호", "Case001_Motel",
            "공식 사건 기록 / 2002. 10. 11.\n실종자: 최민수 (남성, 가명)\n투숙 객실: 404호\n\nCCTV에는 모텔로 들어가는 모습이 남아 있다. 외부로 나가는 모습은 확인되지 않았다.\n\n현장: 지방 소형 모텔. 프런트 기록과 투숙 객실을 확인한다.",
            "404", new[] { "LedgerInspected", "Room404Entered", "Room404PaperInspected" });
        ConfigureNotebook(definition);
        AssetDatabase.CreateAsset(definition, DefinitionPath);
        UpgradeSharedPlayer();
        var archive = EditorSceneManager.OpenScene("Assets/Scenes/ArchiveRoom.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<CaseFile>().Configure(definition);
        PrefabUtility.ApplyPrefabInstance(Object.FindFirstObjectByType<CaseFile>().gameObject, InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(archive);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        environment = new GameObject("MotelEnvironment").transform;
        wallpaper = TextureMaterial("FadedWallpaper", new Color(.48f,.43f,.32f), false);
        carpet = TextureMaterial("WornCarpet", new Color(.16f,.10f,.11f), true);
        wood = LoadMaterial("OfficeLaminate"); steel = LoadMaterial("PaintedSteel"); paper = LoadMaterial("YellowedPaper");
        black = LoadMaterial("Bakelite"); enamel = LoadMaterial("BeigeEnamel"); tube = LoadMaterial("FluorescentTube"); screen = LoadMaterial("CRTGlass"); tile = LoadMaterial("WornLinoleum");
        Lobby(); Stairs(); FourthFloor(); Room404(); Details(); ConfigureWorldText();
        var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/FirstPersonPlayer.prefab"));
        player.transform.position = new Vector3(0,.05f,-3.7f);
        var hud = player.GetComponentInChildren<ArchiveHUD>();
        var context = new GameObject("CaseContext").AddComponent<CaseSceneContext>(); context.Configure(definition,hud);
        var brand = player.GetComponentsInChildren<Text>(true).First(t=>t.name=="ArchiveBrand"); brand.text="CASE 001 / 현장 기록";
        var controls=player.GetComponentsInChildren<Text>(true).First(t=>t.name=="Controls");controls.text="WASD 이동 · SHIFT 달리기 · E 조사 · TAB 사건 기록 · ESC 커서";
        RenderSettings.skybox = null; RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.18f,.16f,.125f);
        RenderSettings.fog = true; RenderSettings.fogColor = new Color(.075f,.07f,.055f); RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = .015f;
        var atmosphere = new GameObject("MotelAtmosphere").AddComponent<Volume>(); atmosphere.isGlobal=true; atmosphere.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Materials/ArchiveRoom/Atmosphere.asset");
        player.GetComponentInChildren<Camera>().backgroundColor = RenderSettings.fogColor;
        EditorSceneManager.SaveScene(scene,ScenePath);
        var scenes=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).ToList();scenes.Add(new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=scenes.ToArray();
        AssetDatabase.SaveAssets(); Case001Room403Builder.Apply(); Case001Room403InteriorBuilder.Apply(); EditorSceneManager.OpenScene("Assets/Scenes/ArchiveRoom.unity",OpenSceneMode.Single);
        Debug.Log("CASE 001 motel slice saved and ArchiveRoom connection configured.");
    }
    private static void UpgradeSharedPlayer()
    {
        const string path="Assets/Prefabs/Player/FirstPersonPlayer.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var hud=root.GetComponentInChildren<ArchiveHUD>();var canvas=hud.GetComponent<Canvas>();
            if(canvas.GetComponent<GraphicRaycaster>()==null)canvas.gameObject.AddComponent<GraphicRaycaster>();
            var card=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Document");
            var start=ButtonUI("StartField",card,"현장 조사 시작",new Vector2(-170,-240),new Vector2(300,48));
            var next=ButtonUI("NextPage",card,"세부 기록 확인",new Vector2(170,-240),new Vector2(280,48));
            var compare=ButtonUI("CompareRecords",card,"객실 기록 대조",new Vector2(0,-240),new Vector2(320,48));
            var toast=TextUI("Observation",hud.transform,"",new Vector2(1350,90),new Vector2(0,-420),24);toast.gameObject.SetActive(false);
            hud.ConfigureCaseUI(start,next,compare,toast);
            var body=root.GetComponentsInChildren<Text>(true).First(t=>t.name=="Description");body.fontSize=22;body.rectTransform.sizeDelta=new Vector2(730,410);body.rectTransform.anchoredPosition=new Vector2(0,-25);
            var events=new GameObject("UIEventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(root.transform,false);events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    private static void Lobby()
    {
        Box("LobbyFloor",new Vector3(0,-.15f,-1),new Vector3(7,.3f,8),tile);
        Box("LobbyCeiling",new Vector3(0,2.8f,-1),new Vector3(7,.2f,8),wallpaper);
        Box("LobbyEastWall",new Vector3(3.5f,1.35f,-1),new Vector3(.2f,2.7f,8),wallpaper);
        Box("LobbyNorthLeft",new Vector3(-2.3f,1.35f,3),new Vector3(2.4f,2.7f,.2f),wallpaper);
        Box("LobbyNorthRight",new Vector3(2.3f,1.35f,3),new Vector3(2.4f,2.7f,.2f),wallpaper);
        Box("LobbyWestLower",new Vector3(-3.5f,1.35f,-2),new Vector3(.2f,2.7f,6),wallpaper);
        Box("LobbyWestUpper",new Vector3(-3.5f,1.35f,2.85f),new Vector3(.2f,2.7f,.3f),wallpaper);
        Box("EntranceLeft",new Vector3(-2.25f,1.35f,-5),new Vector3(2.5f,2.7f,.2f),wallpaper);
        Box("EntranceRight",new Vector3(2.25f,1.35f,-5),new Vector3(2.5f,2.7f,.2f),wallpaper);
        Box("EntranceGlassDoor",new Vector3(0,1.15f,-5),new Vector3(2,2.3f,.08f),steel).AddComponent<InspectableNote>().Configure("바깥은 조용하다. 아직 확인하지 못한 현장 기록이 남아 있다.");
        Label("MotelName","은성 모텔",new Vector3(0,2.5f,-4.84f),new Vector3(0,180,0),.035f);
        Box("ReceptionCounter",new Vector3(-1.85f,.55f,-.4f),new Vector3(2.8f,1.1f,1.15f),wood);
        Box("ReceptionTop",new Vector3(-1.85f,1.12f,-.4f),new Vector3(2.9f,.08f,1.2f),enamel);
        var ledger=Box("GuestLedger",new Vector3(-1.6f,1.18f,-.65f),new Vector3(.62f,.045f,.42f),paper);
        Label("LedgerLabel","숙박 장부",new Vector3(-1.6f,1.207f,-.65f),new Vector3(90,0,0),.014f).color=new Color(.06f,.05f,.04f);
        ledger.AddComponent<InspectableDocument>().Configure(definition,"숙박 장부 / 2002. 10. 11.",new[]{
            "금요일 / 야간 접수\n\n최민수 · 성인 1명 · 현금 결제\n접수 담당: 김○○\n\n객실 배정과 영수증은 다음 칸에 기입되어 있다.\n아랫부분은 묶음 철 때문에 눌려 있다.",
            "숙박 장부 — 세부 기입란\n\n일자: 2002. 10. 11.\n성명: 최민수\n객실: 403\n인원: 1명 / 1박\n결제: 현금 30,000원\n퇴실 확인: 공란\n\n접수 담당자의 작은 도장이 찍혀 있다."},"LedgerInspected",1,"LedgerRoom","403");
        var cabinet=Box("RoomKeyCabinet",new Vector3(-2,1.75f,2.82f),new Vector3(1.5f,.9f,.12f),wood);
        cabinet.AddComponent<InspectableNote>().Configure("404호 예비 열쇠를 챙겼다. 플라스틱 키홀더가 손바닥에 차갑게 닿는다.",definition,"Room404KeyTaken");
        for(int i=0;i<4;i++){Box("KeyHook",new Vector3(-2.6f+i*.4f,1.7f,2.72f),new Vector3(.025f,.06f,.055f),steel,false);Box("PlasticKeyHolder",new Vector3(-2.6f+i*.4f,1.52f,2.71f),new Vector3(.12f,.21f,.035f),enamel,false);Label("KeyNumber",(401+i).ToString(),new Vector3(-2.6f+i*.4f,1.53f,2.68f),Vector3.zero,.009f).color=new Color(.07f,.055f,.045f);}
        var phone=Prop("WiredTelephone",new Vector3(-2.6f,1.25f,-.6f));phone.AddComponent<InspectableNote>().Configure("수화기를 들었지만 연결음은 들리지 않는다. 선은 프런트 아래로 이어져 있다.");
        Label("FrontSign","프런트 / 관리실은 옆문",new Vector3(-1.9f,2.4f,2.86f),Vector3.zero,.02f);
        // Small office reached through the west-wall opening, not through a teleport.
        Box("OfficeFloor",new Vector3(-4.5f,-.15f,2),new Vector3(2,.3f,3),tile);
        Box("OfficeCeiling",new Vector3(-4.5f,2.8f,2),new Vector3(2,.2f,3),wallpaper);
        Box("OfficeWest",new Vector3(-5.5f,1.35f,2),new Vector3(.2f,2.7f,3),wallpaper);
        Box("OfficeNorth",new Vector3(-4.5f,1.35f,3.5f),new Vector3(2,2.7f,.2f),wallpaper);
        Box("OfficeSouth",new Vector3(-4.5f,1.35f,.5f),new Vector3(2,2.7f,.2f),wallpaper);
        Box("CCTVDesk",new Vector3(-4.8f,.5f,2.8f),new Vector3(1.3f,1,.65f),wood);
        var cctv=Prop("CRTMonitor",new Vector3(-4.8f,1.08f,2.75f));
        cctv.GetComponentInChildren<TextMesh>().text="CAM 01\nPLAYBACK / PAUSED";
        cctv.AddComponent<InspectableDocument>().Configure(definition,"CCTV 출입 기록",new[]{"2002. 10. 11. / 입구 카메라\n\n최민수로 추정되는 남성이 현관으로 들어온다.\n이후 출구 화면에서는 같은 옷차림을 찾지 못했다.\n\n재생 위치 03:17\n\n화면 가장자리에 옅은 잡음이 남아 있다. 녹화 장치는 정상 동작한 것으로 기록되어 있다."},"CCTVInspected");
        LightAt("LobbyWarmLight",new Vector3(1.2f,2.48f,-2),new Color(1,.76f,.48f),3.5f,6,false);
        LightAt("ReceptionFluorescent",new Vector3(-1.5f,2.5f,1.5f),new Color(.73f,.87f,.75f),4,5,true);
        LightAt("OfficeLamp",new Vector3(-4.5f,2.48f,2),new Color(.7f,.83f,.75f),3,4,false);
        Corridor("FirstFloor",0,3,10);
        Label("StairSign","객실 4층 ↑",new Vector3(0,2.1f,9.5f),Vector3.zero,.025f);
        Box("FirstFloorClock",new Vector3(1.025f,1.95f,6),new Vector3(.04f,.4f,.4f),enamel).AddComponent<InspectableNote>().Configure("시계 바늘은 열한 시 이십 분을 가리킨다. 초침은 움직이지 않는다.");
        Label("ClockDial","11 : 20",new Vector3(.995f,1.95f,6),new Vector3(0,90,0),.015f);
        var extinguisher=Box("FireExtinguisher",new Vector3(-.9f,.38f,7.8f),new Vector3(.23f,.65f,.23f),wood);
        extinguisher.AddComponent<InspectableNote>().Configure("소화기의 점검표에는 올해 날짜가 적혀 있다. 손잡이에는 먼지가 얇게 쌓여 있다.");
    }
    private static void Corridor(string name,float y,float start,float end)
    {
        Box(name+"Floor",new Vector3(0,y-.1f,(start+end)*.5f),new Vector3(2.2f,.2f,end-start),carpet);
        Box(name+"Ceiling",new Vector3(0,y+2.65f,(start+end)*.5f),new Vector3(2.4f,.15f,end-start),wallpaper);
        Box(name+"East",new Vector3(1.2f,y+1.3f,(start+end)*.5f),new Vector3(.2f,2.6f,end-start),wallpaper);
        if(y==0)Box(name+"West",new Vector3(-1.2f,y+1.3f,(start+end)*.5f),new Vector3(.2f,2.6f,end-start),wallpaper);
        for(float z=start+1;z<end;z+=3.5f)LightAt(name+"Light",new Vector3(0,y+2.45f,z),new Color(.9f,.78f,.56f),2.6f,4.5f,false);
    }
    private static void Stairs()
    {
        Box("StairwellWest",new Vector3(-1.8f,5.7f,12),new Vector3(.2f,11.6f,5.2f),wallpaper);
        Box("StairwellEast",new Vector3(1.8f,5.7f,12),new Vector3(.2f,11.6f,5.2f),wallpaper);
        Box("StairwellBack",new Vector3(0,5.7f,14.5f),new Vector3(3.6f,11.6f,.2f),wallpaper);
        Box("StairwellCeiling",new Vector3(0,11.5f,12),new Vector3(3.6f,.2f,5.2f),wallpaper);
        Box("StairMiddleDivider",new Vector3(0,5.7f,11.75f),new Vector3(.09f,11.4f,2.9f),steel);
        for(int level=0;level<3;level++)
        {
            float y=level*3;
            Box("FloorLanding_"+(level+1),new Vector3(0,y-.1f,9.9f),new Vector3(3.4f,.2f,.8f),tile);
            Box("HalfLanding_"+level,new Vector3(0,y+1.4f,13.78f),new Vector3(3.4f,.2f,1.3f),tile);
            for(int i=0;i<10;i++)
            {
                Box("StairUp_"+level+"_"+i,new Vector3(-.88f,y+(i+1)*.15f-.12f,10.45f+i*.28f),new Vector3(1.62f,.24f,.28f),tile);
                Box("StairReturn_"+level+"_"+i,new Vector3(.88f,y+1.5f+(i+1)*.15f-.12f,12.97f-i*.28f),new Vector3(1.62f,.24f,.28f),tile);
            }
            Label("LandingLevel",(level+1)+"F",new Vector3(-1.68f,y+1.75f,9.9f),new Vector3(0,-90,0),.04f);
            // Mount below the next half-landing slab, or the roof on the top flight.
            float fixtureHeight=level<2?y+4.23f:11.33f;
            LightAt("StairFluorescent",new Vector3(0,fixtureHeight,13.8f),new Color(.72f,.82f,.76f),3.3f,5,false);
        }
        Box("FourthLanding",new Vector3(0,8.9f,9.9f),new Vector3(3.4f,.2f,.8f),tile);
        // Prevent walking into the stairwell void from the sides of landings.
        for(int level=1;level<=2;level++)Box("LandingRearGuard",new Vector3(0,level*3+.55f,9.47f),new Vector3(3.3f,1.1f,.06f),steel);
        foreach(float x in new[]{-1.4f,1.4f})Box("FourthLandingSideGuard",new Vector3(x,9.55f,9.47f),new Vector3(.5f,1.1f,.06f),steel);
    }
    private static void FourthFloor()
    {
        Corridor("FourthFloor",9,-.5f,9.5f);
        float[] positions={7.8f,5.6f,3.4f,1.2f};
        float cursor=-.5f;
        foreach(float z in positions.OrderBy(v=>v))
        {
            float gapStart=z-.55f;
            if(gapStart>cursor)Box("FourthWestWall",new Vector3(-1.2f,10.3f,(cursor+gapStart)/2),new Vector3(.2f,2.6f,gapStart-cursor),wallpaper);
            Box("DoorLintel",new Vector3(-1.2f,11.3f,z),new Vector3(.2f,.6f,1.1f),wallpaper);cursor=z+.55f;
        }
        Box("FourthWestWall",new Vector3(-1.2f,10.3f,(cursor+9.5f)/2),new Vector3(.2f,2.6f,9.5f-cursor),wallpaper);
        for(int i=0;i<4;i++)Door(401+i,positions[i],i==3);
        Box("EmergencyExitDoor",new Vector3(0,10.1f,-.6f),new Vector3(1.05f,2.2f,.09f),steel).AddComponent<InspectableNote>().Configure("비상구 문은 안쪽 걸쇠로 잠겨 있다. 문틈으로 차가운 공기만 들어온다.");
        Box("ExitEndWallLeft",new Vector3(-.85f,10.3f,-.65f),new Vector3(.55f,2.6f,.2f),wallpaper);
        Box("ExitEndWallRight",new Vector3(.85f,10.3f,-.65f),new Vector3(.55f,2.6f,.2f),wallpaper);
        Label("ExitSign","비상구 EXIT",new Vector3(0,11.43f,-.5f),new Vector3(0,180,0),.02f);
        Label("FourthLevel","4F",new Vector3(.98f,10.8f,8.8f),new Vector3(0,90,0),.035f);
        var audio=new GameObject("DistantDoorLatch").AddComponent<AudioSource>();audio.transform.position=new Vector3(-1.3f,10.2f,5.6f);audio.clip=CreateLatch();audio.spatialBlend=1;audio.volume=.2f;audio.minDistance=2;audio.maxDistance=16;audio.rolloffMode=AudioRolloffMode.Linear;audio.playOnAwake=false;
        Trigger("QuietLandingCue",new Vector3(0,3.8f,9.9f),new Vector3(3.2f,1.5f,.6f),"DistantLatchPlayed",audio);
    }
    private static void Door(int room,float z,bool enterable)
    {
        var root=new GameObject("RoomDoor"+room);root.transform.SetParent(environment,false);root.transform.position=new Vector3(-1.13f,9,z);
        var hinge=new GameObject("DoorHinge").transform;hinge.SetParent(root.transform,false);hinge.localPosition=new Vector3(0,0,-.5f);
        Box("DoorLeaf",hinge,new Vector3(0,1.05f,.5f),new Vector3(.09f,2.1f,1),wood);
        Box("DoorHandle",hinge,new Vector3(.09f,1,.85f),new Vector3(.09f,.035f,.12f),steel,false);
        var plaque=Box("NumberPlate",hinge,new Vector3(.058f,1.65f,.5f),new Vector3(.012f,.18f,.32f),enamel,false);
        Label("RoomNumber",hinge,room.ToString(),new Vector3(.07f,1.65f,.5f),new Vector3(0,-90,0),.022f).color=new Color(.05f,.045f,.03f);
        root.AddComponent<InspectableDoor>().Configure(hinge,definition,enterable?"Room404KeyTaken":"",enterable?"404호 문이 잠겨 있다. 프런트의 객실 키를 확인해 볼 수 있다.":room+"호 문은 잠겨 있다. 내부에는 인기척이 없다.",!enterable);
        if(!enterable){plaque.AddComponent<BoxCollider>();plaque.AddComponent<InspectableNote>().Configure("낡은 번호판에는 "+room+"이라고 적혀 있다. 나사가 한쪽으로 기울어져 있다.");}
    }
    private static void Room404()
    {
        Box("Room404Floor",new Vector3(-3.1f,8.9f,1.2f),new Vector3(3.8f,.2f,4.2f),carpet);
        Box("Room404Ceiling",new Vector3(-3.1f,11.65f,1.2f),new Vector3(3.8f,.15f,4.2f),wallpaper);
        Box("Room404West",new Vector3(-5,10.3f,1.2f),new Vector3(.2f,2.6f,4.2f),wallpaper);
        Box("Room404North",new Vector3(-3.1f,10.3f,3.3f),new Vector3(3.8f,2.6f,.2f),wallpaper);
        Box("Room404South",new Vector3(-3.1f,10.3f,-.9f),new Vector3(3.8f,2.6f,.2f),wallpaper);
        Box("BedBase",new Vector3(-3.8f,9.23f,1.6f),new Vector3(1.7f,.46f,2.1f),wood).AddComponent<InspectableNote>().Configure("이불의 한쪽만 접혀 있다. 베개에는 눌린 자국이 남아 있다.",definition,"BedInspected");
        Box("BedCover",new Vector3(-3.8f,9.51f,1.6f),new Vector3(1.72f,.12f,2.08f),enamel,false);
        Box("Pillow",new Vector3(-3.8f,9.61f,2.3f),new Vector3(.75f,.12f,.45f),paper,false);
        Box("Nightstand",new Vector3(-2.55f,9.3f,2.6f),new Vector3(.55f,.6f,.55f),wood).AddComponent<InspectableNote>().Configure("협탁 서랍에는 비닐로 포장된 빗과 빈 성냥갑이 있다. 안내 책자는 보이지 않는다.");
        var phone=Prop("WiredTelephone",new Vector3(-2.55f,9.68f,2.6f));phone.transform.localScale=Vector3.one*.75f;phone.AddComponent<InspectableNote>().Configure("전화기 밑에 접힌 메모가 끼어 있다. 안내 번호는 지워져 있고, 수화기 선은 그대로 연결되어 있다.",definition,"RoomPhoneInspected");
        Box("BathroomDoor",new Vector3(-2.1f,10.05f,-.77f),new Vector3(.85f,2.1f,.08f),enamel).AddComponent<InspectableNote>().Configure("욕실 문은 안쪽에서 걸려 있다. 바닥 아래로 마른 물자국이 이어진다.");
        Label("BathSign","욕실",new Vector3(-2.1f,10.7f,-.71f),new Vector3(0,180,0),.017f);
        Box("GuestBag",new Vector3(-4.35f,9.2f,-.35f),new Vector3(.55f,.4f,.36f),black).AddComponent<InspectableNote>().Configure("작은 가방 안에는 갈아입을 옷과 버스표가 있다. 이름표에는 최민수라고 적혀 있다.",definition,"BelongingsInspected");
        Box("WritingDesk",new Vector3(-2.2f,9.38f,-.05f),new Vector3(.75f,.76f,.5f),wood);
        var record=Box("Room404Paper",new Vector3(-2.2f,9.78f,-.05f),new Vector3(.42f,.018f,.32f),paper);
        Label("RoomPaperLabel","개인 메모",new Vector3(-2.2f,9.793f,-.05f),new Vector3(90,0,0),.012f).color=new Color(.05f,.04f,.03f);
        record.AddComponent<InspectableDocument>().Configure(definition,"404호에서 회수한 메모",new[]{"2002. 10. 11. / 작은 수첩에서 찢은 종이\n\n'접수대에서 적어 준 방과 열쇠에 적힌 방을 확인할 것.'\n'짐은 그대로 두었다. 아침에 다시 내려가 물어보자.'\n\n아랫부분에 최민수라는 이름이 적혀 있다. 종이에는 다른 객실 번호가 직접 적혀 있지 않다.\n\n확인한 현장 기록은 TAB으로 다시 열람할 수 있다."},"Room404PaperInspected");
        Trigger("Room404Entry",new Vector3(-1.85f,9.9f,1.2f),new Vector3(.65f,1.8f,1),"Room404Entered");
        LightAt("Room404WarmLamp",new Vector3(-2.1f,11.38f,.8f),new Color(1,.77f,.48f),3.6f,4.5f,true);
    }
    private static void Trigger(string name,Vector3 position,Vector3 size,string flag,AudioSource cue=null)
    {var go=new GameObject(name);go.transform.position=position;var collider=go.AddComponent<BoxCollider>();collider.size=size;collider.isTrigger=true;go.AddComponent<CaseProgressTrigger>().Configure(definition,flag,cue);}
    [MenuItem("Archive 03:17/Add Motel Details")]
    public static void AddDetailsToExistingScene()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
        var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        if(GameObject.Find("MotelDetails")!=null)return;
        environment=GameObject.Find("MotelEnvironment").transform;
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansCJKkr-Regular.otf");
        definition=AssetDatabase.LoadAssetAtPath<CaseDefinition>(DefinitionPath);
        wood=LoadMaterial("OfficeLaminate");steel=LoadMaterial("PaintedSteel");paper=LoadMaterial("YellowedPaper");black=LoadMaterial("Bakelite");enamel=LoadMaterial("BeigeEnamel");
        Details();ConfigureWorldText();EditorSceneManager.SaveScene(scene);
    }
    private static void Details()
    {
        var baseRoot=environment; environment=new GameObject("MotelDetails").transform;environment.SetParent(baseRoot,false);
        var damp=LoadMaterial("DampPaint");
        const string redPath="Assets/Materials/Motel/FireSafetyRed.mat";
        var red=AssetDatabase.LoadAssetAtPath<Material>(redPath);
        if(red==null){red=new Material(Shader.Find("Universal Render Pipeline/Lit"));red.SetColor("_BaseColor",new Color(.32f,.07f,.045f));red.SetFloat("_Smoothness",.04f);AssetDatabase.CreateAsset(red,redPath);}
        GameObject.Find("FireExtinguisher").GetComponent<Renderer>().sharedMaterial=red;
        Box("ExtinguisherHandle",new Vector3(-.9f,.75f,7.8f),new Vector3(.27f,.055f,.08f),steel,false);
        Box("ExtinguisherTag",new Vector3(-.765f,.4f,7.8f),new Vector3(.015f,.27f,.13f),paper,false);
        // Low wall strips and exposed services suggest continued use rather than a ruin.
        foreach(float x in new[]{-3.39f,3.39f})Box("LobbySkirting",new Vector3(x,.17f,-2),new Vector3(.035f,.32f,5.8f),damp,false);
        foreach(float y in new[]{0f,9f})
        {
            Box("CorridorSkirting",new Vector3(1.087f,y+.12f,5),new Vector3(.025f,.24f,8.6f),damp,false);
            Box("CeilingCableDuct",new Vector3(1.05f,y+2.37f,5),new Vector3(.06f,.075f,8.6f),steel,false);
            for(int i=0;i<3;i++)Box("DampPatch",new Vector3(1.085f,y+.52f+i*.13f,2+i*2.1f),new Vector3(.015f,.35f,.48f+i*.15f),damp,false);
        }
        for(int i=0;i<4;i++)
        {
            float z=7.8f-i*2.2f;
            foreach(float edge in new[]{z-.55f,z+.55f})Box("GuestDoorFrame",new Vector3(-1.08f,10.05f,edge),new Vector3(.075f,2.1f,.065f),enamel,false);
            Box("GuestDoorFrameHeader",new Vector3(-1.08f,11.09f,z),new Vector3(.075f,.08f,1.15f),enamel,false);
        }
        var board=Prop("OldNoticeBoard",new Vector3(3.35f,1.65f,-1.4f));board.transform.rotation=Quaternion.Euler(0,90,0);
        foreach(var text in board.GetComponentsInChildren<TextMesh>())text.text="은성 모텔\n장기 투숙 문의\n퇴실 오전 11시";
        board.AddComponent<InspectableNote>().Configure("퇴실 시간은 오전 11시. 장기 투숙 안내 종이는 새것에 가깝지만 모서리에 습기가 배어 있다.");
        var chair=Prop("OfficeChair",new Vector3(-4.55f,0,1.7f));chair.transform.rotation=Quaternion.Euler(0,-12,0);
        Box("LobbyWaitingBench",new Vector3(2.85f,.42f,-3.15f),new Vector3(.65f,.18f,1.9f),wood);
        Box("WaitingBenchBack",new Vector3(3.12f,.72f,-3.15f),new Vector3(.12f,.65f,1.9f),black);
        foreach(float z in new[]{-3.85f,-2.45f})Box("WaitingBenchLeg",new Vector3(2.85f,.19f,z),new Vector3(.38f,.38f,.12f),steel);
        for(int i=0;i<3;i++)Box("ReceptionReceiptStack",new Vector3(-.72f+i*.014f,1.17f+i*.012f,-.23f),new Vector3(.24f,.012f,.33f),paper,false).transform.rotation=Quaternion.Euler(0,-7+i*4,0);
        Box("ReceptionPen",new Vector3(-.53f,1.18f,-.55f),new Vector3(.017f,.017f,.16f),black,false);
        Box("OfficeFileStack",new Vector3(-4.3f,1.045f,2.8f),new Vector3(.2f,.07f,.28f),paper,false);
        Box("RoomBedHeadboard",new Vector3(-3.8f,9.69f,2.75f),new Vector3(1.8f,.9f,.08f),wood);
        Box("RoomSkirting",new Vector3(-4.885f,9.15f,1.2f),new Vector3(.025f,.3f,4),damp,false);
        Box("RoomWallStain",new Vector3(-3.8f,9.55f,3.185f),new Vector3(.8f,.5f,.015f),damp,false);
        Box("GuestBagHandle",new Vector3(-4.35f,9.44f,-.35f),new Vector3(.25f,.06f,.055f),enamel,false);
        Box("RoomWasteBin",new Vector3(-1.65f,9.2f,-.52f),new Vector3(.22f,.4f,.22f),black);
        var note=Box("ReceptionPaymentNotice",new Vector3(-1.2f,.82f,-.986f),new Vector3(.45f,.3f,.01f),paper,false);
        Label("PaymentNoticeText","현금 / 선불",new Vector3(-1.2f,.82f,-.997f),Vector3.zero,.011f).color=new Color(.08f,.07f,.05f);
        // Keep the distant cue as the only scripted atmosphere event.
        environment=baseRoot;
    }
    public static void ConfigureWorldText()
    {
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/WorldTextDepth.shader");
        foreach(var label in GameObject.Find("MotelEnvironment").GetComponentsInChildren<TextMesh>())
        {
            var depth=label.GetComponent<WorldTextDepth>();if(depth==null)depth=label.gameObject.AddComponent<WorldTextDepth>();depth.Configure(shader);
        }
    }
    public static void ConfigureNotebook(CaseDefinition data)
    {
        data.ConfigureNotebook(new[]{
            new CaseNotebookEntry{progressFlag="LedgerInspected",text="숙박 장부 세부 기록: 객실 {value}호",factKey="LedgerRoom"},
            new CaseNotebookEntry{progressFlag="CCTVInspected",text="출입 영상 확인"},
            new CaseNotebookEntry{progressFlag="Room404PaperInspected",text="객실에서 회수한 종이 기록 확인"}});
        EditorUtility.SetDirty(data);
    }
    private static Material LoadMaterial(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ArchiveRoom/Institutional/"+name+".mat");
    private static GameObject Prop(string name,Vector3 position)
    {var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Institutional/"+name+".prefab"));go.transform.SetParent(environment);go.transform.position=position;return go;}
    private static GameObject Box(string name,Vector3 position,Vector3 size,Material material,bool collision=true)=>Box(name,environment,position,size,material,collision);
    private static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,Material material,bool collision=true)
    {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;if(!collision)Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
    private static TextMesh Label(string name,string text,Vector3 position,Vector3 rotation,float size)=>Label(name,environment,text,position,rotation,size);
    private static TextMesh Label(string name,Transform parent,string text,Vector3 position,Vector3 rotation,float size)
    {var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localEulerAngles=rotation;var label=go.AddComponent<TextMesh>();label.font=font;label.fontSize=64;label.text=text;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=new Color(.72f,.72f,.61f);go.GetComponent<Renderer>().sharedMaterial=font.material;return label;}
    private static void LightAt(string name,Vector3 position,Color color,float intensity,float range,bool shadows)
    {Box(name+"Housing",position,new Vector3(.9f,.07f,.2f),steel,false);Box(name+"Tube",position+Vector3.down*.045f,new Vector3(.8f,.025f,.05f),tube,false);var go=new GameObject(name);go.transform.position=position+Vector3.down*.12f;go.transform.rotation=Quaternion.Euler(90,0,0);var light=go.AddComponent<Light>();light.type=LightType.Spot;light.spotAngle=110;light.intensity=intensity;light.range=range;light.color=color;light.shadows=shadows?LightShadows.Soft:LightShadows.None;}
    private static Text TextUI(string name,Transform parent,string content,Vector2 size,Vector2 position,int fontSize)
    {var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var text=go.GetComponent<Text>();text.font=font;text.text=content;text.fontSize=fontSize;text.color=new Color(.82f,.85f,.78f);text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.rectTransform.sizeDelta=size;text.rectTransform.anchoredPosition=position;return text;}
    private static Button ButtonUI(string name,Transform parent,string caption,Vector2 position,Vector2 size)
    {var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();rect.sizeDelta=size;rect.anchoredPosition=position;go.GetComponent<Image>().color=new Color(.18f,.24f,.23f);var button=go.GetComponent<Button>();button.targetGraphic=go.GetComponent<Image>();TextUI("Label",go.transform,caption,size,Vector2.zero,22);go.SetActive(false);return button;}
    private static Material TextureMaterial(string name,Color color,bool rough)
    {var texture=new Texture2D(128,128,TextureFormat.RGB24,false);var rng=new System.Random(404);for(int y=0;y<128;y++)for(int x=0;x<128;x++){float grain=(float)rng.NextDouble()*.14f-.07f;float stain=(x/13+y/17)%11==0?-.08f:0;float pattern=rough?0:(x%16<2?-.035f:0);texture.SetPixel(x,y,color*(1+grain+stain+pattern));}texture.Apply();string path="Assets/Textures/Motel/"+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.filterMode=FilterMode.Point;importer.mipmapEnabled=true;importer.maxTextureSize=128;importer.SaveAndReimport();var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));material.SetFloat("_Smoothness",.03f);material.SetTextureScale("_BaseMap",new Vector2(2,2));AssetDatabase.CreateAsset(material,"Assets/Materials/Motel/"+name+".mat");return material;}
    private static AudioClip CreateLatch()
    {const int rate=22050;int samples=rate/2;string path="Assets/Audio/Atmosphere/DistantLatch.wav";using(var writer=new BinaryWriter(File.Open(path,FileMode.Create))){writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(samples*2);var rng=new System.Random(404);for(int i=0;i<samples;i++){float t=i/(float)rate;float envelope=Mathf.Min(t/.025f,1)*Mathf.Exp(-t*13);float sound=(Mathf.Sin(t*170*Mathf.PI*2)*.22f+((float)rng.NextDouble()*2-1)*.08f)*envelope;writer.Write((short)(sound*short.MaxValue));}}AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);}
}
