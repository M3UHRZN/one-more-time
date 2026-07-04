using UnityEngine;
using UnityEngine.InputSystem;

namespace OneMoreTime
{
    /// Minimal TPS orbit kamera. FOV / tilt / slide alçalması / FPS modu = issue #5.
    public class OrbitCamera : MonoBehaviour
    {
        [SerializeField] InputActionAsset inputAsset;
        [SerializeField] Transform target;
        [SerializeField] float distance = 4.5f;
        [SerializeField] float height = 1.6f;
        [SerializeField] float sensitivity = 0.12f;
        [SerializeField] float minPitch = -30f;
        [SerializeField] float maxPitch = 70f;

        InputAction _look;
        float _yaw, _pitch = 15f;

        void Awake()
        {
            var map = inputAsset.FindActionMap("Player", true);
            _look = map.FindAction("Look", true);
        }

        void OnEnable() => Cursor.lockState = CursorLockMode.Locked;
        void OnDisable() => Cursor.lockState = CursorLockMode.None;

        void LateUpdate()
        {
            if (!target) return;
            Vector2 d = _look.ReadValue<Vector2>() * sensitivity;
            _yaw += d.x;
            _pitch = Mathf.Clamp(_pitch - d.y, minPitch, maxPitch);

            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 focus = target.position + Vector3.up * height;
            transform.position = focus - rot * Vector3.forward * distance;
            transform.rotation = rot;
        }
    }
}
