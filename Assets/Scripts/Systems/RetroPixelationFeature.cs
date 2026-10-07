using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace Archive0317
{
    public sealed class RetroPixelationFeature : ScriptableRendererFeature
    {
        private RetroPass pass;
        private WorldTextPass textPass;
        public override void Create(){pass=new RetroPass{renderPassEvent=RenderPassEvent.AfterRenderingPostProcessing};textPass=new WorldTextPass();}
        public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data)
        {
            var camera=data.cameraData.camera;
            if(data.cameraData.renderType!=CameraRenderType.Base)return;
            bool styled=data.cameraData.cameraType==CameraType.Game && camera.TryGetComponent<RetroCameraStyle>(out var style) && style.isActiveAndEnabled;
            var material=styled?camera.GetComponent<RetroCameraStyle>().Prepare(camera):null;
            if(material!=null){pass.Setup(material);renderer.EnqueuePass(pass);}
            // Recording and Scene View cameras also draw the custom text, before their normal post effects.
            textPass.renderPassEvent=material!=null?(RenderPassEvent)((int)RenderPassEvent.AfterRenderingPostProcessing+1):RenderPassEvent.BeforeRenderingPostProcessing;
            renderer.EnqueuePass(textPass);
        }
        private sealed class WorldTextPass : ScriptableRenderPass
        {
            private sealed class TextData{public RendererListHandle list;}
            public override void RecordRenderGraph(RenderGraph graph,ContextContainer frameData)
            {
                var resources=frameData.Get<UniversalResourceData>();
                var rendering=frameData.Get<UniversalRenderingData>();var camera=frameData.Get<UniversalCameraData>();var lights=frameData.Get<UniversalLightData>();
                var drawing=RenderingUtils.CreateDrawingSettings(new ShaderTagId("ArchiveWorldText"),rendering,camera,lights,SortingCriteria.CommonTransparent);
                var filtering=new FilteringSettings(RenderQueueRange.transparent,camera.camera.cullingMask);
                using(var builder=graph.AddRasterRenderPass<TextData>("Archive legible world text",out var data))
                {
                    data.list=graph.CreateRendererList(new RendererListParams(rendering.cullResults,drawing,filtering));
                    builder.UseRendererList(data.list);
                    builder.SetRenderAttachment(resources.activeColorTexture,0,AccessFlags.ReadWrite);
                    builder.SetRenderAttachmentDepth(resources.activeDepthTexture,AccessFlags.Read);
                    builder.SetRenderFunc((TextData value,RasterGraphContext context)=>context.cmd.DrawRendererList(value.list));
                }
            }
        }
        private sealed class RetroPass : ScriptableRenderPass
        {
            private Material material;
            public void Setup(Material value){material=value;requiresIntermediateTexture=true;}
            public override void RecordRenderGraph(RenderGraph graph,ContextContainer frameData)
            {
                var resources=frameData.Get<UniversalResourceData>();
                if(resources.isActiveTargetBackBuffer || material==null)return;
                var source=resources.activeColorTexture;var description=graph.GetTextureDesc(source);
                description.name="Archive Retro World Color";description.clearBuffer=false;
                var destination=graph.CreateTexture(description);
                graph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source,destination,material,0),passName:"Archive PS1 World");
                // Swap camera color rather than copying back; the overlay HUD is drawn afterwards.
                resources.cameraColor=destination;
            }
        }
    }
}
