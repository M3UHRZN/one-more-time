using UnityEngine;

namespace OneMoreTime
{
    /// Geçici debug HUD (#7). Gerçek UI #16'da gelince bu dosya silinir.
    public class RunHudDebug : MonoBehaviour
    {
        [SerializeField] RunController run;
        [SerializeField] PlayerRespawner player;

        void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 300, 24), $"Süre: {FormatTime(run.Elapsed)}");
            GUI.Label(new Rect(10, 32, 300, 24), $"Cesetler: {player.CorpseCount}");

            if (run.HasFinished)
            {
                GUI.Label(new Rect(10, 54, 300, 24), $"RIGHT ON TIME: %{run.LastResult.RightChance:0}");
                GUI.Label(new Rect(10, 76, 300, 24), $"Par: {FormatTime(run.ParTime)}");
            }
        }

        static string FormatTime(float seconds)
        {
            int totalMs = Mathf.Max(0, Mathf.RoundToInt(seconds * 100f));
            int mm = totalMs / 6000;
            int ss = (totalMs / 100) % 60;
            int ff = totalMs % 100;
            return $"{mm:00}:{ss:00}.{ff:00}";
        }
    }
}
