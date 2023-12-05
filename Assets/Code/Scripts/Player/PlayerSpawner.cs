using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SK8Controller.Player
{
    [SelectionBase, DisallowMultipleComponent]
    public class PlayerSpawner : MonoBehaviour
    {
        public InputAction joinAction;
        public PlayerInputProvider playerPrefab;

        private List<InputDevice> trackedDevices = new();
        
        private void OnEnable()
        {
            joinAction.Enable();
            joinAction.performed += OnJoinPerformed;
        }
        
        private void OnDisable()
        {
            joinAction.performed -= OnJoinPerformed;
            joinAction.Disable();
        }
        
        private void OnJoinPerformed(InputAction.CallbackContext ctx)
        {
            var device = ctx.control.device;
            if (trackedDevices.Contains(device)) return;
            
            trackedDevices.Add(device);
            var instance = Instantiate(playerPrefab);
            instance.BindToDevice(device);
        }
    }
}