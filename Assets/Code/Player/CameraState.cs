using UnityEngine;

namespace SK8Controller.Player
{
    public abstract class CameraState : ScriptableObject
    {
        public virtual void Enter(SkateboardCamera camera) { }
        public virtual void Tick(SkateboardCamera camera) { }
        public virtual void Exit(SkateboardCamera camera) { }
    }
}