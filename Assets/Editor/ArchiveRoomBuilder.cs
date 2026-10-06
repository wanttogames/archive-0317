using System.IO;
using System.Linq;
using Archive0317;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static class ArchiveRoomBuilder
{
    private const string ScenePath = "Assets/Scenes/ArchiveRoom.unity";
    private static Font font;
    private static Material metal, wall, floor, paper, dark, glow;

    [MenuItem("Archive 03:17/Build ArchiveRoom Prototype")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before building.");
        if (File.Exists(ScenePath)) throw new System.InvalidOperationException("ArchiveRoom already exists; edit the existing scene instead of rebuilding.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        foreach (var scene in Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount).Select(UnityEngine.SceneManagement.SceneManager.GetSceneAt))
        {
            bool changed = false;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var child in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "MCP_Test_Cube").ToArray())
                { Object.DestroyImmediate(child.gameObject); changed = true; }
            if (changed && !string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
        }
        foreach (string folder in new[] { "Assets/Scenes", "Assets/Prefabs/Player", "Assets/Prefabs/Environment", "Assets/Prefabs/Interaction", "Assets/Materials/ArchiveRoom", "Assets/Fonts" }) Directory.CreateDirectory(folder);
        string fontPath = "Assets/Fonts/NotoSansCJKkr-Regular.otf";
        if (!File.Exists(fontPath)) throw new System.InvalidOperationException("The bundled Noto Korean font is missing.");
        AssetDatabase.Refresh();
        font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        metal = Mat("ColdSteel", new Color(.16f,.21f,.23f), .55f, .27f);
        wall = Mat("Concrete", new Color(.28f,.33f,.34f), 0, .12f);
        floor = Mat("Floor", new Color(.12f,.16f,.17f), .12f, .28f);
        paper = Mat("ArchivePaper", new Color(.52f,.49f,.36f), 0, .08f);
        dark = Mat("DarkTrim", new Color(.035f,.05f,.06f), .35f, .2f);
        glow = Mat("Fluorescent", new Color(.64f,.86f,.9f), 0, .4f);
        glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor", new Color(.65f,.85f,.95f)*2);

        var sceneNew = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var room = new GameObject("ArchiveRoom_Geometry").transform;
        Box("Floor", room, new Vector3(0,-.15f,0), new Vector3(10,.3f,12), floor);
        Box("Ceiling", room, new Vector3(0,3.4f,0), new Vector3(10,.25f,12), wall);
        Box("NorthWall", room, new Vector3(0,1.65f,6), new Vector3(10,3.3f,.3f), wall);
        Box("SouthWall", room, new Vector3(0,1.65f,-6), new Vector3(10,3.3f,.3f), wall);
        Box("WestWall", room, new Vector3(-5,1.65f,0), new Vector3(.3f,3.3f,12), wall);
        Box("EastWall", room, new Vector3(5,1.65f,0), new Vector3(.3f,3.3f,12), wall);
        for (int z=-5; z<=5; z++) Box("FloorSeam", room, new Vector3(0,.003f,z), new Vector3(9.7f,.005f,.014f), dark, false);
        for (int x=-4; x<=4; x++) Box("FloorSeam", room, new Vector3(x,.004f,0), new Vector3(.014f,.005f,11.7f), dark, false);
        Box("Door", room, new Vector3(0,1.3f,-5.81f), new Vector3(1.5f,2.6f,.08f), metal);
        Box("DoorHandle", room, new Vector3(.5f,1.2f,-5.7f), new Vector3(.08f,.26f,.08f), paper);
        Label("DoorSign", room, "ARCHIVE / 03:17", new Vector3(0,2.9f,-5.75f), new Vector3(0,180,0), .022f);
        Label("ArchiveSign", room, "ARCHIVE 03:17\nRECORDS DIVISION", new Vector3(0,2.85f,5.81f), Vector3.zero, .025f);
        var desk = new GameObject("InvestigationDesk");
        Box("Desktop", desk.transform, new Vector3(0,1.03f,1), new Vector3(2.8f,.16f,1.3f), metal);
        foreach (float x in new[]{-1.2f,1.2f}) foreach(float z in new[]{.5f,1.5f}) Box("Leg",desk.transform,new Vector3(x,.48f,z),new Vector3(.09f,.96f,.09f),dark);
        Box("Drawer",desk.transform,new Vector3(-.8f,.77f,1),new Vector3(.7f,.4f,1),dark);
        Box("DocumentStack",desk.transform,new Vector3(-.75f,1.16f,1.1f),new Vector3(.45f,.1f,.6f),paper);
        PrefabUtility.SaveAsPrefabAssetAndConnect(desk,"Assets/Prefabs/Environment/InvestigationDesk.prefab",InteractionMode.AutomatedAction);
        var file = new GameObject("CASE 001");
        file.transform.position = new Vector3(.45f,1.15f,.72f);
        Box("Folder",file.transform,Vector3.zero,new Vector3(.55f,.055f,.72f),paper);
        Box("RedactedBand",file.transform,new Vector3(0,.03f,.16f),new Vector3(.48f,.006f,.075f),dark,false);
        Label("CaseLabel",file.transform,"CASE 001\n03:17 / RESTRICTED",new Vector3(0,.033f,-.05f),new Vector3(90,0,0),.005f);
        file.GetComponentInChildren<TextMesh>().color=new Color(.04f,.06f,.06f);
        file.AddComponent<CaseFile>();
        PrefabUtility.SaveAsPrefabAssetAndConnect(file,"Assets/Prefabs/Interaction/Case001.prefab",InteractionMode.AutomatedAction);

        var shelf = new GameObject("CaseStorageShelf");
        foreach(float x in new[]{-.85f,.85f}) foreach(float z in new[]{-.32f,.32f}) Box("Upright",shelf.transform,new Vector3(x,1.25f,z),new Vector3(.06f,2.5f,.06f),metal);
        for(int level=0;level<5;level++)
        {
            float y=.18f+level*.5f;
            Box("Shelf",shelf.transform,new Vector3(0,y,0),new Vector3(1.85f,.055f,.76f),metal);
            if(level<4) for(int book=0;book<9;book++)
            {
                float x=-.72f+book*.17f;
                Box("ArchiveBinder",shelf.transform,new Vector3(x,y+.22f,0),new Vector3(.12f,.38f,.48f),book%3==0?paper:dark);
                Box("SpineLabel",shelf.transform,new Vector3(x,y+.23f,-.246f),new Vector3(.075f,.1f,.009f),paper,false);
            }
        }
        var shelfPrefab=PrefabUtility.SaveAsPrefabAsset(shelf,"Assets/Prefabs/Environment/CaseStorageShelf.prefab");
        Object.DestroyImmediate(shelf);
        foreach(float x in new[]{-3.3f,-1.1f,1.1f,3.3f}) { var instance=(GameObject)PrefabUtility.InstantiatePrefab(shelfPrefab); instance.transform.position=new Vector3(x,0,5.25f); }
        foreach(float z in new[]{-2.5f,.2f,2.9f}) { var instance=(GameObject)PrefabUtility.InstantiatePrefab(shelfPrefab); instance.transform.position=new Vector3(-4.35f,0,z); instance.transform.rotation=Quaternion.Euler(0,90,0); }

        var lights=new GameObject("Lighting").transform;
        foreach(float z in new[]{-3f,1f,4.4f})
        {
            var fixture=new GameObject("FluorescentFixture"); fixture.transform.SetParent(lights); fixture.transform.position=new Vector3(1.2f,3.2f,z);
            Box("Housing",fixture.transform,Vector3.zero,new Vector3(1.6f,.12f,.32f),dark);
            foreach(float tube in new[]{-.09f,.09f}) Box("Tube",fixture.transform,new Vector3(0,-.07f,tube),new Vector3(1.45f,.035f,.035f),glow,false);
            var lightGO=new GameObject("ColdLight"); lightGO.transform.SetParent(fixture.transform,false); lightGO.transform.localPosition=new Vector3(0,-.16f,0);
            var light=lightGO.AddComponent<Light>(); light.type=LightType.Spot; lightGO.transform.localRotation=Quaternion.Euler(90,0,0); light.spotAngle=115; light.color=new Color(.65f,.82f,.92f); light.intensity=5f; light.range=6; light.shadows=LightShadows.Soft;
        }
        RenderSettings.skybox=null; RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.1f,.13f,.15f);
        RenderSettings.fog=true; RenderSettings.fogColor=new Color(.035f,.052f,.062f); RenderSettings.fogMode=FogMode.Exponential; RenderSettings.fogDensity=.023f;
        var volumeGO=new GameObject("Atmosphere"); var volume=volumeGO.AddComponent<Volume>(); volume.isGlobal=true;
        var profile=ScriptableObject.CreateInstance<VolumeProfile>();
        var vignette=profile.Add<Vignette>(true); vignette.intensity.Override(.3f); vignette.smoothness.Override(.65f);
        var color=profile.Add<ColorAdjustments>(true); color.saturation.Override(-25); color.contrast.Override(10);
        AssetDatabase.CreateAsset(profile,"Assets/Materials/ArchiveRoom/Atmosphere.asset"); volume.sharedProfile=profile;

        var playerGO=new GameObject("FirstPersonPlayer"); playerGO.transform.position=new Vector3(0,.05f,-3.3f);
        var controller=playerGO.AddComponent<CharacterController>(); controller.height=1.8f; controller.center=new Vector3(0,.9f,0); controller.radius=.3f; controller.stepOffset=.25f; controller.skinWidth=.03f;
        var player=playerGO.AddComponent<FirstPersonPlayer>();
        var cameraGO=new GameObject("PlayerCamera"); cameraGO.tag="MainCamera"; cameraGO.transform.SetParent(playerGO.transform,false); cameraGO.transform.localPosition=new Vector3(0,1.62f,0);
        var camera=cameraGO.AddComponent<Camera>(); camera.nearClipPlane=.05f; camera.farClipPlane=40; camera.fieldOfView=72; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=RenderSettings.fogColor;
        cameraGO.AddComponent<AudioListener>(); cameraGO.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;
        var ui=new GameObject("ArchiveHUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(ArchiveHUD)); ui.transform.SetParent(playerGO.transform,false);
        ui.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=ui.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
        var hud=ui.GetComponent<ArchiveHUD>();
        var aim=Panel("Crosshair",ui.transform,new Vector2(4,4),Vector2.zero,new Color(.8f,.89f,.9f,.9f));
        var prompt=TextUI("InteractionPrompt",ui.transform,"E 조사",new Vector2(300,50),new Vector2(0,-56),24,TextAnchor.MiddleCenter);
        var controls=TextUI("Controls",ui.transform,"WASD 이동   ·   SHIFT 달리기   ·   E 조사   ·   ESC 커서",new Vector2(900,45),new Vector2(0,40),18,TextAnchor.MiddleCenter);
        controls.rectTransform.anchorMin=controls.rectTransform.anchorMax=new Vector2(.5f,0);
        var brand=TextUI("ArchiveBrand",ui.transform,"ARCHIVE 03:17  /  RECORDS ROOM",new Vector2(800,50),new Vector2(0,-40),20,TextAnchor.MiddleCenter);
        brand.rectTransform.anchorMin=brand.rectTransform.anchorMax=new Vector2(.5f,1);
        var hint=TextUI("CursorHint",ui.transform,"마우스 잠금 해제됨\n클릭 또는 ESC로 계속",new Vector2(600,100),new Vector2(0,-100),24,TextAnchor.MiddleCenter);
        var backdrop=Panel("CaseDescription",ui.transform,new Vector2(1920,1080),Vector2.zero,new Color(.015f,.025f,.033f,.93f));
        var card=Panel("Document",backdrop.transform,new Vector2(850,690),Vector2.zero,new Color(.09f,.12f,.14f,1));
        TextUI("DocumentHeader",card.transform,"ARCHIVE / RESTRICTED ACCESS",new Vector2(730,40),new Vector2(0,280),20,TextAnchor.MiddleLeft);
        var title=TextUI("Title",card.transform,"",new Vector2(730,70),new Vector2(0,210),32,TextAnchor.MiddleLeft);
        var body=TextUI("Description",card.transform,"",new Vector2(730,380),new Vector2(0,-35),24,TextAnchor.UpperLeft);
        TextUI("CloseHint",card.transform,"E 또는 ESC — 파일 닫기",new Vector2(730,45),new Vector2(0,-285),22,TextAnchor.MiddleLeft);
        hud.Configure(backdrop,aim,prompt,title,body,hint,player); player.Configure(camera,hud); backdrop.SetActive(false); prompt.gameObject.SetActive(false); hint.gameObject.SetActive(false);
        PrefabUtility.SaveAsPrefabAssetAndConnect(playerGO,"Assets/Prefabs/Player/FirstPersonPlayer.prefab",InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(sceneNew,ScenePath);
        var existing=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).ToList(); existing.Insert(0,new EditorBuildSettingsScene(ScenePath,true)); EditorBuildSettings.scenes=existing.ToArray();
        AssetDatabase.SaveAssets(); Selection.activeGameObject=playerGO;
        Debug.Log("ArchiveRoom created and saved. Player, desk, shelves and Case001 prefabs created.");
    }
    private static Material Mat(string name, Color color, float metallic, float smoothness)
    {
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit")); material.name=name; material.SetColor("_BaseColor",color); material.SetFloat("_Metallic",metallic); material.SetFloat("_Smoothness",smoothness);
        AssetDatabase.CreateAsset(material,"Assets/Materials/ArchiveRoom/"+name+".mat"); return material;
    }
    private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool collision=true)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=size; go.GetComponent<Renderer>().sharedMaterial=material;
        if(!collision) Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
    }
    private static void Label(string name, Transform parent, string content, Vector3 position, Vector3 rotation, float size)
    {
        var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localEulerAngles=rotation;
        var text=go.AddComponent<TextMesh>(); text.text=content; text.font=font; text.fontSize=64; text.characterSize=size; text.anchor=TextAnchor.MiddleCenter; text.alignment=TextAlignment.Center; text.color=new Color(.74f,.8f,.78f);
        go.GetComponent<MeshRenderer>().sharedMaterial=font.material;
    }
    private static GameObject Panel(string name,Transform parent,Vector2 size,Vector2 position,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false); var rect=go.GetComponent<RectTransform>(); rect.sizeDelta=size; rect.anchoredPosition=position; go.GetComponent<Image>().color=color; go.GetComponent<Image>().raycastTarget=false; return go;
    }
    private static Text TextUI(string name,Transform parent,string content,Vector2 size,Vector2 position,int fontSize,TextAnchor alignment)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false); var rect=go.GetComponent<RectTransform>(); rect.sizeDelta=size; rect.anchoredPosition=position;
        var text=go.GetComponent<Text>(); text.font=font; text.fontSize=fontSize; text.text=content; text.alignment=alignment; text.color=new Color(.78f,.86f,.87f); text.raycastTarget=false; return text;
    }
}
