using UnityEngine;
using JigsawPuzzle.Puzzle;
using JigsawPuzzle.UI;
using JigsawPuzzle.Data;

namespace JigsawPuzzle.Core
{
    /// <summary>
    /// Master game manager coordinating PuzzleBoard, HUDController, WinPanel, and LevelManager.
    /// Tracks elapsed timer, placed piece count, and triggers win state.
    /// Attach to: GameManager GameObject
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState { Playing, Solving, Won }

        [Header("Scene References")]
        public PuzzleBoard puzzleBoard;
        public HUDController hud;
        public WinPanel winPanel;

        [Header("State")]
        public GameState currentState = GameState.Playing;
        private int _totalPieces = 0;
        private int _placedPiecesCount = 0;
        private float _elapsedTime = 0f;
        private bool _isTimerRunning = false;

        public int PlacedPiecesCount => _placedPiecesCount;
        public int TotalPieces => _totalPieces;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (winPanel != null)
            {
                winPanel.gameObject.SetActive(false);
            }

            // Sync level info with HUD
            LevelData currentLevel = null;
            if (LevelManager.Instance != null)
            {
                currentLevel = LevelManager.Instance.GetCurrentLevel();
            }
            else if (puzzleBoard != null)
            {
                currentLevel = puzzleBoard.fallbackLevelData;
            }

            if (currentLevel != null && hud != null)
            {
                hud.SetLevelTitle(currentLevel.levelName);
                if (currentLevel.puzzleSprite != null)
                {
                    hud.SetPreviewSprite(currentLevel.puzzleSprite);
                }
            }
        }

        private void Update()
        {
            if (_isTimerRunning && currentState == GameState.Playing)
            {
                _elapsedTime += Time.deltaTime;
                if (hud != null)
                {
                    hud.UpdateTimer(_elapsedTime);
                }
            }
        }

        /// <summary>
        /// Called by PuzzleBoard once pieces have been generated.
        /// </summary>
        public void OnBoardInitialized(int totalPieces)
        {
            _totalPieces = totalPieces;
            _placedPiecesCount = 0;
            _elapsedTime = 0f;
            _isTimerRunning = true;
            currentState = GameState.Playing;

            if (hud != null)
            {
                hud.UpdatePieceCounter(_placedPiecesCount, _totalPieces);
                hud.UpdateTimer(0f);
            }
        }

        /// <summary>
        /// Called whenever a piece snaps into its target.
        /// </summary>
        public void OnPiecePlaced(PuzzlePiece piece)
        {
            if (currentState == GameState.Won) return;

            _placedPiecesCount++;

            if (hud != null)
            {
                hud.UpdatePieceCounter(_placedPiecesCount, _totalPieces);
            }

            // Win condition check
            if (_placedPiecesCount >= _totalPieces && _totalPieces > 0)
            {
                TriggerWin();
            }
        }

        /// <summary>
        /// Handles click on the green SOLVE button.
        /// </summary>
        public void OnSolveClicked()
        {
            if (currentState != GameState.Playing) return;
            currentState = GameState.Solving;

            if (puzzleBoard != null)
            {
                puzzleBoard.SolvePuzzle();
            }
        }

        private void TriggerWin()
        {
            currentState = GameState.Won;
            _isTimerRunning = false;

            // Play celebratory win jingle
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWin();
            }

            // Display Win Panel
            if (winPanel != null)
            {
                winPanel.Show(_elapsedTime);
            }
        }

        /// <summary>
        /// Restarts current level.
        /// </summary>
        public void RestartLevel()
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.ReloadCurrentLevel();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                );
            }
        }

        /// <summary>
        /// Advances to next level.
        /// </summary>
        public void NextLevel()
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.LoadNextLevel();
            }
            else
            {
                RestartLevel();
            }
        }
    }
}
