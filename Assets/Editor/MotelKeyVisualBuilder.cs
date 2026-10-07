using System;
using System.Collections.Generic;
using System.IO;
using Archive0317;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class MotelKeyVisualBuilder
{
    private const string Folder="Assets/Prefabs/MotelKeys";
    private static Material brass,plastic,ink;
    [MenuItem("Archive 03:17/Dress CASE 001 Keys")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop Play Mode and save the current scene first.");
        var scene=EditorSceneManager.OpenScene(Case001MotelBuilder.ScenePath);
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Prefabs","MotelKeys");
        brass=Material("WornBrass",new Color(.48f,.36f,.16f),.65f);plastic=Material("FadedPlastic",new Color(.27f,.36f,.32f),0);ink=Material("TagWear",new Color(.59f,.55f,.4f),0);
        var data=AssetDatabase.LoadAssetAtPath<CaseDefinition>(Case001MotelBuilder.DefinitionPath);
        var key=Find("Room403KeyEvidence");key.GetComponent<Renderer>().enabled=false;key.transform.rotation=Quaternion.Euler(0,180,0);
        var old=key.transform.Find("KeyTag403");if(old!=null)old.gameObject.SetActive(false);
        ReplaceVisual(key.transform,"EvidenceKeyVisual","403",true);
        var box=key.GetComponent<BoxCollider>();box.center=new Vector3(-.45f,.4f,0);box.size=new Vector3(2.5f,1.4f,1.4f);
        var doc=key.GetComponent<InspectableDocument>();doc.ConfigureImages(new Texture[]{Image("403"),Image("404")},new[]{"앞면 403","뒷면 404"},"E 키 태그 조사");doc.ConfigureKeyEvidenceSound();
        var cabinet=GameObject.Find("RoomKeyCabinet");
        var spare=GameObject.Find("Spare404Key");if(spare==null)spare=new GameObject("Spare404Key");spare.transform.SetParent(cabinet.transform.parent,true);
        spare.transform.position=new Vector3(-1.4f,1.67f,2.63f);spare.transform.rotation=Quaternion.Euler(-90,0,0);spare.transform.localScale=Vector3.one;
        ReplaceVisual(spare.transform,"SpareKeyVisual","404",false);
        if(spare.GetComponent<BoxCollider>()==null)spare.AddComponent<BoxCollider>();spare.GetComponent<BoxCollider>().center=new Vector3(-.12f,-.05f,0);spare.GetComponent<BoxCollider>().size=new Vector3(.63f,.025f,.21f);
        var note=spare.GetComponent<InspectableNote>();if(note==null)note=spare.AddComponent<InspectableNote>();note.Configure("404호 예비 열쇠를 챙겼다. 금속 열쇠와 낡은 플라스틱 태그가 키링에 달려 있다.",data,"Room404KeyTaken");note.ConfigureKeyPickupSound();cabinet.GetComponent<InspectableNote>().ConfigureKeyPickupSound();
        foreach(var label in cabinet.GetComponentsInChildren<TextMesh>())if(label.text=="404" && !label.transform.IsChildOf(spare.transform))label.gameObject.SetActive(false);
        foreach(var t in cabinet.transform.parent.GetComponentsInChildren<Transform>())if(t.name=="PlasticKeyHolder" && Mathf.Abs(t.position.x+1.4f)<.05f)t.gameObject.SetActive(false);
        var state=spare.GetComponent<CaseEnvironmentState>();if(state==null)state=spare.AddComponent<CaseEnvironmentState>();state.Configure(data,Object.FindFirstObjectByType<FirstPersonPlayer>(),new[]{new EnvironmentRule{requirements=new[]{"Room404KeyTaken"},completionFlag="Spare404KeyVisualTaken",changes=new[]{new EnvironmentChange{target=spare,changeActive=true,active=false}}}});
        WorldTextDepth.ApplyLabels();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    private static GameObject Find(string name){foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(t.name==name)return t.gameObject;throw new Exception(name+" missing");}
    private static Material Material(string name,Color color,float metal){string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",.16f);EditorUtility.SetDirty(m);return m;}
    private static void ReplaceVisual(Transform parent,string name,string number,bool reverse)
    {
        var old=parent.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);
        var root=new GameObject(name);root.transform.position=parent.position;root.transform.rotation=parent.rotation;root.transform.SetParent(parent,true);
        Box("PlasticTag",root.transform,new Vector3(.055f,.035f,0),new Vector3(.24f,.018f,.16f),plastic);
        Box("PaperNumberInset",root.transform,new Vector3(.055f,.046f,0),new Vector3(.16f,.004f,.106f),ink);
        Box("MetalShaft",root.transform,new Vector3(-.32f,.026f,0),new Vector3(.23f,.014f,.035f),brass);
        for(int i=0;i<3;i++)Box("KeyTooth",root.transform,new Vector3(-.41f+i*.036f,.026f,-.026f),new Vector3(.025f,.014f,.035f+i*.004f),brass);
        Ring("KeyBow",root.transform,new Vector3(-.18f,.026f,0),.055f,.011f);Ring("SplitKeyRing",root.transform,new Vector3(-.09f,.035f,0),.044f,.006f);
        Label(root.transform,number,new Vector3(.055f,.05f,0),new Vector3(90,180,reverse?0:180));
        if(reverse){Box("ReverseTape",root.transform,new Vector3(.06f,.024f,.04f),new Vector3(.13f,.002f,.017f),ink);Label(root.transform,"404",new Vector3(.055f,.024f,0),new Vector3(-90,0,0));}
        for(int i=0;i<4;i++)Box("PlasticScratch",root.transform,new Vector3(.02f+i*.035f,.047f,-.065f),new Vector3(.018f,.001f,.002f),ink);
        PrefabUtility.SaveAsPrefabAssetAndConnect(root,Folder+"/"+name+".prefab",InteractionMode.AutomatedAction);
    }
    private static void Box(string name,Transform parent,Vector3 position,Vector3 size,Material material){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());}
    private static void Label(Transform parent,string number,Vector3 position,Vector3 rotation){var go=new GameObject("TagNumber"+number);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localEulerAngles=rotation;var label=go.AddComponent<TextMesh>();label.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansCJKkr-Regular.otf");label.fontSize=64;label.characterSize=.011f;label.text=number;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(.09f,.075f,.045f);go.GetComponent<Renderer>().sharedMaterial=label.font.material;}
    private static void Ring(string name,Transform parent,Vector3 position,float radius,float tube)
    {
        string path=Folder+"/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh();mesh.name=name;AssetDatabase.CreateAsset(mesh,path);}mesh.Clear();var v=new List<Vector3>();var tris=new List<int>();
        for(int i=0;i<16;i++)for(int j=0;j<6;j++){float a=i*Mathf.PI/8,b=j*Mathf.PI/3;v.Add(new Vector3(Mathf.Cos(a)*(radius+Mathf.Cos(b)*tube),Mathf.Sin(b)*tube,Mathf.Sin(a)*(radius+Mathf.Cos(b)*tube)));int k=i*6+j,n=i*6+(j+1)%6,l=((i+1)%16)*6+j,q=((i+1)%16)*6+(j+1)%6;tris.AddRange(new[]{k,l,n,n,l,q});}
        mesh.SetVertices(v);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();EditorUtility.SetDirty(mesh);var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=position;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=brass;
    }
    private static Texture2D Image(string number)
    {
        string path=Folder+"/KeyTag"+number+".png";int w=768,h=432;var texture=new Texture2D(w,h);var pixels=new Color[w*h];var rng=new System.Random(403);
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {
            float noise=(float)rng.NextDouble()*.025f;var color=new Color(.09f+noise,.085f+noise,.075f+noise);
            if(x>463 && x<697 && y>126 && y<302)color=new Color(.08f,.08f,.06f);
            if(x>453 && x<687 && y>136 && y<312)color=new Color(.26f+noise,.34f+noise,.29f+noise);
            if(x>483 && x<657 && y>169 && y<279)color=new Color(.59f+noise,.55f+noise,.42f+noise);
            if(x>125 && x<316 && y>201 && y<228 || x>130 && x<210 && y>183 && y<205 && x%37<24)color=new Color(.47f+noise,.36f+noise,.18f+noise);
            float bow=Vector2.Distance(new Vector2(x,y),new Vector2(330,215));float ring=Vector2.Distance(new Vector2(x,y),new Vector2(420,222));if(bow>36 && bow<52 || ring>29 && ring<37)color=new Color(.56f+noise,.44f+noise,.25f+noise);
            if(number=="404" && x>471 && x<671 && y>144 && y<156)color=new Color(.39f,.42f,.34f);
            pixels[y*w+x]=color;
        }
        string[] glyph={"111101101101111","010110010010111","111001111100111","111001111001111","101101111001001"};
        for(int d=0;d<3;d++){string g=glyph[number[d]-'0'];for(int r=0;r<5;r++)for(int c=0;c<3;c++)if(g[r*3+c]=='1')for(int yy=0;yy<14;yy++)for(int xx=0;xx<13;xx++)pixels[(250-r*15+yy)*w+492+d*53+c*14+xx]=new Color(.1f,.095f,.06f);}
        for(int i=0;i<35;i++){int x=458+rng.Next(225),y=140+rng.Next(165);for(int j=0;j<rng.Next(3,15)&&x+j<686;j++)pixels[y*w+x+j]=new Color(.4f,.42f,.33f);}
        texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
