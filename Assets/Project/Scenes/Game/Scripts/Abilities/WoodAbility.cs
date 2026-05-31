using System.Collections;
using UnityEngine;

namespace FiveElements
{
    // ══════════════════════════════════════════════════════════════
    #region MỘC — Wood Ability
    // ══════════════════════════════════════════════════════════════
    /// <summary>
    /// WOOD (Mộc 木) — Growth & Structure
    ///
    /// Primary:   Grow a vine from the ground at the cursor/aim
    ///            position. Vine acts as a climbable platform.
    /// Secondary: Entangle an enemy or object in roots (immobilise).
    /// Passive:   Player leaves a trail of flowers — cosmetic only.
    /// </summary>
    public class WoodAbility : AbilityBase
    {
        [Header("Vine Settings")]
        [SerializeField] private GameObject vinePlatformPrefab;   // a temporary platform prefab
        [SerializeField] private float vineLifetime = 5f;    // seconds before vine withers
        [SerializeField] private float vineMaxRange = 6f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private AudioClip growSound;

        [Header("Root Entangle")]
        [SerializeField] private float entangleRadius = 1.5f;
        [SerializeField] private float entangleDuration = 3f;
        [SerializeField] private GameObject rootVFXPrefab;
        [SerializeField] private AudioClip entangleSound;

        public override ElementType ElementType => ElementType.Wood;

        private int _useCount = 0; // alternates between vine and entangle

        protected override void Execute(PlayerController player)
        {
            _useCount++;
            if (_useCount % 2 == 1)
                GrowVine(player);
            else
                EntangleArea(player);
        }

        private void GrowVine(PlayerController player)
        {
            // Raycast downward from aim point to find ground
            Vector2 aimPos = player.AimPosition;
            RaycastHit2D hit = Physics2D.Raycast(aimPos + Vector2.up * 2f, Vector2.down, vineMaxRange, groundLayer);
            if (hit.collider == null)
            {
                Debug.Log("[WoodAbility] No ground found for vine.");
                return;
            }

            Vector3 spawnPos = hit.point;
            GameObject vine = SpawnEffect(vinePlatformPrefab, spawnPos, Quaternion.identity, vineLifetime);

            // Animate the vine growing upward via coroutine
            if (vine != null)
                StartCoroutine(GrowVineRoutine(vine));

            PlaySound(growSound);
        }

        private IEnumerator GrowVineRoutine(GameObject vine)
        {
            Vector3 finalScale = vine.transform.localScale;
            vine.transform.localScale = new Vector3(finalScale.x, 0f, finalScale.z);

            float elapsed = 0f;
            float growTime = 0.4f;

            while (elapsed < growTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / growTime;
                vine.transform.localScale = Vector3.Lerp(
                    new Vector3(finalScale.x, 0f, finalScale.z), finalScale, t);
                yield return null;
            }

            vine.transform.localScale = finalScale;
        }

        private void EntangleArea(PlayerController player)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                player.AimPosition, entangleRadius);

            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player")) continue;
                var entanglable = hit.GetComponent<IEntanglable>();
                if (entanglable != null)
                    StartCoroutine(EntangleRoutine(entanglable));
            }

            SpawnEffect(rootVFXPrefab, player.AimPosition, Quaternion.identity, 3f);
            PlaySound(entangleSound);
        }

        private IEnumerator EntangleRoutine(IEntanglable target)
        {
            target.Entangle();
            yield return new WaitForSeconds(entangleDuration);
            target.Release();
        }
    }

    public interface IEntanglable
    {
        void Entangle();
        void Release();
    }
    #endregion
}