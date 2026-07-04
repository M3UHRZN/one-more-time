using UnityEngine;
using UnityEngine.InputSystem;

namespace OneMoreTime
{
    /// GDD §3.5: gerçek kayıpta (NOT THIS TIME, jetonsuz) bölüm başına dönüş —
    /// tüm cesetler silinir, jeton 1'e sıfırlanır, yeni koşu başlar. Tuşla onaylanır
    /// (oyuncu kayıp mesajını görebilsin); ince orkestratör, mantık ilgili sınıflarda.
    public class LossFlowController : MonoBehaviour
    {
        [SerializeField] SlotController slot;
        [SerializeField] PlayerRespawner player;
        [SerializeField] PlayerTokens tokens;
        [SerializeField] RunController run;
        [SerializeField] Key continueKey = Key.Enter;

        bool _awaitingContinue;

        void OnEnable() => slot.RunLost += HandleRunLost;
        void OnDisable() => slot.RunLost -= HandleRunLost;

        void HandleRunLost() => _awaitingContinue = true;

        void Update()
        {
            if (!_awaitingContinue) return;
            if (Keyboard.current == null || !Keyboard.current[continueKey].wasPressedThisFrame) return;

            ForceContinue();
        }

        public void ForceContinue()
        {
            player.ClearCorpses();
            player.ResetToSpawn();
            tokens.ResetToDefault();
            slot.ClearAfterLoss();
            run.BeginRun();
            _awaitingContinue = false;
        }
    }
}
