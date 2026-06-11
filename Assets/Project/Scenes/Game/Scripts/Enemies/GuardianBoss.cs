using System.Collections;
using UnityEngine;

namespace FiveElements
{

    // ══════════════════════════════════════════════════════════════
    //  GuardianBoss — elemental boss that cycles through weak phases
    // ══════════════════════════════════════════════════════════════
    /// <summary>
    /// A boss that cycles through elemental phases.
    /// Each phase is only vulnerable to its weakness element.
    /// </summary>
    public class GuardianBoss : ElementalEnemy
    {
        [Header("Boss Phases")]
        [SerializeField] private ElementType[] phases;   // e.g. {Hoa, Tho, Kim}
        [SerializeField] private float phaseHealthThreshold = 0.33f;

        [Header("Phase Transition")]
        [SerializeField] private float phaseTransitionDuration = 2f;
        [SerializeField] private AudioClip phaseTransitionSound;

        private int _currentPhase = 0;
        private bool _inTransition = false;

        protected override void Awake()
        {
            base.Awake();
            if (phases.Length > 0)
                ApplyPhase(phases[0]);
        }

        protected override float ModifyDamage(float raw, ElementType source)
        {
            // Only take full damage from the weakness of current phase
            ElementType currentPhaseElement = phases[_currentPhase];
            ElementType weakness = ElementInteraction.GetWeakness(currentPhaseElement);

            if (source == weakness) return raw * 2f;   // vulnerable
            if (source == currentPhaseElement) return 0f; // immune to own element
            return raw * 0.25f;                            // resistant to everything else
        }

        protected override void UpdateStateMachine()
        {
            if (_inTransition) return;

            // Check for phase transition threshold
            float healthPct = _currentHealth / maxHealth;
            int expectedPhase = Mathf.Min(
                (int)((1f - healthPct) / phaseHealthThreshold),
                phases.Length - 1);

            if (expectedPhase > _currentPhase)
                StartCoroutine(TransitionPhase(expectedPhase));
            else
                base.UpdateStateMachine();
        }

        private void ApplyPhase(ElementType phase)
        {
            // Visual tint based on element from registry
            var data = FindFirstObjectByType<ElementRegistry>()?.Get(phase);
            if (data != null && spriteRenderer != null)
                spriteRenderer.color = data.primaryColor;

            Debug.Log($"[GuardianBoss] Phase → {phase}");
        }

        private IEnumerator TransitionPhase(int newPhase)
        {
            _inTransition = true;
            _rb.linearVelocity = Vector2.zero;

            PlaySound(phaseTransitionSound);
            animator?.SetTrigger("PhaseTransition");

            yield return new WaitForSeconds(phaseTransitionDuration);

            _currentPhase = newPhase;
            ApplyPhase(phases[_currentPhase]);

            _inTransition = false;
        }
    }
}
