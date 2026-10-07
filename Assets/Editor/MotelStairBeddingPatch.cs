using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MotelStairBeddingPatch
{
    private const string Folder="Assets/Prefabs/MotelBedding";
    [MenuItem("Archive 03:17/Repair Stairs and Dress Motel Beds")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play Mode first.");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Cases/Case001_Motel.unity");
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Prefabs","MotelBedding");
        var wall=GameObject.Find("StairwellBack").GetComponent<Renderer>().sharedMaterial;
        for(int level=2;level<=3;level++)
        {
            var name="StairwellFrontWall_"+level;var go=GameObject.Find(name);
            if(go==null){go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(GameObject.Find("StairwellBack").transform.parent);}
            go.transform.position=new Vector3(0,(level-1)*3+1.5f,9.38f);go.transform.localScale=new Vector3(3.6f,3f,.2f);go.GetComponent<Renderer>().sharedMaterial=wall;
        }
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/WovenCloth.png");
        if(texture==null)
        {
            var t=new Texture2D(128,128);var pixels=new Color[128*128];
            for(int y=0;y<128;y++)for(int x=0;x<128;x++){float v=.86f+((x+y)%2)*.045f+Mathf.Sin(x*.6f)*.012f;pixels[y*128+x]=new Color(v,v,v);}
            t.SetPixels(pixels);t.Apply();File.WriteAllBytes(Folder+"/WovenCloth.png",t.EncodeToPNG());Object.DestroyImmediate(t);AssetDatabase.ImportAsset(Folder+"/WovenCloth.png");
            var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"/WovenCloth.png");importer.filterMode=FilterMode.Point;importer.mipmapEnabled=true;importer.maxTextureSize=128;importer.SaveAndReimport();texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/WovenCloth.png");
        }
        var cloth=Material("Cloth",new Color(.43f,.46f,.42f),texture);var sheet=Material("Sheet",new Color(.68f,.64f,.53f),texture);
        foreach(bool tucked in new[]{false,true})
        {
            string coverName=tucked?"Room403BedCover":"BedCover";var cover=Find(coverName);var pillow=Find(tucked?"Room403Pillow":"Pillow");
            var existing=cover.transform.parent.Find(coverName+"Dressings");if(existing!=null)Object.DestroyImmediate(existing.gameObject);
            cover.GetComponent<Renderer>().enabled=false;pillow.GetComponent<Renderer>().enabled=false;
            var root=new GameObject(coverName+"Dressings");root.transform.position=cover.transform.position;root.transform.SetParent(cover.transform.parent,true);
            Box("Mattress",root.transform,new Vector3(0,-.11f,0),new Vector3(1.68f,.19f,2.03f),sheet);
            MeshPart("Quilt",root.transform,MeshAsset(tucked?"TuckedQuilt":"RumpledQuilt",false,tucked),cloth,Vector3.zero);
            Box("FoldedTopEdge",root.transform,new Vector3(tucked?0:.08f,.085f,.35f),new Vector3(1.65f,.07f,.18f),sheet);
            for(int i=0;i<7;i++)Box("QuiltStitch",root.transform,new Vector3(-.68f+i*.225f,.09f,-.24f),new Vector3(.006f,.003f,1.38f),sheet);
            MeshPart("PressedPillow",root.transform,MeshAsset("PressedPillow",true,true),sheet,pillow.transform.position-cover.transform.position);
            Box("PillowHem",root.transform,pillow.transform.position-cover.transform.position+new Vector3(0,-.015f,-.23f),new Vector3(.69f,.012f,.01f),cloth);
            string prefab=Folder+"/"+(tucked?"BedDressings403":"BedDressings404")+".prefab";PrefabUtility.SaveAsPrefabAssetAndConnect(root,prefab,InteractionMode.AutomatedAction);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    private static GameObject Find(string name){foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(t.name==name)return t.gameObject;throw new System.Exception(name+" missing");}
    private static Material Material(string name,Color color,Texture texture)
    {string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetTexture("_BaseMap",texture);m.SetFloat("_Smoothness",.04f);m.SetFloat("_Cull",0);EditorUtility.SetDirty(m);return m;}
    private static void Box(string name,Transform parent,Vector3 position,Vector3 scale,Material material)
    {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());}
    private static void MeshPart(string name,Transform parent,Mesh mesh,Material material,Vector3 position)
    {var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=position;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=material;}
    private static Mesh MeshAsset(string name,bool pillow,bool tucked)
    {
        string path=Folder+"/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh();mesh.name=name;AssetDatabase.CreateAsset(mesh,path);}mesh.Clear();
        int width=17,height=23;var v=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
        for(int z=0;z<height;z++)for(int x=0;x<width;x++)
        {
            float u=x/(float)(width-1),w=z/(float)(height-1);float px=(u-.5f)*(pillow?.75f:1.84f),pz=(w-.5f)*(pillow?.46f:1.82f);
            float y;
            if(pillow)y=.015f+.105f*Mathf.Sin(u*Mathf.PI)*Mathf.Sin(w*Mathf.PI)-.035f*Mathf.Exp(-((u-.55f)*(u-.55f)+(w-.5f)*(w-.5f))*30);
            else {float edge=Mathf.Max(Mathf.Abs(px)-.77f,0);y=.065f-edge*(tucked?.8f:1.65f)+Mathf.Sin(w*32+u*8)*Mathf.Sin(u*Mathf.PI)*.014f;if(!tucked)y+=.055f*Mathf.Exp(-Mathf.Pow((px+.34f)*5,2))*Mathf.Sin(w*9);pz-=.24f;}
            v.Add(new Vector3(px,y,pz));uv.Add(new Vector2(u*(pillow?1:3),w*(pillow?1:4)));
            if(x<width-1 && z<height-1){int a=z*width+x;triangles.AddRange(new[]{a,a+width,a+1,a+1,a+width,a+width+1});}
        }
        if(pillow)
        {
            var edge=new List<int>();for(int x=0;x<width;x++)edge.Add(x);for(int z=1;z<height;z++)edge.Add(z*width+width-1);for(int x=width-2;x>=0;x--)edge.Add((height-1)*width+x);for(int z=height-2;z>0;z--)edge.Add(z*width);
            for(int i=0;i<edge.Count;i++){int a=edge[i],b=edge[(i+1)%edge.Count],c=v.Count;var av=v[a];var bv=v[b];av.y=-.04f;bv.y=-.04f;v.Add(av);v.Add(bv);uv.Add(uv[a]);uv.Add(uv[b]);triangles.AddRange(new[]{a,b,c,b,c+1,c});}
        }
        mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);return mesh;
    }
}
