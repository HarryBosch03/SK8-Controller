using UnityEngine;

public static class Drawing
{
    private static Material lineMaterial;
    
    private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
    private static readonly int Cull = Shader.PropertyToID("_Cull");
    private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");

    private static Material GetLineMaterial()
    {
        if (lineMaterial) return lineMaterial;

        lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        
        // Turn on alpha blending
        lineMaterial.SetInt(SrcBlend, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt(DstBlend, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        // Turn backface culling off
        lineMaterial.SetInt(Cull, (int)UnityEngine.Rendering.CullMode.Off);
        // Turn off depth writes
        lineMaterial.SetInt(ZWrite, 0);
        
        return lineMaterial;
    }
    
    public static void DrawLine(Vector3 start, Vector3 end, Color color, float width = 1.0f)
    {
        GetLineMaterial().SetPass(0);
        
        GL.PushMatrix();
        GL.Begin(GL.LINES);
        
        GL.Color(color);
        GL.Vertex(start);
        GL.Vertex(end);
        
        GL.End();
        GL.PopMatrix();
    }
}