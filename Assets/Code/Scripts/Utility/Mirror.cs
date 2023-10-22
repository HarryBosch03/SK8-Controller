using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace SK8Controller.Utility
{
    public class Mirror : MonoBehaviour
    {
        [SerializeField] private MirrorMethod mirrorMethod;
        [SerializeField] private Vector3 localBasis;
        
        private void OnValidate()
        {
            if (transform.childCount == 0) return;
            
            var original = transform.GetChild(0);
            var copy = GetCopy(original);

            var basis = transform.rotation * Quaternion.Euler(localBasis);
            
            copy.localPosition = Vector3.Reflect(original.position - transform.position, basis * Vector3.forward);
            switch (mirrorMethod)
            {
                default:
                case MirrorMethod.Rotate:
                    copy.localRotation = Quaternion.Euler(0.0f, 180.0f, 0.0f) * original.localRotation;
                    copy.localScale = Vector3.one;
                    break;
                case MirrorMethod.Scale:
                    copy.localRotation = original.localRotation;
                    copy.localScale = new Vector3(1.0f, 1.0f, -1.0f);
                    break;
            }
            copy.name = $"{original.name}.Copy";
        }

        private Transform GetCopy(Transform original)
        {
            if (transform.childCount >= 2) return transform.GetChild(1);

            var instance = Instantiate(original, transform);
            return instance;
        }

        private enum MirrorMethod
        {
            Rotate,
            Scale,
        }
    }
}