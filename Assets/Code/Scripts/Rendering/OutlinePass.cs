using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SK8Controller.Rendering
{
    public class OutlinePass : ScriptableRenderPass
    {
        private static readonly int OutlineRt = Shader.PropertyToID("_OutlineRT");
        private static readonly int IntermediateRt = Shader.PropertyToID("_IntermediateRT");
        private static readonly int TargetRt = Shader.PropertyToID("_CameraColorAttachmentA");

        private Material whiteMaterial;
        private Material blackMaterial;
        private Material blitMaterial;
        
        List<ShaderTagId> shaderTagIdList = new()
        {
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
        };

        public OutlinePass(Material whiteMaterial, Material blackMaterial, Material blitMaterial)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            this.whiteMaterial = whiteMaterial;
            this.blackMaterial = blackMaterial;
            this.blitMaterial = blitMaterial;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            cmd.GetTemporaryRT(OutlineRt, desc, FilterMode.Bilinear);
            cmd.GetTemporaryRT(IntermediateRt, desc, FilterMode.Bilinear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!whiteMaterial) return;
            if (!blackMaterial) return;
            if (!blitMaterial) return;
            
            var cmd = CommandBufferPool.Get("Outline");
            cmd.SetRenderTarget(OutlineRt);
            cmd.ClearRenderTarget(true, true, Color.clear);
            
            var layerMask = 1 << 7;
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);
            var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            
            var sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;

            var drawingSettings = CreateDrawingSettings(shaderTagIdList, ref renderingData, sortingCriteria);
            drawingSettings.overrideMaterial = whiteMaterial;

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);

            filteringSettings.layerMask = ~layerMask;
            drawingSettings.overrideMaterial = blackMaterial;
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);

            cmd.Blit(TargetRt, IntermediateRt, blitMaterial);
            cmd.Blit(IntermediateRt, TargetRt);
            context.ExecuteCommandBuffer(cmd);
            
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(OutlineRt);
            cmd.ReleaseTemporaryRT(IntermediateRt);
        }
    }
}