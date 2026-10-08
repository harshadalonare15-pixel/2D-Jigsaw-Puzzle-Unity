using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JigsawPuzzle.Core;
using JigsawPuzzle.Data;

namespace JigsawPuzzle.Puzzle
{
    /// <summary>
    /// Core puzzle board controller. Slices the source texture into interlocking jigsaw pieces,
    /// sets up slot silhouettes, scatters pieces, and manages the smooth solve sequence.
    /// Attach to: PuzzleBoard GameObject
    /// </summary>
    public class PuzzleBoard : MonoBehaviour
    {
        [Header("Level Fallback (Used if no LevelManager)")]
        public LevelData fallbackLevelData;

        [Header("Prefab Template (Optional - created automatically if null)")]
        public GameObject piecePrefab;

        [Header("Board Layout")]
        public Vector2 boardCenter = new Vector2(0f, 0.2f);

        [Header("Components")]
        public PieceScatter scatter;

        private LevelData _currentLevel;
        private List<PuzzlePiece> _allPieces = new List<PuzzlePiece>();
        private List<GameObject> _allSilhouettes = new List<GameObject>();
        private bool _isSolving = false;

        public int TotalPieces => _allPieces.Count;
        public List<PuzzlePiece> AllPieces => _allPieces;

        private void Awake()
        {
            if (scatter == null)
            {
                scatter = GetComponent<PieceScatter>();
                if (scatter == null) scatter = gameObject.AddComponent<PieceScatter>();
            }
        }

        private void Start()
        {
            InitializeBoard();
        }

