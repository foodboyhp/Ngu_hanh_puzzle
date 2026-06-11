// ============================================================
//  PuzzleObject.cs
//  Base class for every interactive object in a puzzle
//  (pressure plates, elemental doors, shrines, switches, etc.)
//
//  Subclass this for each puzzle piece type.
//  Override OnElementApplied() to handle specific element effects.
//
//  Place in: Assets/Scripts/Puzzle/
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FiveElements
{
    public abstract class PuzzleObject : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────
        [Header("Puzzle Object Settings")]
        [Tooltip("Human-readable ID used by PuzzleRoom to track state.")]
        [SerializeField] protected string puzzleID;

        [Tooltip("Which element(s) activate this object. Empty = any element.")]
        [SerializeField] protected List<ElementType> requiredElements = new();

        [Tooltip("Can this object be activated more than once?")]
        [SerializeField] protected bool repeatable = false;

        [Header("State")]
        [SerializeField] protected bool startsSolved = false;

        [Header("Unity Events (inspector wiring)")]
        public UnityEvent OnActivated;    // fired when puzzle object is successfully activated
        public UnityEvent OnDeactivated;  // fired when it resets/deactivates
        public UnityEvent OnSolved;       // fired when permanently solved

        // ── State ─────────────────────────────────────────────────
        protected bool _isActivated = false;
        protected bool _isSolved = false;

        public bool IsActivated => _isActivated;
        public bool IsSolved => _isSolved;
        public string PuzzleID => puzzleID;

        // ── Events (code subscriptions) ──────────────────────────
        public System.Action<PuzzleObject> OnObjectActivated;
        public System.Action<PuzzleObject> OnObjectSolved;

        // ──────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ──────────────────────────────────────────────────────────
        protected virtual void Start()
        {
            if (startsSolved) Solve();
        }

        // ──────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────

        /// <summary>
        /// Called by abilities, player controller, or enemy AI when
        /// an element is applied to this object.
        /// </summary>
        public virtual void OnElementApplied(ElementType element)
        {
            if (_isSolved && !repeatable) return;

            bool accepted = AcceptsElement(element);
            if (!accepted)
            {
                OnWrongElement(element);
                return;
            }

            // Check if applying this element creates a combo with the active element
            ElementType active = ElementManager.Instance != null
                ? ElementManager.Instance.ActiveElement
                : ElementType.None;

            if (active != element && active != ElementType.None)
            {
                if (ElementInteraction.TryGetCombo(active, element, out var combo))
                {
                    Debug.Log($"[PuzzleObject:{puzzleID}] Combo triggered: {combo.Name}");
                    HandleCombo(combo, element);
                }
            }

            Activate(element);
        }

        /// <summary>Force-activate without element check (cutscenes, etc.).</summary>
        public void ForceActivate()
        {
            _isActivated = true;
            OnActivate(ElementType.None);
            OnActivated?.Invoke();
            OnObjectActivated?.Invoke(this);
        }

        /// <summary>Reset to initial state.</summary>
        public virtual void Reset()
        {
            if (_isSolved && !repeatable) return;
            _isActivated = false;
            OnDeactivate();
            OnDeactivated?.Invoke();
        }

        /// <summary>Mark this object as permanently solved.</summary>
        public virtual void Solve()
        {
            _isSolved = true;
            _isActivated = true;
            OnSolve();
            OnSolved?.Invoke();
            OnObjectSolved?.Invoke(this);
        }

        // ──────────────────────────────────────────────────────────
        // Abstract / Virtual — subclasses implement these
        // ──────────────────────────────────────────────────────────

        /// <summary>What happens visually/physically when this object activates.</summary>
        protected abstract void OnActivate(ElementType element);

        /// <summary>Cleanup when deactivated/reset.</summary>
        protected virtual void OnDeactivate() { }

        /// <summary>Final state when solved (lock in place, glow, etc.).</summary>
        protected virtual void OnSolve() { }

        /// <summary>Called when an incompatible element is applied.</summary>
        protected virtual void OnWrongElement(ElementType element)
        {
            Debug.Log($"[PuzzleObject:{puzzleID}] Wrong element: {element}");
        }

        /// <summary>Called when the applied element triggers a combo with the active one.</summary>
        protected virtual void HandleCombo(ComboEffect combo, ElementType applied) { }

        // ──────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────
        protected bool AcceptsElement(ElementType element)
        {
            if (requiredElements == null || requiredElements.Count == 0) return true;
            return requiredElements.Contains(element);
        }

        private void Activate(ElementType element)
        {
            _isActivated = true;
            OnActivate(element);
            OnActivated?.Invoke();
            OnObjectActivated?.Invoke(this);
        }
    }

}