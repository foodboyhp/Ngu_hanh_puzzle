using UnityEngine;
using UnityEngine.UI;
namespace FiveElements
{
    // ══════════════════════════════════════════════════════════════
    //  EnergyBar
    // ══════════════════════════════════════════════════════════════
    public class EnergyBar : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.7f, 1f);
        [SerializeField] private Color emptyColor = new Color(0.5f, 0.1f, 0.1f);
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseAmount = 0.05f;

        private float _fillPct = 1f;
        private bool _isPulsing = false;

        private void Start()
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.OnEnergyChanged += UpdateEnergy;
        }

        private void Update()
        {
            if (_isPulsing && fillImage != null)
            {
                float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                fillImage.fillAmount = Mathf.Clamp01(_fillPct + pulse);
            }
        }

        private void UpdateEnergy(float current, float max)
        {
            _fillPct = max > 0 ? current / max : 0f;
            if (fillImage != null)
            {
                fillImage.fillAmount = _fillPct;
                fillImage.color = Color.Lerp(emptyColor, normalColor, _fillPct);
            }
            _isPulsing = _fillPct < 0.2f;
        }
    }
}
