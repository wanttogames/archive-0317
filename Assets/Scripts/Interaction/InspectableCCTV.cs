using System;
using UnityEngine;

namespace Archive0317
{
    public sealed class InspectableCCTV : Inspectable
    {
        [SerializeField] private CaseDefinition definition;
        [SerializeField] private InspectableDocument entryRecord;
        [SerializeField] private string[] requirements;
        [SerializeField] private string evidenceFlag;
        [SerializeField] private Camera recordingCamera;
        [SerializeField] private Renderer screen;
        [SerializeField] private GameObject pausedLabel;
        [SerializeField] private Shader imageShader;
        [SerializeField] private string timestamp;
        [SerializeField] private string heading="CCTV";
        private RenderTexture source,output;
        private Material effect,screenMaterial;
        private Material originalScreen;
        private float nextFrame;
        public Texture Frame=>output;
        public string Timestamp=>timestamp;
        public string Title=>heading;
        public void ConfigureTitle(string title){heading=title;}
        public Camera RecordingCamera=>recordingCamera;
        public bool IsReady
        {
            get { if(definition==null)return false;var progress=CaseProgressStore.Get(definition);foreach(var flag in requirements??Array.Empty<string>())if(!progress.Has(flag))return false;return true; }
        }
        public void Configure(CaseDefinition data,InspectableDocument baseline,string[] conditions,string flag,Camera camera,Renderer display,GameObject label,Shader shader,string stamp)
        {definition=data;entryRecord=baseline;requirements=conditions;evidenceFlag=flag;recordingCamera=camera;screen=display;pausedLabel=label;imageShader=shader;timestamp=stamp;}
        private void Awake()
        {
            source=new RenderTexture(512,288,24){filterMode=FilterMode.Point};output=new RenderTexture(512,288,0){filterMode=FilterMode.Point};source.Create();output.Create();
            effect=new Material(imageShader);
            originalScreen=screen.sharedMaterial;screenMaterial=new Material(originalScreen);
        }
        public void RenderFrame()
        {
            recordingCamera.targetTexture=source;recordingCamera.Render();Graphics.Blit(source,output,effect);
        }
        private void LateUpdate()
        {
            if(!IsReady || Time.time<nextFrame)return;
            nextFrame=Time.time+.25f;RenderFrame();
            screenMaterial.SetTexture("_BaseMap",output);screen.sharedMaterial=screenMaterial;
            if(pausedLabel!=null)pausedLabel.SetActive(false);
        }
        public override void Inspect(FirstPersonPlayer player)
        {
            if(!IsReady){entryRecord.Inspect(player);return;}
            RenderFrame();player.HUD.ShowCCTV(this);
            CaseProgressStore.Mark(definition,"CCTVInspected");CaseProgressStore.Mark(definition,evidenceFlag);
        }
        private void OnDestroy()
        {
            if(recordingCamera!=null)recordingCamera.targetTexture=null;
            if(screen!=null)screen.sharedMaterial=originalScreen;
            if(source!=null){source.Release();Destroy(source);}if(output!=null){output.Release();Destroy(output);}
            if(effect!=null)Destroy(effect);if(screenMaterial!=null)Destroy(screenMaterial);
        }
    }
}
