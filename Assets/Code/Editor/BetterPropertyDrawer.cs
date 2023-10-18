
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SK8ControllerEditor
{
    public abstract class BetterPropertyDrawer<T> : PropertyDrawer
    {
        public static float SingleLineHeight => EditorGUIUtility.singleLineHeight;
        public virtual float Pad => 2.0f;

        public Rect position;
        public SerializedProperty property;
        public GUIContent label;

        public Rect linePosition;

        public T target;

        private Object targetObject;
        private FieldInfo field;
        
        public sealed override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            this.position = position;
            this.property = property;
            this.label = label;

            linePosition = position;
            linePosition.height = SingleLineHeight;

            Deserialize();
            GUI();
            Serialize();
        }

        private void Deserialize()
        {
            position.height = EditorGUIUtility.singleLineHeight;
            
            targetObject = property.serializedObject.targetObject;
            var targetObjectType = targetObject.GetType();
            field = targetObjectType.GetField(property.propertyPath);

            target = (T)field.GetValue(targetObject);
        }

        private void Serialize()
        {
            Undo.RecordObject(targetObject, $"Edited {property.propertyPath}");
            field.SetValue(targetObject, target);
        }
        
        public Rect Next()
        {
            var oldLinePosition = linePosition;
            linePosition.y += linePosition.height + Pad;
            return oldLinePosition;
        }
        
        public void Indent(int delta)
        {
            var width = 10.0f * delta;
            linePosition.x += width;
            linePosition.width -= width;
        }

        protected virtual void GUI() { }

        public static bool GetFoldoutState(GUIContent content)
        {
            var key = $"ADSRCurvePropertyDrawer.{content.text}";
            return EditorPrefs.GetBool(key, false);
        }
        
        public static bool Foldout(Rect position, GUIContent content)
        {
            var key = $"ADSRCurvePropertyDrawer.{content.text}";  
            
            var state = EditorPrefs.GetBool(key, false);
            state = EditorGUI.Foldout(position, state, content, true);
            EditorPrefs.SetBool(key, state);
            return state;
        }
    }
}