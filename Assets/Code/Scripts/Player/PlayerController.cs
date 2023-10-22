using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private SkateboardSettings settings;

    [SerializeField] private InputAction throttleAction;
    [SerializeField] private InputAction leanAction;
    [SerializeField] private InputAction jumpAction;
    [SerializeField] private InputAction discreteTurnAction;
    [SerializeField] public int logIndex;

    [SerializeField] [Range(-1.0f, 1.0f)] private float rawSteerInput;

    private Camera mainCam;
    private bool useMouse;
    public bool isOnGround;
    private int wheelsOnGround;
    private Vector2 leanInput;

    public bool jump;
    public bool flipped;
    public float jumpTimer;

    public Truck[] trucks = new Truck[4];
    private Transform board;

    public Rigidbody Body { get; private set; }
    public float Steer { get; private set; }

    public float GetForwardSpeed() => Vector3.Dot(Body.velocity, transform.forward);

    private void Awake()
    {
        GetFromHierarchy();
        mainCam = Camera.main;
    }

    private void GetFromHierarchy()
    {
        if (!Body) Body = GetComponent<Rigidbody>();

        trucks[0] = new Truck(this, "Skateboard/Truck.Front/Wheel.FL", -1, 1);
        trucks[1] = new Truck(this, "Skateboard/Truck.Front/Wheel.FR", 1, 1);
        trucks[2] = new Truck(this, "Skateboard/Truck.Rear/Wheel.BL", -1, -1);
        trucks[3] = new Truck(this, "Skateboard/Truck.Rear/Wheel.BR", 1, -1);
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
        leanAction.Enable();
        jumpAction.Enable();
        discreteTurnAction.Enable();
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;

        throttleAction.Disable();
        leanAction.Disable();
        jumpAction.Disable();
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

        leanInput = leanAction.ReadValue<Vector2>();
        if (jumpAction.WasPressedThisFrame()) jump = true;
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

        CheckIfFlipped();
        UpdateTrucks();
        UpdateBoardVisuals();
        ApplyResistance();
        ApplyPushForce();
        ApplyLeanForces();
    }

    private void CheckIfFlipped()
    {
        var ray = new Ray(transform.position, transform.up);

        flipped = false;
        var hit = new RaycastHit();
        foreach (var e in Physics.RaycastAll(ray, settings.flipCheckDistance))
        {
            if (e.collider.transform.IsChildOf(transform)) continue;
            flipped = true;
            hit = e;
            break;
        }

        Debug.Log(hit.transform, hit.transform);

        var jump = this.jump;
        this.jump = false;

        if (!flipped)
        {
            jumpTimer = 0.0f;
            return;
        }

        jumpTimer += Time.deltaTime;
        if (jumpTimer > settings.jumpCooldown && jump)
        {
            Body.AddForce(hit.normal * settings.flipJumpForce * Body.mass, ForceMode.Impulse);
        }
    }

    private void ApplyLeanForces()
    {
        if (isOnGround) return;

        var lean = transform.forward * -leanInput.x + transform.right * leanInput.y;
        var torque = lean * settings.leanForce - Body.angularVelocity * settings.leanDamping;
        Body.AddTorque(torque * Body.mass);
    }

    private void ApplyPushForce()
    {
        var forwardSpeed = Vector3.Dot(transform.forward, Body.velocity);
        var throttle = throttleAction.ReadValue<float>();
        var target = Mathf.Sign(throttle) * settings.maxSpeed;
        throttle = Mathf.Abs(throttle);

        var force = transform.forward * (target - forwardSpeed) * settings.acceleration * throttle;
        force *= wheelsOnGround / 4.0f;
        Body.AddForce(force * Body.mass);
    }

    private void UpdateBoardVisuals()
    {
        board.localRotation = Quaternion.Euler(0.0f, 0.0f, -Steer / settings.maxSteer * settings.boardRoll);
    }

    private void UpdateTrucks()
    {
        wheelsOnGround = 0;
        foreach (var e in trucks)
        {
            e.Process();
            if (!e.isOnGround) continue;

            wheelsOnGround++;
        }

        isOnGround = wheelsOnGround > 0;
    }

    private void ApplyResistance()
    {
        var velocity = Body.velocity;

        var force = -velocity.normalized * velocity.sqrMagnitude * settings.airResistance;
        force -= Vector3.Project(velocity, transform.forward) * settings.rollingResistance;
        Body.AddForce(force * Body.mass, ForceMode.Acceleration);
    }

    private void OnDrawGizmos()
    {
        if (!settings) return;

        if (!Application.isPlaying) GetFromHierarchy();
        foreach (var e in trucks) e.DrawGizmos();

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.up * settings.flipCheckDistance);
    }

    public class Truck
    {
        public PlayerController controller;
        public Transform transform;

        public RaycastHit groundHit;
        public bool isOnGround;
        public Ray groundRay;
        public float groundRayLength;
        public int xSign, zSign;
        public float rotation;
        public float evaluatedTangentialFriction;

        private List<(Vector3, Vector3, Vector3)> log = new();

        public Vector3 Position => controller.transform.TransformPoint(Settings.truckOffset.x * xSign, Settings.truckOffset.y, Settings.truckOffset.z * zSign);

        private SkateboardSettings Settings => controller.settings;

        public Truck(PlayerController controller, string path, int xSign, int zSign)
        {
            this.controller = controller;

            transform = controller.transform.Find(path);

            this.xSign = xSign > 0 ? 1 : -1;
            this.zSign = zSign > 0 ? 1 : -1;
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
            if (isOnGround)
            {
                var velocity = controller.Body.GetPointVelocity(groundHit.point);
                var speed = Vector3.Dot(velocity, controller.transform.forward);
                rotation += speed / Settings.wheelRadius * Time.deltaTime * Mathf.Rad2Deg;
            }

            rotation %= 360.0f;
            transform.localRotation = Quaternion.Euler(rotation, controller.Steer * zSign, 0.0f);
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
            groundRayLength = Settings.distanceToGround - Settings.truckOffset.y;
            groundRay = new Ray(Position, -controller.transform.up);
        }

        private void Depenetrate()
        {
            var force = Vector3.zero;

            if (isOnGround)
            {
                var point = groundHit.point;
                var normal = groundHit.normal;
                force += Vector3.Project(groundHit.normal * (groundRayLength - groundHit.distance), normal) * Settings.truckDepenetrationSpring;

                var velocity = controller.Body.GetPointVelocity(point);
                var dot = Vector3.Dot(velocity, normal);
                force += normal * Mathf.Max(0.0f, -dot) * Settings.truckDepenetrationDamper;
                controller.Body.AddForceAtPosition(force / 8 * controller.Body.mass, point);

                Debug.DrawLine(Position, point, Color.red);
            }

            log.Add((controller.transform.position, Position, force));
            if (log.Count > 5000) log.RemoveAt(0);
        }

        private void ApplySidewaysFriction()
        {
            if (!isOnGround) return;

            var velocity = controller.Body.GetPointVelocity(Position);
            var dot = Vector3.Dot(transform.right, -velocity);
            evaluatedTangentialFriction = dot * Settings.tangentialFriction;
            var force = transform.right * evaluatedTangentialFriction;

            controller.Body.AddForceAtPosition(force * controller.Body.mass, Position);
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
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(groundRay.origin, groundRay.GetPoint(Settings.distanceToGround));
        }
    }
}