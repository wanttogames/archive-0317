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
        public override void Create(){pass=new RetroPass{renderPassEvent=RenderPassEvent.AfterRenderingPostProcessing};}
        public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data)
        {
            var camera=data.cameraData.camera;
            if(data.cameraData.cameraType!=CameraType.Game || data.cameraData.renderType!=CameraRenderType.Base)return;
            if(!camera.TryGetComponent<RetroCameraStyle>(out var style) || !style.isActiveAndEnabled)return;
            var material=style.Prepare(camera);if(material==null)return;
            pass.Setup(material);renderer.EnqueuePass(pass);
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
