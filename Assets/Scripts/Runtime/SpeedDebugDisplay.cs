using UnityEngine;
using UnityEngine.InputSystem;

namespace OneMoreTime
{
    [RequireComponent(typeof(Rigidbody))]
    public class SpeedDebugDisplay : MonoBehaviour
    {
        [SerializeField] bool show = true;
        [SerializeField] InputActionAsset inputAsset;

        Rigidbody _rb;
        PlayerMovementController _controller;
        PlayerInput _playerInput;
        InputAction _move;
        InputAction _crouch;
        InputAction _sprint;
        GUIStyle _style;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _controller = GetComponent<PlayerMovementController>();
            _playerInput = GetComponent<PlayerInput>();

            inputAsset = inputAsset != null ? inputAsset : _playerInput?.actions;
            inputAsset = inputAsset != null ? inputAsset : InputSystem.actions;
            if (inputAsset != null)
            {
                InputActionMap playerMap = inputAsset.FindActionMap("Player", false);
                _move = playerMap?.FindAction("Move", false);
                _crouch = playerMap?.FindAction("Crouch", false);
                _sprint = playerMap?.FindAction("Sprint", false);
            }
        }

        void OnGUI()
        {
            if (!show || _rb == null) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    normal = { textColor = Color.white }
                };
            }

            Vector3 velocity = _rb.linearVelocity;
            float horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            Vector2 move = _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
            bool crouch = _crouch != null && _crouch.IsPressed();
            bool sprint = _sprint != null && _sprint.IsPressed();
            string grounded = ReadPrivateBool("_grounded");
            string sliding = ReadPrivateBool("_sliding");

            GUI.Box(new Rect(10f, 10f, 330f, 180f), GUIContent.none);
            GUI.Label(new Rect(20f, 18f, 240f, 24f), $"Speed: {velocity.magnitude:0.00} m/s", _style);
            GUI.Label(new Rect(20f, 42f, 240f, 24f), $"Horizontal: {horizontalSpeed:0.00} m/s", _style);
            GUI.Label(new Rect(20f, 66f, 240f, 24f), $"Vertical: {velocity.y:0.00} m/s", _style);
            GUI.Label(new Rect(20f, 90f, 240f, 24f), $"Km/h: {horizontalSpeed * 3.6f:0.0}", _style);
            GUI.Label(new Rect(20f, 114f, 300f, 24f), $"Move input: {move}", _style);
            GUI.Label(new Rect(20f, 138f, 300f, 24f), $"Sprint: {sprint}  Crouch: {crouch}", _style);
            GUI.Label(new Rect(20f, 162f, 300f, 24f), $"Grounded: {grounded}  Sliding: {sliding}", _style);
        }

        string ReadPrivateBool(string fieldName)
        {
            if (_controller == null) return "no controller";

            var field = typeof(PlayerMovementController).GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            return field != null ? field.GetValue(_controller).ToString() : "missing";
        }
    }
}
