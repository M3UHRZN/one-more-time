using System;

namespace OneMoreTime
{
    /// GDD §3.4: çevirme oturumunu yönetir. pRIGHT session boyunca sabit (koşu kalitesine
    /// kilitli); yalnızca ONE MORE TIME gelince çevirme sayısı artar (pNOT yükselir).
    /// Saf, test edilebilir; RNG enjekte edilir.
    public class SlotMachine
    {
        readonly Func<float> _rng;
        float _pRight;

        public SlotMachine(Func<float> rng)
        {
            _rng = rng;
        }

        public int SpinNumber { get; private set; } = 1;
        public SlotOdds CurrentOdds => SlotOdds.From(_pRight, SpinNumber);

        /// Jeton affı (#10) ONE MORE TIME gibi çevirme sayısını ilerletir — "artan kıyamet" korunur.
        public void AdvanceSpin() => SpinNumber++;

        public void BeginSession(float pRightLocked)
        {
            _pRight = pRightLocked;
            SpinNumber = 1;
        }

        public SlotSpinResult Spin()
        {
            SlotOdds odds = CurrentOdds;
            SlotOutcome outcome = SlotDraw.Pick(odds, _rng());
            var result = new SlotSpinResult(outcome, odds, SpinNumber);

            if (outcome == SlotOutcome.OneMoreTime)
                AdvanceSpin();

            return result;
        }
    }
}
