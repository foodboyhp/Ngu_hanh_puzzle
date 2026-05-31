// ============================================================
//  AbilityBase.cs
//  Abstract base class for all elemental abilities.
//  Each concrete ability (Water, Wood, Fire, etc.) inherits
//  this and overrides the abstract methods.
//
//  Attach pattern: All 5 ability components sit on the
//  Player GameObject. PlayerController activates/deactivates
//  them based on the active element.
//
//  Place in: Assets/Scripts/Abilities/
// ============================================================

using System.Collections;
using UnityEngine;

namespace FiveElements
{
    public abstract class AbilityBase : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────
        [Header("Cooldown")]
        [Tooltip("Seconds between uses. 0 = no cooldown.")]
        [SerializeField] protected float cooldownDuration = 2f;

        [Header("Energy Cost")]
        [Tooltip("How much energy each use costs. 0 = free.")]
        [SerializeField] protected float energyCost = 10f;

        [Header("Optional References")]
        [SerializeField] protected ParticleSystem abilityParticles;

        protected AudioSource audioSource;

        // ── State ─────────────────────────────────────────────────
        protected bool _isOnCooldown = false;
        protected bool _isActive = false;  // for toggle-style abilities
        protected bool _isUnlocked = false;

        // ── Properties ────────────────────────────────────────────
        public abstract ElementType ElementType { get; }
        public bool IsOnCooldown => _isOnCooldown;
        public bool IsActive => _isActive;
        public bool IsUnlocked => _isUnlocked;
        public float CooldownDuration => cooldownDuration;

        /// <summary>Remaining cooldown time (0–cooldownDuration).</summary>
        public float CooldownRemaining { get; private set; } = 0f;

        // ── Events ────────────────────────────────────────────────
        /// <summary>Fired when the ability successfully executes.</summary>
        public System.Action<AbilityBase> OnAbilityUsed;

        /// <summary>Fired when cooldown finishes.</summary>
        public System.Action<AbilityBase> OnCooldownComplete;

        // ──────────────────────────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────────────────────────
        protected virtual void Awake()
        {
            // Auto-find AudioSource if not assigned
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        protected virtual void OnEnable()
        {
            // Subscribe to ElementManager events when the player object is active
            if (ElementManager.Instance != null)
                ElementManager.Instance.OnElementAbsorbed += HandleElementAbsorbed;
        }

        protected virtual void OnDisable()
        {
            if (ElementManager.Instance != null)
                ElementManager.Instance.OnElementAbsorbed -= HandleElementAbsorbed;

            // If this ability was toggled on, clean it up
            if (_isActive) Deactivate();
        }

        // ──────────────────────────────────────────────────────────
        // Public API — called by PlayerController
        // ──────────────────────────────────────────────────────────

        /// <summary>
        /// Attempt to use this ability. Checks unlock, cooldown, and energy.
        /// Returns true if execution was attempted.
        /// </summary>
        public bool TryUse(PlayerController player)
        {
            if (!_isUnlocked)
            {
                Debug.Log($"[{GetType().Name}] Not unlocked yet.");
                return false;
            }

            if (_isOnCooldown)
            {
                Debug.Log($"[{GetType().Name}] On cooldown ({CooldownRemaining:F1}s remaining).");
                return false;
            }

            if (!HasEnoughEnergy(player))
            {
                Debug.Log($"[{GetType().Name}] Not enough energy.");
                return false;
            }

            // Deduct energy
            player.UseEnergy(energyCost);

            // Execute the ability-specific logic
            Execute(player);

            // Start cooldown (if any)
            if (cooldownDuration > 0f)
                StartCoroutine(CooldownRoutine());

            OnAbilityUsed?.Invoke(this);
            return true;
        }

        /// <summary>
        /// Called when this element becomes the active element.
        /// Use to show visual indicators, stance changes, etc.
        /// </summary>
        public virtual void Activate()
        {
            _isActive = true;
            if (abilityParticles != null && !abilityParticles.isPlaying)
                abilityParticles.Play();
            OnActivate();
        }

        /// <summary>
        /// Called when the player switches away from this element.
        /// Clean up any persistent effects here.
        /// </summary>
        public virtual void Deactivate()
        {
            _isActive = false;
            if (abilityParticles != null && abilityParticles.isPlaying)
                abilityParticles.Stop();
            OnDeactivate();
        }

        // ──────────────────────────────────────────────────────────
        // Abstract / Virtual — subclasses implement these
        // ──────────────────────────────────────────────────────────

        /// <summary>Core ability logic. Called after all checks pass.</summary>
        protected abstract void Execute(PlayerController player);

        /// <summary>Called when this element becomes active (stance enter).</summary>
        protected virtual void OnActivate() { }

        /// <summary>Called when this element becomes inactive (stance exit).</summary>
        protected virtual void OnDeactivate() { }

        /// <summary>
        /// Override to add element-specific unlock requirements.
        /// Default: unlocks when the element is absorbed.
        /// </summary>
        protected virtual bool MeetsUnlockRequirements() => true;

        // ──────────────────────────────────────────────────────────
        // Private Helpers
        // ──────────────────────────────────────────────────────────
        private void HandleElementAbsorbed(ElementType absorbed)
        {
            if (absorbed == ElementType && MeetsUnlockRequirements())
            {
                _isUnlocked = true;
                Debug.Log($"[{GetType().Name}] Unlocked!");
                OnUnlocked();
            }
        }

        /// <summary>Override to add a celebration effect when first unlocked.</summary>
        protected virtual void OnUnlocked() { }

        private bool HasEnoughEnergy(PlayerController player) =>
            energyCost <= 0f || player.CurrentEnergy >= energyCost;

        private IEnumerator CooldownRoutine()
        {
            _isOnCooldown = true;
            CooldownRemaining = cooldownDuration;

            while (CooldownRemaining > 0f)
            {
                CooldownRemaining -= Time.deltaTime;
                yield return null;
            }

            CooldownRemaining = 0f;
            _isOnCooldown = false;
            OnCooldownComplete?.Invoke(this);
        }

        /// <summary>
        /// Utility: Play an AudioClip on the ability's AudioSource (one-shot).
        /// </summary>
        protected void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }

        /// <summary>
        /// Utility: Spawn a prefab at a world position with no parent.
        /// Automatically destroys it after 'lifetime' seconds.
        /// </summary>
        protected GameObject SpawnEffect(GameObject prefab, Vector3 position,
                                          Quaternion rotation, float lifetime = 3f)
        {
            if (prefab == null) return null;
            var go = Instantiate(prefab, position, rotation);
            if (lifetime > 0f) Destroy(go, lifetime);
            return go;
        }
    }
}