using SK8Controller.Code.Maths;
using UnityEngine;
using UnityEngine.Serialization;


[CreateAssetMenu(menuName = "Config/Skateboard Physics Settings")]
public class SkateboardSettings : ScriptableObject
{
    public float steerSensitivity = 0.1f;
    public float maxSteer = 3.0f;
    public float boardRoll = 5.0f;
    [Range(0.0f, 1.0f)] public float steerInputSmoothing = 0.9f;

    [Space]
    public float acceleration = 1.0f;
    public float maxSpeed = 20.0f;

    [FormerlySerializedAs("leanSpring")] public float leanForce = 0.2f;
    public float leanDamping = 0.02f;
    [Range(0.0f, 50.0f)] public float rollingResistance;
    [Range(0.0f, 50.0f)] public float airResistance;
    [Range(0.0f, 50.0f)] public float tangentialFriction = 20.0f;

    [Space]
    public float distanceToGround = 0.13f;
    public Vector3 truckOffset;
    public float truckDepenetrationSpring = 100.0f;
    public float truckDepenetrationDamper = 10.0f;
    public float wheelRadius = 0.12f;

    [Space]
    public float flipCheckDistance = 0.1f;
    [FormerlySerializedAs("flipJumpCooldown")] public float jumpCooldown = 1.0f;
    public float flipJumpForce = 0.1f;

    [Space]
    public float airborneVelocityAlignSpring;
    public float airborneVelocityAlignDamper;
}