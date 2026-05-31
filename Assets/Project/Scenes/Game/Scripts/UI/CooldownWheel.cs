using UnityEngine;
using UnityEngine.UI;

namespace FiveElements
{

    // ??????????????????????????????????????????????????????????????
    //  CooldownWheel  —  radial Image.fillAmount overlay
    // ??????????????????????????????????????????????????????????????
    public class CooldownWheel : MonoBehaviour
    {
        [SerializeField] private Image cooldownOverlay;  // radial fill type, fill origin = top

        private AbilityBase _trackedAbility;

        private void Start()
        {
            // Listen for active element changes to swap tracked ability
            if (ElementManager.Instance != null)
                ElementManager.Instance.OnActiveElementChanged += OnActiveElementChanged;

            if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
        }

        private void OnDestroy()
        {
            if (ElementManager.Instance != null)
                ElementManager.Instance.OnActiveElementChanged -= OnActiveElementChanged;
        }

        private void Update()
        {
            if (_trackedAbility == null || cooldownOverlay == null) return;

            if (_trackedAbility.IsOnCooldown)
            {
                float pct = _trackedAbility.CooldownRemaining / _trackedAbility.CooldownDuration;
                cooldownOverlay.fillAmount = pct;
            }
            else
            {
                cooldownOverlay.fillAmount = 0f;
            }
        }

        private void OnActiveElementChanged(ElementType element)
        {
            // Find the matching ability component on the player
            var player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;

            foreach (var ability in player.GetComponents<AbilityBase>())
                if (ability.ElementType == element) { _trackedAbility = ability; return; }

            _trackedAbility = null;
        }
    }
}
