using UnityEngine;

namespace SK8Controller.Player
{
    [CreateAssetMenu(menuName = "Config/Camera/Airborne Camera State")]
    public class AirborneCameraState : CameraState
    {
        public float spring, damper;
        public float followDistance;
        public float verticalOffset;

        public override void Tick(SkateboardCamera camera)
        {
            camera.targetPosition = Vector3.ClampMagnitude(camera.Position - camera.target.transform.position, followDistance) + camera.target.transform.position + Vector3.up * verticalOffset;
            camera.lookAtTarget = camera.target.transform.TransformPoint(camera.groundedCameraState.lookOffset);
            camera.targetVelocity = camera.target.Body.velocity;
            
            camera.Move(spring, damper);
        }
    }
}