using System.Collections.Generic;
using UnityEngine;

namespace JigsawPuzzle.Puzzle
{
    /// <summary>
    /// Scatters puzzle pieces randomly into areas outside the puzzle board bounds
    /// so that pieces never overlap with the active board slots.
    /// Attach to: PuzzleBoard or ScatterManager GameObject
    /// </summary>
    public class PieceScatter : MonoBehaviour
    {
        [Header("Scatter Area Settings")]
        [Tooltip("Margin buffer kept between the board and scattered pieces in world units.")]
        public float boardMargin = 0.6f;

        [Tooltip("Screen edge padding kept within camera view in world units.")]
        public float screenMargin = 0.5f;

        private struct ScatterZone
        {
            public float minX;
            public float maxX;
            public float minY;
            public float maxY;

            public float Area => Mathf.Max(0f, maxX - minX) * Mathf.Max(0f, maxY - minY);
            public bool IsValid => (maxX > minX + 0.3f) && (maxY > minY + 0.3f);

            public Vector2 GetRandomPoint()
            {
                return new Vector2(
                    Random.Range(minX, maxX),
                    Random.Range(minY, maxY)
                );
            }
        }

        /// <summary>
        /// Distributes a list of pieces randomly across zones outside the board bounding box.
        /// </summary>
        public void ScatterPieces(List<PuzzlePiece> pieces, Bounds boardBounds, Camera cam)
        {
            if (pieces == null || pieces.Count == 0) return;
            if (cam == null) cam = Camera.main;

            // Compute camera viewport in world coordinates
            float camHeight = 2f * cam.orthographicSize;
            float camWidth = camHeight * cam.aspect;
            Vector3 camPos = cam.transform.position;

            float camMinX = camPos.x - camWidth * 0.5f + screenMargin;
            float camMaxX = camPos.x + camWidth * 0.5f - screenMargin;
            float camMinY = camPos.y - camHeight * 0.5f + screenMargin;
            float camMaxY = camPos.y + camHeight * 0.5f - screenMargin;

            // Board exclusion bounds
            float bMinX = boardBounds.min.x - boardMargin;
            float bMaxX = boardBounds.max.x + boardMargin;
            float bMinY = boardBounds.min.y - boardMargin;
            float bMaxY = boardBounds.max.y + boardMargin;

            // Define scatter zones surrounding the board
            List<ScatterZone> validZones = new List<ScatterZone>();

            // 1. Left Zone
            ScatterZone leftZone = new ScatterZone
            {
                minX = camMinX,
                maxX = Mathf.Min(camMaxX, bMinX),
                minY = camMinY,
                maxY = camMaxY
            };
            if (leftZone.IsValid) validZones.Add(leftZone);

            // 2. Right Zone
            ScatterZone rightZone = new ScatterZone
            {
                minX = Mathf.Max(camMinX, bMaxX),
                maxX = camMaxX,
                minY = camMinY,
                maxY = camMaxY
            };
            if (rightZone.IsValid) validZones.Add(rightZone);

            // 3. Bottom Zone
            ScatterZone bottomZone = new ScatterZone
            {
                minX = camMinX,
                maxX = camMaxX,
                minY = camMinY,
                maxY = Mathf.Min(camMaxY, bMinY)
            };
            if (bottomZone.IsValid) validZones.Add(bottomZone);

            // 4. Top Zone (if extra space below top HUD)
            ScatterZone topZone = new ScatterZone
            {
                minX = camMinX,
                maxX = camMaxX,
                minY = Mathf.Max(camMinY, bMaxY),
                maxY = camMaxY - 0.8f // keep clear of top HUD buttons
            };
            if (topZone.IsValid) validZones.Add(topZone);

            // Fallback if camera is too tight: use side trays
            if (validZones.Count == 0)
            {
                ScatterZone fallback = new ScatterZone
                {
                    minX = bMaxX,
                    maxX = bMaxX + 2.5f,
                    minY = bMinY,
                    maxY = bMaxY
                };
                validZones.Add(fallback);
            }

            // Scatter pieces across valid zones with slight jitter grid distribution
            for (int i = 0; i < pieces.Count; i++)
            {
                // Select a zone weighted by available area
                ScatterZone chosenZone = SelectWeightedZone(validZones);
                Vector2 randomPos = chosenZone.GetRandomPoint();

                pieces[i].transform.position = new Vector3(randomPos.x, randomPos.y, 0f);
            }
        }

        private ScatterZone SelectWeightedZone(List<ScatterZone> zones)
        {
            float totalArea = 0f;
            for (int i = 0; i < zones.Count; i++)
            {
                totalArea += zones[i].Area;
            }

            if (totalArea <= 0.001f) return zones[0];

            float roll = Random.Range(0f, totalArea);
            float accum = 0f;

            for (int i = 0; i < zones.Count; i++)
            {
                accum += zones[i].Area;
                if (roll <= accum)
                {
                    return zones[i];
                }
            }

            return zones[zones.Count - 1];
        }
    }
}
