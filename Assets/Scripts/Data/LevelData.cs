using UnityEngine;

namespace JigsawPuzzle.Data
{
    /// <summary>
    /// ScriptableObject defining puzzle level configuration, image, and grid parameters.
    /// Create new levels via: Right-Click > Create > Jigsaw Puzzle > Level Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "Jigsaw Puzzle/Level Data", order = 1)]
    public class LevelData : ScriptableObject
    {
        [Header("Level Information")]
        [Tooltip("Display name of the level (e.g., Farm Day, Sunny Meadow).")]
        public string levelName = "Farm Morning";

        [Header("Image Source")]
        [Tooltip("The sprite to be sliced into jigsaw pieces. Its Texture MUST have 'Read/Write' enabled in Import Settings.")]
        public Sprite puzzleSprite;

        [Header("Grid Dimensions")]
        [Tooltip("Number of horizontal rows (vertical piece count). Recommended: 3, 4, 5.")]
        [Range(2, 10)]
        public int rows = 3;

        [Tooltip("Number of vertical columns (horizontal piece count). Recommended: 3, 4, 5.")]
        [Range(2, 10)]
        public int columns = 3;

        [Header("Gameplay Settings")]
        [Tooltip("Snap distance tolerance as a fraction of piece size (e.g., 0.3 = snap if within 30% of piece width).")]
        [Range(0.1f, 0.6f)]
        public float snapTolerance = 0.28f;

        [Tooltip("Silhouette slot tint color displayed on the board for empty slots.")]
        public Color silhouetteColor = new Color(0.56f, 0.83f, 0.93f, 0.60f); // #8FD3EC with nice alpha

        /// <summary>
        /// Total number of pieces in this level.
        /// </summary>
        public int TotalPieces => rows * columns;
    }
}
