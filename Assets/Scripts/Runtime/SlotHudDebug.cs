using UnityEngine;

namespace OneMoreTime
{
    /// Geçici debug HUD (#8). Gerçek makara sunumu #9'da, gerçek UI #16'da gelince silinir.
    public class SlotHudDebug : MonoBehaviour
    {
        [SerializeField] SlotController slot;

        void OnGUI()
        {
            GUI.Label(new Rect(10, 88, 300, 24), $"Jeton: {slot.TokenCount}");

            if (slot.CanSpin)
            {
                SlotOdds odds = slot.CurrentOdds;
                GUI.Label(new Rect(10, 110, 400, 24),
                    $"RIGHT %{odds.Right:0}  ONE %{odds.One:0}  NOT %{odds.Not:0}");
                GUI.Label(new Rect(10, 132, 300, 24), $"Çevirme #{slot.SpinNumber}");
                GUI.Label(new Rect(10, 154, 300, 24), "[Enter] çevir");
            }

            if (slot.LastSpin.HasValue)
            {
                if (slot.LastSpin.Value.Outcome == SlotOutcome.NotThisTime && slot.LastSpinForgiven)
                    GUI.Label(new Rect(10, 176, 300, 24), $"JETON YANDI (kalan: {slot.TokenCount})");
                else
                    GUI.Label(new Rect(10, 176, 300, 24), $"Son sonuç: {slot.LastSpin.Value.Outcome}");
            }

            if (slot.Won)
                GUI.Label(new Rect(10, 198, 300, 24), "RIGHT ON TIME — WIN");
            else if (slot.Lost)
                GUI.Label(new Rect(10, 198, 300, 24), "NOT THIS TIME — LOSS");
        }
    }
}
