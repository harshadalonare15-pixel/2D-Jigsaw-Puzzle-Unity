using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JigsawPuzzle.Core;

namespace JigsawPuzzle.UI
{
    /// <summary>
    /// Controls HUD elements: placed pieces counter, elapsed timer,
    /// Restart button, Solve button, and Preview image toggle.
    /// Attach to: HUD GameObject inside Canvas
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Text Displays (TextMeshPro)")]
        [Tooltip("Displays piece progress, e.g. 'Placed: 4 / 16'.")]
        public TextMeshProUGUI counterText;

        [Tooltip("Displays elapsed play time, e.g. '01:23'.")]
        public TextMeshProUGUI timerText;

        [Tooltip("Displays level title, e.g. 'Farm Morning'.")]
        public TextMeshProUGUI levelTitleText;

        [Header("Buttons")]
        public Button restartButton;
        public Button solveButton;
        public Button previewButton;

        [Header("Preview Overlay")]
        public GameObject previewPanel;
        public Image previewImage;

        private bool _isPreviewOpen = false;

        private void Start()
        {
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (solveButton != null)
            {
                solveButton.onClick.AddListener(OnSolveClicked);
            }

            if (previewButton != null)
            {
                previewButton.onClick.AddListener(TogglePreview);
            }

            if (previewPanel != null)
            {
                previewPanel.SetActive(false);
            }
        }

        public void SetLevelTitle(string title)
        {
            if (levelTitleText != null)
            {
                levelTitleText.text = title;
            }
        }

        public void SetPreviewSprite(Sprite sprite)
        {
            if (previewImage != null)
            {
                previewImage.sprite = sprite;
            }
        }

        public void UpdatePieceCounter(int placed, int total)
        {
            if (counterText != null)
            {
                counterText.text = $"Pieces: {placed} / {total}";
            }
        }

        public void UpdateTimer(float seconds)
        {
            if (timerText != null)
            {
                int mins = Mathf.FloorToInt(seconds / 60f);
                int secs = Mathf.FloorToInt(seconds % 60f);
                timerText.text = $"Time: {mins:00}:{secs:00}";
            }
        }

        public void TogglePreview()
        {
            if (previewPanel == null) return;
            _isPreviewOpen = !_isPreviewOpen;
            previewPanel.SetActive(_isPreviewOpen);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClick();
            }
        }

        private void OnRestartClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartLevel();
            }
        }

        private void OnSolveClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnSolveClicked();
            }
        }
    }
}
