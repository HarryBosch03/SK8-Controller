using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputAction pushAction;
    [SerializeField] private InputAction flipAction;
    [SerializeField] private InputAction resetAction;
    [SerializeField] private InputAction discreteTurnAction;
    
    [Space]
    [SerializeField] private float steerSensitivity;
    [SerializeField] private float maxSteer;
    [SerializeField] private float boardRoll;
    [SerializeField][Range(0.0f, 1.0f)] private float steerInputSmoothing;
    [SerializeField][Range(-1.0f, 1.0f)] private float rawSteerInput;

    [Space]
    [SerializeField] private float pushDuration = 1.0f;
    [SerializeField] private AnimationCurve pushForceCurve = AnimationCurve.Constant(0.0f, 1.0f, 1.0f);
    [SerializeField] private float pushForceMagnitude = 1.0f;

    [Space]
    [SerializeField] private float uprightForce;

    [Space]
    [SerializeField][Range(0.0f, 50.0f)] private float rollingResistance;
    [SerializeField][Range(0.0f, 50.0f)] private float airResistance;
    [SerializeField][Range(0.0f, 50.0f)] private float tangentialFriction = 20.0f;

    [Space]
    public float distanceToGround;
    public float tangentOffset;
    public float truckDepenetrationSpring;
    public float truckDepenetrationDamper;

    private Camera mainCam;

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

        rawSteerInput += delta.x * steerSensitivity;
        rawSteerInput = Mathf.Clamp(rawSteerInput, -1.0f, 1.0f);

        var discreteTurn = discreteTurnAction.ReadValue<float>();
        if (Mathf.Abs(discreteTurn) > 0.1f)
        {
            rawSteerInput = discreteTurn;
        }
        
        if (resetAction.WasPerformedThisFrame())
        {
            Body.position = Vector3.up * 0.5f;
            Body.rotation = Quaternion.identity;
            Body.velocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
        }

        if (pushAction?.WasPerformedThisFrame() ?? false)
        {
            pushTrigger = true;
        }

        if (flipAction.WasPerformedThisFrame())
        {
            transform.rotation *= Quaternion.Euler(0.0f, 180.0f, 0.0f);
        }

        var cross = Vector3.Cross(transform.up, Vector3.up);
        Body.AddTorque(cross * uprightForce, ForceMode.Acceleration);
    }

    private void FixedUpdate()
    {
        Steer += (rawSteerInput * maxSteer - Steer) * (1.0f - steerInputSmoothing);

        UpdateTrucks();
        UpdateBoardVisuals();
        ApplyResistance();
        ApplyPushForce();
    }

    private void ApplyPushForce()
    {
        if (pushTimer > 0.0f)
        {
            var force = transform.forward * pushForceMagnitude * pushForceCurve.Evaluate(pushDuration - pushTimer) / pushDuration;
            Body.AddForce(force, ForceMode.Acceleration);

            pushTimer -= Time.deltaTime;
        }
        else if (pushTrigger)
        {
            pushTrigger = false;
            pushTimer = pushDuration;
        }
    }

    private void UpdateBoardVisuals()
    {
        board.localRotation = Quaternion.Euler(0.0f, 0.0f, -Steer / maxSteer * boardRoll);
    }

    private void UpdateTrucks()
    {
        foreach (var e in trucks) e.FixedUpdate();
    }

    private void ApplyResistance()
    {
        var velocity = Body.velocity;

        var force = -velocity.normalized * velocity.sqrMagnitude * airResistance;
        force -= Vector3.Project(velocity, transform.forward) * rollingResistance;
        Body.AddForce(force, ForceMode.Acceleration);
    }

    private void OnDrawGizmos()
    {
        GetFromHierarchy();
        foreach (var e in trucks) e.DrawGizmos();
    }
    
    public class Truck
    {
        public PlayerController controller;
        public Transform transform;
        public int sign;
        public int tangentSign;

        private RaycastHit groundHit;
        private bool isOnGround;
        private Ray groundRay;
        private float groundRayLength;
        
        private (Vector3, Vector3)[] gizmoLines = new (Vector3, Vector3)[3];

        public Vector3 Position => transform.position + transform.right * tangentSign * controller.tangentOffset;
        
        public Truck(PlayerController controller, string path, int tangentSign)
        {
            this.controller = controller;
            
            transform = controller.transform.Find(path);
            sign = transform.localPosition.z > 0.0f ? 1 : -1;
            
            this.tangentSign = tangentSign;
        }

        public void FixedUpdate()
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
            groundRayLength = controller.distanceToGround - localPosition.y;
            groundRay = new Ray(Position, -transform.up);
            
            //gizmoLines[0] = (groundRay.origin, groundRay.GetPoint(groundRayLength));
        }
        
        private void Depenetrate()
        {
            if (!isOnGround) return;
            
            var force = groundHit.normal * (groundRayLength - groundHit.distance) * controller.truckDepenetrationSpring;
            
            var velocity = controller.Body.GetPointVelocity(Position);
            force += Vector3.Project(-velocity, groundHit.normal) * controller.truckDepenetrationDamper;
            
            controller.Body.AddForceAtPosition(force, Position, ForceMode.Acceleration);
        }

        private void ApplySidewaysFriction()
        {
            if (!isOnGround) return;

            var velocity = controller.Body.GetPointVelocity(transform.position);
            var dot = Vector3.Dot(transform.right, -velocity);
            var force = transform.right * dot * controller.tangentialFriction;
            
            controller.Body.AddForceAtPosition(force, transform.position, ForceMode.Acceleration);
        }

        public void DrawGizmos()
        {
            GetGroundRay();

            var colors = new[]
            {
                Color.yellow, Color.cyan, Color.magenta,
            };

            for (var i = 0; i < 3; i++)
            {
                Drawing.DrawLine(gizmoLines[i].Item1, gizmoLines[i].Item2, colors[i]);
            }
        }
    }
}