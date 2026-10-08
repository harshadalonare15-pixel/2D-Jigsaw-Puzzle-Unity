using System.Collections;
using UnityEngine;
using JigsawPuzzle.Core;

namespace JigsawPuzzle.Puzzle
{
    /// <summary>
    /// Controls individual jigsaw piece interaction: dragging with mouse/touch,
    /// sorting order elevation, snap-to-target detection, scale pop tween,
    /// and automated solve motion.
    /// Attach to: PuzzlePiece Prefab / GameObject
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PuzzlePiece : MonoBehaviour
    {
        [Header("State")]
        public int row;
        public int col;
        public Vector3 targetPosition;
        public float snapToleranceDistance = 0.5f;
        public bool isPlaced = false;

        [Header("References")]
        public GameObject silhouetteObject;

        private SpriteRenderer _spriteRenderer;
        private Collider2D _collider;
        private Vector3 _grabOffset;
        private bool _isDragging = false;
        private Camera _mainCamera;

        // Sorting order layers
        private const int SORT_PLACED = 2;
        private const int SORT_IDLE = 10;
        private const int SORT_DRAGGING = 100;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();
            _mainCamera = Camera.main;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = SORT_IDLE;
            }
        }

        private void Start()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (isPlaced) return;

            // Handle touch or mouse dragging
            HandleInput();
        }

        private void HandleInput()
        {
            // Support both mouse and touch input
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                Vector3 worldPos = GetWorldPosition(touch.position);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        if (IsPointerOverPiece(worldPos))
                        {
                            StartDragging(worldPos);
                        }
                        break;

                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (_isDragging)
                        {
                            ContinueDragging(worldPos);
                        }
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (_isDragging)
                        {
                            EndDragging();
                        }
                        break;
                }
            }
            else
            {
                // Fallback to standard mouse input
                Vector3 mouseWorldPos = GetWorldPosition(Input.mousePosition);

                if (Input.GetMouseButtonDown(0))
                {
                    if (IsPointerOverPiece(mouseWorldPos))
                    {
                        StartDragging(mouseWorldPos);
                    }
                }
                else if (Input.GetMouseButton(0) && _isDragging)
                {
                    ContinueDragging(mouseWorldPos);
                }
                else if (Input.GetMouseButtonUp(0) && _isDragging)
                {
                    EndDragging();
                }
            }
        }

        private bool IsPointerOverPiece(Vector3 worldPos)
        {
            if (_collider == null) return false;
            // Raycast or check point overlap
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            return hit == _collider;
        }

        private Vector3 GetWorldPosition(Vector3 screenPos)
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            screenPos.z = -_mainCamera.transform.position.z;
            return _mainCamera.ScreenToWorldPoint(screenPos);
        }

        private void StartDragging(Vector3 pointerWorldPos)
        {
            _isDragging = true;
            _grabOffset = transform.position - pointerWorldPos;
            _grabOffset.z = 0;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = SORT_DRAGGING;
            }
        }

        private void ContinueDragging(Vector3 pointerWorldPos)
        {
            Vector3 targetPos = pointerWorldPos + _grabOffset;
            targetPos.z = 0;
            transform.position = targetPos;
        }

        private void EndDragging()
        {
            _isDragging = false;

            // Check if dropped within snap tolerance of correct spot
            float distance = Vector2.Distance(transform.position, targetPosition);
            if (distance <= snapToleranceDistance)
            {
                SnapToTarget();
            }
            else
            {
                // Returned to unplaced state
                if (_spriteRenderer != null)
                {
                    _spriteRenderer.sortingOrder = SORT_IDLE;
                }
            }
        }

        /// <summary>
        /// Snaps the piece exactly to its target, locks it, hides silhouette, and triggers pop tween.
        /// </summary>
        public void SnapToTarget()
        {
            isPlaced = true;
            _isDragging = false;
            transform.position = targetPosition;

            if (_collider != null)
            {
                _collider.enabled = false;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = SORT_PLACED;
            }

            if (silhouetteObject != null)
            {
                silhouetteObject.SetActive(false);
            }

            // Play snap sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySnap();
            }

            // Play scale pop tween
            StartCoroutine(PlayPopAnimation());

            // Notify game manager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPiecePlaced(this);
            }
        }

        /// <summary>
        /// Plain C# scale pop tween: 1.0 -> 1.20 -> 1.0 with smooth easing.
        /// Zero external asset dependencies.
        /// </summary>
        private IEnumerator PlayPopAnimation()
        {
            Vector3 originalScale = transform.localScale;
            Vector3 peakScale = originalScale * 1.18f;
            float duration = 0.18f;
            float elapsed = 0f;

            // Scale Up
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                transform.localScale = Vector3.Lerp(originalScale, peakScale, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }

            elapsed = 0f;
            // Scale Down
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.5f);
                transform.localScale = Vector3.Lerp(peakScale, originalScale, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }

            transform.localScale = originalScale;
        }

        /// <summary>
        /// Coroutine for smooth solver motion with easing and optional delay.
        /// </summary>
        public IEnumerator AnimateToTarget(float duration, float delay)
        {
            if (isPlaced) yield break;

            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }

            if (isPlaced) yield break;

            Vector3 startPos = transform.position;
            float elapsed = 0f;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = SORT_DRAGGING;
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease Out Cubic
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                transform.position = Vector3.Lerp(startPos, targetPosition, ease);
                yield return null;
            }

            SnapToTarget();
        }
    }
}