        /// <summary>
        /// Reads level configuration, slices the image, and builds the board.
        /// </summary>
        public void InitializeBoard()
        {
            ClearBoard();

            // 1. Resolve Active Level
            if (LevelManager.Instance != null && LevelManager.Instance.GetCurrentLevel() != null)
            {
                _currentLevel = LevelManager.Instance.GetCurrentLevel();
            }
            else if (fallbackLevelData != null)
            {
                _currentLevel = fallbackLevelData;
            }

            // Fallback: check Resources for any LevelData
            if (_currentLevel == null || _currentLevel.puzzleSprite == null)
            {
                LevelData[] found = Resources.LoadAll<LevelData>("");
                if (found != null && found.Length > 0)
                {
                    _currentLevel = found[0];
                }
            }

            // 2. Resolve Texture and Grid Parameters
            Texture2D sourceTex = null;
            Sprite fullSprite = null;
            int rows = 3;
            int cols = 3;
            float ppu = 100f;
            float snapTol = 0.28f;
            Color silColor = new Color(0.56f, 0.83f, 0.93f, 0.65f);

            if (_currentLevel != null && _currentLevel.puzzleSprite != null)
            {
                fullSprite = _currentLevel.puzzleSprite;
                sourceTex = fullSprite.texture;
                rows = _currentLevel.rows;
                cols = _currentLevel.columns;
                ppu = fullSprite.pixelsPerUnit;
                snapTol = _currentLevel.snapTolerance;
                silColor = _currentLevel.silhouetteColor;
            }
            else
            {
                // Standalone Fail-safe: Generate cartoon farm texture procedurally at runtime
                Debug.Log("[PuzzleBoard] Generating procedural cartoon farm texture fallback...");
                sourceTex = GenerateRuntimeFarmTexture();
                ppu = 100f;
                rows = 3;
                cols = 3;
                fullSprite = Sprite.Create(sourceTex, new Rect(0, 0, sourceTex.width, sourceTex.height), new Vector2(0.5f, 0.5f), ppu);
            }

            // Ensure source texture is CPU-readable
            sourceTex = EnsureTextureReadable(sourceTex);

            if (GameManager.Instance != null && GameManager.Instance.hud != null)
            {
                GameManager.Instance.hud.SetPreviewSprite(fullSprite);
                if (_currentLevel != null) GameManager.Instance.hud.SetLevelTitle(_currentLevel.levelName);
            }

            int srcW = sourceTex.width;
            int srcH = sourceTex.height;

            float corePixelW = (float)srcW / cols;
            float corePixelH = (float)srcH / rows;
            float coreWorldW = corePixelW / ppu;
            float coreWorldH = corePixelH / ppu;

            float boardWidth = cols * coreWorldW;
            float boardHeight = rows * coreWorldH;

            // Dynamically adjust camera orthographic size to fit board and trays
            AdjustCameraToFit(boardWidth, boardHeight);

            // Origin of bottom-left cell center
            float originX = boardCenter.x - (boardWidth * 0.5f) + (coreWorldW * 0.5f);
            float originY = boardCenter.y - (boardHeight * 0.5f) + (coreWorldH * 0.5f);

            // Generate interlocking edge tab/blank layout
            JigsawMaskGenerator.PieceEdgeData[,] edgeMap = JigsawMaskGenerator.GenerateGridEdgeMap(rows, cols);

            Transform piecesContainer = new GameObject("Pieces").transform;
            piecesContainer.SetParent(transform);

            Transform silhouettesContainer = new GameObject("Silhouettes").transform;
            silhouettesContainer.SetParent(transform);

            float snapDistance = Mathf.Min(coreWorldW, coreWorldH) * snapTol;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Vector3 targetPos = new Vector3(originX + c * coreWorldW, originY + r * coreWorldH, 0f);

                    // Procedurally generate piece sprite, silhouette sprite, and polygon collider
                    JigsawMaskGenerator.CreatePieceSprites(
                        sourceTex,
                        r, c,
                        rows, cols,
                        edgeMap[r, c],
                        ppu,
                        silColor,
                        out Sprite pieceSprite,
                        out Sprite silhouetteSprite,
                        out Vector2[] colliderPoints
                    );

                    // 1. Create Silhouette slot on board
                    GameObject silObj = new GameObject($"Silhouette_{r}_{c}");
                    silObj.transform.SetParent(silhouettesContainer);
                    silObj.transform.position = targetPos;
                    SpriteRenderer silRenderer = silObj.AddComponent<SpriteRenderer>();
                    silRenderer.sprite = silhouetteSprite;
                    silRenderer.sortingOrder = 0;
                    _allSilhouettes.Add(silObj);

                    // 2. Create Puzzle Piece
                    GameObject pieceObj = new GameObject($"Piece_{r}_{c}");
                    pieceObj.transform.SetParent(piecesContainer);
                    pieceObj.transform.position = targetPos;

                    SpriteRenderer pieceRenderer = pieceObj.AddComponent<SpriteRenderer>();
                    pieceRenderer.sprite = pieceSprite;
                    pieceRenderer.sortingOrder = 10;

                    PolygonCollider2D polyCol = pieceObj.AddComponent<PolygonCollider2D>();
                    polyCol.points = colliderPoints;

                    PuzzlePiece piece = pieceObj.AddComponent<PuzzlePiece>();
                    piece.row = r;
                    piece.col = c;
                    piece.targetPosition = targetPos;
                    piece.snapToleranceDistance = snapDistance;
                    piece.silhouetteObject = silObj;

                    _allPieces.Add(piece);
                }
            }

            // Scatter pieces across zones outside the board
            Bounds boardBounds = new Bounds(boardCenter, new Vector3(boardWidth, boardHeight, 1f));
            scatter.ScatterPieces(_allPieces, boardBounds, Camera.main);

