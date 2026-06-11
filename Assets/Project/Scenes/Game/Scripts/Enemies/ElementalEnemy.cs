using System.Collections;
using UnityEngine;
namespace FiveElements
{
    // ══════════════════════════════════════════════════════════════
    //  ElementalEnemy  — adds elemental affinity, weakness, immunity
    // ══════════════════════════════════════════════════════════════
    public class ElementalEnemy : EnemyBase, IFreezable, IEntanglable, IBurnable
    {
        [Header("Elemental Affinity")]
        [Tooltip("This enemy's own element. Determines weakness/resistance.")]
        [SerializeField] private ElementType enemyElement = ElementType.None;

        [Tooltip("Visual tint applied based on element.")]
        [SerializeField] private Color elementTintColor = Color.white;

        [Header("Status Effects")]
        [SerializeField] private GameObject frozenVFX;
        [SerializeField] private GameObject rootVFX;
        [SerializeField] private GameObject burnVFX;

        // Status flags
        private bool _isFrozen = false;
        private bool _isEntangled = false;
        private bool _isBurning = false;

        public ElementType EnemyElement => enemyElement;

        // ──────────────────────────────────────────────────────────
        protected override void Awake()
        {
            base.Awake();

            // Apply elemental tint to sprite
            if (spriteRenderer != null && elementTintColor != Color.white)
                spriteRenderer.color = elementTintColor;
        }

        // ──────────────────────────────────────────────────────────
        // Damage Modification
        // ──────────────────────────────────────────────────────────
        protected override float ModifyDamage(float raw, ElementType source)
        {
            if (source == ElementType.None || enemyElement == ElementType.None)
                return raw;

            float multiplier = ElementInteraction.GetDamageMultiplier(source, enemyElement);

            if (multiplier >= 2f)
                Debug.Log($"[ElementalEnemy] Weakness hit! {source} vs {enemyElement} → {multiplier}×");
            else if (multiplier <= 0.5f)
                Debug.Log($"[ElementalEnemy] Resistance! {source} vs {enemyElement} → {multiplier}×");

            return raw * multiplier;
        }

        // ──────────────────────────────────────────────────────────
        // IFreezable
        // ──────────────────────────────────────────────────────────
        public void Freeze()
        {
            if (_isFrozen || _state == EnemyState.Dead) return;
            _isFrozen = true;
            _rb.constraints = RigidbodyConstraints2D.FreezeAll;
            if (frozenVFX != null) frozenVFX.SetActive(true);
            if (spriteRenderer != null) spriteRenderer.color = Color.cyan;
            StartCoroutine(UnfreezeAfter(4f));
        }

        public void Unfreeze()
        {
            _isFrozen = false;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            if (frozenVFX != null) frozenVFX.SetActive(false);
            if (spriteRenderer != null) spriteRenderer.color = elementTintColor;
        }

        private IEnumerator UnfreezeAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Unfreeze();
        }

        // ──────────────────────────────────────────────────────────
        // IEntanglable
        // ──────────────────────────────────────────────────────────
        public void Entangle()
        {
            if (_isEntangled || _state == EnemyState.Dead) return;
            _isEntangled = true;
            _rb.constraints = RigidbodyConstraints2D.FreezeAll;
            if (rootVFX != null) rootVFX.SetActive(true);
        }

        public void Release()
        {
            _isEntangled = false;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            if (rootVFX != null) rootVFX.SetActive(false);
        }

        // ──────────────────────────────────────────────────────────
        // IBurnable
        // ──────────────────────────────────────────────────────────
        public override void Ignite()
        {
            if (_isBurning || _state == EnemyState.Dead) return;
            _isBurning = true;
            if (burnVFX != null) burnVFX.SetActive(true);
            base.Ignite(); // uses BurnRoutine in EnemyBase
        }

        public void Extinguish()
        {
            _isBurning = false;
            if (burnVFX != null) burnVFX.SetActive(false);
            StopAllCoroutines(); // stops burn coroutine
        }

        // ──────────────────────────────────────────────────────────
        protected override void UpdateStateMachine()
        {
            // Frozen / entangled enemies can't move
            if (_isFrozen || _isEntangled) return;
            base.UpdateStateMachine();
        }
    }
}