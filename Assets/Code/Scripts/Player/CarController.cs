using SK8Controller.Config;
using UnityEngine;

namespace SK8Controller.Player
{
    public class CarController : MonoBehaviour
    {
        [SerializeField] private CarSettings settings;
        [SerializeField] private Vector3 centerOfMass;

        [SerializeField] [Range(-1.0f, 1.0f)] private float rawSteerInput;

        public Vector3 up;
        public PlayerInputProvider inputProvider;
        public PlayerInputData inputData;
        public int wheelsOnGround;
        public bool isOnGround;
        private Wheel[] wheels = new Wheel[4];

        public Rigidbody Body { get; private set; }
        public float SteerAngle { get; private set; }

        public event System.Action<Vector3> LandEvent;

        public float GetForwardSpeed(bool abs = false)
        {
            var v = Vector3.Dot(Body.velocity, transform.forward);
            return abs ? Mathf.Abs(v) : v;
        }

        private void Awake()
        {
            GetFromHierarchy();
        }

        private void GetFromHierarchy()
        {
            if (!Body) Body = GetComponent<Rigidbody>();

            wheels[0] = transform.Find("Car/WheelAnchor.FL").GetComponent<Wheel>();
            wheels[1] = transform.Find("Car/WheelAnchor.FR").GetComponent<Wheel>();
            wheels[2] = transform.Find("Car/WheelAnchor.BL").GetComponent<Wheel>();
            wheels[3] = transform.Find("Car/WheelAnchor.BR").GetComponent<Wheel>();

            inputProvider = GetComponent<PlayerInputProvider>();
        }

        private void OnEnable()
        {
            if (!settings)
            {
                enabled = false;
                return;
            }

            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
        }

        private void Update()
        {
            inputData = inputProvider.InputData;
        }

        public void ResetBoard()
        {
            Body.position = Vector3.up * 0.5f;
            Body.rotation = Quaternion.identity;

            Body.velocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
        }

        private void FixedUpdate()
        {
            Body.centerOfMass = centerOfMass;
            
            var steerAngle = Mathf.Lerp(settings.steerAngleMin, settings.steerAngleMax, GetForwardSpeed(true) / GetMaxSpeed());
            SteerAngle += (inputData.steer * steerAngle - SteerAngle) * (1.0f - settings.steerInputSmoothing);

            wheelsOnGround = 0;
            var up = Vector3.zero;
            foreach (var wheel in wheels)
            {
                if (!wheel.isOnGround) continue;

                wheelsOnGround++;
                up += wheel.groundHit.normal;
            }

            var wasOnGround = isOnGround;
            isOnGround = wheelsOnGround > 0;
            
            if (isOnGround)
            {
                this.up = up.normalized;
                if (!wasOnGround)
                {
                    var velocity = Body.velocity;
                    velocity -= transform.forward * Vector3.Dot(velocity, transform.forward);
                    
                    LandEvent?.Invoke(velocity);
                }
            }

            ApplyPushForce();
            ApplyDownForce();
            ApplyGravity();
            
            wheels[0].SteerAngle = SteerAngle;
            wheels[1].SteerAngle = SteerAngle;
        }

        private void ApplyGravity()
        {
            if (!Body.useGravity) return;

            Body.AddForce(Physics.gravity * (settings.gravityScale - 1.0f), ForceMode.Acceleration);
        }

        private void ApplyDownForce()
        {
            if (wheelsOnGround < 4) return;

            var fwdSpeed = Mathf.Abs(GetForwardSpeed());
            Body.AddForce(-transform.up * settings.downforce * fwdSpeed);
        }

        private void ApplyPushForce()
        {
            var forwardSpeed = Vector3.Dot(transform.forward, Body.velocity);
            var throttle = inputData.throttle;
            var target = Mathf.Sign(throttle) * GetMaxSpeed();
            throttle = Mathf.Abs(throttle);

            var force = (target - forwardSpeed) * settings.acceleration * throttle;
            wheels[2].DriveForce = force;
            wheels[3].DriveForce = force;
        }

        public float GetMaxSpeed()
        {
            var throttle = inputData.throttle;
            return throttle > float.Epsilon ? settings.maxForwardSpeed : settings.maxReverseSpeed;
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawSphere(centerOfMass, 0.02f);
        }
    }
}