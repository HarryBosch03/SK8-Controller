using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private SkateboardSettings settings;

    [SerializeField] private InputAction pushAction;
    [SerializeField] private InputAction flipAction;
    [SerializeField] private InputAction resetAction;
    [SerializeField] private InputAction discreteTurnAction;

    private Truck frontTruck;
    private Truck rearTruck;
    private float truckDistance;
    private bool reset;

    private Vector3 startPosition;
    private Quaternion startRotation;

    public float pushTimer;

    private void Awake()
    {
        frontTruck = new Truck(this, "Skateboard/Truck.Front");
        rearTruck = new Truck(this, "Skateboard/Truck.Rear");
        truckDistance = (frontTruck.position - rearTruck.position).magnitude;
    }

    private void OnEnable()
    {
        pushAction.Enable();
        flipAction.Enable();
        resetAction.Enable();
        discreteTurnAction.Enable();
    }

    private void OnDisable()
    {
        pushAction.Disable();
        flipAction.Disable();
        resetAction.Disable();
        discreteTurnAction.Disable();
    }

    private void Update()
    {
        if (pushTimer >= 0.0f && pushAction.WasPressedThisFrame())
        {
            pushTimer += settings.pushDuration;
        }

        if (resetAction.WasPressedThisFrame()) reset = true;
    }

    private void FixedUpdate()
    {
        if (reset)
        {
            reset = false;
            transform.position = Vector3.up;
            transform.rotation = Quaternion.identity;

            frontTruck.velocity = Vector3.zero;
            rearTruck.velocity = Vector3.zero;
        }
        
        var position = (frontTruck.position + rearTruck.position) / 2.0f;
        var velocity = (frontTruck.velocity + rearTruck.velocity) / 2.0f;
        var force = Vector3.zero;

        if (pushTimer > 0.0f)
        {
            var t = 1.0f - (pushTimer / settings.pushDuration);
            var magnitude = settings.pushCurve.Evaluate(t);
            var forwardSpeed = Vector3.Dot(transform.forward, velocity);
            force += transform.forward * (settings.pushMaxSpeed - forwardSpeed) * magnitude;
            
            pushTimer -= Time.deltaTime;
        }
        
        frontTruck.force += force;
        rearTruck.force += force;
        
        frontTruck.FixedUpdate();
        rearTruck.FixedUpdate();
        
        var center = (frontTruck.position + rearTruck.position) / 2.0f;
        var normal = (frontTruck.normal + rearTruck.normal).normalized;
        
        var v = (frontTruck.position - center).normalized;
        frontTruck.position = center + v * truckDistance / 2.0f;
        rearTruck.position = center - v * truckDistance / 2.0f;
        
        transform.rotation = Quaternion.LookRotation(v, normal);
        transform.position = center;
        frontTruck.transform.position = frontTruck.position;
        rearTruck.transform.position = rearTruck.position;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawRay(transform.position, -transform.up * settings.groundDistance);
    }

    public class Truck
    {
        public PlayerController controller;
        public Transform transform;

        public Vector3 position;
        public Vector3 velocity;
        public Vector3 force;
        public Vector3 normal;

        public Truck(PlayerController controller, string path)
        {
            this.controller = controller;
            transform = controller.transform.Find(path);
            position = transform.position;
            normal = transform.up;
        }

        public void FixedUpdate()
        {
            position = transform.position;

            Collide();
            Integrate();
        }

        private void Collide()
        {
            var ray = new Ray(position, -transform.up);
            var distance = controller.settings.groundDistance;

            if (!Physics.Raycast(ray, out var hit, distance)) return;
            
            position = hit.point + transform.up * distance;
            normal = hit.normal;

            var dot = Vector3.Dot(hit.normal, velocity);
            velocity += hit.normal * Mathf.Max(0.0f, -dot);
        }

        public void Integrate()
        {
            position += velocity * Time.deltaTime;
            velocity += force * Time.deltaTime;
            force = Physics.gravity;
        }
    }
}