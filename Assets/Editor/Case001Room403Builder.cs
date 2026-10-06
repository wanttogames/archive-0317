using System;
using System.IO;
using System.Linq;
using Archive0317;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class Case001Room403Builder
{
    private static Transform root;
    private static Font font;
    private static CaseDefinition definition;
    [MenuItem("Archive 03:17/Add CASE 001 Room 403 Anomaly")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
        var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(current.isDirty)EditorSceneManager.SaveScene(current);
        var scene=EditorSceneManager.OpenScene(Case001MotelBuilder.ScenePath,OpenSceneMode.Single);
        if(GameObject.Find("Room403Anomaly")!=null)return;
        UpgradeViewer();
        definition=AssetDatabase.LoadAssetAtPath<CaseDefinition>(Case001MotelBuilder.DefinitionPath);
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansCJKkr-Regular.otf");
        root=new GameObject("Room403Anomaly").transform;
        var player=Object.FindFirstObjectByType<FirstPersonPlayer>();
        // Keep the investigated room intact and move it as a unit beside room 402.
        foreach(var parent in new[]{GameObject.Find("MotelEnvironment").transform,GameObject.Find("MotelDetails").transform})
            foreach(var item in parent.Cast<Transform>().ToArray())if(item.position.y>8 && item.position.x<-1.3f)item.position+=Vector3.forward*2.2f;
        GameObject.Find("Room404Entry").transform.position+=Vector3.forward*2.2f;
        var door404=GameObject.Find("RoomDoor404");door404.transform.position+=Vector3.forward*2.2f;
        var door405=GameObject.Find("RoomDoor403");door405.name="RoomDoor405";door405.transform.position-=Vector3.forward*2.2f;
        door405.GetComponentInChildren<TextMesh>().text="405";
        door405.GetComponent<InspectableDoor>().Configure(door405.transform.Find("DoorHinge"),definition,"","405호 문은 잠겨 있다. 내부에는 인기척이 없다.",true);
        var note405=door405.GetComponentInChildren<InspectableNote>();note405.Configure("405호");
        Plate(GameObject.Find("RoomDoor402"),"402호","Room402PlateSeen");Plate(door404,"404호","Room404PlateSeen");
        var missingWall=GameObject.Find("MotelEnvironment").GetComponentsInChildren<Transform>().First(t=>t.name=="FourthWestWall" && Mathf.Abs(t.position.z-4.5f)<.1f).gameObject;
        missingWall.name="MissingRoom403Wall";
        var absentDoor=Object.Instantiate(door405,root);absentDoor.name="RoomDoor403";absentDoor.transform.position=new Vector3(-1.13f,9,4.5f);
        absentDoor.GetComponentInChildren<TextMesh>().text="403";
        Object.DestroyImmediate(absentDoor.GetComponent<InspectableDoor>());
        foreach(var note in absentDoor.GetComponentsInChildren<InspectableNote>())Object.DestroyImmediate(note);
        var hinge=absentDoor.transform.Find("DoorHinge");
        hinge.Find("NumberPlate").localRotation=Quaternion.Euler(0,0,1.3f);
        var innerSound=Sound("Behind403Receiver",new Vector3(-1.6f,10.2f,4.5f),Clip("ReceiverBell",false,false),.075f,7);
        absentDoor.AddComponent<InspectableSoundNote>().Configure(definition,"Room403DoorInspected","문이 잠겨 있다.",innerSound);
        // Opaque backing and the locked leaf prevent access; there is no 403 interior.
        Box("Sealed403Backing",new Vector3(-1.38f,10.05f,4.5f),new Vector3(.12f,2.1f,1.1f),Mat("PaintedSteel"),absentDoor.transform);
        Box("Revealed403Lintel",new Vector3(-1.2f,11.3f,4.5f),new Vector3(.2f,.6f,1.1f),AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Motel/FadedWallpaper.mat"),absentDoor.transform);
        foreach(float z in new[]{3.95f,5.05f})Box("Revealed403Frame",new Vector3(-1.08f,10.05f,z),new Vector3(.075f,2.1f,.06f),Mat("BeigeEnamel"),absentDoor.transform);
        absentDoor.SetActive(false);
        var arrival=Volume("FourthFloorArrival",new Vector3(0,9.9f,8.7f),new Vector3(2.1f,1.8f,1.4f));
        var corridor=Volume("FourthFloorArea",new Vector3(0,10,4.6f),new Vector3(2.1f,2,9.8f));
        var room=Volume("Room404Area",new Vector3(-3.1f,10,3.4f),new Vector3(3.8f,2,4.2f));
        var lead=Sound("Missing403Telephone",new Vector3(-1.35f,10.1f,4.5f),Clip("TelephoneLead",false,true),.24f,19);
        var light=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.name=="FourthFloorLight" && l.transform.position.z>7);
        var tube=GameObject.Find("MotelEnvironment").GetComponentsInChildren<Transform>().First(t=>t.name=="FourthFloorLightTube" && t.position.z>7).gameObject;
        var environment=root.gameObject.AddComponent<CaseEnvironmentState>();
        environment.Configure(definition,player,new[]{
            new EnvironmentRule{requirements=new[]{"LedgerInspected","Room402PlateSeen","Room404PlateSeen"},completionFlag="MissingRoomNoticed",observation="…403호는 어디 있지?"},
            new EnvironmentRule{requirements=new[]{"Room404Entered","Room404PaperInspected","MissingRoomNoticed"},completionFlag="CorridorAltered",flagsToMark=new[]{"RoomNumberMismatchFound"},outsideArea=room,hiddenFromView=tube.GetComponent<Renderer>(),changes=new[]{new EnvironmentChange{target=tube,changeActive=true,active=false},new EnvironmentChange{light=light,lightEnabled=false}}},
            new EnvironmentRule{requirements=new[]{"CCTVContradictionFound"},completionFlag="PhoneLeadPlayed",insideArea=arrival,cue=lead},
            new EnvironmentRule{requirements=new[]{"CCTVContradictionFound","PhoneLeadPlayed"},completionFlag="Room403Revealed",delayAfterReady=.8f,insideArea=corridor,hiddenFromView=missingWall.GetComponent<Renderer>(),changes=new[]{new EnvironmentChange{target=missingWall,changeActive=true,active=false},new EnvironmentChange{target=absentDoor,changeActive=true,active=true}}}
        });
        ConfigureCCTV(player);
        foreach(var label in root.GetComponentsInChildren<TextMesh>(true)){var depth=label.GetComponent<WorldTextDepth>();if(depth==null)depth=label.gameObject.AddComponent<WorldTextDepth>();depth.Configure(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/WorldTextDepth.shader"));}
        var hum=Clip("FacilityHum",true,false);var pipe=Clip("PipeAir",true,true);
        Loop("FluorescentHum",new Vector3(0,11.2f,7.5f),hum,.026f,9);
        Loop("Ventilation",new Vector3(1.55f,5.2f,13.7f),pipe,.022f,9);
        Loop("PipeRoomTone",new Vector3(-4.9f,10.1f,3.4f),pipe,.012f,5);
        Loop("CCTVElectronics",new Vector3(-4.8f,1.4f,2.75f),hum,.017f,3);
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("Room 403 anomaly saved. Initial corridor: 401 / 402 / 404 / 405.");
    }
    private static void UpgradeViewer()
    {
        const string path="Assets/Prefabs/Player/FirstPersonPlayer.prefab";var player=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var hud=player.GetComponentInChildren<ArchiveHUD>();var card=player.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Document");
            if(card.Find("CCTVFrame")!=null && card.Find("CCTVTimestamp")!=null)
            {
                hud.ConfigureCCTV(card.Find("CCTVFrame").GetComponent<RawImage>(),card.Find("CCTVTimestamp").GetComponent<Text>());
                PrefabUtility.SaveAsPrefabAsset(player,path);return;
            }
            var frame=new GameObject("CCTVFrame",typeof(RectTransform),typeof(RawImage));frame.transform.SetParent(card,false);
            var image=frame.GetComponent<RawImage>();image.raycastTarget=false;image.rectTransform.sizeDelta=new Vector2(720,405);image.rectTransform.anchoredPosition=new Vector2(0,-15);
            var stamp=new GameObject("CCTVTimestamp",typeof(RectTransform),typeof(Text));stamp.transform.SetParent(card,false);var text=stamp.GetComponent<Text>();
            text.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansCJKkr-Regular.otf");text.fontSize=22;text.color=new Color(.8f,.84f,.78f);text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.rectTransform.sizeDelta=new Vector2(730,32);text.rectTransform.anchoredPosition=new Vector2(0,-211);
            hud.ConfigureCCTV(image,text);frame.SetActive(false);stamp.SetActive(false);PrefabUtility.SaveAsPrefabAsset(player,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(player);}
    }
    private static void Plate(GameObject door,string text,string flag)
    {
        var plate=door.transform.Find("DoorHinge/NumberPlate").gameObject;if(plate.GetComponent<Collider>()==null)plate.AddComponent<BoxCollider>();
        var note=plate.GetComponent<InspectableNote>();if(note==null)note=plate.AddComponent<InspectableNote>();note.Configure(text,definition,flag);
    }
    private static void ConfigureCCTV(FirstPersonPlayer player)
    {
        var monitor=GameObject.Find("CRTMonitor");Object.DestroyImmediate(monitor.GetComponent<InspectableDocument>());
        var data=new GameObject("EntranceRecording");data.transform.SetParent(monitor.transform,false);var record=data.AddComponent<InspectableDocument>();
        record.Configure(definition,"CCTV 출입 기록",new[]{"2002. 10. 11. / 입구 카메라\n\n최민수로 추정되는 남성이 현관으로 들어온다. 이후 출구 화면에서는 같은 옷차림을 찾지 못했다.\n\n보관 목록에는 입구와 4층 복도의 녹화가 나누어져 있다. 화면 가장자리에 옅은 잡음이 남아 있다."},"CCTVInspected");
        var recording=new GameObject("RecordedFourthFloor");recording.transform.SetParent(root,false);
        var grey=new Material(Shader.Find("Universal Render Pipeline/Unlit"));grey.SetColor("_BaseColor",new Color(.38f,.38f,.38f));AssetDatabase.CreateAsset(grey,"Assets/Materials/Motel/RecordedCorridor.mat");
        var dark=new Material(grey);dark.SetColor("_BaseColor",new Color(.13f,.13f,.13f));AssetDatabase.CreateAsset(dark,"Assets/Materials/Motel/RecordedDoor.mat");
        var coat=new Material(grey);coat.SetColor("_BaseColor",new Color(.25f,.25f,.25f));AssetDatabase.CreateAsset(coat,"Assets/Materials/Motel/RecordedGuest.mat");
        Box("RecordedWall",new Vector3(60,10.25f,4.5f),new Vector3(.18f,2.5f,8.5f),grey,recording.transform);
        Box("RecordedFloor",new Vector3(61,8.95f,4.5f),new Vector3(2,.1f,8.5f),dark,recording.transform);
        Box("RecordedCeiling",new Vector3(61,11.55f,4.5f),new Vector3(2,.1f,8.5f),dark,recording.transform);
        for(int i=0;i<4;i++)
        {
            float z=1.2f+i*2.2f;
            Box("RecordedDoor"+(401+i),new Vector3(60.11f,10.05f,z),new Vector3(.09f,2.1f,1.05f),dark,recording.transform);
            Box("RecordedPlaque",new Vector3(60.175f,10.65f,z),new Vector3(.02f,.34f,.75f),grey,recording.transform);
            Box("RecordedHandle",new Vector3(60.21f,10.03f,z+.35f),new Vector3(.12f,.055f,.14f),grey,recording.transform);
            Label("RecordedNumber"+(401+i),(401+i).ToString(),new Vector3(60.19f,10.65f,z),new Vector3(0,-90,0),.052f,recording.transform);
        }
        // A stationary, clothed guest in a paused recording.
        Box("RecordedGuestCoat",new Vector3(60.8f,9.85f,3.05f),new Vector3(.34f,.7f,.24f),coat,recording.transform);
        Box("RecordedGuestHead",new Vector3(60.8f,10.35f,3.05f),new Vector3(.22f,.25f,.2f),grey,recording.transform);
        foreach(float z in new[]{2.94f,3.16f})Box("RecordedGuestLeg",new Vector3(60.8f,9.25f,z),new Vector3(.15f,.5f,.13f),coat,recording.transform);
        foreach(float z in new[]{2.82f,3.28f})Box("RecordedGuestArm",new Vector3(60.8f,9.87f,z),new Vector3(.16f,.56f,.13f),coat,recording.transform);
        foreach(var item in recording.GetComponentsInChildren<Transform>())item.gameObject.layer=30;
        var camera=new GameObject("CCTVRecordingCamera").AddComponent<Camera>();camera.transform.SetParent(root);camera.transform.position=new Vector3(66,10.35f,6);camera.transform.LookAt(new Vector3(60,10.1f,4.5f));camera.orthographic=true;camera.orthographicSize=2.5f;camera.aspect=16f/9;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.045f,.045f,.045f);camera.nearClipPlane=.1f;camera.farClipPlane=12;camera.enabled=false;camera.allowHDR=false;camera.allowMSAA=false;
        player.ViewCamera.cullingMask&=~(1<<30);
        monitor.AddComponent<InspectableCCTV>().Configure(definition,record,new[]{"LedgerInspected","MissingRoomNoticed","CorridorAltered"},"CCTVContradictionFound",camera,monitor.transform.Find("CRTScreen").GetComponent<Renderer>(),monitor.transform.Find("CRTDisplay").gameObject,AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/CCTVImage.shader"),"2002-10-11 03:17");
        monitor.GetComponent<InspectableCCTV>().ConfigureTitle("CCTV / CAM 02 · 4층 복도");
    }
    private static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ArchiveRoom/Institutional/"+name+".mat");
    private static GameObject Box(string name,Vector3 position,Vector3 size,Material material,Transform parent)
    {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent);go.transform.position=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;return go;}
    private static void Label(string name,string text,Vector3 pos,Vector3 rotation,float size,Transform parent)
    {var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=pos;go.transform.eulerAngles=rotation;var mesh=go.AddComponent<TextMesh>();mesh.font=font;mesh.text=text;mesh.fontSize=64;mesh.characterSize=size;mesh.anchor=TextAnchor.MiddleCenter;mesh.color=new Color(.82f,.82f,.82f);go.GetComponent<Renderer>().sharedMaterial=font.material;}
    private static BoxCollider Volume(string name,Vector3 position,Vector3 size)
    {var go=new GameObject(name);go.transform.SetParent(root);go.transform.position=position;var box=go.AddComponent<BoxCollider>();box.isTrigger=true;box.size=size;return box;}
    private static AudioSource Sound(string name,Vector3 position,AudioClip clip,float volume,float range)
    {var go=new GameObject(name);go.transform.SetParent(root);go.transform.position=position;var source=go.AddComponent<AudioSource>();source.clip=clip;source.spatialBlend=1;source.volume=volume;source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=1.5f;source.maxDistance=range;source.playOnAwake=false;return source;}
    private static void Loop(string name,Vector3 pos,AudioClip clip,float volume,float range){var source=Sound(name,pos,clip,volume,range);source.loop=true;source.playOnAwake=true;}
    private static AudioClip Clip(string name,bool loop,bool variant)
    {
        const int rate=22050;int length=loop?rate*4:(variant?rate*3:rate);string path="Assets/Audio/Atmosphere/"+name+".wav";
        using(var w=new BinaryWriter(File.Open(path,FileMode.Create)))
        {
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+length*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(length*2);var rng=new System.Random(403);float filtered=0;
            for(int i=0;i<length;i++){float t=i/(float)rate;float sample;
                if(loop){filtered=Mathf.Lerp(filtered,(float)rng.NextDouble()*2-1,.025f);sample=variant?filtered*.35f:(Mathf.Sin(t*100*2*Mathf.PI)*.10f+Mathf.Sin(t*200*2*Mathf.PI)*.04f);}
                else{float local=variant?t%1.5f:t;float env=local<.9f?Mathf.Min(local/.04f,1)*Mathf.Min((.9f-local)/.16f,1):0;sample=(Mathf.Sin(t*1240*2*Mathf.PI)+Mathf.Sin(t*1570*2*Mathf.PI))*.12f*env*(.7f+.3f*Mathf.Sin(t*23*2*Mathf.PI));}
                w.Write((short)(Mathf.Clamp(sample,-1,1)*short.MaxValue));}
        }
        AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
