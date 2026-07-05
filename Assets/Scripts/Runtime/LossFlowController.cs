using UnityEngine;
using UnityEngine.InputSystem;

namespace OneMoreTime
{
    /// GDD §3.5: gerçek kayıpta (NOT THIS TIME, jetonsuz) bölüm başına dönüş —
    /// tüm cesetler silinir, jeton 1'e sıfırlanır, yeni koşu başlar. Kayıp ekranı (LOSE)
    /// gösterilir, herhangi bir tuşla onaylanır; ince orkestratör, mantık ilgili sınıflarda.
    public class LossFlowController : MonoBehaviour
    {
        [SerializeField] SlotController slot;
        [SerializeField] PlayerRespawner player;
        [SerializeField] PlayerTokens tokens;
        [SerializeField] RunController run;
        [SerializeField] SlotMachineInteraction interaction;
        [SerializeField] LoseScreen loseScreen;

        bool _lossArmed;
        bool _awaitingContinue;

        void OnEnable() => slot.RunLost += HandleRunLost;
        void OnDisable() => slot.RunLost -= HandleRunLost;

        // RunLost spin çözülür çözülmez (animasyondan önce) gelir; burada yalnızca kur.
        // Kayıp ekranını, makine "NOT THIS TIME"ı gösterdikten sonra SlotMachineInteraction açar.
        void HandleRunLost() => _lossArmed = true;

        /// Slot kayıp animasyonu bittikten sonra SlotMachineInteraction çağırır: ekranı karartır,
        /// LOSE + "Press any key to continue" gösterir, herhangi bir tuşu beklemeye başlar.
        public void ShowLoseScreen()
        {
            if (!_lossArmed) return;

            loseScreen.Show();
            GameAudioEvents.RaiseSlotLoseScreenShown();
            _awaitingContinue = true;
        }

        void Update()
        {
            if (!_awaitingContinue) return;
            if (Keyboard.current == null || !Keyboard.current.anyKey.wasPressedThisFrame) return;

            _awaitingContinue = false;
            _lossArmed = false;
            ForceContinue();
            loseScreen.Hide();
        }

        public void ForceContinue()
        {
            interaction.EndInteraction(instant: true); // teleport ile çakışmasın diye anlık
            player.ClearCorpses();
            player.ResetToSpawn();
            tokens.ResetToDefault();
            slot.ClearAfterLoss();
            run.BeginRun();
            GameAudioEvents.RaiseLossContinueConfirmed();
            _awaitingContinue = false;
        }
    }
}
