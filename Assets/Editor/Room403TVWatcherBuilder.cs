using Archive0317;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class Room403TVWatcherBuilder
{
    [MenuItem("Archive 03:17/Add Room 403 TV Watcher")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Stop Play Mode and save first.");
        var scene=EditorSceneManager.OpenScene(Case001MotelBuilder.ScenePath);
        int layer=LayerMask.NameToLayer("Room403TVOnly");
        if(layer<0){var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);var layers=tags.FindProperty("layers");for(int i=29;i>=24;i--)if(string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)){layer=i;layers.GetArrayElementAtIndex(i).stringValue="Room403TVOnly";break;}if(layer<0)throw new System.Exception("No free TV layer.");tags.ApplyModifiedProperties();}
        var television=Find("Room403CRTTV").GetComponent<InspectableCaseTelevision>();var camera=television.RecordingCamera;
        camera.name="Room403RoomSurveillanceCamera";camera.transform.position=new Vector3(-22,11.4f,2.7f);camera.transform.LookAt(new Vector3(-20,9.8f,4.8f));camera.fieldOfView=88;camera.nearClipPlane=.08f;camera.farClipPlane=10;camera.enabled=false;camera.allowHDR=false;camera.allowMSAA=false;
        var extra=camera.GetUniversalAdditionalCameraData();extra.renderPostProcessing=false;
        foreach(var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))c.cullingMask&=~(1<<layer);camera.cullingMask|=1<<layer;
        var old=FindOptional("Room403TVOnlyActors");if(old!=null)Object.DestroyImmediate(old);
        var root=new GameObject("Room403TVOnlyActors");root.transform.SetParent(television.transform.parent,true);
        var figure=Body(root.transform,"TVWatcherFigure",layer,Material("TVWatcherSilhouette",new Color(.005f,.007f,.006f)));
        var proxy=Body(root.transform,"TVPlayerProxy",layer,Material("TVPlayerCoat",new Color(.32f,.35f,.32f)));
        figure.gameObject.SetActive(false);proxy.gameObject.SetActive(false);
        var watcher=television.GetComponent<Room403TVWatcher>();if(watcher==null)watcher=television.gameObject.AddComponent<Room403TVWatcher>();
        watcher.Configure(AssetDatabase.LoadAssetAtPath<CaseDefinition>(Case001MotelBuilder.DefinitionPath),Object.FindFirstObjectByType<FirstPersonPlayer>(),Find("Room403Area").GetComponent<Collider>(),Find("Room403TVScreen").GetComponent<Renderer>(),proxy,figure,new Vector3(-18.5f,9.05f,3f));
        var environment=Find("Room403Interior").GetComponent<CaseEnvironmentState>();var state=new SerializedObject(environment);var rules=state.FindProperty("rules");
        for(int i=0;i<rules.arraySize;i++){var rule=rules.GetArrayElementAtIndex(i);if(rule.FindPropertyRelative("completionFlag").stringValue=="KeyTagVisible"){var requirements=rule.FindPropertyRelative("requirements");requirements.arraySize=2;requirements.GetArrayElementAtIndex(0).stringValue="TelevisionInspected";requirements.GetArrayElementAtIndex(1).stringValue="TVWatcherFinished";}}
        state.ApplyModifiedProperties();EditorUtility.SetDirty(watcher);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    private static Material Material(string name,Color color){string path="Assets/Materials/Motel/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(m,path);}m.color=color;EditorUtility.SetDirty(m);return m;}
    private static Transform Body(Transform parent,string name,int layer,Material material)
    {
        var root=new GameObject(name).transform;root.SetParent(parent,false);root.gameObject.layer=layer;
        Part(root,"Head",new Vector3(0,1.57f,0),new Vector3(.21f,.25f,.21f),material,layer);
        Part(root,"Torso",new Vector3(0,1.05f,0),new Vector3(.38f,.68f,.23f),material,layer);
        Part(root,"Shoulders",new Vector3(0,1.35f,0),new Vector3(.5f,.15f,.24f),material,layer);
        foreach(float x in new[]{-.25f,.25f})Part(root,"Arm",new Vector3(x,.98f,0),new Vector3(.11f,.75f,.12f),material,layer);
        foreach(float x in new[]{-.1f,.1f})Part(root,"Leg",new Vector3(x,.37f,0),new Vector3(.14f,.74f,.17f),material,layer);
        return root;
    }
    private static void Part(Transform parent,string name,Vector3 position,Vector3 scale,Material m,int layer){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.layer=layer;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;var r=go.GetComponent<Renderer>();r.sharedMaterial=m;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;Object.DestroyImmediate(go.GetComponent<Collider>());}
    private static GameObject FindOptional(string name){foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(t.name==name)return t.gameObject;return null;}
    private static GameObject Find(string name)=>FindOptional(name)??throw new System.Exception(name+" missing");
}
