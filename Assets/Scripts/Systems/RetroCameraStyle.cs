using UnityEngine;

namespace Archive0317
{
    [ExecuteAlways,DisallowMultipleComponent,RequireComponent(typeof(Camera))]
    public sealed class RetroCameraStyle : MonoBehaviour
    {
        [SerializeField] private RetroVisualProfile profile;
        private Material material;
        public RetroVisualProfile Profile=>profile;
        public void Configure(RetroVisualProfile value){profile=value;}
        public Material Prepare(Camera camera)
        {
            if(profile==null || profile.shader==null || !profile.shader.isSupported)return null;
            if(material!=null && material.shader!=profile.shader)Release();
            if(material==null)material=new Material(profile.shader){hideFlags=HideFlags.HideAndDontSave};
            int height=Mathf.Min(profile.pixelHeight,camera.pixelHeight);
            float intensity=Mathf.Clamp01(PlayerPrefs.GetFloat(MainMenuController.VisualIntensityKey,.65f));
            material.SetVector("_PixelGrid",new Vector4(Mathf.Max(1,Mathf.RoundToInt(height*camera.aspect)),Mathf.Max(1,height),profile.pixelStrength,profile.ditherStrength*intensity));
            material.SetVector("_RetroTone",new Vector4(profile.grain*intensity,profile.saturation,0,0));
            material.SetColor("_RetroTint",profile.tint);return material;
        }
        private void OnDisable(){Release();}
        private void Release(){if(material==null)return;if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);material=null;}
    }
}
