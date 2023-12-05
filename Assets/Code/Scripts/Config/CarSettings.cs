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
        public float downforce;
        public float gravityScale = 3.0f;
    }
}