using UnityEngine;
using Random = UnityEngine.Random;

namespace SK8Controller.Player
{
    public class SkateboardCamera : MonoBehaviour
    {
        public GroundedCameraState groundedCameraState;
        public AirborneCameraState airborneCameraState;
        [SerializeField] private float warpDistance;
        [SerializeField][Range(0.0f, 1.0f)] private float shakeSmoothing;

        [HideInInspector] public CameraState currentState;
        [HideInInspector] public PlayerController target;

        [HideInInspector] public Rigidbody cameraBody;

        [HideInInspector] public Vector3 targetPosition;
        [HideInInspector] public Vector3 targetVelocity;
        [HideInInspector] public float targetFieldOfView;
        [HideInInspector] public Vector3 lookAtTarget;
        [HideInInspector] public float shake;
        
        private Camera cam;
        private Matrix4x4 basis;
        private Vector3 shakeOffset;

        public Vector3 Position => cameraBody.position;
        public Quaternion Rotation => cameraBody.rotation;

        private void OnEnable()
        {
            target = FindObjectOfType<PlayerController>();
            cam = Camera.main;

            cameraBody = transform.Find("CameraBody").GetComponent<Rigidbody>();
            cameraBody.mass = 0.0f;
            cameraBody.constraints = RigidbodyConstraints.FreezeRotation;
            cameraBody.transform.SetParent(null);

            ChangeState(groundedCameraState);
        }

        private void FixedUpdate()
        {
            switch (target.isOnGround)
            {
                case true when currentState is AirborneCameraState:
                    ChangeState(groundedCameraState);
                    break;
                case false when currentState is GroundedCameraState:
                    ChangeState(airborneCameraState);
                    break;
            }

            if (currentState) currentState.Tick(this);
        }

        public void ChangeState(CameraState newState)
        {
            if (currentState) currentState.Exit(this);
            currentState = newState;
            if (currentState) currentState.Enter(this);
        }

        public void Move(float spring, float damper)
        {
            if ((targetPosition - cameraBody.position).magnitude > warpDistance)
            {
                cameraBody.position = targetPosition;
            }
            
            var force = (targetPosition - cameraBody.position) * spring + (targetVelocity - cameraBody.velocity) * damper;
            var rotation = Quaternion.LookRotation(lookAtTarget - cameraBody.position);

            cameraBody.AddForce(force, ForceMode.Acceleration);
            cameraBody.rotation = rotation;

            shakeOffset = Vector3.Lerp(Random.insideUnitSphere * shake / 100.0f, shakeOffset, shakeSmoothing);
            cam.transform.position = cameraBody.position + shakeOffset;
            cam.transform.rotation = cameraBody.rotation;
            cam.fieldOfView = targetFieldOfView;
        }
    }
}