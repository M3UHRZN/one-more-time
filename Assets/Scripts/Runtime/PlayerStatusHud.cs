using UnityEngine;
using TMPro;

namespace OneMoreTime
{
    /// Always-on HUD: death count (this run's corpses) + token count. Replaces the relevant
    /// bits of the temporary RunHudDebug/SlotHudDebug OnGUI overlays.
    public class PlayerStatusHud : MonoBehaviour
    {
        [SerializeField] PlayerRespawner player;
        [SerializeField] PlayerTokens tokens;
        [SerializeField] TMP_Text deathsText;
        [SerializeField] TMP_Text tokensText;

        void Update()
        {
            deathsText.text = $"DEATHS  {player.CorpseCount}";
            tokensText.text = $"TOKENS  {tokens.Count}";
        }
    }
}
