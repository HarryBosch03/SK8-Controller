using System;
using UnityEngine;

namespace SK8Controller.Utility
{
    [ExecuteAlways]
    public sealed class ConstrainRotation : MonoBehaviour
    {
        [SerializeField] private Vector3 basis;
        
        private void LateUpdate()
        {
            transform.rotation = Quaternion.Euler(basis);
        }
    }
}
