using Archive0317;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CorridorRearCueBuilder
{
    [MenuItem("Archive 03:17/Add Corridor Rear Cues")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play Mode first.");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Save the current scene first.");
        var scene=EditorSceneManager.OpenScene(Case001MotelBuilder.ScenePath);
        if(GameObject.Find("CorridorRearCues")!=null)return;
        var root=new GameObject("CorridorRearCues");
        var figure=new GameObject("DistantDoorwayFigure");figure.transform.SetParent(root.transform);
        string path="Assets/Materials/Motel/DistantFigure.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=new Color(.045f,.047f,.042f);material.SetFloat("_Smoothness",0);AssetDatabase.CreateAsset(material,path);}
        Part(figure.transform,"Coat",new Vector3(0,.95f,0),new Vector3(.4f,.85f,.22f),material);
        Part(figure.transform,"Head",new Vector3(0,1.52f,0),new Vector3(.2f,.25f,.19f),material);
        foreach(float x in new[]{-.1f,.1f})Part(figure.transform,"Leg",new Vector3(x,.35f,0),new Vector3(.13f,.7f,.16f),material);
        var anchors=new Transform[2];
        for(int i=0;i<2;i++){var anchor=new GameObject("RearDoorway_"+i).transform;anchor.SetParent(root.transform);anchor.position=new Vector3(-.76f,9,GameObject.Find(i==0?"RoomDoor405":"RoomDoor401").transform.position.z);anchors[i]=anchor;}
        var audio=new GameObject("QuietRearAudio").AddComponent<AudioSource>();audio.transform.SetParent(root.transform);audio.playOnAwake=false;audio.spatialBlend=1;audio.volume=.32f;audio.rolloffMode=AudioRolloffMode.Linear;audio.minDistance=1;audio.maxDistance=13;audio.dopplerLevel=0;
        root.AddComponent<CorridorRearCue>().Configure(Object.FindFirstObjectByType<FirstPersonPlayer>(),AssetDatabase.LoadAssetAtPath<CaseDefinition>(Case001MotelBuilder.DefinitionPath),figure.transform,anchors,audio);
        figure.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    private static void Part(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
    {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());}
}
