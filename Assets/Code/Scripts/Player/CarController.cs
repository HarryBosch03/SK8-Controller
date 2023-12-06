using System;
using System.Collections.Generic;
using SK8Controller.Config;
using SK8Controller.Utilities;
using UnityEngine;

namespace SK8Controller.Player
{
    public class CarController : MonoBehaviour
    {
        public CarSettings settings;
        public Vector3 centerOfMass;

        [Range(0.0f, 1.0f)]
        public float friction;

        [HideInInspector] public Vector3 up;
        private PlayerInputProvider inputProvider;
        private PlayerInputData inputData;
        [HideInInspector] public bool isOnGround;
        private Transform model;

        private ParticleSystem[] driftParticles;
        private ParticleSystem[] driftBoostParticles;
        private ParticleSystem boostLines;

        private float driftBoostTimer;
        private float boostTimer;
        private float boostScalar;

        private int driftSign;
        private bool IsDrifting => driftSign != 0;

        public Rigidbody Body { get; private set; }
        public float SteerAngle { get; private set; }

        public event System.Action<Vector3> LandEvent;

        public float GetForwardSpeed(bool abs = false)
        {
            var v = Vector3.Dot(Body.velocity, transform.forward);
            return abs ? Mathf.Abs(v) : v;
        }

        private void Awake() { GetFromHierarchy(); }

        private void GetFromHierarchy()
        {
            if (!Body) Body = GetComponent<Rigidbody>();

            model = transform.Find("Model");
            inputProvider = GetComponent<PlayerInputProvider>();

            driftParticles = new[]
            {
                transform.Find<ParticleSystem>("DriftFX.L/Smoke"),
                transform.Find<ParticleSystem>("DriftFX.R/Smoke"),
            };

            driftBoostParticles = new[]
            {
                transform.Find<ParticleSystem>("DriftFX.L/Sparks"),
                transform.Find<ParticleSystem>("DriftFX.R/Sparks"),
            };

            boostLines = transform.Find<ParticleSystem>("BoostLines");
            boostLines.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        private void OnEnable()
        {
            if (!settings)
            {
                enabled = false;
                return;
            }

            up = transform.up;

            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnDisable() { Cursor.lockState = CursorLockMode.None; }

        private void Update()
        {
            inputData = inputProvider.InputData;

            var roll = Vector3.Dot(transform.up, Body.angularVelocity) * settings.turnRoll;
            model.transform.localRotation = Quaternion.Euler(Vector3.forward * roll);

            if (inputData.drift)
            {
                if (!IsDrifting && Mathf.Abs(inputData.steer) > 0.1f)
                {
                    driftSign = inputData.steer > 0.0f ? 1 : -1;
                }
            }
            else
            {
                driftSign = 0;
            }

            if (IsDrifting)
            {
                if (inputData.steer * driftSign < 0.0f) inputData.steer = 0.0f;
            }
            
            UpdateDriftFX();
        }

        private void FixedUpdate()
        {
            Body.centerOfMass = centerOfMass;

            var steerAngle = Mathf.Lerp(settings.steerAngleMin, settings.steerAngleMax, GetForwardSpeed(true) / GetMaxSpeed());
            SteerAngle += (inputData.steer * steerAngle - SteerAngle) * (1.0f - settings.steerInputSmoothing);

            Drift();
            CheckForGround();
            ApplyDriveForce();
            ApplyTorque();
            ApplyFriction();
            ApplyGravity();
        }

        private void Drift()
        {
            if (IsDrifting)
            {
                if (GetForwardSpeed() / settings.maxForwardSpeed < 0.1f) driftSign = 0;
            }

            if (IsDrifting)
            {
                driftBoostTimer += Time.deltaTime * Mathf.Abs(inputData.steer);
            }
            else if (driftBoostTimer > 0.0f)
            {
                if (driftBoostTimer >= settings.driftTimeForBoost)
                {
                    boostScalar = Mathf.Max(boostScalar, settings.driftBoostPower);
                    boostTimer = Mathf.Max(boostTimer, settings.driftBoostDuration);
                }

                driftBoostTimer = 0.0f;
            }
        }

        private void UpdateDriftFX()
        {
            foreach (var e in driftParticles)
            {
                if (IsDrifting != e.isEmitting)
                {
                    if (IsDrifting) e.Play();
                    else e.Stop();
                }
            }

            var boostCharged = driftBoostTimer > settings.driftTimeForBoost && IsDrifting;
            foreach (var e in driftBoostParticles)
            {
                if (boostCharged != e.isEmitting)
                {
                    if (!e.isEmitting) e.Play();
                    else e.Stop();
                }
            }

            if (boostTimer > 0.0f != boostLines.isEmitting)
            {
                if (!boostLines.isEmitting) boostLines.Play();
                else boostLines.Stop();
            }
        }

        private void ApplyTorque()
        {
            if (!isOnGround) return;

            Body.angularVelocity = transform.up * SteerAngle * Mathf.Deg2Rad * GetForwardSpeed() / settings.maxForwardSpeed;
        }

        private void CheckForGround()
        {
            var ray = new Ray(Body.position + up, -up);
            var hits = Physics.RaycastAll(ray, 1.0f + settings.castExtension);

            isOnGround = false;
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;

                isOnGround = true;
                up = hit.normal;

                var fwd = Vector3.Cross(transform.right, up).normalized;
                var rotation = Quaternion.LookRotation(fwd, up);

                Body.rotation = Quaternion.Slerp(rotation, Body.rotation, settings.rotationSmoothing);
                Body.position += Vector3.Project(hit.point - Body.position, hit.normal);
                Body.velocity += Vector3.Project(-Body.velocity, hit.normal);
                break;
            }
        }

        private void ApplyFriction()
        {
            if (!isOnGround) return;

            var tFriction = IsDrifting ? settings.driftFriction : settings.friction;
            friction = Mathf.Lerp(tFriction, friction, settings.frictionSmoothing);

            var force = Vector3.Project(-Body.velocity, transform.right) * friction;
            Body.velocity += force;
        }

        private void ApplyGravity()
        {
            if (!Body.useGravity) return;

            Body.AddForce(Physics.gravity * (settings.gravityScale - 1.0f), ForceMode.Acceleration);
        }

        private void ApplyDriveForce()
        {
            if (!isOnGround) return;

            var throttle = inputData.throttle;
            if (boostTimer > 0.0f) throttle = boostScalar;

            var target = throttle * GetMaxSpeed();

            var force = (target - GetForwardSpeed()) * settings.acceleration;
            if (boostTimer > 0.0f) force *= boostScalar;
            Body.AddForce(transform.forward * force);

            boostTimer -= Time.deltaTime;
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

        private void OnCollisionEnter(Collision collision)
        {
            if (IsDrifting) driftSign = 0;
        }
    }
}