using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SK8Controller.Rendering
{
    public class OutlineFeature : ScriptableRendererFeature
    {
        public Material whiteMaterial;
        public Material blackMaterial;
        public Material blitMaterial;
        
        private OutlinePass pass;
        
        public override void Create()
        {
            pass = new OutlinePass(whiteMaterial, blackMaterial, blitMaterial);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(pass);
        }
    }
}