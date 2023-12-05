using System;
using System.Collections.Generic;
using UnityEngine;

namespace SK8Controller.Player
{
    [SelectionBase, DisallowMultipleComponent]
    public class PlayerCameraController : MonoBehaviour
    {
        public Vector3 positionOffset;
        public Vector3 lookAtOffset;
        public float fieldOfView = 90.0f;
        public float positionDamping = 0.1f;
        public float rotationDamping = 0.1f;

        [Space]
        public float shakeSpring = 100.0f;
        public float shakeDamping = 10.0f;
        public float shakeInputResponse = 1.0f;
        public float shakeOutputResponse = 1.0f;

        private CarController target;
        private Camera targetCamera;
        private Vector3 up;

        private Vector3 shakePosition;
        private Vector3 shakeVelocity;
        private Vector3 shakeForce;

        private Vector3 targetPosition;
        private Quaternion targetRotation;

        private Vector3 position;
        private Quaternion rotation;

        private static List<PlayerCameraController> All = new();

        private void Awake()
        {
            target = GetComponentInParent<CarController>();
            targetCamera = GetComponentInChildren<Camera>();
        }

        private void OnEnable()
        {
            targetCamera.enabled = true;

            target.LandEvent += OnLand;
            All.Add(this);

            UpdateAllCameras();
        }
        
        private void OnDisable()
        {
            targetCamera.enabled = false;
            
            target.LandEvent -= OnLand;
            All.Remove(this);
        }

        private static void UpdateAllCameras()
        {
            var cameras = All.Count;

            switch (cameras)
            {
                case 1:
                {
                    setRect(0, 0, 0, 1, 1);
                    break;
                }
                case 2:
                {
                    setRect(0, 0, 1, 1, 2);
                    setRect(1, 0, 0, 1, 2);
                    break;
                }
                case 3:
                {
                    setRect(0, 0   , 1, 2, 2);
                    setRect(1, 1   , 1, 2, 2);
                    setRect(2, 0.5f, 0, 2, 2);
                    break;
                }
                case 4:
                {
                    setRect(0, 0, 1, 2, 2);
                    setRect(1, 1, 1, 2, 2);
                    setRect(2, 0, 0, 2, 2);
                    setRect(3, 1, 0, 2, 2);
                    break;
                }
            }

            void setRect(int i, float x, float y, float width, float height)
            {
                var cam = All[i].targetCamera;
                cam.rect = new Rect(x * 0.5f, y * 0.5f, 1.0f / width, 1.0f / height);
            }
        }

        private void OnLand(Vector3 landVelocity)
        {
            shakeVelocity += landVelocity * shakeInputResponse;
        }

        private void FixedUpdate()
        {
            if (!target) return;

            TrackTarget();
            Shake();
            ApplySmoothing();
        }

        private void Shake()
        {
            shakeForce += -shakePosition * shakeSpring - shakeVelocity * shakeDamping;
        }

        private void ApplySmoothing()
        {
            position = Vector3.Lerp(position, targetPosition, Time.deltaTime / Mathf.Max(positionDamping, Time.unscaledDeltaTime));
            rotation = Quaternion.Slerp(rotation, targetRotation, Time.deltaTime / Mathf.Max(rotationDamping, Time.unscaledDeltaTime));

            shakePosition += shakeVelocity * Time.deltaTime;
            shakeVelocity += shakeForce * Time.deltaTime;
            shakeForce = Vector3.zero;
        }

        private void TrackTarget()
        {
            if (target.isOnGround)
            {
                targetPosition = target.transform.TransformPoint(positionOffset);
                up = target.transform.up;
            }
            else
            {
                var distance = positionOffset.magnitude;
                targetPosition = Vector3.ClampMagnitude(targetPosition - target.transform.position, distance) + target.transform.position;
            }

            var lookAt = target.transform.TransformPoint(lookAtOffset);
            targetRotation = Quaternion.LookRotation(lookAt - position, up);
        }

        private void LateUpdate() { UpdateCamera(); }

        private void UpdateCamera()
        {
            targetCamera.transform.position = position + targetCamera.transform.TransformVector(shakePosition) * shakeOutputResponse;
            targetCamera.transform.rotation = rotation;

            targetCamera.fieldOfView = fieldOfView;
        }

        private void OnDrawGizmosSelected()
        {
            target = GetComponentInParent<CarController>();
            if (!target) return;

            var position = target.transform.TransformPoint(positionOffset);
            Gizmos.DrawLine(transform.position, position);

            var lookAt = target.transform.TransformPoint(lookAtOffset);
            var rotation = Quaternion.LookRotation(lookAt - position, up);
            Gizmos.matrix = target.transform.localToWorldMatrix * Matrix4x4.TRS(positionOffset, rotation, Vector3.one);
            Gizmos.DrawFrustum(Vector3.zero, fieldOfView, 0.0f, 10.0f, 16.0f / 9.0f);
        }
    }
}