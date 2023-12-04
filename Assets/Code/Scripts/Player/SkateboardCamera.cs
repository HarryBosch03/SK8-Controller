using UnityEngine;
using Random = UnityEngine.Random;

namespace SK8Controller.Player
{
    public class SkateboardCamera : MonoBehaviour
    {
        public Vector3 translationOffset;
        public Vector3 lookAtOffset;
        public float spring;
        public float damper;
        public float baseFov = 90.0f;
        [Range(0.0f, 1.0f)] public float shakeSmoothing;
        
        [HideInInspector] public CarController target;

        private Vector3 targetPosition;
        private Vector3 targetVelocity;
        private float targetFieldOfView;
        private Vector3 lookAtTarget;
        private float shake;
        
        private Camera cam;
        private Matrix4x4 basis;
        private Vector3 shakeOffset;

        private Vector3 position;
        private Vector3 velocity;
        private Vector3 force;
        private Quaternion rotation;
        private Quaternion offsetRotation;

        private void OnEnable()
        {
            target = FindObjectOfType<CarController>();
            cam = Camera.main;
        }

        private void FixedUpdate()
        {
            if (target.wheelsOnGround == 4) offsetRotation = target.transform.rotation;
            targetPosition = target.transform.position + offsetRotation * translationOffset;
            lookAtTarget =  target.transform.position + offsetRotation * lookAtOffset;
            
            targetFieldOfView = baseFov;

            Move();
        }

        public void Move()
        {
            force = (targetPosition - position) * spring + (targetVelocity - velocity) * damper;
            rotation = Quaternion.LookRotation(lookAtTarget - position);

            position += velocity * Time.deltaTime;
            velocity += force * Time.deltaTime;
            force = Vector3.zero;
            
            shakeOffset = Vector3.Lerp(Random.insideUnitSphere * shake / 100.0f, shakeOffset, shakeSmoothing);
            cam.transform.position = position + shakeOffset;
            cam.transform.rotation = rotation;
            cam.fieldOfView = targetFieldOfView;
        }
    }
}