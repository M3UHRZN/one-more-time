using UnityEngine;
using UnityEngine.InputSystem;

namespace OneMoreTime
{
    /// Birinci şahıs bakış: yaw gövdeye, pitch kameraya. GDD: FP parkur.
    public class FirstPersonLook : MonoBehaviour
    {
        [SerializeField] InputActionAsset inputAsset;
        [SerializeField] Transform cameraTransform;
        [SerializeField] float lookSensitivity = 0.12f; // derece / piksel
        [SerializeField] float pitchClamp = 85f;

        InputActionMap _playerMap;
        InputAction _look;
        float _yaw, _pitch;

        void Awake()
        {
            _playerMap = inputAsset.FindActionMap("Player", true);
            _look = _playerMap.FindAction("Look", true);
            _yaw = transform.eulerAngles.y;
        }

        void OnEnable()
        {
            _playerMap.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            Vector2 d = _look.ReadValue<Vector2>();
            _yaw += d.x * lookSensitivity;
            _pitch = Mathf.Clamp(_pitch - d.y * lookSensitivity, -pitchClamp, pitchClamp);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
    }
}
