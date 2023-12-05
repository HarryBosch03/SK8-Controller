
using UnityEngine;

namespace SK8Controller.Player
{
    [System.Serializable]
    public struct PlayerInputData
    {
        public float throttle;
        public Vector2 lean;
        public float steer;
        public bool drift;
    }
}