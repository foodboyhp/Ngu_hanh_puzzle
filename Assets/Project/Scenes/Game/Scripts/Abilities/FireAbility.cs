using System.Collections;
using UnityEngine;

namespace FiveElements
{
    // ══════════════════════════════════════════════════════════════
    #region HỎA — Fire Ability
    // ══════════════════════════════════════════════════════════════
    /// <summary>
    /// FIRE (Hỏa 火) — Energy & Destruction
    ///
    /// Primary:   Fire dash — propels player in facing direction,
    ///            leaving a flame trail that damages enemies.
    /// Secondary: Ignite — sets a targeted object on fire.
    /// Passive:   Immunity to fire damage.
    /// </summary>
    public class FireAbility : AbilityBase
    {
        [Header("Fire Dash")]
        [SerializeField] private float dashForce = 18f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private GameObject flameTrailPrefab;
        [SerializeField] private AudioClip dashSound;

        [Header("Ignite")]
        [SerializeField] private float igniteRange = 5f;
        [SerializeField] private LayerMask igniteLayerMask;
        [SerializeField] private GameObject fireVFXPrefab;
        [SerializeField] private AudioClip igniteSound;

        public override ElementType ElementType => ElementType.Fire;

        private bool _isDashing = false;

        protected override void Execute(PlayerController player)
        {
            if (!_isDashing)
                StartCoroutine(FireDash(player));
            // Secondary ignite can be a separate button — hooked via PlayerController
        }

        /// <summary>Called externally for the secondary ignite action.</summary>
        public void Ignite(Vector3 targetPos)
        {
            var hits = Physics2D.OverlapCircleAll(targetPos, 0.5f, igniteLayerMask);
            foreach (var hit in hits)
            {
                var burnable = hit.GetComponent<IBurnable>();
                burnable?.Ignite();
            }
            SpawnEffect(fireVFXPrefab, targetPos, Quaternion.identity, 4f);
            PlaySound(igniteSound);
        }

        private IEnumerator FireDash(PlayerController player)
        {
            _isDashing = true;
            player.SetDashing(true);

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            Vector2 dir = player.FacingDirection;

            rb.linearVelocity = dir * dashForce;
            PlaySound(dashSound);

            float elapsed = 0f;
            while (elapsed < dashDuration)
            {
                // Spawn trail particles
                SpawnEffect(flameTrailPrefab, player.transform.position,
                            Quaternion.identity, 0.8f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            player.SetDashing(false);
            _isDashing = false;
        }

        protected override void OnActivate()
        {
            // Could show fire aura shader on player renderer
        }
    }

    public interface IBurnable
    {
        void Ignite();
        void Extinguish();
    }
    #endregion
}