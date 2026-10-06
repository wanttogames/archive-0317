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
        public void Configure(Shader shader) { depthShader=shader; }
        private void OnEnable()
        {
            text=GetComponent<TextMesh>(); textRenderer=GetComponent<Renderer>();
            if(depthShader==null || text.font==null)return;
            original=textRenderer.sharedMaterial;
            material=new Material(depthShader); textRenderer.sharedMaterial=material;
            Font.textureRebuilt+=RefreshAtlas;
            text.font.RequestCharactersInTexture(text.text,text.fontSize,text.fontStyle);
            RefreshAtlas(text.font);
        }
        private void RefreshAtlas(Font rebuilt)
        { if(material!=null && rebuilt==text.font)material.mainTexture=rebuilt.material.mainTexture; }
        private void OnDisable()
        {
            Font.textureRebuilt-=RefreshAtlas;
            if(material==null)return;
            textRenderer.sharedMaterial=original; Destroy(material); material=null;
        }
    }
}
