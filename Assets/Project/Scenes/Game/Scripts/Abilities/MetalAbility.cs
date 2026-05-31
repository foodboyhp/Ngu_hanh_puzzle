using System.Collections;
using UnityEngine;

namespace FiveElements
{
    // ══════════════════════════════════════════════════════════════
    #region KIM — Metal Ability
    // ══════════════════════════════════════════════════════════════
    /// <summary>
    /// METAL (Kim 金) — Hardness & Magnetism
    ///
    /// Primary:   Magnetic Pull — attract metallic objects or
    ///            enemies toward player.
    /// Secondary: Reflect — create a brief reflective barrier
    ///            that bounces projectiles.
    /// Passive:   Deflects one hit per 8 seconds (metal armor).
    /// </summary>
    public class MetalAbility : AbilityBase
    {
        [Header("Magnetic Pull")]
        [SerializeField] private float pullRadius = 5f;
        [SerializeField] private float pullForce = 10f;
        [SerializeField] private float pullDuration = 1f;
        [SerializeField] private LayerMask metallicLayer;
        [SerializeField] private AudioClip pullSound;

        [Header("Reflect Barrier")]
        [SerializeField] private GameObject reflectBarrierPrefab;
        [SerializeField] private float barrierDuration = 1.5f;
        [SerializeField] private AudioClip reflectSound;

        [Header("Passive Armor")]
        [SerializeField] private float armorCooldown = 8f;

        public override ElementType ElementType => ElementType.Metal;

        private bool _armorReady = true;

        private int _useCount = 0;

        protected override void Awake()
        {
            base.Awake();
            StartCoroutine(ArmorRechargeRoutine());
        }

        protected override void Execute(PlayerController player)
        {
            _useCount++;
            if (_useCount % 2 == 1)
                StartCoroutine(MagneticPullRoutine(player));
            else
                StartCoroutine(ReflectBarrierRoutine(player));
        }

        private IEnumerator MagneticPullRoutine(PlayerController player)
        {
            PlaySound(pullSound);
            float elapsed = 0f;

            while (elapsed < pullDuration)
            {
                var hits = Physics2D.OverlapCircleAll(
                    player.transform.position, pullRadius, metallicLayer);

                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Player")) continue;
                    Rigidbody2D rb = hit.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        Vector2 dir = (player.transform.position - hit.transform.position).normalized;
                        rb.AddForce(dir * pullForce, ForceMode2D.Force);
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator ReflectBarrierRoutine(PlayerController player)
        {
            PlaySound(reflectSound);
            GameObject barrier = Instantiate(reflectBarrierPrefab,
                                              player.transform.position,
                                              Quaternion.identity,
                                              player.transform);  // child of player
            yield return new WaitForSeconds(barrierDuration);
            if (barrier != null) Destroy(barrier);
        }

        /// <summary>
        /// Called by the damage system before applying damage.
        /// If armor is ready, absorbs one hit and returns true.
        /// </summary>
        public bool TryAbsorbHit()
        {
            if (!_isUnlocked || !_armorReady) return false;
            _armorReady = false;
            Debug.Log("[MetalAbility] Passive armor absorbed a hit!");
            return true;
        }

        private IEnumerator ArmorRechargeRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(armorCooldown);
                if (_isUnlocked)
                {
                    _armorReady = true;
                    Debug.Log("[MetalAbility] Passive armor recharged.");
                }
            }
        }
    }
    #endregion
}