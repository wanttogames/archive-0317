using System.Collections;
using UnityEngine;

namespace Archive0317
{
    public sealed class InspectableCaseTelevision : Inspectable
    {
        [SerializeField] private CaseDefinition definition;
        [SerializeField] private Renderer screen;
        [SerializeField] private Camera recording;
        [SerializeField] private Shader imageShader;
        [SerializeField] private TextMesh timestamp;
        [SerializeField] private AudioSource electronics;
        [SerializeField] private string activationFlag;
        private RenderTexture source,output;
        private Material effect,display,original;
        private Texture2D noise;
        private bool started;
        private float nextFrame;
        public bool FootageVisible {get;private set;}
        public Texture Frame=>output;
        public void Configure(CaseDefinition data,Renderer surface,Camera camera,Shader shader,TextMesh stamp,AudioSource hum,string flag)
        {definition=data;screen=surface;recording=camera;imageShader=shader;timestamp=stamp;electronics=hum;activationFlag=flag;}
        private void Start()
        {
            source=new RenderTexture(384,216,24){filterMode=FilterMode.Point};output=new RenderTexture(384,216,0){filterMode=FilterMode.Point};source.Create();output.Create();
            effect=new Material(imageShader);original=screen.sharedMaterial;display=new Material(original);
            noise=new Texture2D(64,36,TextureFormat.RGB24,false){filterMode=FilterMode.Point};var rng=new System.Random(403);var pixels=new Color[64*36];for(int i=0;i<pixels.Length;i++){float value=(float)rng.NextDouble()*.16f;pixels[i]=new Color(value,value,value);}noise.SetPixels(pixels);noise.Apply();
            timestamp.gameObject.SetActive(false);
        }
        private void Update()
        {
            if(!CaseProgressStore.Get(definition).Has(activationFlag))return;
            if(!started){started=true;StartCoroutine(Tune());}
            if(FootageVisible && Time.time>=nextFrame){nextFrame=Time.time+.25f;RenderFrame();}
        }
        private IEnumerator Tune()
        {display.SetColor("_BaseColor",new Color(1.8f,1.8f,1.8f,1));display.SetTexture("_BaseMap",noise);screen.sharedMaterial=display;if(electronics!=null)electronics.Play();yield return new WaitForSeconds(2);FootageVisible=true;timestamp.gameObject.SetActive(true);RenderFrame();CaseProgressStore.Mark(definition,"TelevisionEventTriggered");}
        private void RenderFrame(){recording.targetTexture=source;recording.Render();Graphics.Blit(source,output,effect);display.SetTexture("_BaseMap",output);}
        public override void Inspect(FirstPersonPlayer player)
        {player.HUD.ShowToast(FootageVisible?"화면에는 4층 복도가 비친다.":"전원 버튼 아래에 먼지가 쌓여 있다.");if(FootageVisible)CaseProgressStore.Mark(definition,"TelevisionInspected");}
        private void OnDestroy(){if(recording!=null)recording.targetTexture=null;if(screen!=null)screen.sharedMaterial=original;if(source!=null){source.Release();Destroy(source);}if(output!=null){output.Release();Destroy(output);}if(effect!=null)Destroy(effect);if(display!=null)Destroy(display);if(noise!=null)Destroy(noise);}
    }
}
