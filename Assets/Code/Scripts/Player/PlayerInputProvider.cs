using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SK8Controller.Player
{
    [SelectionBase, DisallowMultipleComponent]
    public class PlayerInputProvider : MonoBehaviour
    {
        public InputActionAsset inputAsset;
        public float mouseSensitivity;
        public bool useMouse;
        [Range(-1.0f, 1.0f)]
        public float mouseSteerAngle;

        public InputDevice device;
        public int playerId;

        private InputActionReference throttle;
        private InputActionReference lean;
        private InputActionReference steer;
        private InputActionReference drift;

        public PlayerInputData InputData { get; private set; }

        private void Awake()
        {
            inputAsset = Instantiate(inputAsset);
            inputAsset.Enable();

            var fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.FieldType != typeof(InputActionReference)) return;

                var name = field.Name;
                name = name[0].ToString().ToUpper() + name[1..];
                var reference = InputActionReference.Create(inputAsset.FindAction(name));
                field.SetValue(this, reference);
            }

            mouseSteerAngle = transform.eulerAngles.y;
        }

        public void Update()
        {
            var mouseInput = Mouse.current.delta.ReadValue().x;
            mouseSteerAngle += mouseInput * mouseSensitivity * Time.deltaTime;
            mouseSteerAngle = Mathf.Clamp(mouseSteerAngle, -1.0f, 1.0f);

            var steerInput = useMouse ? mouseSteerAngle : inputV1(steer);
            
            InputData = new PlayerInputData
            {
                throttle = inputV1(throttle),
                steer = steerInput,
                lean = inputV2(lean),
                drift = inputState(drift),
            };

            bool inputState(InputActionReference input) => inputV1(input) > 0.5f;
            float inputV1(InputActionReference input) => input.action?.ReadValue<float>() ?? 0.0f;
            Vector2 inputV2(InputActionReference input) => input.action?.ReadValue<Vector2>() ?? default;
        }

        private void OnDestroy()
        {
            Destroy(inputAsset);
        }

        public void BindToDevice(int playerId, InputDevice device)
        {
            this.playerId = playerId;
            this.device = device;
            inputAsset.devices = new[] { device };
            useMouse = device is Keyboard;
        }
    }
}