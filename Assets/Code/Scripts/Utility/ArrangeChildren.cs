using UnityEngine;

namespace SK8Controller.Utility
{
    [SelectionBase, DisallowMultipleComponent]
    public sealed class ArrangeChildren : MonoBehaviour
    {
        [SerializeField] private Vector3 translationStart;
        [SerializeField] private Vector3 translationIncrement;
        [SerializeField] private Vector3 rotationStart;
        [SerializeField] private Vector3 rotationIncrement;
        [SerializeField] private bool center;

        private void OnEnable()
        {
            Instance();
        }

        private void OnValidate()
        {
            Instance();
        }

        private void Instance()
        {
            var centerPosition = Vector3.zero;

            var position = Vector3.zero;
            var rotation = Quaternion.identity;

            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                child.name = $"{name}.{i + 1}";
                child.transform.localPosition = rotation * (translationStart + position);
                child.transform.localRotation = Quaternion.Euler(rotationStart) * rotation;

                centerPosition += child.transform.localPosition / transform.childCount;
                
                position += rotation * translationIncrement;
                rotation = Quaternion.Euler(rotationIncrement) * rotation;
            }

            if (!center) return;
            
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                child.transform.localPosition -= centerPosition;
            }
        }
    }
}