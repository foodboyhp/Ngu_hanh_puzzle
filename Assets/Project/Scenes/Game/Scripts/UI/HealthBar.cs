using UnityEngine;
using UnityEngine.UI;

namespace FiveElements
{
    // ══════════════════════════════════════════════════════════════
    //  HealthBar
    // ══════════════════════════════════════════════════════════════
    public class HealthBar : MonoBehaviour
    {
        [Header("Bar Images")]
        [SerializeField] private Image fillImage;         // actual HP
        [SerializeField] private Image ghostFillImage;    // trailing "damage ghost"

        [Header("Settings")]
        [SerializeField] private float ghostDelay = 0.5f;  // seconds before ghost starts draining
        [SerializeField] private float ghostDrainSpeed = 1.5f;

        [Header("Color Thresholds")]
        [SerializeField] private Color fullColor = Color.green;
        [SerializeField] private Color halfColor = Color.yellow;
        [SerializeField] private Color lowColor = Color.red;

        private float _ghostFill = 1f;
        private float _ghostTimer = 0f;
        private float _currentFill = 1f;

        private void Start()
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.OnHealthChanged += UpdateHealth;
        }

        private void Update()
        {
            // Drain ghost bar
            if (_ghostFill > _currentFill)
            {
                _ghostTimer -= Time.deltaTime;
                if (_ghostTimer <= 0f)
                {
                    _ghostFill = Mathf.MoveTowards(
                        _ghostFill, _currentFill, ghostDrainSpeed * Time.deltaTime);
                    if (ghostFillImage != null) ghostFillImage.fillAmount = _ghostFill;
                }
            }
        }

        private void UpdateHealth(float current, float max)
        {
            float pct = max > 0 ? current / max : 0f;

            // Snap fill bar
            _currentFill = pct;
            if (fillImage != null) fillImage.fillAmount = pct;

            // Reset ghost timer so it waits before draining
            _ghostTimer = ghostDelay;

            // Color gradient
            if (fillImage != null)
            {
                if (pct > 0.5f)
                    fillImage.color = Color.Lerp(halfColor, fullColor, (pct - 0.5f) * 2f);
                else
                    fillImage.color = Color.Lerp(lowColor, halfColor, pct * 2f);
            }
        }
    }
}
