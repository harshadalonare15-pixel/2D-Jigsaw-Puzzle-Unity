using System.Collections.Generic;
using UnityEngine;

namespace JigsawPuzzle.Puzzle
{
    /// <summary>
    /// Generates interlocking jigsaw piece edges (tabs and blanks) using Bezier curve splines,
    /// masks source texture pixels with padding, and creates matching Sprite and Collider data.
    /// </summary>
    public static class JigsawMaskGenerator
    {
        // Edge types: 0 = Flat border, 1 = Outward Tab, -1 = Inward Blank
        public const int FLAT = 0;
        public const int TAB = 1;
        public const int BLANK = -1;

        /// <summary>
        /// Stores edge types for each of the 4 borders of a single piece.
        /// </summary>
        public struct PieceEdgeData
        {
            public int top;
            public int right;
            public int bottom;
            public int left;
        }

        /// <summary>
        /// Generates a globally consistent edge map for an entire grid.
        /// Neighboring pieces will always have perfectly complementary edges (Tab meets Blank).
        /// </summary>
        public static PieceEdgeData[,] GenerateGridEdgeMap(int rows, int cols)
        {
            PieceEdgeData[,] map = new PieceEdgeData[rows, cols];

            // Horizontal seams between row r and r+1: [rows-1, cols]
            // +1 means tab points UP into row r+1
            int[,] hSeams = new int[rows - 1, cols];
            for (int r = 0; r < rows - 1; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    hSeams[r, c] = (Random.value > 0.5f) ? TAB : BLANK;
                }
            }

