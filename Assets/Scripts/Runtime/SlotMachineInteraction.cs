using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OneMoreTime
{
    /// Slot makinesine yürüyüp Interact (E, Hold) ile etkileşim akışını yönetir: koşuyu bitirir,
    /// kamerayı makineye yaklaştırır, oyuncu kontrolünü kilitler, her E basışında makine
    /// animasyonunu oynatıp animasyon bitene kadar girdi kilitler. İnce orkestratör; slot mantığı
    /// SlotController'da kalır.
    public class SlotMachineInteraction : MonoBehaviour
    {
        [SerializeField] PlayerMovementController movement;
        [SerializeField] FirstPersonLook look;
        [SerializeField] SpeedFovEffect fov;
        [SerializeField] SlotController slot;
        [SerializeField] RunController run;
        [SerializeField] SlotHudDebug hud;
        [SerializeField] Transform cameraTransform;
        [SerializeField] Transform viewpoint;
        [SerializeField] Animator machineAnimator;
        [SerializeField] InputActionAsset inputAsset;

        [SerializeField] float zoomDuration = 0.6f;
        [SerializeField] float leverPullDuration = 2.233f; // pullthelever.anim uzunluğu
        [SerializeField] float shuffleHoldDuration = 0.5f; // GDD §3.4 "kaybettin" hissi payı
        [SerializeField] float resultDisplayDuration = 1.1f; // sonuç kliplerinin en uzunu (~1.08s)
        [SerializeField] float winDisplayDelay = 1.5f;

        static readonly int SpinTrigger = Animator.StringToHash("Spin");
        static readonly int WinTrigger = Animator.StringToHash("Win");
        static readonly int OneMoreTrigger = Animator.StringToHash("OneMore");
        static readonly int LoseTrigger = Animator.StringToHash("Lose");

        InputAction _interact;
        bool _playerInRange;
        bool _interacting;
        Vector3 _savedCamPos;
        Quaternion _savedCamRot;
        Coroutine _cameraRoutine;

        void Awake() => _interact = inputAsset.FindActionMap("Player", true).FindAction("Interact", true);

        void OnEnable() => slot.SpinResolved += HandleSpinResolved;
        void OnDisable() => slot.SpinResolved -= HandleSpinResolved;

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerRespawner>()) _playerInRange = true;
        }

        void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PlayerRespawner>()) _playerInRange = false;
        }

        void Update()
        {
            if (_interacting || !_playerInRange || slot.Won || slot.Lost) return;
            if (_interact.WasPerformedThisFrame()) BeginInteraction();
        }

        void BeginInteraction()
        {
            run.Finish(); // koşu zaten bittiyse no-op (RunController.HasFinished guard'lı)
            _interacting = true;

            _savedCamPos = cameraTransform.position;
            _savedCamRot = cameraTransform.rotation;

            movement.SetControlEnabled(false);
            look.SetControlEnabled(false);
            fov.enabled = false;

            if (_cameraRoutine != null) StopCoroutine(_cameraRoutine);
            _cameraRoutine = StartCoroutine(MoveCamera(viewpoint.position, viewpoint.rotation, zoomDuration, null));
        }

        void HandleSpinResolved(SlotSpinResult result)
        {
            hud.enabled = false;
            slot.InputLocked = true;
            machineAnimator.SetTrigger(SpinTrigger); // idle -> Pull-lever -> Shuffle (otomatik)
            StartCoroutine(PlaySpinSequence(result.Outcome));
        }

        IEnumerator PlaySpinSequence(SlotOutcome outcome)
        {
            yield return new WaitForSeconds(leverPullDuration + shuffleHoldDuration);

            switch (outcome)
            {
                case SlotOutcome.RightOnTime: machineAnimator.SetTrigger(WinTrigger); break;
                case SlotOutcome.OneMoreTime: machineAnimator.SetTrigger(OneMoreTrigger); break;
                case SlotOutcome.NotThisTime: machineAnimator.SetTrigger(LoseTrigger); break;
            }
            hud.enabled = true;

            yield return new WaitForSeconds(resultDisplayDuration);
            slot.InputLocked = false;

            if (slot.Won)
            {
                yield return new WaitForSeconds(winDisplayDelay);
                EndInteraction();
            }
            // Lost: LossFlowController, Space'e basılınca EndInteraction(instant:true) çağırır.
            // OneMoreTime / affedilmiş NotThisTime: oturum açık kalır, oyuncu tekrar E'ye basar.
        }

        /// Kamerayı geri alır, kontrolü serbest bırakır. Kayıp-devam akışı (LossFlowController)
        /// teleport ile çakışmasın diye instant=true kullanır.
        public void EndInteraction(bool instant = false)
        {
            _interacting = false;
            hud.enabled = true;
            slot.InputLocked = false;

            if (_cameraRoutine != null) StopCoroutine(_cameraRoutine);

            if (instant)
            {
                cameraTransform.SetPositionAndRotation(_savedCamPos, _savedCamRot);
                RestoreControl();
            }
            else
            {
                _cameraRoutine = StartCoroutine(MoveCamera(_savedCamPos, _savedCamRot, zoomDuration, RestoreControl));
            }
        }

        void RestoreControl()
        {
            movement.SetControlEnabled(true);
            look.SetControlEnabled(true);
            fov.enabled = true;
        }

        IEnumerator MoveCamera(Vector3 targetPos, Quaternion targetRot, float duration, Action onComplete)
        {
            Vector3 startPos = cameraTransform.position;
            Quaternion startRot = cameraTransform.rotation;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                cameraTransform.SetPositionAndRotation(
                    Vector3.Lerp(startPos, targetPos, k),
                    Quaternion.Slerp(startRot, targetRot, k));
                yield return null;
            }

            cameraTransform.SetPositionAndRotation(targetPos, targetRot);
            onComplete?.Invoke();
        }
    }
}
