using UnityEngine;
using TMPro;

namespace OneMoreTime
{
    /// GDD §3.3: jeton affıyla hayatta kalınca ekran ortasında kısa bilgi mesajı.
    /// SpinResolved, LastSpinForgiven ayarlanmadan ÖNCE ateşlenir (SlotController.Spin sırası) —
    /// bu yüzden event'e değil, her karede LastSpinForgiven'ın false->true geçişine bakılır.
    public class TokenSpentToast : MonoBehaviour
    {
        [SerializeField] SlotController slot;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text text;
        [SerializeField] float showDuration = 2.2f;
        [SerializeField] float fadeDuration = 0.35f;

        bool _wasForgiven;
        float _timer;

        void Update()
        {
            bool forgivenNow = slot.LastSpin.HasValue
                && slot.LastSpin.Value.Outcome == SlotOutcome.NotThisTime
                && slot.LastSpinForgiven;

            if (forgivenNow && !_wasForgiven)
            {
                _timer = showDuration;
                text.text = "TOKEN SPENT — YOU SURVIVED. ONE MORE TIME.";
            }
            _wasForgiven = forgivenNow;

            if (_timer > 0f) _timer -= Time.deltaTime;

            float target = _timer > 0f ? 1f : 0f;
            group.alpha = Mathf.MoveTowards(group.alpha, target, Time.deltaTime / fadeDuration);
        }
    }
}
