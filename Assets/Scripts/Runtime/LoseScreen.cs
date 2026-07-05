using System.Collections;
using UnityEngine;

namespace OneMoreTime
{
    /// Gerçek kayıpta (NOT THIS TIME, jetonsuz) ekranı siyaha karartıp LOSE + "Press any key
    /// to continue" gösterir. İnce sunum bileşeni; reset mantığı LossFlowController'da.
    public class LoseScreen : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] float fadeDuration = 0.5f;

        Coroutine _fade;

        void Awake()
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        public void Show() => Fade(1f, blocksRaycasts: true);
        public void Hide() => Fade(0f, blocksRaycasts: false);

        void Fade(float targetAlpha, bool blocksRaycasts)
        {
            group.blocksRaycasts = blocksRaycasts;
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(FadeTo(targetAlpha));
        }

        IEnumerator FadeTo(float targetAlpha)
        {
            float startAlpha = group.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(startAlpha, targetAlpha,
                    Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }

            group.alpha = targetAlpha;
        }
    }
}
