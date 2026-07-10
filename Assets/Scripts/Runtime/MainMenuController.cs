using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneMoreTime
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] string startSceneName = "lvl1";
        [SerializeField] GameObject mainMenuPanel;
        [SerializeField] GameObject creditsPanel;

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
            if (string.IsNullOrWhiteSpace(startSceneName))
            {
                Debug.LogWarning("MainMenuController: Start scene name is empty.");
                return;
            }

            SceneManager.LoadScene(startSceneName);
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
