using UnityEngine;

namespace FiveElements
{
    // ══════════════════════════════════════════════════════════════
    #region THỦY — Water Ability
    // ══════════════════════════════════════════════════════════════
    /// <summary>
    /// WATER (Thủy 水) — Flow & Adaptation
    ///
    /// Primary:   Shoot a water projectile that can push objects
    ///            or fill water-activated switches.
    /// Secondary: Freeze a body of water (call TryUse twice:
    ///            first fires, second freezes the last hit area).
    /// Passive:   Player can swim instead of sinking.
    /// </summary>
    public class WaterAbility : AbilityBase
    {
        [Header("Water Settings")]
        [SerializeField] private GameObject waterProjectilePrefab;
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField] private float projectileRange = 8f;
        [SerializeField] private AudioClip waterShootSound;

        [Header("Freeze Sub-Ability")]
        [SerializeField] private float freezeRadius = 2f;
        [SerializeField] private LayerMask freezeLayerMask;
        [SerializeField] private GameObject freezeVFXPrefab;
        [SerializeField] private AudioClip freezeSound;

        public override ElementType ElementType => ElementType.Water;

        // Internal state
        private bool _nextShotFreezes = false;
        private Vector3 _lastProjectileHitPos;

        protected override void Execute(PlayerController player)
        {
            if (!_nextShotFreezes)
                ShootWaterProjectile(player);
            else
                FreezeArea(_lastProjectileHitPos);
        }

        private void ShootWaterProjectile(PlayerController player)
        {
            if (waterProjectilePrefab == null) return;

            Vector3 origin = player.transform.position;
            Vector2 direction = player.FacingDirection;

            GameObject proj = SpawnEffect(waterProjectilePrefab, origin,
                                          Quaternion.LookRotation(Vector3.forward, direction),
                                          projectileRange / projectileSpeed + 0.5f);

            if (proj != null)
            {
                var rb = proj.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = direction * projectileSpeed;

                // Give the projectile a callback so we can track where it hits
                var projComp = proj.GetComponent<WaterProjectile>();
                if (projComp != null)
                    projComp.OnHit += pos =>
                    {
                        _lastProjectileHitPos = pos;
                        _nextShotFreezes = true;
                    };
            }

            PlaySound(waterShootSound);
        }

        private void FreezeArea(Vector3 center)
        {
            // Find all IFreezable objects in radius
            var hits = Physics2D.OverlapCircleAll(center, freezeRadius, freezeLayerMask);
            foreach (var hit in hits)
            {
                var freezable = hit.GetComponent<IFreezable>();
                freezable?.Freeze();
            }

            SpawnEffect(freezeVFXPrefab, center, Quaternion.identity, 2f);
            PlaySound(freezeSound);
            _nextShotFreezes = false;
        }

        protected override void OnActivate()
        {
            // Could tint player blue, show water aura, etc.
        }
    }

    /// <summary>Simple projectile behaviour attached to the water bolt prefab.</summary>
    public class WaterProjectile : MonoBehaviour
    {
        public System.Action<Vector3> OnHit;

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Don't hit the player itself
            if (other.CompareTag("Player")) return;
            OnHit?.Invoke(transform.position);
            Destroy(gameObject);
        }
    }

    /// <summary>Implement on any GameObject that can be frozen.</summary>
    public interface IFreezable
    {
        void Freeze();
        void Unfreeze();
    }
    #endregion
}
