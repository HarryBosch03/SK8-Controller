using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private SkateboardSettings settings;

    [SerializeField] private InputAction pushAction;
    [SerializeField] private InputAction flipAction;
    [SerializeField] private InputAction resetAction;
    [SerializeField] private InputAction discreteTurnAction;

    [SerializeField] [Range(-1.0f, 1.0f)] private float rawSteerInput;

    private Camera mainCam;
    private bool useMouse;
    private bool isOnGround;
    private bool reset;

    private bool pushTrigger;
    private float pushTimer;

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

        pushAction.Enable();
        flipAction.Enable();
        resetAction.Enable();
        discreteTurnAction.Enable();
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;

        pushAction.Disable();
        flipAction.Disable();
        resetAction.Disable();
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

        if (resetAction.WasPerformedThisFrame())
        {
            reset = true;
        }

        if (pushAction?.WasPerformedThisFrame() ?? false)
        {
            pushTrigger = true;
        }

        if (flipAction.WasPerformedThisFrame())
        {
            transform.rotation *= Quaternion.Euler(0.0f, 180.0f, 0.0f);
            Body.AddForce(-Vector3.Project(Body.velocity, transform.forward), ForceMode.VelocityChange);
        }
    }

    public void ResetBoard()
    {
        reset = false;

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

        if (reset) ResetBoard();
    }

    private void ApplyUprightForces()
    {
        var cross = Vector3.Cross(transform.up, Vector3.up);
        Body.AddTorque(cross * settings.uprightForce, ForceMode.Acceleration);
    }

    private void ApplyPushForce()
    {
        if (pushTimer > 0.0f)
        {
            if (!isOnGround)
            {
                pushTimer = 0.0f;
                return;
            }

            var forwardSpeed = Vector3.Dot(transform.forward, Body.velocity);
            var force = transform.forward * (settings.pushMaxSpeed - forwardSpeed) * settings.pushForce * settings.pushForceCurve.Evaluate(settings.pushDuration - pushTimer) / settings.pushDuration;
            Body.AddForce(force, ForceMode.Acceleration);

            pushTimer -= Time.deltaTime;
        }
        else if (pushTrigger)
        {
            pushTrigger = false;
            pushTimer = settings.pushDuration;
        }
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

        GetFromHierarchy();
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
            if (!isOnGround) return;

            var force = groundHit.normal * (groundRayLength - groundHit.distance) * Settings.truckDepenetrationSpring;

            var velocity = controller.Body.GetPointVelocity(Position);
            var dot = Vector3.Dot(-velocity, groundHit.normal);
            force += groundHit.normal * dot * Settings.truckDepenetrationDamper;

            controller.Body.AddForceAtPosition(force, Position, ForceMode.Acceleration);
        }

        private void ApplySidewaysFriction()
        {
            if (!isOnGround) return;

            var velocity = controller.Body.GetPointVelocity(transform.position);
            var dot = Vector3.Dot(transform.right, -velocity);
            var force = transform.right * dot * Settings.tangentialFriction;

            controller.Body.AddForceAtPosition(force, transform.position, ForceMode.Acceleration);
        }

        public void DrawGizmos()
        {
            GetGroundRay();
        }
    }
}