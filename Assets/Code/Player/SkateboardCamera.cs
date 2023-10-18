using UnityEngine;

namespace SK8Controller.Player
{
    public class SkateboardCamera : MonoBehaviour
    {
        [SerializeField] private Vector3 cameraOffset = Vector3.back;
        [SerializeField] private Vector3 lookOffset;
        [SerializeField] private float fov = 40.0f;

        [SerializeField] private float spring, damper;
        [SerializeField] private float warpDistance;

        [SerializeField] private float dollyResponse;
        [SerializeField][Range(0.0f, 1.0f)] private float dollySmoothing;
        [SerializeField] private float dollyFov;
        [SerializeField] private Vector3 dollyOffset;
        [SerializeField][Range(0.0f, 1.0f)] private float dolly;
        
        private PlayerController target;
        private Camera cam;

        private Rigidbody cameraBody;

        private void OnEnable()
        {
            target = FindObjectOfType<PlayerController>();
            cam = Camera.main;

            cameraBody = transform.Find("CameraBody").GetComponent<Rigidbody>();
            cameraBody.mass = 0.0f;
            cameraBody.constraints = RigidbodyConstraints.FreezeRotation;
            cameraBody.transform.SetParent(null);
        }

        private void FixedUpdate()
        {
            var tPos = target.transform.TransformPoint(cameraOffset);
            var tVel = target.Body.velocity;
            var tFov = fov;

            if ((tPos - cameraBody.position).magnitude > warpDistance)
            {
                cameraBody.position = tPos;
            }

            ApplyDolly(ref tPos, ref tFov);
            
            var force = (tPos - cameraBody.position) * spring + (tVel - cameraBody.velocity) * damper;
            var lookPosition = target.transform.TransformPoint(lookOffset);
            var rotation = Quaternion.LookRotation(lookPosition - cameraBody.position);

            cameraBody.AddForce(force, ForceMode.Acceleration);
            cameraBody.rotation = rotation;
            
            cam.transform.position = cameraBody.position;
            cam.transform.rotation = cameraBody.rotation;
            cam.fieldOfView = tFov;
        }

        private void ApplyDolly(ref Vector3 tPos, ref float tFov)
        {
            var forwardSpeed = Mathf.Max(0.0f, Vector3.Dot(target.transform.forward, target.Body.velocity));
            dolly = Mathf.Lerp(Mathf.Clamp01(forwardSpeed * dollyResponse), dolly, dollySmoothing);

            tFov = Mathf.Lerp(tFov, dollyFov, dolly);
            tPos += target.Body.rotation * dollyOffset * dolly;
        }
    }
}