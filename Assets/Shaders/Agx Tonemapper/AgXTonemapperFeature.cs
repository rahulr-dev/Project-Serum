using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public class AgXTonemapperFeature : ScriptableRendererFeature
{
    class AgXPass : ScriptableRenderPass
    {
        private Material _material;

        public AgXPass(Material material)
        {
            _material = material;
            // Execute after standard post-processing (ensure URP Volume Tonemapping is set to 'None')
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle src = resourceData.activeColorTexture;

            // Corrected type: TextureDesc
            TextureDesc desc = src.GetDescriptor(renderGraph);
            desc.name = "_AgXTempTarget";
            desc.clearBuffer = false;
            TextureHandle dst = renderGraph.CreateTexture(desc);

            RenderGraphUtils.BlitMaterialParameters blitToParams = new(src, dst, _material, 0);
            renderGraph.AddBlitPass(blitToParams, "AgX Mathematical Transform");

            RenderGraphUtils.BlitMaterialParameters blitFromParams = new(dst, src, Blitter.GetBlitMaterial(TextureDimension.Tex2D), 0);
            renderGraph.AddBlitPass(blitFromParams, "AgX Target Copy");
        }
    }

    [SerializeField] private Shader agxShader;
    private Material _material;
    private AgXPass _pass;

    public override void Create()
    {
        if (agxShader == null) return;
        _material = CoreUtils.CreateEngineMaterial(agxShader);
        _pass = new AgXPass(_material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (agxShader == null || renderingData.cameraData.cameraType != CameraType.Game) return;
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_material);
    }
}
