using System.Collections;
using UnityEngine;

namespace FiveElements
{
    // ══════════════════════════════════════════════════════════════
    #region THỔ — Earth Ability
    // ══════════════════════════════════════════════════════════════
    /// <summary>
    /// EARTH (Thổ 土) — Weight & Stability
    ///
    /// Primary:   Tremor — slam the ground, stunning nearby enemies
    ///            and triggering pressure plates.
    /// Secondary: Raise Wall — erect a temporary stone barrier at
    ///            aim position.
    /// Passive:   Reduced knockback; can push heavy boulders.
    /// </summary>
    public class EarthAbility : AbilityBase
    {
        [Header("Tremor")]
        [SerializeField] private float tremorRadius = 3f;
        [SerializeField] private float tremorStunTime = 2f;
        [SerializeField] private LayerMask tremorLayerMask;
        [SerializeField] private GameObject tremorVFXPrefab;
        [SerializeField] private AudioClip tremorSound;

        [Header("Stone Wall")]
        [SerializeField] private GameObject stoneWallPrefab;
        [SerializeField] private float wallLifetime = 6f;
        [SerializeField] private AudioClip wallSound;

        public override ElementType ElementType => ElementType.Earth;

        private int _useCount = 0;

        protected override void Execute(PlayerController player)
        {
            _useCount++;
            if (_useCount % 2 == 1)
                DoTremor(player);
            else
                RaiseWall(player);
        }

        private void DoTremor(PlayerController player)
        {
            Vector3 pos = player.transform.position;
            var hits = Physics2D.OverlapCircleAll(pos, tremorRadius, tremorLayerMask);

            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player")) continue;

                // Stun enemies
                var stunnable = hit.GetComponent<IStunnable>();
                if (stunnable != null)
                    StartCoroutine(StunRoutine(stunnable));

                // Trigger pressure-plate-style puzzle objects
                var puzzleObj = hit.GetComponent<PuzzleObject>();
                puzzleObj?.OnElementApplied(ElementType.Earth);
            }

            // Camera shake via a separate CameraShaker component
            CameraShaker.ShakeIfPresent(0.3f, 0.2f);

            SpawnEffect(tremorVFXPrefab, pos, Quaternion.identity, 1.5f);
            PlaySound(tremorSound);
        }

        private IEnumerator StunRoutine(IStunnable target)
        {
            target.Stun();
            yield return new WaitForSeconds(tremorStunTime);
            target.Recover();
        }

        private void RaiseWall(PlayerController player)
        {
            Vector2 aim = player.AimPosition;
            SpawnEffect(stoneWallPrefab, aim, Quaternion.identity, wallLifetime);
            PlaySound(wallSound);
        }
    }

    public interface IStunnable
    {
        void Stun();
        void Recover();
    }
    #endregion
}