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

using System.Collections;
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

    // ──────────────────────────────────────────────────────────────
    // Concrete Puzzle Objects
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// A door or gate that opens when all required elements are applied.
    /// Accepts a list of required elements in ANY order.
    /// </summary>
    public class ElementalDoor : PuzzleObject
    {
        [Header("Elemental Door")]
        [SerializeField] private Transform doorTransform;
        [SerializeField] private Vector3 openPosition;
        [SerializeField] private float openSpeed = 2f;
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip wrongElementSound;

        private HashSet<ElementType> _appliedElements = new HashSet<ElementType>();
        private Vector3 _closedPosition;

        protected override void Start()
        {
            base.Start();
            if (doorTransform != null)
                _closedPosition = doorTransform.localPosition;
        }

        public override void OnElementApplied(ElementType element)
        {
            if (_isSolved) return;
            if (!AcceptsElement(element))
            {
                OnWrongElement(element);
                return;
            }

            _appliedElements.Add(element);
            Debug.Log($"[ElementalDoor:{puzzleID}] Got {element} " +
                      $"({_appliedElements.Count}/{requiredElements.Count})");

            // Flash the door rune for this element
            OnActivate(element);

            // Check if all required elements have been applied
            bool allApplied = true;
            foreach (var req in requiredElements)
                if (!_appliedElements.Contains(req)) { allApplied = false; break; }

            if (allApplied) Solve();
        }

        protected override void OnActivate(ElementType element)
        {
            // Visual rune glow could be driven by animator triggers here
        }

        protected override void OnSolve()
        {
            StartCoroutine(OpenDoor());
            if (openSound != null) AudioSource.PlayClipAtPoint(openSound, transform.position);
        }

        protected override void OnWrongElement(ElementType element)
        {
            if (wrongElementSound != null)
                AudioSource.PlayClipAtPoint(wrongElementSound, transform.position);
        }

        private IEnumerator OpenDoor()
        {
            if (doorTransform == null) yield break;
            Vector3 target = _closedPosition + openPosition;
            while (Vector3.Distance(doorTransform.localPosition, target) > 0.01f)
            {
                doorTransform.localPosition = Vector3.MoveTowards(
                    doorTransform.localPosition, target, openSpeed * Time.deltaTime);
                yield return null;
            }
            doorTransform.localPosition = target;
        }

        public override void Reset()
        {
            base.Reset();
            _appliedElements.Clear();
            if (doorTransform != null)
                doorTransform.localPosition = _closedPosition;
        }
    }

    /// <summary>
    /// A pressure plate activated by the Earth tremor or a heavy object.
    /// </summary>
    public class PressurePlate : PuzzleObject
    {
        [Header("Pressure Plate")]
        [SerializeField] private float pressDepth = 0.1f;
        [SerializeField] private AudioClip pressSound;
        [SerializeField] private AudioClip releaseSound;

        private Vector3 _restPosition;

        protected override void Start()
        {
            base.Start();
            _restPosition = transform.localPosition;
        }

        protected override void OnActivate(ElementType element)
        {
            transform.localPosition = _restPosition - new Vector3(0, pressDepth, 0);
            if (pressSound != null) AudioSource.PlayClipAtPoint(pressSound, transform.position);
        }

        protected override void OnDeactivate()
        {
            transform.localPosition = _restPosition;
            if (releaseSound != null) AudioSource.PlayClipAtPoint(releaseSound, transform.position);
        }

        // Physical objects can also step on this
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.CompareTag("HeavyObject"))
                ForceActivate();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.CompareTag("HeavyObject"))
                Reset();
        }
    }

    /// <summary>
    /// A water-basin puzzle object. Fills when Water is applied,
    /// freezes when Water+Metal combo fires.
    /// </summary>
    public class WaterBasin : PuzzleObject, IFreezable
    {
        [Header("Water Basin")]
        [SerializeField] private SpriteRenderer waterRenderer;
        [SerializeField] private Color emptyColor = Color.gray;
        [SerializeField] private Color filledColor = new Color(0.2f, 0.5f, 1f, 0.8f);
        [SerializeField] private Color frozenColor = new Color(0.8f, 0.95f, 1f, 0.9f);
        [SerializeField] private AudioClip fillSound;
        [SerializeField] private AudioClip freezeSound;

        private bool _isFilled = false;
        private bool _isFrozen = false;

        public bool IsFilled => _isFilled;
        public bool IsFrozen => _isFrozen;

        protected override void Start()
        {
            base.Start();
            if (waterRenderer) waterRenderer.color = emptyColor;
        }

        protected override void OnActivate(ElementType element)
        {
            if (element == ElementType.Water)
            {
                _isFilled = true;
                if (waterRenderer) waterRenderer.color = filledColor;
                if (fillSound) AudioSource.PlayClipAtPoint(fillSound, transform.position);
            }
        }

        protected override void HandleCombo(ComboEffect combo, ElementType applied)
        {
            // Crystal Water combo: Metal purifies filled water
            if (_isFilled && combo.Name == "Crystal Water")
                Solve();
        }

        public void Freeze()
        {
            if (!_isFilled) return;
            _isFrozen = true;
            if (waterRenderer) waterRenderer.color = frozenColor;
            if (freezeSound) AudioSource.PlayClipAtPoint(freezeSound, transform.position);
            // Make it walkable — enable a platform collider
            var platformCollider = GetComponent<PlatformEffector2D>();
            if (platformCollider != null) platformCollider.enabled = true;
        }

        public void Unfreeze()
        {
            _isFrozen = false;
            if (waterRenderer) waterRenderer.color = _isFilled ? filledColor : emptyColor;
            var platformCollider = GetComponent<PlatformEffector2D>();
            if (platformCollider != null) platformCollider.enabled = false;
        }

        public override void Reset()
        {
            base.Reset();
            _isFilled = false;
            _isFrozen = false;
            if (waterRenderer) waterRenderer.color = emptyColor;
        }
    }
}