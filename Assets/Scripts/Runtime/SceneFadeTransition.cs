using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneMoreTime
{
    /// Sahneyi siyaha fade ederek yükler, yeni sahne açılınca ekranı tekrar görünür yapar.
    public class SceneFadeTransition : MonoBehaviour
    {
        [SerializeField] CanvasGroup fadeGroup;
        [SerializeField] float fadeDuration = 0.5f;

        bool _transitioning;

        void Awake()
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
        }

        public void LoadScene(string sceneName)
        {
            if (_transitioning) return;

            _transitioning = true;
            GameAudioEvents.RaiseSceneTransitionStarted();
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        IEnumerator LoadSceneRoutine(string sceneName)
        {
            DontDestroyOnLoad(gameObject);
            fadeGroup.blocksRaycasts = true;

            yield return FadeTo(1f);
            yield return SceneManager.LoadSceneAsync(sceneName);
            yield return FadeTo(0f);

            fadeGroup.blocksRaycasts = false;
            Destroy(gameObject);
        }

        IEnumerator FadeTo(float targetAlpha)
        {
            float startAlpha = fadeGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha,
                    Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }

            fadeGroup.alpha = targetAlpha;
        }
    }
}