            // Notify GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnBoardInitialized(_allPieces.Count);
            }

            Debug.Log($"[PuzzleBoard] Successfully initialized board with {_allPieces.Count} pieces!");
        }

        private Texture2D EnsureTextureReadable(Texture2D tex)
        {
            try
            {
                tex.GetPixel(0, 0);
                return tex;
            }
            catch
            {
                // Convert to readable texture via RenderTexture
                RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(tex, rt);
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;

                Texture2D readable = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                readable.Apply();

                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                return readable;
            }
        }

        private Texture2D GenerateRuntimeFarmTexture()
        {
            int width = 512;
            int height = 512;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            Color skyBlue = new Color(0.40f, 0.72f, 0.95f);
            Color grassGreen = new Color(0.38f, 0.78f, 0.28f);
            Color darkGrass = new Color(0.28f, 0.62f, 0.20f);
            Color barnRed = new Color(0.85f, 0.22f, 0.20f);
            Color roofDark = new Color(0.35f, 0.15f, 0.15f);
            Color sunYellow = new Color(1.0f, 0.88f, 0.25f);
            Color fenceWhite = new Color(0.95f, 0.95f, 0.92f);

            for (int y = 0; y < height; y++)
            {
                float ny = (float)y / height;
                for (int x = 0; x < width; x++)
                {
                    float nx = (float)x / width;
                    Color col = skyBlue;

                    float hillLine = 0.42f + Mathf.Sin(nx * Mathf.PI * 2f) * 0.08f;
                    if (ny < hillLine)
                    {
                        col = Color.Lerp(darkGrass, grassGreen, ny / hillLine);
                    }
                    else
                    {
                        float sunDist = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.22f, 0.80f));
                        if (sunDist < 0.12f) col = sunYellow;
                    }

                    if (nx >= 0.55f && nx <= 0.82f && ny >= 0.28f && ny <= 0.55f)
                    {
                        col = barnRed;
                        if (nx >= 0.65f && nx <= 0.72f && ny >= 0.28f && ny <= 0.42f) col = Color.white;
                    }

                    if (nx >= 0.53f && nx <= 0.84f && ny > 0.55f && ny <= 0.68f)
                    {
                        float roofSlope = 1f - Mathf.Abs((nx - 0.685f) / 0.155f);
                        if ((ny - 0.55f) / 0.13f <= roofSlope) col = roofDark;
                    }

                    if (ny >= 0.14f && ny <= 0.24f)
                    {
                        if ((ny >= 0.16f && ny <= 0.18f) || (ny >= 0.21f && ny <= 0.23f))
                        {
                            if (nx < 0.52f) col = fenceWhite;
                        }
                        if ((x % 40) < 8 && nx < 0.52f) col = fenceWhite;
                    }

                    tex.SetPixel(x, y, col);
                }
            }

            tex.Apply();
            return tex;
        }

        private void AdjustCameraToFit(float boardWidth, float boardHeight)
        {
            Camera cam = Camera.main;
            if (cam == null || !cam.orthographic) return;

            float requiredHeight = (boardHeight + 3.0f) * 0.5f;
            float requiredWidthHeight = ((boardWidth + 5.0f) / cam.aspect) * 0.5f;

            float targetOrthoSize = Mathf.Max(5.0f, Mathf.Max(requiredHeight, requiredWidthHeight));
            cam.orthographicSize = targetOrthoSize;
        }

        /// <summary>
        /// Solves the puzzle by smoothly animating all unplaced pieces to their targets.
        /// </summary>
        public void SolvePuzzle()
        {
            if (_isSolving) return;
            StartCoroutine(SolveSequenceCoroutine());
        }

        private IEnumerator SolveSequenceCoroutine()
        {
            _isSolving = true;

            List<PuzzlePiece> unplaced = new List<PuzzlePiece>();
            for (int i = 0; i < _allPieces.Count; i++)
            {
                if (!_allPieces[i].isPlaced)
                {
                    unplaced.Add(_allPieces[i]);
                }
            }

            float staggerDelay = 0.07f;
            float duration = 0.55f;

            for (int i = 0; i < unplaced.Count; i++)
            {
                StartCoroutine(unplaced[i].AnimateToTarget(duration, i * staggerDelay));
            }

            float totalWait = (unplaced.Count * staggerDelay) + duration + 0.1f;
            yield return new WaitForSeconds(totalWait);

            _isSolving = false;
        }

        private void ClearBoard()
        {
            _allPieces.Clear();
            _allSilhouettes.Clear();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}
