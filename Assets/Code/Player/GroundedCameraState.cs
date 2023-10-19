using UnityEngine;

namespace SK8Controller.Player
{
    [System.Serializable]
    public class GroundedCameraSettings : CameraState
    {
        [SerializeField] private Vector3 cameraOffset = Vector3.back;
        [SerializeField] private Vector3 lookOffset;
        [SerializeField] private float absoluteVerticalOffset;
        [SerializeField] private float fov = 40.0f;

        [SerializeField] private float spring, damper;

        [SerializeField] private float dollyResponse;
        [SerializeField][Range(0.0f, 1.0f)] private float dollySmoothing;
        [SerializeField] private float dollyFov;
        [SerializeField] private Vector3 dollyOffset;
        [SerializeField][Range(0.0f, 1.0f)] private float dolly;
        
        private Matrix4x4 basis;

        public override void Tick(SkateboardCamera camera)
        {
            if (!camera.target.isOnGround)
            {
                camera.ChangeState();
            }
            
            basis = camera.target.transform.localToWorldMatrix;
            
            camera.targetPosition = basis.MultiplyPoint(cameraOffset) + Vector3.up * absoluteVerticalOffset;
            camera.targetVelocity = camera.target.Body.velocity;
            camera.targetFieldOfView = fov;
            camera.lookAtTarget = basis.MultiplyPoint(lookOffset);

            ApplyDolly(camera);
            camera.Move(spring, damper);
        }

        private void ApplyDolly(SkateboardCamera camera)
        {
            var forwardSpeed = Mathf.Max(0.0f, Vector3.Dot(camera.target.transform.forward, camera.target.Body.velocity));
            dolly = Mathf.Lerp(Mathf.Clamp01(forwardSpeed * dollyResponse), dolly, dollySmoothing);

            camera.targetFieldOfView = Mathf.Lerp(camera.targetFieldOfView, dollyFov, dolly);
            camera.targetPosition += camera.target.Body.rotation * dollyOffset * dolly;
        }
    }
}