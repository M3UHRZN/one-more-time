using UnityEngine;

namespace OneMoreTime
{
    /// GDD §3.6 öncelik 3: hıza bağlı FOV 90°→105°, lineer + yumuşatma.
    [RequireComponent(typeof(Camera))]
    public class SpeedFovEffect : MonoBehaviour
    {
        [SerializeField] PlayerMovementController controller;
        [SerializeField] float baseFov = 90f;
        [SerializeField] float maxFov = 105f;
        [SerializeField] float minSpeed = 8f;    // bu hızın altında baseFov
        [SerializeField] float maxSpeed = 18f;   // bu hızda maxFov
        [SerializeField] float smoothing = 6f;   // 1/sn lerp katsayısı

        Camera _cam;

        void Awake() => _cam = GetComponent<Camera>();

        void LateUpdate()
        {
            float t = Mathf.InverseLerp(minSpeed, maxSpeed, controller.HorizontalSpeed);
            float target = Mathf.Lerp(baseFov, maxFov, t);
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, target, smoothing * Time.deltaTime);
        }
    }
}
