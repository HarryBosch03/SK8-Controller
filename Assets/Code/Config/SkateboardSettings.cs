using SK8Controller.Code.Maths;
using UnityEngine;


[CreateAssetMenu(menuName = "Config/Skateboard Physics Settings")]
public class SkateboardSettings : ScriptableObject
{
    public float steerSensitivity = 0.1f;
    public float maxSteer = 3.0f;
    public float boardRoll = 5.0f;
    [Range(0.0f, 1.0f)] public float steerInputSmoothing = 0.9f;

    [Space]
    public float pushDuration = 1.0f;
    public AnimationCurve pushForceCurve = AnimationCurve.Constant(0.0f, 1.0f, 1.0f);
    public float pushForce = 1.0f;
    public float pushMaxSpeed = 10.0f;

    [Space]
    public float uprightForce = 4.0f;
    [Range(0.0f, 50.0f)] public float rollingResistance;
    [Range(0.0f, 50.0f)] public float airResistance;
    [Range(0.0f, 50.0f)] public float tangentialFriction = 20.0f;

    [Space]
    public float distanceToGround = 0.13f;
    public float truckWidth = 0.09f;
    public float truckDepenetrationSpring = 100.0f;
    public float truckDepenetrationDamper = 10.0f;

    [Space]
    public float airborneVelocityAlignSpring;
    public float airborneVelocityAlignDamper;
}