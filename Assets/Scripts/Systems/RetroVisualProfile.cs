using UnityEngine;

namespace Archive0317
{
    [CreateAssetMenu(menuName="Archive 03:17/Retro Visual Profile")]
    public sealed class RetroVisualProfile : ScriptableObject
    {
        public Shader shader;
        [Range(270,720)] public int pixelHeight=360;
        [Range(0,1)] public float pixelStrength=.85f;
        [Range(0,1)] public float ditherStrength=.18f;
        [Range(0,.02f)] public float grain=.0025f;
        [Range(0,1)] public float saturation=.82f;
        public Color tint=Color.white;
    }
}
