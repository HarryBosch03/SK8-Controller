using UnityEngine;

namespace SK8Controller.Player
{
    [CreateAssetMenu(menuName = "Config/Camera/Grounded Camera State")]
    public class GroundedCameraState : CameraState
    {
        public Vector3 cameraOffset = Vector3.back;
        public Vector3 lookOffset;
        public float absoluteVerticalOffset;
        public float fov = 40.0f;

         public float spring, damper;

         public float dollyResponse;
        [Range(0.0f, 1.0f)] public float dollySmoothing;
         public float dollyFov;
         public Vector3 dollyOffset;
        [Range(0.0f, 1.0f)] public float dolly;
        
        private Matrix4x4 basis;

        public override void Tick(SkateboardCamera camera)
        {
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