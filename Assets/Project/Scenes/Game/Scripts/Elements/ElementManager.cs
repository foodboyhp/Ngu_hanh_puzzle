// ============================================================
//  ElementManager.cs
//  Singleton. Tracks which elements the player has absorbed,
//  which element is currently active, and fires events for
//  every state change so all other systems can react.
//
//  Place in: Assets/Scripts/Elements/
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace FiveElements
{
    public class ElementManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────
        public static ElementManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────
        [Header("Configuration")]
        [Tooltip("Drag the ElementRegistry ScriptableObject here.")]
        [SerializeField] private ElementRegistry registry;

        [Tooltip("Maximum elements the player can hold simultaneously.")]
        [SerializeField] private int maxElements = 5;

        // ── Events ────────────────────────────────────────────────
        /// <summary>Fired when the player absorbs a new element.</summary>
        public event Action<ElementType> OnElementAbsorbed;

        /// <summary>Fired when the player switches active element.</summary>
        public event Action<ElementType> OnActiveElementChanged;

        /// <summary>Fired when an element is removed (e.g. boss strips it).</summary>
        public event Action<ElementType> OnElementLost;

        // ── State ─────────────────────────────────────────────────
        /// <summary>Ordered list of absorbed elements (insertion order).</summary>
        private readonly List<ElementType> _absorbed = new List<ElementType>();

        /// <summary>Currently active/selected element.</summary>
        private ElementType _activeElement = ElementType.None;

        // ── Properties ────────────────────────────────────────────
        public IReadOnlyList<ElementType> AbsorbedElements => _absorbed;
        public ElementType ActiveElement => _activeElement;
        public int ElementCount => _absorbed.Count;
        public bool IsFull => _absorbed.Count >= maxElements;

        // ──────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ──────────────────────────────────────────────────────────
        private void Awake()
        {
            // Enforce singleton pattern — persist across scene loads
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Player starts with Water (Thủy) by design
            AbsorbElement(ElementType.Water);
        }

        // ──────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────

        /// <summary>
        /// Attempt to absorb a new element.
        /// Returns true on success, false if already absorbed or inventory full.
        /// </summary>
        public bool AbsorbElement(ElementType element)
        {
            if (element == ElementType.None)
            {
                Debug.LogWarning("[ElementManager] Cannot absorb ElementType.None.");
                return false;
            }

            if (_absorbed.Contains(element))
            {
                Debug.Log($"[ElementManager] Player already has {element}.");
                return false;
            }

            if (IsFull)
            {
                Debug.Log("[ElementManager] Element inventory is full.");
                return false;
            }

            _absorbed.Add(element);
            Debug.Log($"[ElementManager] Absorbed: {element}  (total: {_absorbed.Count})");

            // If this is the first element, auto-select it
            if (_absorbed.Count == 1)
                SetActiveElement(element);

            OnElementAbsorbed?.Invoke(element);

            // Play absorption VFX / SFX via the registry
            PlayAbsorbEffect(element);

            return true;
        }

        /// <summary>
        /// Set the currently active element (must already be absorbed).
        /// </summary>
        public bool SetActiveElement(ElementType element)
        {
            if (!_absorbed.Contains(element))
            {
                Debug.LogWarning($"[ElementManager] Cannot activate {element} — not yet absorbed.");
                return false;
            }

            if (_activeElement == element) return true; // no change

            _activeElement = element;
            Debug.Log($"[ElementManager] Active element → {element}");
            OnActiveElementChanged?.Invoke(_activeElement);
            return true;
        }

        /// <summary>
        /// Cycle to the next absorbed element (wraps around).
        /// </summary>
        public void CycleNext()
        {
            if (_absorbed.Count == 0) return;
            int idx = _absorbed.IndexOf(_activeElement);
            int next = (idx + 1) % _absorbed.Count;
            SetActiveElement(_absorbed[next]);
        }

        /// <summary>
        /// Cycle to the previous absorbed element (wraps around).
        /// </summary>
        public void CyclePrevious()
        {
            if (_absorbed.Count == 0) return;
            int idx = _absorbed.IndexOf(_activeElement);
            int prev = (idx - 1 + _absorbed.Count) % _absorbed.Count;
            SetActiveElement(_absorbed[prev]);
        }

        /// <summary>
        /// Remove an element (e.g. boss steals it). Switches active if needed.
        /// </summary>
        public bool LoseElement(ElementType element)
        {
            if (!_absorbed.Remove(element)) return false;

            Debug.Log($"[ElementManager] Lost element: {element}");

            if (_activeElement == element)
            {
                _activeElement = _absorbed.Count > 0 ? _absorbed[0] : ElementType.None;
                OnActiveElementChanged?.Invoke(_activeElement);
            }

            OnElementLost?.Invoke(element);
            return true;
        }

        /// <summary>Returns true if the player has absorbed the given element.</summary>
        public bool HasElement(ElementType element) => _absorbed.Contains(element);

        /// <summary>Returns ElementData for the active element from the registry.</summary>
        public ElementData GetActiveElementData() =>
            registry != null ? registry.Get(_activeElement) : null;

        // ──────────────────────────────────────────────────────────
        // Private Helpers
        // ──────────────────────────────────────────────────────────
        private void PlayAbsorbEffect(ElementType element)
        {
            if (registry == null) return;
            ElementData data = registry.Get(element);
            if (data == null) return;

            // Sound
            if (data.absorbSound != null)
                AudioSource.PlayClipAtPoint(data.absorbSound, transform.position);

            // VFX prefab spawned at player position
            if (data.absorbVFXPrefab != null)
                Instantiate(data.absorbVFXPrefab, transform.position, Quaternion.identity);
        }
    }
}