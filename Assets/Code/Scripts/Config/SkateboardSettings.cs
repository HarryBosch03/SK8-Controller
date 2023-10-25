using UnityEngine;


[CreateAssetMenu(menuName = "Config/Skateboard Physics Settings")]
public class SkateboardSettings : ScriptableObject
{
    public float steerSensitivity = 0.1f;
    public float maxSteer = 3.0f;
    [Range(0.0f, 1.0f)] public float steerInputSmoothing = 0.9f;

    [Space]
    public float acceleration = 1.0f;
    public float maxSpeed = 20.0f;
    public float downforce;
}