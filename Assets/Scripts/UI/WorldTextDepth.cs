using UnityEngine;

namespace Archive0317
{
    [RequireComponent(typeof(TextMesh))]
    public sealed class WorldTextDepth : MonoBehaviour
    {
        [SerializeField] private Shader depthShader;
        private TextMesh text;
        private Renderer textRenderer;
        private Material original;
        private Material material;
        private bool subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplyToSceneText()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded-=OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded+=OnSceneLoaded;
            ApplyLabels();
        }
        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,UnityEngine.SceneManagement.LoadSceneMode mode){ApplyLabels();}
        public static void ApplyLabels()
        {
            var shader=Shader.Find("Archive0317/WorldTextDepth");
            if(shader==null)return;
            foreach(var label in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                var clarity=label.GetComponent<WorldTextDepth>();
                if(clarity==null)clarity=label.gameObject.AddComponent<WorldTextDepth>();
                clarity.Configure(shader);
            }
        }

        public void Configure(Shader shader)
        {
            depthShader=shader;
            if(isActiveAndEnabled)Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Apply()
        {
            if(material!=null)return;
            text=GetComponent<TextMesh>();
            textRenderer=GetComponent<Renderer>();
            if(depthShader==null)depthShader=Shader.Find("Archive0317/WorldTextDepth");
            if(depthShader==null || text==null || text.font==null || textRenderer==null)return;
            original=textRenderer.sharedMaterial;
            material=new Material(depthShader);
            textRenderer.sharedMaterial=material;
            if(!subscribed)
            {
                Font.textureRebuilt+=RefreshAtlas;
                subscribed=true;
            }
            text.font.RequestCharactersInTexture(text.text,text.fontSize,text.fontStyle);
            RefreshAtlas(text.font);
        }
        private void RefreshAtlas(Font rebuilt)
        { if(material!=null && rebuilt==text.font)material.mainTexture=rebuilt.material.mainTexture; }
        private void OnDisable()
        {
            if(subscribed)
            {
                Font.textureRebuilt-=RefreshAtlas;
                subscribed=false;
            }
            if(material==null)return;
            if(textRenderer!=null)textRenderer.sharedMaterial=original;
            Destroy(material);
            material=null;
        }
    }
}
