using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SK8Controller.Utility
{
    public static class Drawing
    {
        public static void Matrix(Matrix4x4 matrix)
        {
#if UNITY_EDITOR
            Handles.matrix = matrix;
#endif
        }
        
        public static void Color(Color color)
        {
#if UNITY_EDITOR
            Handles.color = color;
#endif
        }

        public static void DrawLine(Vector3 start, Vector3 end, float width = 1.0f)
        {
#if UNITY_EDITOR
            Handles.DrawAAPolyLine(width, start, end);
#endif
        }
    }
}