using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using JigsawPuzzle.Data;

namespace JigsawPuzzle.Core
{
    /// <summary>
    /// Manages the list of puzzle levels, track current level index, and handles scene navigation.
    /// Attach to: LevelManager GameObject
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("Level Sequence")]
        [Tooltip("List of all puzzle levels in progression order.")]
        public List<LevelData> levels = new List<LevelData>();

        [Tooltip("The index of the level currently being played.")]
        public int currentLevelIndex = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (levels == null || levels.Count == 0)
            {
                LevelData[] found = Resources.LoadAll<LevelData>("");
                if (found != null && found.Length > 0)
                {
                    levels = new List<LevelData>(found);
                }
            }
        }

        /// <summary>
        /// Returns the active LevelData ScriptableObject.
        /// </summary>
        public LevelData GetCurrentLevel()
        {
            if (levels == null || levels.Count == 0)
            {
                Debug.LogWarning("[LevelManager] No levels assigned in LevelManager! Please assign LevelData assets.");
                return null;
            }

            if (currentLevelIndex < 0 || currentLevelIndex >= levels.Count)
            {
                currentLevelIndex = 0;
            }

            return levels[currentLevelIndex];
        }

        /// <summary>
        /// Advances to the next level and reloads the Game scene.
        /// </summary>
        public void LoadNextLevel()
        {
            if (levels == null || levels.Count == 0) return;

            currentLevelIndex++;
            if (currentLevelIndex >= levels.Count)
            {
                // Completed all levels, cycle back or return to Level Select
                currentLevelIndex = 0;
            }

            SceneManager.LoadScene("Game");
        }

        /// <summary>
        /// Reloads the current puzzle.
        /// </summary>
        public void ReloadCurrentLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// Selects a specific level and launches the Game scene.
        /// </summary>
        public void SelectLevel(int index)
        {
            if (levels == null || index < 0 || index >= levels.Count) return;
            currentLevelIndex = index;
            SceneManager.LoadScene("Game");
        }

        /// <summary>
        /// Navigates to a specific scene by name.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
