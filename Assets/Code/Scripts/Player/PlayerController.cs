using System;
using SK8Controller.Player;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private SkateboardSettings settings;

    [SerializeField] private InputAction throttleAction;
    [SerializeField] private InputAction leanAction;
    [SerializeField] private InputAction discreteTurnAction;
    [SerializeField] private Vector3 centerOfMass;

    [SerializeField] [Range(-1.0f, 1.0f)] private float rawSteerInput;

    private Vector3 up;
    private bool useMouse;
    public int wheelsOnGround;
    private Wheel[] wheels = new Wheel[4];

    public Rigidbody Body { get; private set; }
    public float Steer { get; private set; }

    public float GetForwardSpeed() => Vector3.Dot(Body.velocity, transform.forward);

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
        discreteTurnAction.Enable();
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;

        throttleAction.Disable();
        leanAction.Disable();
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

        var steer = Mathf.Pow(Steer, settings.steerExponent * 2 + 1);
        
        wheels[0].SteerAngle = Steer;
        wheels[1].SteerAngle = Steer;
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
        
        Steer += (rawSteerInput * settings.maxSteer - Steer) * (1.0f - settings.steerInputSmoothing);

        wheelsOnGround = 0;
        var up = Vector3.zero;
        foreach (var wheel in wheels)
        {
            if (!wheel.isOnGround) continue;
            
            wheelsOnGround++;
            up += wheel.groundHit.normal;
        }

        if (wheelsOnGround > 0)
        {
            this.up = up.normalized;
        }
        
        ApplyPushForce();
        ApplyDownForce();
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
        var throttle = throttleAction.ReadValue<float>();
        var target = Mathf.Sign(throttle) * settings.maxSpeed;
        throttle = Mathf.Abs(throttle);

        var force = (target - forwardSpeed) * settings.acceleration * throttle;
        wheels[2].DriveForce = force;
        wheels[3].DriveForce = force;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawSphere(centerOfMass, 0.02f);
    }
}