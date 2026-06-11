// ============================================================
//  EnemyBase.cs / ElementalEnemy.cs
//  Base enemy class with patrol/chase/attack FSM.
//  ElementalEnemy extends it with elemental weaknesses,
//  resistances, and death drops.
//
//  Place in: Assets/Scripts/Enemy/
// ============================================================

using System.Collections;
using UnityEngine;

namespace FiveElements
{
    // ══════════════════════════════════════════════════════════════
    //  EnemyBase
    // ══════════════════════════════════════════════════════════════
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class EnemyBase : MonoBehaviour, IStunnable
    {
        // ── Inspector ─────────────────────────────────────────────
        [Header("Stats")]
        [SerializeField] protected float maxHealth = 30f;
        [SerializeField] protected float moveSpeed = 2.5f;
        [SerializeField] protected float chaseSpeed = 4f;
        [SerializeField] protected float attackDamage = 10f;
        [SerializeField] protected float attackCooldown = 1.5f;
        [SerializeField] protected float attackRange = 1f;

        [Header("Detection")]
        [SerializeField] protected float detectionRange = 6f;
        [SerializeField] protected float loseAggroRange = 10f;
        [SerializeField] protected LayerMask playerLayer;
        [SerializeField] protected LayerMask obstacleLayer;

        [Header("Patrol")]
        [SerializeField] protected Transform patrolPointA;
        [SerializeField] protected Transform patrolPointB;

        [Header("Drop")]
        [SerializeField] protected GameObject deathDropPrefab;
        [SerializeField][Range(0f, 1f)] protected float dropChance = 0.4f;

        [Header("Visuals")]
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected Animator animator;

        [Header("Audio")]
        [SerializeField] protected AudioClip hurtSound;
        [SerializeField] protected AudioClip deathSound;
        [SerializeField] protected AudioClip attackSound;

        // ── State Machine ─────────────────────────────────────────
        protected enum EnemyState { Idle, Patrol, Chase, Attack, Stunned, Dead }
        protected EnemyState _state = EnemyState.Patrol;

        // ── Runtime ───────────────────────────────────────────────
        protected float _currentHealth;
        protected Rigidbody2D _rb;
        protected Transform _player;
        protected bool _isAttackOnCooldown = false;
        protected bool _facingRight = true;
        protected int _patrolTarget = 0;   // 0 = A, 1 = B

        // ──────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ──────────────────────────────────────────────────────────
        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _currentHealth = maxHealth;
        }

