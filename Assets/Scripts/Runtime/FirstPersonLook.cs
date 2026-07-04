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

        [Header("Slide kamera hissi")]
        [SerializeField] PlayerMovementController controller;
        [SerializeField] float slideEyeDrop = 0.7f;   // m, slide'da göz alçalması
        [SerializeField] float slideTilt = 8f;        // derece, roll
        [SerializeField] float slideSmoothing = 12f;  // 1/sn

        InputActionMap _playerMap;
        InputAction _look;
        float _yaw, _pitch;
        float _eyeHeight;
        float _roll;

        void Awake()
        {
            _playerMap = inputAsset.FindActionMap("Player", true);
            _look = _playerMap.FindAction("Look", true);
            _yaw = transform.eulerAngles.y;
            _eyeHeight = cameraTransform.localPosition.y;
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
            bool sliding = controller && controller.IsSliding;
            float targetY = sliding ? _eyeHeight - slideEyeDrop : _eyeHeight;
            float targetRoll = sliding ? slideTilt : 0f;
            float k = slideSmoothing * Time.deltaTime;
            Vector3 lp = cameraTransform.localPosition;
            lp.y = Mathf.Lerp(lp.y, targetY, k);
            cameraTransform.localPosition = lp;
            _roll = Mathf.Lerp(_roll, targetRoll, k);
            cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, _roll);
        }
    }
}
