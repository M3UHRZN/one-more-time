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
        [SerializeField] Key spinKey = Key.Enter;

        readonly SlotMachine _machine = new SlotMachine(() => UnityEngine.Random.value);

        public bool CanSpin { get; private set; }
        public bool Won { get; private set; }
        public bool Lost { get; private set; }
        public SlotOdds CurrentOdds => _machine.CurrentOdds;
        public int SpinNumber => _machine.SpinNumber;
        public SlotSpinResult? LastSpin { get; private set; }

        public event Action<SlotSpinResult> SpinResolved;

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
            if (!CanSpin) return;
            if (Keyboard.current == null || !Keyboard.current[spinKey].wasPressedThisFrame) return;

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
                    Lost = true;
                    CanSpin = false;
                    break;
                // OneMoreTime: CanSpin kalır, oyuncu tekrar çevirebilir.
            }
        }
    }
}