            // Vertical seams between col c and c+1: [rows, cols-1]
            // +1 means tab points RIGHT into col c+1
            int[,] vSeams = new int[rows, cols - 1];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols - 1; c++)
                {
                    vSeams[r, c] = (Random.value > 0.5f) ? TAB : BLANK;
                }
            }

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    PieceEdgeData edges = new PieceEdgeData();

                    // Bottom edge (r = 0 is flat boundary)
                    edges.bottom = (r == 0) ? FLAT : -hSeams[r - 1, c];

                    // Top edge (r = rows-1 is flat boundary)
                    edges.top = (r == rows - 1) ? FLAT : hSeams[r, c];

                    // Left edge (c = 0 is flat boundary)
                    edges.left = (c == 0) ? FLAT : -vSeams[r, c - 1];

                    // Right edge (c = cols-1 is flat boundary)
                    edges.right = (c == cols - 1) ? FLAT : vSeams[r, c];

                    map[r, c] = edges;
                }
            }

            return map;
        }

        /// <summary>
        /// Generates the closed 2D polygon contour for a piece in texture pixel space.
        /// </summary>
        public static List<Vector2> GeneratePieceContour(float padX, float padY, float pw, float ph, PieceEdgeData edges)
        {
            List<Vector2> contour = new List<Vector2>();

            // Corner positions in padded texture space
            Vector2 bl = new Vector2(padX, padY);
            Vector2 br = new Vector2(padX + pw, padY);
            Vector2 tr = new Vector2(padX + pw, padY + ph);
            Vector2 tl = new Vector2(padX, padY + ph);

            // 1. Bottom edge: BL to BR (normal points DOWN: Vector2.down)
            AddEdgePoints(contour, bl, br, Vector2.down, edges.bottom);

            // 2. Right edge: BR to TR (normal points RIGHT: Vector2.right)
            AddEdgePoints(contour, br, tr, Vector2.right, edges.right);

            // 3. Top edge: TR to TL (normal points UP: Vector2.up)
            AddEdgePoints(contour, tr, tl, Vector2.up, edges.top);

            // 4. Left edge: TL to BL (normal points LEFT: Vector2.left)
            AddEdgePoints(contour, tl, bl, Vector2.left, edges.left);

            return contour;
        }

        /// <summary>
        /// Evaluates a smooth Bezier tab curve along an edge from start to end.
        /// </summary>
        private static void AddEdgePoints(List<Vector2> contour, Vector2 start, Vector2 end, Vector2 normal, int edgeType)
        {
            Vector2 dir = end - start;
            float length = dir.magnitude;
            Vector2 tangent = dir / length;

            if (edgeType == FLAT)
            {
                // Straight segment
                contour.Add(start);
                return;
            }

            // Tab / Blank profile defined via cubic Bezier segments
            // normalized u in [0, 1] along tangent, v in normalized height along normal
            // Tab (+1) pushes outward along normal, Blank (-1) pushes inward
            float outwardSign = (edgeType == TAB) ? 1.0f : -1.0f;

            // Key control coordinates for canonical jigsaw tab:
            // Neck narrows slightly, bulb expands smoothly
            Vector2[] tabProfile = new Vector2[]
            {
                new Vector2(0.00f, 0.00f),
                new Vector2(0.35f, 0.00f), // shoulder start
                new Vector2(0.37f, -0.03f), // neck indent
                new Vector2(0.36f, 0.12f),  // bulb base
                new Vector2(0.42f, 0.22f),  // bulb expand
                new Vector2(0.50f, 0.24f),  // apex
                new Vector2(0.58f, 0.22f),  // bulb return
                new Vector2(0.64f, 0.12f),  // bulb base
                new Vector2(0.63f, -0.03f), // neck indent
                new Vector2(0.65f, 0.00f),  // shoulder end
                new Vector2(1.00f, 0.00f)
            };

            // Sample along profile curve
            int steps = 24;
            for (int i = 0; i < steps; i++)
            {
                float t = (float)i / steps;
                Vector2 localPt = SampleProfile(tabProfile, t);
                Vector2 worldPt = start + tangent * (localPt.x * length) + normal * (localPt.y * length * outwardSign);
                contour.Add(worldPt);
            }
        }

        private static Vector2 SampleProfile(Vector2[] profile, float t)
        {
            // Linear segment search with Catmull-Rom smoothing
            float fIndex = t * (profile.Length - 1);
            int idx = Mathf.Clamp(Mathf.FloorToInt(fIndex), 0, profile.Length - 2);
            float fract = fIndex - idx;

            Vector2 p0 = profile[Mathf.Max(0, idx - 1)];
            Vector2 p1 = profile[idx];
            Vector2 p2 = profile[Mathf.Min(profile.Length - 1, idx + 1)];
            Vector2 p3 = profile[Mathf.Min(profile.Length - 1, idx + 2)];

            return CatmullRom(p0, p1, p2, p3, fract);
        }

        private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        /// <summary>
        /// Fast point-in-polygon test (Jordan Curve theorem / Ray Casting).
        /// </summary>
        public static bool IsPointInPolygon(Vector2 pt, List<Vector2> poly)
        {
            bool inside = false;
            int count = poly.Count;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                if (((poly[i].y > pt.y) != (poly[j].y > pt.y)) &&
                    (pt.x < (poly[j].x - poly[i].x) * (pt.y - poly[i].y) / (poly[j].y - poly[i].y + 0.00001f) + poly[i].x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        /// <summary>
        /// Slices and masks the piece texture from the source image, with transparent padding.
        /// Also generates an accurate silhouette sprite tinted with silhouetteColor.
        /// </summary>
        public static void CreatePieceSprites(
            Texture2D sourceTex,
            int row, int col,
            int rows, int cols,
            PieceEdgeData edges,
            float pixelsPerUnit,
            Color silhouetteColor,
            out Sprite pieceSprite,
            out Sprite silhouetteSprite,
            out Vector2[] colliderPoints)
        {
            int srcW = sourceTex.width;
            int srcH = sourceTex.height;

            int pw = srcW / cols;
            int ph = srcH / rows;

            // 28% padding ensures even wide tabs are never clipped
            int padX = Mathf.CeilToInt(pw * 0.28f);
            int padY = Mathf.CeilToInt(ph * 0.28f);

            int targetW = pw + 2 * padX;
            int targetH = ph + 2 * padY;

            // Generate closed polygon contour in pixel space
            List<Vector2> contour = GeneratePieceContour(padX, padY, pw, ph, edges);

            // Bounding box shortcuts for fast masking
            float innerMinX = padX + pw * 0.25f;
            float innerMaxX = padX + pw * 0.75f;
            float innerMinY = padY + ph * 0.25f;
            float innerMaxY = padY + ph * 0.75f;

            Color[] piecePixels = new Color[targetW * targetH];
            Color[] silhPixels = new Color[targetW * targetH];
            Color transparent = new Color(0, 0, 0, 0);

            // Source origin for this piece's core
            int srcBaseX = col * pw;
            int srcBaseY = row * ph;

            for (int y = 0; y < targetH; y++)
            {
                int srcY = srcBaseY - padY + y;
                bool yInBounds = (srcY >= 0 && srcY < srcH);

                for (int x = 0; x < targetW; x++)
                {
                    int index = y * targetW + x;
                    Vector2 pt = new Vector2(x + 0.5f, y + 0.5f);

                    bool inside;
                    if (pt.x >= innerMinX && pt.x <= innerMaxX && pt.y >= innerMinY && pt.y <= innerMaxY)
                    {
                        inside = true; // Core area is unconditionally inside
                    }
                    else
                    {
                        inside = IsPointInPolygon(pt, contour);
                    }

                    int srcX = srcBaseX - padX + x;
                    bool inSrc = inside && yInBounds && (srcX >= 0 && srcX < srcW);

                    if (inSrc)
                    {
                        Color srcCol = sourceTex.GetPixel(srcX, srcY);
                        piecePixels[index] = srcCol;
                        silhPixels[index] = silhouetteColor;
                    }
                    else
                    {
                        piecePixels[index] = transparent;
                        silhPixels[index] = transparent;
                    }
                }
            }

            // Create piece texture
            Texture2D pieceTex = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
            pieceTex.filterMode = FilterMode.Bilinear;
            pieceTex.wrapMode = TextureWrapMode.Clamp;
            pieceTex.SetPixels(piecePixels);
            pieceTex.Apply();

            // Create silhouette texture
            Texture2D silhTex = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
            silhTex.filterMode = FilterMode.Bilinear;
            silhTex.wrapMode = TextureWrapMode.Clamp;
            silhTex.SetPixels(silhPixels);
            silhTex.Apply();

            // Pivot is centered at the CORE rectangle center, not padded texture center
            Vector2 coreCenterInPadded = new Vector2(padX + pw * 0.5f, padY + ph * 0.5f);
            Vector2 normalizedPivot = new Vector2(coreCenterInPadded.x / targetW, coreCenterInPadded.y / targetH);

            pieceSprite = Sprite.Create(
                pieceTex,
                new Rect(0, 0, targetW, targetH),
                normalizedPivot,
                pixelsPerUnit
            );

            silhouetteSprite = Sprite.Create(
                silhTex,
                new Rect(0, 0, targetW, targetH),
                normalizedPivot,
                pixelsPerUnit
            );

            // Compute local 2D collider points from polygon contour relative to pivot
            colliderPoints = new Vector2[contour.Count];
            for (int i = 0; i < contour.Count; i++)
            {
                Vector2 localPx = contour[i] - coreCenterInPadded;
                colliderPoints[i] = localPx / pixelsPerUnit;
            }
        }
    }
}
