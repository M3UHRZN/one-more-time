using UnityEngine;
using TMPro;

namespace OneMoreTime
{
    /// #16: GDD §4.3 — slot ekranında canlı olasılık gösterimi. Oyuncu makineye yaklaşınca
    /// (proximity) beliren world-space panel; SlotHudDebug'ın (#8) yerini alır.
    /// İnce görüntüleyici; olasılık mantığı SlotOdds/SlotController'da kalır.
    public class SlotOddsPanel : MonoBehaviour
    {
        [SerializeField] SlotController slot;
        [SerializeField] SlotMachineInteraction interaction;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text rightText;
        [SerializeField] TMP_Text oneText;
        [SerializeField] TMP_Text notText;
        [SerializeField] float fadeDuration = 0.35f;

        void Update()
        {
            bool visible = interaction.PlayerInRange && slot.CanSpin;
            float target = visible ? 1f : 0f;
            group.alpha = Mathf.MoveTowards(group.alpha, target, Time.deltaTime / fadeDuration);

            if (!visible) return;

            SlotOdds odds = slot.CurrentOdds;
            rightText.text = $"%{odds.Right:0}";
            oneText.text = $"%{odds.One:0}";
            notText.text = $"%{odds.Not:0}";
        }
    }
}
