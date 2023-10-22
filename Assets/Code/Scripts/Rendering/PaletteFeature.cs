using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SK8Controller.Rendering
{
    public sealed class PaletteFeature : ScriptableRendererFeature
    {
        public Material material;
        
        private Pass pass;
        
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

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (!material) return;
                var cmd = CommandBufferPool.Get("PalettePass");
                
                cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
                
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }
        }
    }
}
