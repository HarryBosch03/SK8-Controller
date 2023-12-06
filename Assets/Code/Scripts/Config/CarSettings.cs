using UnityEngine;

namespace SK8Controller.Config
{
    [CreateAssetMenu(menuName = "Config/Skateboard Physics Settings")]
    public class CarSettings : ScriptableObject
    {
        public float steerAngleMin = 4.0f;
        public float steerAngleMax = 4.0f;
        [Range(0.0f, 1.0f)] public float steerInputSmoothing = 0.9f;

        [Space]
        public float acceleration = 1.0f;
        public float maxForwardSpeed = 80.0f;
        public float maxReverseSpeed = 20.0f;
        public float gravityScale = 3.0f;
        public float castExtension = 0.5f;
        [Range(0.0f, 1.0f)]
        public float friction = 0.8f;
        [Range(0.0f, 1.0f)]
        public float driftFriction = 0.2f;
        [Range(0.0f, 1.0f)]
        public float frictionSmoothing;
        [Range(0.0f, 1.0f)]
        public float rotationSmoothing = 0.2f;
        public float turnRoll = 0.5f;

        [Space]
        public float driftTimeForBoost = 0.6f;
        public float driftBoostPower = 1.5f;
        public float driftBoostDuration = 0.4f;
    }
}