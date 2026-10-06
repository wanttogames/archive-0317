using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// Editor-only art pass: keep the existing scene and gameplay components intact.
public static class ArchiveRoomVisualPass
{
    private const string RootName = "InstitutionalDetails";
    private static Material plaster, lowerWall, tile, steel, wood, cardboard, paper, black, enamel, tube, screen, green;
    private static Font font;
    private static System.Random random;

    [MenuItem("Archive 03:17/Apply Basement Visual Direction")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/ArchiveRoom.unity")
            throw new InvalidOperationException("Open ArchiveRoom in Edit Mode first.");
        if (GameObject.Find(RootName)) { Debug.Log("Basement art pass is already present."); return; }
        random = new System.Random(317);
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansCJKkr-Regular.otf");
        Directory.CreateDirectory("Assets/Textures/ArchiveRoom");
        Directory.CreateDirectory("Assets/Materials/ArchiveRoom/Institutional");
        Directory.CreateDirectory("Assets/Prefabs/Environment/Institutional");
        AssetDatabase.Refresh();
        plaster = Surface("AgedPlaster",new Color(.52f,.53f,.46f),0);
        lowerWall = Surface("DampPaint",new Color(.24f,.32f,.29f),1);
        tile = Surface("WornLinoleum",new Color(.34f,.38f,.32f),2);
        steel = Surface("PaintedSteel",new Color(.25f,.31f,.29f),1);
        wood = Surface("OfficeLaminate",new Color(.33f,.25f,.17f),3);
        cardboard = Surface("DocumentCardboard",new Color(.48f,.41f,.28f),1);
        paper = Surface("YellowedPaper",new Color(.65f,.6f,.44f),0);
        black = Surface("Bakelite",new Color(.045f,.052f,.045f),0);
        enamel = Surface("BeigeEnamel",new Color(.51f,.5f,.39f),0);
        tube = Flat("FluorescentTube",new Color(.65f,.77f,.62f));
        screen = Flat("CRTGlass",new Color(.035f,.075f,.053f));
        green = Flat("ExitGreen",new Color(.13f,.29f,.2f));

        var room = GameObject.Find("ArchiveRoom_Geometry").transform;
        Resize(room,"Floor",new Vector3(0,-.15f,0),new Vector3(8,.3f,9),tile);
        Resize(room,"Ceiling",new Vector3(0,2.88f,0),new Vector3(8,.25f,9),plaster);
        Resize(room,"NorthWall",new Vector3(0,1.375f,4.5f),new Vector3(8,2.75f,.3f),plaster);
        Resize(room,"SouthWall",new Vector3(0,1.375f,-4.5f),new Vector3(8,2.75f,.3f),plaster);
        Resize(room,"WestWall",new Vector3(-4,1.375f,0),new Vector3(.3f,2.75f,9),plaster);
        Resize(room,"EastWall",new Vector3(4,1.375f,0),new Vector3(.3f,2.75f,9),plaster);
        foreach(var seam in room.Cast<Transform>().Where(t=>t.name=="FloorSeam").ToArray()) Object.DestroyImmediate(seam.gameObject);
        Resize(room,"Door",new Vector3(-.65f,1.12f,-4.32f),new Vector3(1.25f,2.24f,.09f),steel);
        Resize(room,"DoorHandle",new Vector3(-.18f,1.1f,-4.23f),new Vector3(.065f,.22f,.08f),black);
        var sign=room.Find("DoorSign"); sign.localPosition=new Vector3(-.65f,2.46f,-4.28f); sign.localRotation=Quaternion.Euler(0,180,0);
        sign.GetComponent<TextMesh>().text="지하 1층 · 기록보관실"; sign.GetComponent<TextMesh>().characterSize=.017f;
        sign=room.Find("ArchiveSign"); sign.localPosition=new Vector3(0,2.55f,4.3f); sign.GetComponent<TextMesh>().text="기록물 보존구역\n관계자 외 출입금지"; sign.GetComponent<TextMesh>().characterSize=.017f;

        var detail = new GameObject(RootName).transform;
        Box("NorthDampBand",detail,new Vector3(0,.45f,4.32f),new Vector3(7.7f,.9f,.035f),lowerWall);
        Box("SouthDampBand",detail,new Vector3(0,.45f,-4.32f),new Vector3(7.7f,.9f,.035f),lowerWall);
        Box("WestDampBand",detail,new Vector3(-3.82f,.45f,0),new Vector3(.035f,.9f,8.7f),lowerWall);
        Box("EastDampBand",detail,new Vector3(3.82f,.45f,0),new Vector3(.035f,.9f,8.7f),lowerWall);
        // Door face sits in front of the wall paint; the paint strip never blocks the doorway interior.
        Box("SupportColumn",detail,new Vector3(2.85f,1.375f,2.6f),new Vector3(.42f,2.75f,.48f),plaster);
        Box("ArchivePartition",detail,new Vector3(3.45f,1.375f,2.6f),new Vector3(.8f,2.75f,.15f),plaster);
        Box("CeilingBeam",detail,new Vector3(0,2.63f,2.6f),new Vector3(7.7f,.25f,.45f),lowerWall);
        Box("CableDuct",detail,new Vector3(3.67f,2.48f,0),new Vector3(.12f,.14f,8.3f),enamel);
        for(int i=0;i<7;i++) Box("DuctJoint",detail,new Vector3(3.59f,2.48f,-3.8f+i*1.2f),new Vector3(.018f,.16f,.035f),steel,false);
        Box("VentGrille",detail,new Vector3(3.73f,2.18f,-2.5f),new Vector3(.075f,.45f,.65f),black);
        for(int i=0;i<7;i++) Box("VentSlat",detail,new Vector3(3.68f,2.02f+i*.055f,-2.5f),new Vector3(.045f,.018f,.61f),steel,false);
        for(int i=0;i<15;i++)
        {
            float x=(float)random.NextDouble()*7-3.5f;
            var stain=Box("CeilingMoisture",detail,new Vector3(x,2.746f,(float)random.NextDouble()*8-4),new Vector3(.3f+(float)random.NextDouble(),.006f,.15f+(float)random.NextDouble()*.4f),lowerWall,false);
            stain.transform.localRotation=Quaternion.Euler(0,random.Next(-15,15),0);
        }
        var shelves=scene.GetRootGameObjects().Where(g=>g.name=="CaseStorageShelf").ToArray();
        float[] back={-2.85f,-.95f,.95f,2.85f};
        for(int i=0;i<shelves.Length;i++)
        {
            shelves[i].transform.position=i<4?new Vector3(back[i],0,3.93f):new Vector3(-3.42f,0,-2.5f+(i-4)*2.2f);
            foreach(var renderer in shelves[i].GetComponentsInChildren<MeshRenderer>())
            {
                if(renderer.name=="Shelf"||renderer.name=="Upright") renderer.sharedMaterial=steel;
                if(renderer.name=="ArchiveBinder")
                {
                    renderer.transform.localPosition+=new Vector3(0,0,(float)random.NextDouble()*.13f-.065f);
                    renderer.transform.localRotation=Quaternion.Euler(0,random.Next(-7,8),random.Next(-3,4));
                    renderer.sharedMaterial=random.Next(3)==0?cardboard:lowerWall;
                }
                if(renderer.name=="SpineLabel") renderer.sharedMaterial=paper;
            }
            DocumentBox("ShelfDocumentBox",detail,shelves[i].transform.position+new Vector3(0,2.48f,0),new Vector3(.62f,.22f,.56f));
        }
        var desk=GameObject.Find("InvestigationDesk");
        foreach(var renderer in desk.GetComponentsInChildren<MeshRenderer>()) renderer.sharedMaterial=renderer.name=="Desktop"?wood:steel;
        desk.transform.Find("DocumentStack").GetComponent<Renderer>().sharedMaterial=paper;
        var crt=new GameObject("CRTMonitor").transform; crt.SetParent(detail); crt.localPosition=new Vector3(-.77f,1.15f,1.14f);
        Box("MonitorStand",crt,new Vector3(0,.035f,0),new Vector3(.38f,.06f,.32f),enamel);
        Box("CRTBody",crt,new Vector3(0,.29f,.04f),new Vector3(.58f,.47f,.48f),enamel);
        Box("CRTBezel",crt,new Vector3(0,.3f,-.21f),new Vector3(.5f,.38f,.028f),black);
        Box("CRTScreen",crt,new Vector3(0,.31f,-.228f),new Vector3(.44f,.29f,.018f),screen,false);
        Label("CRTDisplay",crt,"RECORD INDEX\n----------------\nACCESS: LOCAL",new Vector3(0,.31f,-.24f),Vector3.zero,.006f,new Color(.24f,.4f,.27f));
        Box("MonitorPower",crt,new Vector3(.2f,.07f,-.21f),new Vector3(.022f,.016f,.015f),lowerWall,false);
        SaveProp(crt.gameObject,"CRTMonitor");
        Box("Keyboard",detail,new Vector3(-.8f,1.143f,.54f),new Vector3(.6f,.04f,.21f),enamel);
        for(int row=0;row<3;row++) for(int key=0;key<12;key++) Box("KeyboardKey",detail,new Vector3(-1.07f+key*.048f,1.168f,.47f+row*.055f),new Vector3(.035f,.008f,.035f),lowerWall,false);
        var phone=new GameObject("WiredTelephone").transform; phone.SetParent(detail); phone.localPosition=new Vector3(1.05f,1.14f,1.15f);
        Box("TelephoneBase",phone,Vector3.zero,new Vector3(.38f,.08f,.3f),black);
        Box("Handset",phone,new Vector3(0,.1f,.06f),new Vector3(.38f,.065f,.1f),black);
        foreach(float x in new[]{-.16f,.16f}) Box("Receiver",phone,new Vector3(x,.075f,.06f),new Vector3(.1f,.11f,.14f),black);
        for(int row=0;row<4;row++) for(int col=0;col<3;col++) Box("TelephoneButton",phone,new Vector3(-.06f+col*.055f,.045f,-.09f+row*.04f),new Vector3(.033f,.018f,.025f),enamel,false);
        for(int i=0;i<22;i++) Box("CoiledCord",phone,new Vector3(-.25f+(i%2)*.025f,-.015f,-.05f+i*.012f),new Vector3(.038f,.016f,.012f),black,false);
        SaveProp(phone.gameObject,"WiredTelephone");
        for(int i=0;i<5;i++) {var loose=Box("LooseDocument",detail,new Vector3(.1f+random.Next(-10,10)*.018f,1.118f+i*.002f,1.35f+random.Next(-10,10)*.01f),new Vector3(.3f,.003f,.4f),paper,false); loose.transform.localRotation=Quaternion.Euler(0,random.Next(-20,20),0);}
        var chair=new GameObject("OfficeChair").transform;chair.SetParent(detail);chair.localPosition=new Vector3(-.4f,0,2.1f);chair.localRotation=Quaternion.Euler(0,-14,0);
        Box("Seat",chair,new Vector3(0,.46f,0),new Vector3(.48f,.09f,.45f),lowerWall);
        Box("ChairBack",chair,new Vector3(0,.86f,.22f),new Vector3(.46f,.52f,.07f),lowerWall);
        foreach(float x in new[]{-.18f,.18f}) foreach(float z in new[]{-.17f,.17f}) Box("ChairLeg",chair,new Vector3(x,.23f,z),new Vector3(.035f,.46f,.035f),steel);
        SaveProp(chair.gameObject,"OfficeChair");
        DocumentBox("FloorBox_A",detail,new Vector3(2.8f,.25f,3.6f),new Vector3(.72f,.5f,.58f));
        DocumentBox("FloorBox_B",detail,new Vector3(3.1f,.74f,3.55f),new Vector3(.62f,.45f,.5f));
        DocumentBox("UnsortedRecords",detail,new Vector3(-2.9f,.2f,-3.7f),new Vector3(.62f,.4f,.55f));
        NoticeBoard(detail); WallClock(detail);
        Box("ExitSignHousing",detail,new Vector3(-.65f,2.57f,-4.24f),new Vector3(.83f,.21f,.06f),enamel);
        Box("ExitSignFace",detail,new Vector3(-.65f,2.57f,-4.2f),new Vector3(.76f,.16f,.012f),green,false);
        Label("EmergencyExit",detail,"← 비상구 EXIT",new Vector3(-.65f,2.57f,-4.18f),new Vector3(0,180,0),.011f,new Color(.78f,.83f,.64f));
        var file=GameObject.Find("CASE 001");
        file.transform.Find("Folder").GetComponent<Renderer>().sharedMaterial=Surface("CaseCover",new Color(.72f,.58f,.32f),0);
        var caseText=file.GetComponentInChildren<TextMesh>(); caseText.text="CASE 001\n보존 원본";caseText.fontSize=80;caseText.characterSize=.009f;caseText.color=new Color(.055f,.045f,.03f);
        var fixtures=GameObject.Find("Lighting").transform;
        float[] zLights={-2.6f,.8f,3.3f};
        int index=0;
        foreach(Transform fixture in fixtures)
        {
            fixture.name="FluorescentLight_"+(char)('A'+index); fixture.position=new Vector3(index==1?.65f:1.3f,2.63f,zLights[index]);
            foreach(var renderer in fixture.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=renderer.name=="Tube"?tube:steel;
            var light=fixture.GetComponentInChildren<Light>();light.color=new Color(.72f,.86f,.76f);light.intensity=index==1?5.5f:2.5f; light.range=5;light.spotAngle=110; light.shadows=index==1?LightShadows.Soft:LightShadows.None;
            if(index==2) { light.intensity=1.5f; fixture.GetComponentsInChildren<Renderer>().Last().sharedMaterial=enamel; }
            index++;
        }
        RenderSettings.ambientLight=new Color(.135f,.16f,.14f); RenderSettings.fogDensity=.018f;RenderSettings.fogColor=new Color(.065f,.085f,.075f);
        var volume=GameObject.Find("Atmosphere").GetComponent<Volume>();
        if(!volume.sharedProfile.TryGet<FilmGrain>(out var grain))grain=volume.sharedProfile.Add<FilmGrain>(true);
        grain.intensity.Override(.055f);grain.response.Override(.75f);
        if(volume.sharedProfile.TryGet<ColorAdjustments>(out var grading)){grading.saturation.Override(-18);grading.contrast.Override(6);}
        // Grain applies to camera colour before the overlay HUD; no pixelation/aberration obscures text.
        EditorUtility.SetDirty(volume.sharedProfile);
        var camera=GameObject.Find("PlayerCamera").GetComponent<Camera>();camera.backgroundColor=RenderSettings.fogColor;
        PrefabUtility.ApplyPrefabInstance(file,InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("Basement visual pass saved; gameplay components preserved.");
    }
    private static void Resize(Transform root,string name,Vector3 position,Vector3 size,Material material)
    {var item=root.Find(name);item.localPosition=position;item.localScale=size;item.GetComponent<Renderer>().sharedMaterial=material;}
    private static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,Material material,bool collision=true)
    {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;if(!collision)Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
    private static void Label(string name,Transform parent,string text,Vector3 position,Vector3 rotation,float size,Color color)
    {var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localEulerAngles=rotation;var label=go.AddComponent<TextMesh>();label.font=font;label.fontSize=64;label.text=text;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=color;go.GetComponent<Renderer>().sharedMaterial=font.material;}
    private static void DocumentBox(string name,Transform parent,Vector3 position,Vector3 size)
    {var box=new GameObject(name).transform;box.SetParent(parent,false);box.localPosition=position;box.localRotation=Quaternion.Euler(0,random.Next(-9,10),0);Box("CardboardBox",box,Vector3.zero,size,cardboard);Box("BoxLid",box,new Vector3(0,size.y*.5f,0),new Vector3(size.x+.02f,.025f,size.z+.02f),cardboard);Box("PaperLabel",box,new Vector3(0,0,-size.z*.5f-.003f),new Vector3(size.x*.5f,size.y*.35f,.008f),paper,false);}
    private static void NoticeBoard(Transform parent)
    {var board=new GameObject("OldNoticeBoard").transform;board.SetParent(parent,false);board.localPosition=new Vector3(3.74f,1.65f,-.9f);board.localRotation=Quaternion.Euler(0,-90,0);Box("NoticeFrame",board,Vector3.zero,new Vector3(1.2f,.9f,.08f),wood);Box("Cork",board,new Vector3(0,0,-.05f),new Vector3(1.1f,.8f,.018f),cardboard,false);for(int i=0;i<4;i++){var page=Box("PinnedMemo",board,new Vector3(-.36f+i*.23f,i%2==0?.12f:-.1f,-.067f),new Vector3(.22f,.32f,.005f),paper,false);page.transform.localRotation=Quaternion.Euler(0,0,random.Next(-8,9));}Label("NoticeHeading",board,"문서 반출 금지\n정리 담당자 확인",new Vector3(0,.1f,-.075f),Vector3.zero,.012f,new Color(.08f,.08f,.06f));SaveProp(board.gameObject,"OldNoticeBoard");}
    private static void WallClock(Transform parent)
    {var clock=new GameObject("WallClock").transform;clock.SetParent(parent,false);clock.localPosition=new Vector3(2.2f,2.1f,4.29f);var face=GameObject.CreatePrimitive(PrimitiveType.Cylinder);face.name="ClockFace";face.transform.SetParent(clock,false);face.transform.localRotation=Quaternion.Euler(90,0,0);face.transform.localScale=new Vector3(.42f,.025f,.42f);face.GetComponent<Renderer>().sharedMaterial=enamel;Object.DestroyImmediate(face.GetComponent<Collider>());Label("ClockNumbers",clock,"12\n\n9       3\n\n6",new Vector3(0,0,-.03f),Vector3.zero,.008f,new Color(.07f,.08f,.06f));var hour=Box("HourHand",clock,new Vector3(-.045f,.025f,-.033f),new Vector3(.12f,.012f,.006f),black,false);hour.transform.localRotation=Quaternion.Euler(0,0,-30);var minute=Box("MinuteHand",clock,new Vector3(.03f,-.065f,-.034f),new Vector3(.012f,.17f,.006f),black,false);minute.transform.localRotation=Quaternion.Euler(0,0,25);SaveProp(clock.gameObject,"WallClock");}
    private static void SaveProp(GameObject go,string name)
    {PrefabUtility.SaveAsPrefabAssetAndConnect(go,"Assets/Prefabs/Environment/Institutional/"+name+".prefab",InteractionMode.AutomatedAction);}
    private static Material Flat(string name,Color color)
    {var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",color);AssetDatabase.CreateAsset(material,"Assets/Materials/ArchiveRoom/Institutional/"+name+".mat");return material;}
    private static Material Surface(string name,Color baseColor,int pattern)
    {
        const int n=128;var texture=new Texture2D(n,n,TextureFormat.RGB24,false);texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Repeat;
        var rng=new System.Random(317+pattern*31);var pixels=new Color[n*n];
        for(int y=0;y<n;y++)for(int x=0;x<n;x++)
        {
            float noise=(float)rng.NextDouble()*.12f-.06f;
            float value=1+noise;
            if(pattern==1)value-=((x*7+y*11)%43<4?.16f:0);
            if(pattern==2){value+=(x/64+y/64)%2==0?.09f:-.07f;if(x%64<2||y%64<2)value*=.6f;}
            if(pattern==3)value+=Mathf.Sin(y*.5f+x*.04f)*.07f;
            if((x/8+y/11)%13==0)value-=.08f;
            pixels[y*n+x]=new Color(Mathf.Round(baseColor.r*value*31)/31,Mathf.Round(baseColor.g*value*31)/31,Mathf.Round(baseColor.b*value*31)/31);
        }
        texture.SetPixels(pixels);texture.Apply();string path="Assets/Textures/ArchiveRoom/"+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.filterMode=FilterMode.Point;importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=128;importer.SaveAndReimport();
        var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",.04f);if(pattern==2)mat.SetTextureScale("_BaseMap",new Vector2(8,9));
        AssetDatabase.CreateAsset(mat,"Assets/Materials/ArchiveRoom/Institutional/"+name+".mat");return mat;
    }
}