        protected virtual void Start()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _player = playerObj.transform;
        }

        protected virtual void Update()
        {
            if (_state == EnemyState.Dead) return;
            if (_state == EnemyState.Stunned) return;

            UpdateStateMachine();
            UpdateAnimator();
        }

        // ──────────────────────────────────────────────────────────
        // State Machine
        // ──────────────────────────────────────────────────────────
        protected virtual void UpdateStateMachine()
        {
            float distToPlayer = _player != null
                ? Vector2.Distance(transform.position, _player.position)
                : float.MaxValue;

            switch (_state)
            {
                case EnemyState.Idle:
                    HandleIdle();
                    if (distToPlayer <= detectionRange) TransitionTo(EnemyState.Chase);
                    break;

                case EnemyState.Patrol:
                    HandlePatrol();
                    if (distToPlayer <= detectionRange) TransitionTo(EnemyState.Chase);
                    break;

                case EnemyState.Chase:
                    HandleChase();
                    if (distToPlayer <= attackRange) TransitionTo(EnemyState.Attack);
                    if (distToPlayer >= loseAggroRange) TransitionTo(EnemyState.Patrol);
                    break;

                case EnemyState.Attack:
                    HandleAttack();
                    if (distToPlayer > attackRange * 1.5f) TransitionTo(EnemyState.Chase);
                    break;
            }
        }

        protected virtual void HandleIdle() { }

        protected virtual void HandlePatrol()
        {
            if (patrolPointA == null || patrolPointB == null) return;

            Transform target = _patrolTarget == 0 ? patrolPointA : patrolPointB;
            MoveToward(target.position, moveSpeed);

            if (Vector2.Distance(transform.position, target.position) < 0.2f)
                _patrolTarget = 1 - _patrolTarget;
        }

        protected virtual void HandleChase()
        {
            if (_player == null) return;
            MoveToward(_player.position, chaseSpeed);
        }

        protected virtual void HandleAttack()
        {
            if (_isAttackOnCooldown) return;
            StartCoroutine(AttackRoutine());
        }

        protected virtual IEnumerator AttackRoutine()
        {
            _isAttackOnCooldown = true;
            animator?.SetTrigger("Attack");
            PlaySound(attackSound);

            yield return new WaitForSeconds(0.3f); // wind-up

            // Damage player if still in range
            if (_player != null &&
                Vector2.Distance(transform.position, _player.position) <= attackRange)
            {
                var player = _player.GetComponent<PlayerController>();
                player?.TakeDamage(attackDamage, gameObject);
            }

            yield return new WaitForSeconds(attackCooldown);
            _isAttackOnCooldown = false;
        }

        protected virtual void TransitionTo(EnemyState newState)
        {
            if (_state == newState) return;
            _state = newState;
            OnStateEnter(newState);
        }

        protected virtual void OnStateEnter(EnemyState state) { }

        // ──────────────────────────────────────────────────────────
        // Damage & Death
        // ──────────────────────────────────────────────────────────
        public virtual void TakeDamage(float amount, ElementType sourceElement = ElementType.None)
        {
            if (_state == EnemyState.Dead) return;

            float finalDamage = ModifyDamage(amount, sourceElement);
            _currentHealth -= finalDamage;

            ShowDamageNumber(finalDamage);
            PlaySound(hurtSound);
            animator?.SetTrigger("Hurt");

            if (_currentHealth <= 0f) Die();
        }

        /// <summary>Override in subclasses to apply elemental modifiers.</summary>
        protected virtual float ModifyDamage(float raw, ElementType source) => raw;

        protected virtual void Die()
        {
            _state = EnemyState.Dead;
            _rb.linearVelocity = Vector2.zero;
            _rb.gravityScale = 0f;

            GetComponent<Collider2D>().enabled = false;

            animator?.SetTrigger("Die");
            PlaySound(deathSound);

            // Random loot drop
            if (deathDropPrefab != null && Random.value <= dropChance)
                Instantiate(deathDropPrefab, transform.position, Quaternion.identity);

            Destroy(gameObject, 2f);
            OnDeath();
        }

        protected virtual void OnDeath() { }

        // ──────────────────────────────────────────────────────────
        // IStunnable
        // ──────────────────────────────────────────────────────────
        public void Stun()
        {
            if (_state == EnemyState.Dead) return;
            _state = EnemyState.Stunned;
            _rb.linearVelocity = Vector2.zero;
            animator?.SetBool("IsStunned", true);
        }

        public void Recover()
        {
            if (_state == EnemyState.Dead) return;
            _state = EnemyState.Patrol;
            animator?.SetBool("IsStunned", false);
        }

        // ──────────────────────────────────────────────────────────
        // IBurnable
        // ──────────────────────────────────────────────────────────
        public virtual void Ignite()
        {
            StartCoroutine(BurnRoutine());
        }

        private IEnumerator BurnRoutine()
        {
            float burnTime = 3f;
            float burnDamage = 2f;
            float elapsed = 0f;
            while (elapsed < burnTime && _state != EnemyState.Dead)
            {
                TakeDamage(burnDamage);
                elapsed += 0.5f;
                yield return new WaitForSeconds(0.5f);
            }
        }

        // ──────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────
        protected void MoveToward(Vector3 target, float speed)
        {
            Vector2 dir = (target - transform.position).normalized;
            _rb.linearVelocity = new Vector2(dir.x * speed, _rb.linearVelocity.y);

            // Flip sprite
            if (dir.x > 0.01f && !_facingRight) Flip();
            else if (dir.x < -0.01f && _facingRight) Flip();
        }

        protected void Flip()
        {
            _facingRight = !_facingRight;
            if (spriteRenderer != null) spriteRenderer.flipX = !spriteRenderer.flipX;
        }

        protected void PlaySound(AudioClip clip)
        {
            if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position);
        }

        private void ShowDamageNumber(float amount)
        {
            // Hook into a DamageNumberPool if you have one
            Debug.Log($"[{gameObject.name}] -{amount:F0} HP");
        }

        protected virtual void UpdateAnimator()
        {
            if (animator == null) return;
            animator.SetFloat("Speed", Mathf.Abs(_rb.linearVelocity.x));
            animator.SetBool("IsChasing", _state == EnemyState.Chase);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}