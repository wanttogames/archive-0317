using System;
using System.IO;
using System.Linq;
using Archive0317;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class Case001FinaleBuilder
{
    private static Transform root;
    private static Font font;
    private static CaseDefinition data;
    [MenuItem("Archive 03:17/Add CASE 001 Episode Closure")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)EditorSceneManager.SaveScene(scene);
        data=AssetDatabase.LoadAssetAtPath<CaseDefinition>(Case001MotelBuilder.DefinitionPath);font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansCJKkr-Regular.otf");
        data.ConfigureEvidence(new[]{
            Evidence("ledger","숙박 장부 — 최초 객실 403","LedgerInspected"),Evidence("cctv","4층 CCTV — 실제 복도와 다른 객실 배열","CCTVContradictionFound"),
            Evidence("bag","최민수의 가방과 개인 물품","BagInspected"),Evidence("receipt","숙박 영수증 — 최초 객실 404","ReceiptInspected"),
            Evidence("double_key","403 / 404 양면 키 태그","KeyEvidenceFound"),Evidence("vanished_room","출입 후 사라진 403호","Room403Completed"),
            Evidence("supporting_key","404호 협탁의 403 키홀더","FinalKeyInspected")});EditorUtility.SetDirty(data);
        UpgradeHUD();Motel();Archive();AssetDatabase.SaveAssets();
        Debug.Log("CASE 001 episode closure saved: return, evidence, verdict, CASE 00.");
    }
    private static EvidenceDefinition Evidence(string id,string title,params string[] flags)=>new EvidenceDefinition{id=id,title=title,requirements=flags};
    private static void UpgradeHUD()
    {
        const string path="Assets/Prefabs/Player/FirstPersonPlayer.prefab";var player=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var hud=player.GetComponentInChildren<ArchiveHUD>();var card=player.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Document");
            if(card.Find("ReportActions")!=null)return;
            var actions=new GameObject("ReportActions",typeof(RectTransform));actions.transform.SetParent(card,false);
            var choices=new[]{Button(actions.transform,"VerdictHuman","범죄/인위적 사건",new Vector2(-184,-167),new Vector2(360,42)),Button(actions.transform,"VerdictRecord","기록 오류 또는 조작",new Vector2(184,-167),new Vector2(360,42)),Button(actions.transform,"VerdictUnexplained","설명 불가",new Vector2(-184,-214),new Vector2(360,42)),Button(actions.transform,"VerdictDeferred","판단 보류",new Vector2(184,-214),new Vector2(360,42))};
            var confirm=Button(actions.transform,"ConfirmVerdict","이 판정으로 보관",new Vector2(0,-266),new Vector2(730,40));
            var back=Button(actions.transform,"ReturnToArchive","돌아간다",new Vector2(-184,-214),new Vector2(360,42));var resume=Button(actions.transform,"ContinueInvestigation","계속 조사한다",new Vector2(184,-214),new Vector2(360,42));
            hud.ConfigureClosure(actions,choices,confirm,back,resume);actions.SetActive(false);PrefabUtility.SaveAsPrefabAsset(player,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(player);}
    }
    private static UnityEngine.UI.Button Button(Transform parent,string name,string caption,Vector2 position,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(UnityEngine.UI.Button));go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();rect.sizeDelta=size;rect.anchoredPosition=position;
        go.GetComponent<Image>().color=new Color(.19f,.22f,.22f);var button=go.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=go.GetComponent<Image>();var colours=button.colors;colours.highlightedColor=new Color(.86f,.88f,.8f);colours.pressedColor=new Color(.65f,.68f,.62f);button.colors=colours;
        var label=new GameObject("Label",typeof(RectTransform),typeof(Text));label.transform.SetParent(go.transform,false);var r=label.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(8,0);r.offsetMax=new Vector2(-8,0);var text=label.GetComponent<Text>();text.font=font;text.fontSize=23;text.text=caption;text.color=new Color(.8f,.82f,.75f);text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;return button;
    }
    private static void Motel()
    {
        var scene=EditorSceneManager.OpenScene(Case001MotelBuilder.ScenePath);if(GameObject.Find("MotelClosure")!=null)return;root=new GameObject("MotelClosure").transform;var player=Object.FindFirstObjectByType<FirstPersonPlayer>();
        var ledger=GameObject.Find("GuestLedger").GetComponent<InspectableDocument>();ledger.ConfigureVariant("Room403Completed",new[]{ledger.Page(0),ledger.Page(1).Replace("객실: 403","객실: 404")},"FinalLedgerInspected","404");
        var key=Box("Final404KeyHolder",new Vector3(-2.48f,9.627f,4.65f),new Vector3(.18f,.035f,.25f),Institutional("BeigeEnamel"));
        key.AddComponent<InspectableDocument>().Configure(data,"협탁 위의 객실 키",new[]{"플라스틱 키홀더\n\n403\n\n금속 고리와 투명 테이프에 작은 긁힌 자국이 남아 있다."},"FinalKeyInspected");
        var label=Label("FinalKeyNumber","403",key.transform.position+Vector3.up*.021f,new Vector3(90,180,0),.014f);label.transform.SetParent(key.transform,true);key.SetActive(false);
        var door=GameObject.Find("RoomDoor404");var scar=Box("Plate404Scratch",new Vector3(-1.049f,10.66f,3.42f),new Vector3(.004f,.012f,.14f),Institutional("PaintedSteel"));scar.transform.rotation=Quaternion.Euler(18,0,0);scar.transform.SetParent(door.transform.Find("DoorHinge"),true);scar.SetActive(false);
        var cctv=GameObject.Find("CRTMonitor").GetComponent<InspectableCCTV>();var failure=new GameObject("DVRUnavailableRecord");failure.transform.SetParent(root);var doc=failure.AddComponent<InspectableDocument>();doc.Configure(data,"녹화 보관 목록",new[]{"2002. 10. 11. / CAM 02 — 4층 복도\n\n목록 번호 017\nDATA ERROR\n\n테이프 목록은 남아 있다. 해당 구간은 재생되지 않는다."},"FinalCCTVInspected");
        var image=NoSignalImage();cctv.ConfigureUnavailable("Room403Completed",doc,image);
        var screenNotice=Label("DVRDataError","DATA ERROR",new Vector3(-4.8f,1.42f,2.457f),Vector3.zero,.011f);screenNotice.gameObject.SetActive(false);
        var exit=GameObject.Find("EntranceGlassDoor");Object.DestroyImmediate(exit.GetComponent<InspectableNote>());exit.AddComponent<InspectableCaseExit>().Configure(data,"ArchiveRoom","ReturnedToArchive",new[]{"KeyEvidenceFound","Room403Completed"});
        root.gameObject.AddComponent<CaseEnvironmentState>().Configure(data,player,new[]{new EnvironmentRule{requirements=new[]{"Room403Completed"},completionFlag="MotelFinalState",changes=new[]{new EnvironmentChange{target=key,changeActive=true,active=true},new EnvironmentChange{target=scar,changeActive=true,active=true},new EnvironmentChange{target=screenNotice.gameObject,changeActive=true,active=true}}}});
        root.gameObject.AddComponent<CompletedCaseRedirect>().Configure(data,"Case001Completed","ArchiveRoom");
        DepthLabels();EditorSceneManager.SaveScene(scene);
    }
    private static Texture2D NoSignalImage()
    {
        const string path="Assets/Textures/Motel/DVRUnavailable.png";var texture=new Texture2D(128,72,TextureFormat.RGB24,false);var pixels=new Color[128*72];var random=new System.Random(17);
        for(int i=0;i<pixels.Length;i++){float value=.018f+(float)random.NextDouble()*.009f;pixels[i]=new Color(value,value,value);}texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static void Archive()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/ArchiveRoom.unity");if(GameObject.Find("ArchiveClosure")!=null)return;root=new GameObject("ArchiveClosure").transform;var player=Object.FindFirstObjectByType<FirstPersonPlayer>();
        var file=Object.FindFirstObjectByType<CaseFile>();var report=file.gameObject.AddComponent<CaseReport>();report.Configure(data,"ReturnedToArchive","Case001Completed","Case001Verdict",new[]{"KeyEvidenceFound","Room403Completed"});report.ConfigureFacts(new[]{new CaseNotebookEntry{progressFlag="OfficialRecordSeen",text="투숙객 실종 / 외부로 나간 출입 기록 없음"},new CaseNotebookEntry{progressFlag="RoomNumberMismatchFound",text="공식 기록과 숙박 장부의 객실 번호 불일치"},new CaseNotebookEntry{progressFlag="CCTVContradictionFound",text="CCTV와 실제 복도의 객실 구성 불일치"}});file.ConfigureReport(report);PrefabUtility.RecordPrefabInstancePropertyModifications(file);
        var memo=Box("FieldInvestigationMemo",new Vector3(.82f,1.19f,.72f),new Vector3(.2f,.008f,.12f),Institutional("YellowedPaper"));var memoText=Label("FieldMemoPrint","조사 완료",memo.transform.position+Vector3.up*.006f,new Vector3(90,180,0),.008f);memoText.color=new Color(.12f,.12f,.1f);memoText.transform.SetParent(memo.transform,true);memo.SetActive(false);
        // Reserve one small slot without deleting the original binder objects.
        foreach(var binder in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>(t.name=="ArchiveBinder" || t.name=="SpineLabel") && t.position.x>2.4f && t.position.x<2.9f && t.position.y>1.2f && t.position.y<1.6f && t.position.z>3.5f).ToArray())
        {binder.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(binder.gameObject);}
        var unknown=Box("CASE 00",new Vector3(2.62f,1.42f,3.77f),new Vector3(.44f,.4f,.23f),Institutional("Bakelite"));
        var cover=Label("Case00Cover","CASE 00",new Vector3(2.62f,1.48f,3.643f),Vector3.zero,.018f);cover.transform.SetParent(unknown.transform,true);
        var mystery=unknown.AddComponent<InspectableDocument>();mystery.Configure(data,"CASE 00",new[]{"CASE 00\n\n발생일: [기록 없음]\n\n장소: [기록 없음]\n\n담당 조사관:\n기록 담당자 0317", "첨부 사진", "이후 기록 없음"},"Case00Inspected");
        var photograph=Photograph(file.gameObject,unknown);mystery.ConfigureImage(1,photograph,"촬영 기록 1998. 11. 14. / 지하 기록보관실");unknown.SetActive(false);
        var deskArea=new GameObject("ArchiveDeskArea");deskArea.transform.SetParent(root);deskArea.transform.position=new Vector3(0,.9f,-.3f);var box=deskArea.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(3.3f,1.8f,2.3f);
        root.gameObject.AddComponent<CaseEnvironmentState>().Configure(data,player,new[]{
            new EnvironmentRule{requirements=new[]{"ReturnedToArchive"},completionFlag="ArchiveFieldMemoPlaced",changes=new[]{new EnvironmentChange{target=memo,changeActive=true,active=true}}},
            new EnvironmentRule{requirements=new[]{"Case001Completed"},completionFlag="ArchiveCase001Filed",changes=new[]{new EnvironmentChange{label=memoText,text="보관"}}},
            new EnvironmentRule{requirements=new[]{"Case001Completed"},restoreRequirements=new[]{"Case001Completed"},completionFlag="Case00Activated",delayAfterReady=4,outsideArea=box,hiddenFromView=unknown.GetComponent<Renderer>(),changes=new[]{new EnvironmentChange{target=unknown,changeActive=true,active=true}}}
        });DepthLabels();EditorSceneManager.SaveScene(scene);
    }
    private static Texture2D Photograph(GameObject known,GameObject unknown)
    {
        const string path="Assets/Textures/ArchiveRoom/Case00ArchivePhoto.png";
        var clone=Object.Instantiate(unknown);clone.name="PhotoCase00";clone.transform.position=new Vector3(-.03f,1.165f,.73f);clone.transform.rotation=Quaternion.Euler(90,0,0);clone.transform.localScale=new Vector3(.42f,.35f,.06f);Object.DestroyImmediate(clone.GetComponent<InspectableDocument>());Object.DestroyImmediate(clone.GetComponent<Collider>());
        var label=clone.GetComponentInChildren<TextMesh>();label.transform.position=new Vector3(-.03f,1.205f,.73f);label.transform.rotation=Quaternion.Euler(90,180,0);label.characterSize=.02f;
        var go=new GameObject("ArchivePhotoCamera");var camera=go.AddComponent<Camera>();camera.transform.position=new Vector3(1.7f,2.5f,-.5f);camera.transform.LookAt(new Vector3(.2f,1.05f,.8f));camera.fieldOfView=53;camera.nearClipPlane=.05f;camera.farClipPlane=20;camera.enabled=false;camera.allowHDR=false;camera.allowMSAA=false;var rt=new RenderTexture(768,432,24);rt.Create();var prior=RenderTexture.active;
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(768,432,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,768,432),0,0);image.Apply();var pixels=image.GetPixels();var random=new System.Random(1998);for(int i=0;i<pixels.Length;i++){float grey=pixels[i].grayscale*.85f+.06f+(float)random.NextDouble()*.008f;pixels[i]=new Color(grey*1.06f,grey*.98f,grey*.84f);}image.SetPixels(pixels);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());Object.DestroyImmediate(image);}
        finally{RenderTexture.active=prior;camera.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(go);Object.DestroyImmediate(clone);}
        AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static Material Institutional(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ArchiveRoom/Institutional/"+name+".mat");
    private static GameObject Box(string name,Vector3 pos,Vector3 scale,Material material){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root);go.transform.position=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;return go;}
    private static TextMesh Label(string name,string text,Vector3 pos,Vector3 rotation,float size){var go=new GameObject(name);go.transform.SetParent(root);go.transform.position=pos;go.transform.eulerAngles=rotation;var mesh=go.AddComponent<TextMesh>();mesh.font=font;mesh.text=text;mesh.fontSize=64;mesh.characterSize=size;mesh.anchor=TextAnchor.MiddleCenter;mesh.color=new Color(.7f,.72f,.65f);go.GetComponent<Renderer>().sharedMaterial=font.material;return mesh;}
    private static void DepthLabels(){foreach(var label in root.GetComponentsInChildren<TextMesh>(true))if(label.GetComponent<WorldTextDepth>()==null){var depth=label.gameObject.AddComponent<WorldTextDepth>();depth.Configure(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/WorldTextDepth.shader"));}}
}
