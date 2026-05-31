// ============================================================
//  ElementShrine.cs
//  An interactable world object. When the player enters its
//  trigger and presses Interact, the shrine grants a new
//  element — but only if the puzzle room gating it is solved.
//
//  Place in: Assets/Scripts/World/
// ============================================================

using System.Collections;
using UnityEngine;

namespace FiveElements
{
    public class ElementShrine : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────
        [Header("Element to Grant")]
        [SerializeField] private ElementType grantElement;

        [Header("Gate (optional)")]
        [Tooltip("If assigned, the shrine is locked until this room is solved.")]
        [SerializeField] private PuzzleRoom requiredRoom;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer shrineRenderer;
        [SerializeField] private ParticleSystem idleParticles;
        [SerializeField] private ParticleSystem absorbParticles;
        [SerializeField] private GameObject promptUI;          // "Press F to absorb"
        [SerializeField] private Animator shrineAnimator;

        [Header("Colors")]
        [SerializeField] private Color lockedColor = Color.gray;
        [SerializeField] private Color availableColor = Color.white;
        [SerializeField] private Color absorbedColor = new Color(0.3f, 0.3f, 0.3f);

        [Header("Audio")]
        [SerializeField] private AudioClip interactSound;
        [SerializeField] private AudioClip alreadyAbsorbedSound;
        [SerializeField] private AudioClip lockedSound;

        // ── State ─────────────────────────────────────────────────
        private bool _playerInRange = false;
        private bool _absorbed = false;

        // ──────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ──────────────────────────────────────────────────────────
        private void Start()
        {
            UpdateVisuals();

            // Subscribe to know when the required room is solved
            if (requiredRoom != null)
                requiredRoom.OnSolved += _ => UpdateVisuals();

            // If element is already absorbed on load (continue save), mark used
            if (ElementManager.Instance != null &&
                ElementManager.Instance.HasElement(grantElement))
            {
                _absorbed = true;
                UpdateVisuals();
            }
        }

        private void Update()
        {
            if (_playerInRange && Input.GetKeyDown(KeyCode.F))
                TryAbsorb();
        }

        // ──────────────────────────────────────────────────────────
        // Trigger
        // ──────────────────────────────────────────────────────────
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInRange = true;
            if (promptUI != null) promptUI.SetActive(true && !_absorbed && IsUnlocked());
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInRange = false;
            if (promptUI != null) promptUI.SetActive(false);
        }

        // ──────────────────────────────────────────────────────────
        // Absorb Logic
        // ──────────────────────────────────────────────────────────
        private void TryAbsorb()
        {
            if (_absorbed)
            {
                PlaySound(alreadyAbsorbedSound);
                return;
            }

            if (!IsUnlocked())
            {
                PlaySound(lockedSound);
                shrineAnimator?.SetTrigger("Locked");
                return;
            }

            StartCoroutine(AbsorbSequence());
        }

        private IEnumerator AbsorbSequence()
        {
            _absorbed = true;
            if (promptUI != null) promptUI.SetActive(false);

            // Play absorb animation
            shrineAnimator?.SetTrigger("Absorb");
            PlaySound(interactSound);

            // Burst particles
            if (idleParticles != null) idleParticles.Stop();
            if (absorbParticles != null) absorbParticles.Play();

            yield return new WaitForSeconds(0.5f);

            // Grant the element via LevelProgressionManager (handles cutscene + timing)
            LevelProgressionManager.Instance?.TriggerElementUnlock(grantElement);

            UpdateVisuals();
        }

        // ──────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────
        private bool IsUnlocked() =>
            requiredRoom == null || requiredRoom.IsSolved;

        private void UpdateVisuals()
        {
            if (shrineRenderer == null) return;

            if (_absorbed)
                shrineRenderer.color = absorbedColor;
            else if (IsUnlocked())
                shrineRenderer.color = availableColor;
            else
                shrineRenderer.color = lockedColor;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }
}