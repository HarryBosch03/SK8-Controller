
using System;
using UnityEngine;

namespace SK8Controller.Player
{
    [SelectionBase, DisallowMultipleComponent]
    public class Wheel : MonoBehaviour
    {
        public Vector3 wheelStart;
        public Vector3 wheelEnd;
        public WheelSettings settings;

        public bool isOnGround;
        private Vector3 velocity;
        private RaycastHit groundHit;
        private float contraction;
        private Rigidbody body;
        
        public float SteerAngle { get; set; }
        public float DriveForce { get; set; }
        public Quaternion Rotation => transform.rotation * Quaternion.Euler(0.0f, SteerAngle, 0.0f);

        private void Awake()
        {
            body = GetComponentInParent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            CheckForGround();

            velocity = isOnGround ? body.GetPointVelocity(groundHit.point) : Vector3.zero;

            ApplyDrive();
            ApplySuspension();
            ApplyTangentFriction();
        }

        private void ApplyDrive()
        {
            if (!isOnGround) return;

            var right = Rotation * Vector3.forward;
            body.AddForceAtPosition(right * DriveForce, groundHit.point, ForceMode.Acceleration);
        }

        private void ApplyTangentFriction()
        {
            if (!isOnGround) return;
            
            var right = Rotation * Vector3.right;
            var force = Vector3.Dot(right, -velocity) * settings.tangentialFriction;
            body.AddForceAtPosition(right * force, groundHit.point, ForceMode.VelocityChange);   
        }

        private void ApplySuspension()
        {
            if (!isOnGround) return;

            var force = contraction * settings.spring - Vector3.Dot(groundHit.normal, velocity) * settings.damper;
            body.AddForceAtPosition(groundHit.normal * force, groundHit.point, ForceMode.Acceleration);
        }

        private void CheckForGround()
        {
            var a = transform.TransformPoint(wheelStart);
            var b = transform.TransformPoint(wheelEnd);
            var maxDistance = (b - a).magnitude;

            isOnGround = Physics.Linecast(a, b, out groundHit);
            contraction = isOnGround ? 1.0f - groundHit.distance / maxDistance : 0.0f;
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(wheelStart, wheelEnd);
        }
    }
}