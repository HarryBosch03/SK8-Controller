
using UnityEngine;

namespace SK8Controller.Player
{
    [CreateAssetMenu(menuName = "Config/Wheel Settings")]
    public class WheelSettings : ScriptableObject
    {
        public float spring;
        public float damper;
        [Range(0.0f, 1.0f)] public float tangentialFriction;
    }
}