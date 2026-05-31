// ============================================================
//  CameraController.cs + CameraShaker.cs
//  Smooth follow camera with look-ahead, bounds clamping,
//  screen shake, and an element-reactive color vignette.
//
//  Place in: Assets/Scripts/Camera/
// ============================================================

using UnityEngine;

namespace FiveElements
{
    // ══════════════════════════════════════════════════════════════
    //  CameraController
    // ══════════════════════════════════════════════════════════════
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────
        [Header("Follow Target")]
        [SerializeField] private Transform target;
        [SerializeField] private float followSmoothing = 5f;

        [Header("Look-ahead")]
        [Tooltip("How far the camera peeks ahead in the player's facing direction.")]
        [SerializeField] private float lookAheadDistance = 2f;
        [SerializeField] private float lookAheadSmoothing = 3f;

        [Header("Vertical Offset")]
        [SerializeField] private float verticalOffset = 1f;

        [Header("Bounds (optional — leave at zero to disable)")]
        [SerializeField] private bool useBounds = false;
        [SerializeField] private float minX, maxX, minY, maxY;

        [Header("Zoom")]
        [SerializeField] private float defaultZoom = 5f;
        [SerializeField] private float zoomSmoothing = 3f;

        // ── State ─────────────────────────────────────────────────
        private Camera _cam;
        private Vector3 _velocity = Vector3.zero;
        private Vector3 _lookAheadOffset = Vector3.zero;
        private float _targetZoom;
        private PlayerController _player;

        // ──────────────────────────────────────────────────────────
        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _targetZoom = defaultZoom;
        }

        private void Start()
        {
            if (target == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    target = playerObj.transform;
                    _player = playerObj.GetComponent<PlayerController>();
                }
            }

            // Subscribe to element changes for vignette
            if (ElementManager.Instance != null)
                ElementManager.Instance.OnActiveElementChanged += OnActiveElementChanged;
        }

        private void OnDisable()
        {
            if (ElementManager.Instance != null)
                ElementManager.Instance.OnActiveElementChanged -= OnActiveElementChanged;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            UpdateLookAhead();
            FollowTarget();
            UpdateZoom();
        }

        // ──────────────────────────────────────────────────────────
        private void UpdateLookAhead()
        {
            if (_player == null) return;
            Vector3 desiredAhead = _player.FacingDirection * lookAheadDistance;
            _lookAheadOffset = Vector3.Lerp(_lookAheadOffset, desiredAhead,
                                             lookAheadSmoothing * Time.deltaTime);
        }

        private void FollowTarget()
        {
            Vector3 desired = target.position
                            + _lookAheadOffset
                            + Vector3.up * verticalOffset;
            desired.z = transform.position.z; // maintain camera Z

            Vector3 smoothed = Vector3.SmoothDamp(transform.position, desired,
                                                   ref _velocity, 1f / followSmoothing);

            if (useBounds)
            {
                float halfHeight = _cam.orthographicSize;
                float halfWidth = halfHeight * _cam.aspect;
                smoothed.x = Mathf.Clamp(smoothed.x, minX + halfWidth, maxX - halfWidth);
                smoothed.y = Mathf.Clamp(smoothed.y, minY + halfHeight, maxY - halfHeight);
            }

            transform.position = smoothed;
        }

        private void UpdateZoom()
        {
            _cam.orthographicSize = Mathf.Lerp(
                _cam.orthographicSize, _targetZoom, zoomSmoothing * Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────
        public void SetZoom(float zoom) => _targetZoom = zoom;
        public void ResetZoom() => _targetZoom = defaultZoom;

        public void SetBounds(float x0, float x1, float y0, float y1)
        {
            useBounds = true;
            minX = x0; maxX = x1; minY = y0; maxY = y1;
        }

        // Called by ElementManager when active element changes
        private void OnActiveElementChanged(ElementType element)
        {
            // Trigger vignette colour change on the PostProcess volume or UI overlay
            ElementVignetteController.SetElement(element);
        }
    }
}