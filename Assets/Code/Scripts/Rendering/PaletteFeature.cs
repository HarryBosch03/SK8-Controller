using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SK8Controller.Rendering
{
    public sealed class PaletteFeature : ScriptableRendererFeature
    {
        public Material material;
        private Pass pass;

        private static int PaletteTexture = Shader.PropertyToID("_PaletteTexture");
        private static int CameraColorAttachment = Shader.PropertyToID("_CameraColorAttachmentA");
        
        public override void Create()
        {
            pass = new Pass(material);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(pass);
        }
        
        public class Pass : ScriptableRenderPass
        {
            public Material material;

            public Pass(Material material)
            {
                this.material = material;
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                cmd.GetTemporaryRT(PaletteTexture, renderingData.cameraData.cameraTargetDescriptor);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (!material) return;
                var cmd = CommandBufferPool.Get("PalettePass");
                
                cmd.Blit(BuiltinRenderTextureType.CameraTarget, PaletteTexture);
                cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
                
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(PaletteTexture);
            }
        }
    }
}
