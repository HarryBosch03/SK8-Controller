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
        public int playerCount;
        public int columns;
        public Vector2 playerSpacing;
        public float columnOffset;

        private List<InputDevice> trackedDevices = new();
        private List<PlayerInputProvider> players = new();

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

            SpawnPlayer(device);
        }

        private void SpawnPlayer(InputDevice device)
        {
            var playerId = players.Count;
            var sp = GetSpawnPoint(playerId);
            
            var instance = Instantiate(playerPrefab, sp, transform.rotation);
            instance.BindToDevice(playerId, device);
            players.Add(instance);   
        }

        private Vector3 GetSpawnPoint(int i)
        {
            var c = i % columns;
            var r = i / columns;

            return transform.TransformPoint(c * playerSpacing.x - (playerSpacing.x * (columns - 1.0f) * 0.5f), 0.0f, (r + c * columnOffset) * playerSpacing.y);
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            for (var i = 0; i < playerCount; i++)
            {
                Gizmos.DrawSphere(GetSpawnPoint(i), 0.2f);
            }
            
            Gizmos.DrawRay(transform.position, Vector3.up * 10.0f);
            Gizmos.DrawSphere(transform.position, 1.0f);
        }
    }
}