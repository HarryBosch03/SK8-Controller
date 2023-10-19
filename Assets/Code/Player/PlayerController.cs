using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private SkateboardSettings settings;

    [SerializeField] private InputAction throttleAction;
    [SerializeField] private InputAction discreteTurnAction;
    [SerializeField] public int logIndex;

    [SerializeField] [Range(-1.0f, 1.0f)] private float rawSteerInput;

    private Camera mainCam;
    private bool useMouse;
    private bool isOnGround;

    private bool pushTrigger;

    private Truck[] trucks = new Truck[4];
    private Transform board;

    public Rigidbody Body { get; private set; }
    public float Steer { get; private set; }

    private void Awake()
    {
        GetFromHierarchy();
        mainCam = Camera.main;
    }

    private void GetFromHierarchy()
    {
        if (!Body) Body = GetComponent<Rigidbody>();

        trucks[0] = new Truck(this, "Skateboard/Truck.Front", -1);
        trucks[1] = new Truck(this, "Skateboard/Truck.Front", 1);
        trucks[2] = new Truck(this, "Skateboard/Truck.Rear", -1);
        trucks[3] = new Truck(this, "Skateboard/Truck.Rear", 1);
        if (!board) board = transform.Find("Skateboard/Board");
    }

    private void OnEnable()
    {
        if (!settings)
        {
            enabled = false;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;

        throttleAction.Enable();
        discreteTurnAction.Enable();
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;

        throttleAction.Disable();
        discreteTurnAction.Disable();
    }

    private void Update()
    {
        var mouse = Mouse.current;
        var delta = mouse.delta.ReadValue();
        var discreteTurn = discreteTurnAction.ReadValue<float>();

        if (useMouse)
        {
            rawSteerInput += delta.x * settings.steerSensitivity;
            rawSteerInput = Mathf.Clamp(rawSteerInput, -1.0f, 1.0f);

            if (Mathf.Abs(discreteTurn) > 0.1f) useMouse = false;
        }
        else
        {
            rawSteerInput = discreteTurn;

            if (delta.magnitude >= 0.5f) useMouse = true;
        }

        if (throttleAction?.WasPerformedThisFrame() ?? false)
        {
            pushTrigger = true;
        }
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
        Steer += (rawSteerInput * settings.maxSteer - Steer) * (1.0f - settings.steerInputSmoothing);

        UpdateTrucks();
        UpdateBoardVisuals();
        ApplyResistance();
        ApplyPushForce();
        ApplyUprightForces();
    }

    private void ApplyUprightForces()
    {
        var cross = Vector3.Cross(transform.up, Vector3.up);
        Body.AddTorque(cross * settings.uprightForce);
    }

    private void ApplyPushForce()
    {
        var forwardSpeed = Vector3.Dot(transform.forward, Body.velocity);
        var throttle = throttleAction.ReadValue<float>();
        var target = Mathf.Sign(throttle) * settings.maxSpeed;
        throttle = Mathf.Abs(throttle);

        var force = transform.forward * (target - forwardSpeed) * settings.acceleration * throttle;
        Body.AddForce(force, ForceMode.Acceleration);
    }

    private void UpdateBoardVisuals()
    {
        board.localRotation = Quaternion.Euler(0.0f, 0.0f, -Steer / settings.maxSteer * settings.boardRoll);
    }

    private void UpdateTrucks()
    {
        var c = 0;
        foreach (var e in trucks)
        {
            e.Process();
            if (!e.isOnGround) continue;

            c++;
        }

        isOnGround = c > 0;
    }

    private void ApplyResistance()
    {
        var velocity = Body.velocity;

        var force = -velocity.normalized * velocity.sqrMagnitude * settings.airResistance;
        force -= Vector3.Project(velocity, transform.forward) * settings.rollingResistance;
        Body.AddForce(force, ForceMode.Acceleration);
    }

    private void OnDrawGizmos()
    {
        if (!settings) return;

        if (!Application.isPlaying) GetFromHierarchy();
        foreach (var e in trucks) e.DrawGizmos();
    }

    public class Truck
    {
        public PlayerController controller;
        public Transform transform;
        public int sign;
        public int tangentSign;

        public RaycastHit groundHit;
        public bool isOnGround;
        public Ray groundRay;
        public float groundRayLength;

        private List<(Vector3, Vector3, Vector3)> log = new();

        public Vector3 Position => transform.position + transform.right * tangentSign * Settings.truckWidth;

        private SkateboardSettings Settings => controller.settings;

        public Truck(PlayerController controller, string path, int tangentSign)
        {
            this.controller = controller;

            transform = controller.transform.Find(path);
            sign = transform.localPosition.z > 0.0f ? 1 : -1;

            this.tangentSign = tangentSign;
        }

        public void Process()
        {
            Orient();
            LookForGround();
            Depenetrate();
            ApplySidewaysFriction();
        }

        private void Orient()
        {
            transform.localRotation = Quaternion.Euler(0.0f, controller.Steer * sign, 0.0f);
        }

        private void LookForGround()
        {
            GetGroundRay();
            isOnGround = Physics.Raycast(groundRay, out groundHit, groundRayLength);

            isOnGround = false;
            var results = Physics.RaycastAll(groundRay, groundRayLength);
            var best = float.MaxValue;

            foreach (var e in results)
            {
                if (e.transform.IsChildOf(transform)) continue;
                if (e.distance > best) continue;

                best = e.distance;
                groundHit = e;
                isOnGround = true;
            }
        }

        private void GetGroundRay()
        {
            var localPosition = controller.transform.InverseTransformPoint(Position);
            groundRayLength = Settings.distanceToGround - localPosition.y;
            groundRay = new Ray(Position, -transform.up);

            Debug.DrawLine(groundRay.origin, groundRay.GetPoint(groundRayLength), Color.yellow);
        }

        private void Depenetrate()
        {
            var force = Vector3.zero;

            if (isOnGround)
            {
                force = groundHit.normal * (groundRayLength - groundHit.distance) * Settings.truckDepenetrationSpring;

                var velocity = controller.Body.GetPointVelocity(Position);
                var dot = Vector3.Dot(-velocity, groundHit.normal);
                force += groundHit.normal * dot * Settings.truckDepenetrationDamper;
                controller.Body.AddForceAtPosition(force, Position);
            }

            log.Add((controller.transform.position, Position, force));
            if (log.Count > 5000) log.RemoveAt(0);
        }

        private void ApplySidewaysFriction()
        {
            if (!isOnGround) return;

            var velocity = controller.Body.GetPointVelocity(Position);
            var dot = Vector3.Dot(transform.right, -velocity);
            var force = transform.right * dot * Settings.tangentialFriction;

            controller.Body.AddForceAtPosition(force, Position);
        }

        public void DrawGizmos()
        {
            if (log.Count > 0)
            {
                var i = log.Count - 1 - controller.logIndex;
                if (i < 0) i = 0;
                if (i >= log.Count) i = log.Count - 1;

                var e = log[i];
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(e.Item1, e.Item2);
                Gizmos.DrawRay(e.Item2, e.Item3);
            }

            GetGroundRay();
        }
    }
}