// ============================================================
//  PlayerController.cs
//  Handles all player movement, jumping, ability input dispatch,
//  health, energy, and coordinates with ElementManager to
//  activate the correct AbilityBase component.
//
//  Place in: Assets/Scripts/Player/
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;   // Requires "Input System" package

namespace FiveElements
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float jumpForce = 14f;
        [SerializeField] private float fallMultiplier = 2.5f;   // snappier fall
        [SerializeField] private float lowJumpMultiplier = 2f;    // hold for higher jump
        [SerializeField] private float coyoteTime = 0.12f;  // seconds after edge to still jump
        [SerializeField] private float jumpBufferTime = 0.1f;   // press jump slightly before landing

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Health & Energy")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float energyRegen = 5f;   // per second

        [Header("Invincibility Frames")]
        [SerializeField] private float iFrameDuration = 0.8f;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;

        // ── Private Components ────────────────────────────────────
        private Rigidbody2D _rb;
        private bool _isGrounded;
        private bool _isDashing;   // set by FireAbility

        // ── Input state ───────────────────────────────────────────
        private Vector2 _moveInput;
        private bool _jumpPressed;
        private bool _jumpHeld;
        private bool _abilityPressed;

        // ── Coyote / Jump Buffer ──────────────────────────────────
        private float _coyoteTimer;
        private float _jumpBufferTimer;

        // ── Health / Energy ───────────────────────────────────────
        private float _currentHealth;
        private float _currentEnergy;
        private bool _isInvincible;

        // ── Abilities ─────────────────────────────────────────────
        // All ability components on this GameObject, keyed by element
        private Dictionary<ElementType, AbilityBase> _abilities;
        private AbilityBase _activeAbility;

        // ── Aiming ────────────────────────────────────────────────
        private Vector2 _aimPosition;   // world-space aim (mouse or right stick)
        private Vector2 _facingDirection = Vector2.right;

        // ── Properties ────────────────────────────────────────────
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => maxHealth;
        public float CurrentEnergy => _currentEnergy;
        public float MaxEnergy => maxEnergy;
        public bool IsGrounded => _isGrounded;
        public bool IsDashing => _isDashing;
        public Vector2 FacingDirection => _facingDirection;
        public Vector2 AimPosition => _aimPosition;

        // ── Events ────────────────────────────────────────────────
        public System.Action<float, float> OnHealthChanged;   // (current, max)
        public System.Action<float, float> OnEnergyChanged;   // (current, max)
        public System.Action OnDeath;

        // ──────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ──────────────────────────────────────────────────────────
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _currentHealth = maxHealth;
            _currentEnergy = maxEnergy;

            BuildAbilityDictionary();
        }

        private void OnEnable()
        {
            if (ElementManager.Instance != null)
            {
                ElementManager.Instance.OnActiveElementChanged += HandleActiveElementChanged;
                ElementManager.Instance.OnElementAbsorbed += HandleElementAbsorbed;
            }
        }

        private void OnDisable()
        {
            if (ElementManager.Instance != null)
            {
                ElementManager.Instance.OnActiveElementChanged -= HandleActiveElementChanged;
                ElementManager.Instance.OnElementAbsorbed -= HandleElementAbsorbed;
            }
        }

        private void Update()
        {
            GatherInput();
            UpdateGroundState();
            UpdateTimers();
            HandleJump();
            HandleElementSwitch();
            HandleAbilityInput();
            UpdateAimPosition();
            UpdateFacing();
            RegenerateEnergy();
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            if (!_isDashing)
                ApplyMovement();
            ApplyBetterGravity();
        }

        // ──────────────────────────────────────────────────────────
        // Input (New Input System — also works with legacy via GetAxis)
        // ──────────────────────────────────────────────────────────
        private void GatherInput()
        {
            _moveInput = new Vector2(Input.GetAxisRaw("Horizontal"),
                                          Input.GetAxisRaw("Vertical"));

            _jumpHeld = Input.GetButton("Jump");

            if (Input.GetButtonDown("Jump"))
                _jumpBufferTimer = jumpBufferTime;

            // Ability: Fire1 (left mouse / gamepad south)
            _abilityPressed = Input.GetButtonDown("Fire1");
        }

        // ──────────────────────────────────────────────────────────
        // Movement
        // ──────────────────────────────────────────────────────────
        private void ApplyMovement()
        {
            _rb.linearVelocity = new Vector2(_moveInput.x * moveSpeed, _rb.linearVelocity.y);
        }

        private void ApplyBetterGravity()
        {
            // Faster falling, variable jump height
            if (_rb.linearVelocity.y < 0)
                _rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            else if (_rb.linearVelocity.y > 0 && !_jumpHeld)
                _rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }

        // ──────────────────────────────────────────────────────────
        // Jump
        // ──────────────────────────────────────────────────────────
        private void HandleJump()
        {
            bool canJump = _isGrounded || _coyoteTimer > 0f;

            if (_jumpBufferTimer > 0f && canJump)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
                animator?.SetTrigger("Jump");
            }
        }

        // ──────────────────────────────────────────────────────────
        // Ground Check & Timers
        // ──────────────────────────────────────────────────────────
        private void UpdateGroundState()
        {
            bool wasGrounded = _isGrounded;
            _isGrounded = Physics2D.OverlapCircle(
                groundCheck.position, groundCheckRadius, groundLayer);

            if (wasGrounded && !_isGrounded)
                _coyoteTimer = coyoteTime;  // just left a ledge
        }

        private void UpdateTimers()
        {
            if (_coyoteTimer > 0f) _coyoteTimer -= Time.deltaTime;
            if (_jumpBufferTimer > 0f) _jumpBufferTimer -= Time.deltaTime;
        }

        // ──────────────────────────────────────────────────────────
        // Element Switching
        // ──────────────────────────────────────────────────────────
        private void HandleElementSwitch()
        {
            // Scroll wheel or Q/E to cycle
            float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
            if (scroll > 0f) ElementManager.Instance?.CycleNext();
            if (scroll < 0f) ElementManager.Instance?.CyclePrevious();

            if (Input.GetKeyDown(KeyCode.Q)) ElementManager.Instance?.CyclePrevious();
            if (Input.GetKeyDown(KeyCode.E)) ElementManager.Instance?.CycleNext();

            // Direct hotkeys 1–5 for each element
            if (Input.GetKeyDown(KeyCode.Alpha1)) TrySwitchToElement(ElementType.Water);
            if (Input.GetKeyDown(KeyCode.Alpha2)) TrySwitchToElement(ElementType.Wood);
            if (Input.GetKeyDown(KeyCode.Alpha3)) TrySwitchToElement(ElementType.Fire);
            if (Input.GetKeyDown(KeyCode.Alpha4)) TrySwitchToElement(ElementType.Earth);
            if (Input.GetKeyDown(KeyCode.Alpha5)) TrySwitchToElement(ElementType.Metal);
        }

        private void TrySwitchToElement(ElementType type)
        {
            if (ElementManager.Instance != null && ElementManager.Instance.HasElement(type))
                ElementManager.Instance.SetActiveElement(type);
        }

        // ──────────────────────────────────────────────────────────
        // Ability Input
        // ──────────────────────────────────────────────────────────
        private void HandleAbilityInput()
        {
            if (_activeAbility == null) return;

            if (_abilityPressed)
                _activeAbility.TryUse(this);

            // Right-click / Fire2 for secondary (e.g. Fire ignite)
            if (Input.GetButtonDown("Fire2"))
            {
                if (_activeAbility is FireAbility fireAbility)
                    fireAbility.Ignite(_aimPosition);
            }
        }

        // ──────────────────────────────────────────────────────────
        // Aim & Facing
        // ──────────────────────────────────────────────────────────
        private void UpdateAimPosition()
        {
            // Convert mouse screen position to world position
            if (Camera.main != null)
                _aimPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        private void UpdateFacing()
        {
            if (_moveInput.x > 0.01f)
            {
                _facingDirection = Vector2.right;
                if (spriteRenderer != null) spriteRenderer.flipX = false;
            }
            else if (_moveInput.x < -0.01f)
            {
                _facingDirection = Vector2.left;
                if (spriteRenderer != null) spriteRenderer.flipX = true;
            }
        }

        // ──────────────────────────────────────────────────────────
        // Health & Energy
        // ──────────────────────────────────────────────────────────
        public void TakeDamage(float amount, GameObject source = null)
        {
            if (_isInvincible) return;

            // Check Metal passive armor first
            if (_abilities.TryGetValue(ElementType.Metal, out var metalAbility) &&
                metalAbility is MetalAbility metal &&
                metal.TryAbsorbHit())
            {
                return;  // Hit absorbed
            }

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);

            if (_currentHealth <= 0f)
            {
                Die();
                return;
            }

            StartCoroutine(InvincibilityFrames());
            animator?.SetTrigger("Hurt");
        }

        public void Heal(float amount)
        {
            _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        public void UseEnergy(float amount)
        {
            _currentEnergy = Mathf.Max(0f, _currentEnergy - amount);
            OnEnergyChanged?.Invoke(_currentEnergy, maxEnergy);
        }

        public void RestoreEnergy(float amount)
        {
            _currentEnergy = Mathf.Min(maxEnergy, _currentEnergy + amount);
            OnEnergyChanged?.Invoke(_currentEnergy, maxEnergy);
        }

        private void RegenerateEnergy()
        {
            if (_currentEnergy < maxEnergy)
                RestoreEnergy(energyRegen * Time.deltaTime);
        }

        private void Die()
        {
            Debug.Log("[PlayerController] Player died.");
            animator?.SetTrigger("Die");
            OnDeath?.Invoke();
            // Actual death handling (respawn, game over) is done by LevelProgressionManager
        }

        private IEnumerator InvincibilityFrames()
        {
            _isInvincible = true;
            // Simple flicker effect
            for (float t = 0; t < iFrameDuration; t += 0.1f)
            {
                if (spriteRenderer) spriteRenderer.enabled = !spriteRenderer.enabled;
                yield return new WaitForSeconds(0.1f);
            }
            if (spriteRenderer) spriteRenderer.enabled = true;
            _isInvincible = false;
        }

        // ──────────────────────────────────────────────────────────
        // Public Setters (called by abilities)
        // ──────────────────────────────────────────────────────────
        public void SetDashing(bool dashing) => _isDashing = dashing;

        // ──────────────────────────────────────────────────────────
        // Animator Updates
        // ──────────────────────────────────────────────────────────
        private void UpdateAnimator()
        {
            if (animator == null) return;
            animator.SetFloat("Speed", Mathf.Abs(_moveInput.x));
            animator.SetBool("IsGrounded", _isGrounded);
            animator.SetFloat("VelocityY", _rb.linearVelocity.y);
            animator.SetBool("IsDashing", _isDashing);
        }

        // ──────────────────────────────────────────────────────────
        // Event Handlers
        // ──────────────────────────────────────────────────────────
        private void HandleActiveElementChanged(ElementType newElement)
        {
            // Deactivate old
            if (_activeAbility != null)
                _activeAbility.Deactivate();

            // Activate new
            if (_abilities.TryGetValue(newElement, out var next))
            {
                _activeAbility = next;
                _activeAbility.Activate();
                Debug.Log($"[PlayerController] Active ability → {newElement}");
            }
            else
            {
                _activeAbility = null;
            }
        }

        private void HandleElementAbsorbed(ElementType element)
        {
            // Force re-activate current (in case it's the first element)
            if (ElementManager.Instance.ActiveElement == element)
                HandleActiveElementChanged(element);
        }

        // ──────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────
        private void BuildAbilityDictionary()
        {
            _abilities = new Dictionary<ElementType, AbilityBase>();
            foreach (var ability in GetComponents<AbilityBase>())
            {
                if (!_abilities.ContainsKey(ability.ElementType))
                    _abilities[ability.ElementType] = ability;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }
        }
    }
}