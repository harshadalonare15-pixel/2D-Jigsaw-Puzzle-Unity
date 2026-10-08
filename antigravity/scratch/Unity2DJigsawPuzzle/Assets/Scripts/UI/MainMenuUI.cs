using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using JigsawPuzzle.Core;

namespace JigsawPuzzle.UI
{
    /// <summary>
    /// Controls Main Menu interactions: Play, Level Select, and Quit.
    /// Attach to: MainMenuCanvas GameObject in MainMenu scene
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Menu Buttons")]
        public Button playButton;
        public Button levelSelectButton;
        public Button quitButton;

        private void Start()
        {
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);

            if (levelSelectButton != null)
                levelSelectButton.onClick.AddListener(OnLevelSelectClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void OnPlayClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.SelectLevel(0);
            }
            else
            {
                SceneManager.LoadScene("Game");
            }
        }

        private void OnLevelSelectClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            SceneManager.LoadScene("LevelSelect");
        }

        private void OnQuitClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            Debug.Log("[MainMenuUI] Quitting application...");
            Application.Quit();
        }
    }
}
