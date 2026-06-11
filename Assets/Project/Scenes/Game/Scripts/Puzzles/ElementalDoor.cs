using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FiveElements
{

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
}