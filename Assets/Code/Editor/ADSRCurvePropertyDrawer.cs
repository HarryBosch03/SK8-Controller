using System.Net.NetworkInformation;
using SK8Controller.Maths;
using UnityEditor;
using UnityEngine;

namespace SK8ControllerEditor
{
    [CustomPropertyDrawer(typeof(ADSRCurve))]
    public class ADSRCurvePropertyDrawer : BetterPropertyDrawer<ADSRCurve>
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (GetFoldoutState(label)) return SingleLineHeight * 7 + Pad * 7 + 100.0f;
            return SingleLineHeight;
        }

        protected override void GUI()
        {
            if (Foldout(Next(), label))
            {
                Indent(1);
                target.attack = EditorGUI.FloatField(Next(), "Attack", target.attack);
                target.decay = EditorGUI.FloatField(Next(), "Decay", target.decay);
                target.sustain = EditorGUI.FloatField(Next(), "Sustain", target.sustain);
                target.release = EditorGUI.FloatField(Next(), "Release", target.release);
                target.delay = EditorGUI.FloatField(Next(), "Delay", target.delay);
                target.duration = EditorGUI.FloatField(Next(), "Duration", target.duration);

                DrawGraph();
            }
            
            property.serializedObject.Update();
        }

        private void DrawGraph()
        {
            var graph = linePosition;
            graph.height = 100.0f;

            var border = 1.0f;
            EditorGUI.DrawRect(graph, new Color(0.9f, 0.9f, 0.9f, 1.0f));
            graph.x += border;
            graph.y += border;
            graph.width -= 2.0f * border;
            graph.height -= 2.0f * border;
            EditorGUI.DrawRect(graph, new Color(0.1f, 0.1f, 0.1f, 1.0f));

            var pad = 0.2f;
            
            var xMin = 0.0f;
            var xMax = 1.0f;
            var yMin = -pad;
            var yMax = 1.0f + pad;

            GL.PushMatrix();
            GL.Begin(GL.LINE_STRIP);

            var step = 0.001f;
            for (var p = 0.0f; p < 1.0f; p += step)
            {
                var t = p * (target.delay + target.duration);
                GL.Vertex(toGraph(p, target.Sample(t)));
            }
            
            GL.End();
            GL.Begin(GL.LINES);

            for (var t = 0.0f; t < target.duration; t += 1.0f)
            {
                GL.Color(new Color(1.0f, 1.0f, 1.0f, 0.2f));
                GL.Vertex(toGraph(t / target.duration, yMin));
                GL.Vertex(toGraph(t / target.duration, yMax));
            }

            GL.End();
            GL.PopMatrix();

            Vector2 toGraph(float x, float y)
            {
                return new Vector2
                {
                    x = Mathf.Lerp(graph.xMin, graph.xMax, Mathf.InverseLerp(xMin, xMax, x)),
                    y = Mathf.Lerp(graph.yMin, graph.yMax, Mathf.InverseLerp(yMax, yMin, y)),
                };
            }
        }
    }
}