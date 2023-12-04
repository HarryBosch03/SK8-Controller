using SK8Controller.Player;
using UnityEngine;

[SelectionBase, DisallowMultipleComponent]
public class ResetModifier : MonoBehaviour
{
    [SerializeField] private float duration = 1.0f;
    
    private CarController target;

    private float timer;
    
    private void Awake()
    {
        target = FindObjectOfType<CarController>();
    }

    private void FixedUpdate()
    {
        if (timer > duration)
        {
            target.Body.position = transform.position;
            target.Body.rotation = transform.rotation;
            target.Body.velocity = Vector3.zero;
            target.Body.angularVelocity = Vector3.zero;
            
            timer -= duration;
        }
        timer += Time.deltaTime;
    }
}