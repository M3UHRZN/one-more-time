using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneMoreTime
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] string startSceneName = "LVL1";
        [SerializeField] GameObject mainMenuPanel;
        [SerializeField] GameObject creditsPanel;
        [SerializeField] SceneFadeTransition fadeTransition;

        bool _starting;

        void Awake()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
            }

            if (creditsPanel != null)
            {
                creditsPanel.SetActive(false);
            }
        }

        public void StartGame()
        {
            if (_starting) return;

            if (string.IsNullOrWhiteSpace(startSceneName))
            {
                Debug.LogWarning("MainMenuController: Start scene name is empty.");
                return;
            }

            _starting = true;

            if (fadeTransition != null)
            {
                fadeTransition.LoadScene(startSceneName);
            }
            else
            {
                SceneManager.LoadScene(startSceneName);
            }
        }

        public void OpenCredits()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }

            if (creditsPanel != null)
            {
                creditsPanel.SetActive(true);
            }
        }

        public void CloseCredits()
        {
            if (creditsPanel != null)
            {
                creditsPanel.SetActive(false);
            }

            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
            }
        }

        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
