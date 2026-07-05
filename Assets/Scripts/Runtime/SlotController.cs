using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OneMoreTime
{
    /// GDD §3.4: bitişte kilitlenen pRIGHT ile çevirme oturumu başlatır, tuşla çevirir.
    /// İnce orkestratör; mantık SlotMachine'de. Kazanma/kayıp routing'i (#10/#11/#12)
    /// SpinResolved event'ine bağlanacak — burada yalnızca durum + gösterim.
    public class SlotController : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] PlayerTokens tokens;
        [SerializeField] Key spinKey = Key.Enter;

        readonly SlotMachine _machine = new SlotMachine(() => UnityEngine.Random.value);

        public bool CanSpin { get; private set; }
        public bool Won { get; private set; }
        public bool Lost { get; private set; }
        public SlotOdds CurrentOdds => _machine.CurrentOdds;
        public int SpinNumber => _machine.SpinNumber;
        public SlotSpinResult? LastSpin { get; private set; }
        public int TokenCount => tokens ? tokens.Count : 0;
        public bool LastSpinForgiven { get; private set; }

        /// Makine animasyonu oynarken (#slot etkileşim akışı) girdi kilidi — animasyon
        /// bitmeden ikinci bir çevirme tetiklenmesin diye orkestratör tarafından ayarlanır.
        public bool InputLocked { get; set; }

        public event Action<SlotSpinResult> SpinResolved;
        public event Action RunLost;

        void OnEnable() => run.RunFinished += HandleRunFinished;
        void OnDisable() => run.RunFinished -= HandleRunFinished;

        void HandleRunFinished(RunResult result)
        {
            _machine.BeginSession(result.RightChance);
            CanSpin = true;
            Won = false;
            Lost = false;
            LastSpin = null;
        }

        void Update()
        {
            if (!CanSpin || InputLocked) return;
            if (Keyboard.current == null || !Keyboard.current[spinKey].wasPressedThisFrame) return;

            Spin();
        }

        public void Spin()
        {
            if (!CanSpin) return;

            LastSpinForgiven = false;
            SlotSpinResult result = _machine.Spin();
            LastSpin = result;
            SpinResolved?.Invoke(result);

            switch (result.Outcome)
            {
                case SlotOutcome.RightOnTime:
                    Won = true;
                    CanSpin = false;
                    break;
                case SlotOutcome.NotThisTime:
                    // GDD §3.3: jeton varsa NOT THIS TIME affedilir — ONE MORE TIME gibi devam eder.
                    if (tokens != null && tokens.TrySpend())
                    {
                        _machine.AdvanceSpin();
                        LastSpinForgiven = true;
                    }
                    else
                    {
                        Lost = true;
                        CanSpin = false;
                        RunLost?.Invoke();
                    }
                    break;
                // OneMoreTime: CanSpin kalır, oyuncu tekrar çevirebilir.
            }
        }

        /// #11: kayıp resetlendiğinde stale HUD durumunu temizler. Yeni session
        /// bir sonraki RunFinished'da HandleRunFinished ile zaten kurulacak.
        public void ClearAfterLoss()
        {
            Won = false;
            Lost = false;
            CanSpin = false;
            LastSpin = null;
            LastSpinForgiven = false;
        }
    }
}
