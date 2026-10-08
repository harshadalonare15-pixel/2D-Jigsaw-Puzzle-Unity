using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JigsawPuzzle.Core;

namespace JigsawPuzzle.UI
{
    /// <summary>
    /// Displays victory screen with stats (time taken), Next Level button,
    /// Replay button, and Main Menu button.
    /// Attach to: WinPanel GameObject inside Canvas
    /// </summary>
    public class WinPanel : MonoBehaviour
    {
        [Header("UI Text Displays")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI timeText;

        [Header("Buttons")]
        public Button nextLevelButton;
        public Button replayButton;
        public Button menuButton;

        private void Awake()
        {
            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(OnNextLevelClicked);

            if (replayButton != null)
                replayButton.onClick.AddListener(OnReplayClicked);

            if (menuButton != null)
                menuButton.onClick.AddListener(OnMenuClicked);
        }

        public void Show(float completionTime)
        {
            gameObject.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "You Win!";
            }

            if (timeText != null)
            {
                int mins = Mathf.FloorToInt(completionTime / 60f);
                int secs = Mathf.FloorToInt(completionTime % 60f);
                timeText.text = $"Completed in {mins:00}:{secs:00}";
            }

            // Animate scale entrance
            StartCoroutine(AnimateEntrance());
        }

        private IEnumerator AnimateEntrance()
        {
            transform.localScale = Vector3.zero;
            float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Back-ease overshoot effect
                float s = 1.70158f;
                float ease = (t - 1) * (t - 1) * ((s + 1) * (t - 1) + s) + 1;
                transform.localScale = Vector3.one * ease;
                yield return null;
            }

            transform.localScale = Vector3.one;
        }

        private void OnNextLevelClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.NextLevel();
            }
        }

        private void OnReplayClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartLevel();
            }
        }

        private void OnMenuClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.LoadScene("MainMenu");
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            }
        }
    }
}
