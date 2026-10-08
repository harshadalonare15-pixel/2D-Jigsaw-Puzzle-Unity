using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using JigsawPuzzle.Core;
using JigsawPuzzle.Data;

namespace JigsawPuzzle.UI
{
    /// <summary>
    /// Populates and manages the Level Select screen, displaying available puzzle levels
    /// and routing the player to the selected level in the Game scene.
    /// Attach to: LevelSelectCanvas GameObject in LevelSelect scene
    /// </summary>
    public class LevelSelectUI : MonoBehaviour
    {
        [Header("Containers & Templates")]
        [Tooltip("Transform container with GridLayoutGroup for level buttons.")]
        public Transform gridContainer;

        [Tooltip("Prefab or template button used for each level.")]
        public GameObject levelButtonPrefab;

        [Header("Navigation Buttons")]
        public Button backButton;

        private void Start()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }

            PopulateLevelGrid();
        }

        private void PopulateLevelGrid()
        {
            if (gridContainer == null) return;

            // Clear dummy children if any
            if (levelButtonPrefab != null)
            {
                for (int i = gridContainer.childCount - 1; i >= 0; i--)
                {
                    Transform child = gridContainer.GetChild(i);
                    if (child.gameObject != levelButtonPrefab)
                    {
                        Destroy(child.gameObject);
                    }
                }
                levelButtonPrefab.SetActive(false);
            }

            if (LevelManager.Instance == null || LevelManager.Instance.levels == null || LevelManager.Instance.levels.Count == 0)
            {
                Debug.LogWarning("[LevelSelectUI] No levels found in LevelManager.");
                return;
            }

            for (int i = 0; i < LevelManager.Instance.levels.Count; i++)
            {
                int levelIndex = i;
                LevelData level = LevelManager.Instance.levels[i];
                if (level == null) continue;

                GameObject btnObj = (levelButtonPrefab != null)
                    ? Instantiate(levelButtonPrefab, gridContainer)
                    : CreateDefaultButton(gridContainer);

                btnObj.name = $"LevelBtn_{levelIndex + 1}";
                btnObj.SetActive(true);

                TextMeshProUGUI label = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = $"{levelIndex + 1}. {level.levelName}\n({level.rows}x{level.columns})";
                }

                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnLevelSelected(levelIndex));
                }
            }
        }

        private GameObject CreateDefaultButton(Transform parent)
        {
            GameObject obj = new GameObject("LevelButton", typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(obj.transform, false);
            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 24;
            tmp.color = Color.black;

            return obj;
        }

        private void OnLevelSelected(int index)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.SelectLevel(index);
            }
            else
            {
                SceneManager.LoadScene("Game");
            }
        }

        private void OnBackClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            SceneManager.LoadScene("MainMenu");
        }
    }
}
