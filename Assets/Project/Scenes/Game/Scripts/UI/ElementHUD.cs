using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace FiveElements
{
    // ══════════════════════════════════════════════════════════════
    //  ElementHUD  —  5 element slots arranged in a row/wheel
    // ══════════════════════════════════════════════════════════════
    public class ElementHUD : MonoBehaviour
    {
        [System.Serializable]
        private class ElementSlot
        {
            public ElementType element;
            public Image iconImage;         // the element icon
            public Image backgroundImage;   // the slot background
            public Image lockOverlay;       // shown when not absorbed yet
            public GameObject activeIndicator;   // glow ring / border
            public TextMeshProUGUI hotkeyLabel;    // "1" through "5"
        }

        [Header("Slots (configure one per element in order)")]
        [SerializeField] private List<ElementSlot> slots = new();

        [Header("Colours")]
        [SerializeField] private Color activeSlotColor = Color.white;
        [SerializeField] private Color inactiveSlotColor = new Color(1f, 1f, 1f, 0.4f);
        [SerializeField] private Color lockedSlotColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);

        [Header("Animation")]
        [SerializeField] private float punchScale = 1.25f;
        [SerializeField] private float punchDuration = 0.15f;

        [SerializeField] private ElementRegistry registry;

        // ──────────────────────────────────────────────────────────
        private void Start()
        {
            if (ElementManager.Instance != null)
            {
                ElementManager.Instance.OnElementAbsorbed += HandleAbsorbed;
                ElementManager.Instance.OnActiveElementChanged += HandleActiveChanged;
                ElementManager.Instance.OnElementLost += HandleLost;
            }

            InitialiseSlots();
        }

        private void OnDestroy()
        {
            if (ElementManager.Instance != null)
            {
                ElementManager.Instance.OnElementAbsorbed -= HandleAbsorbed;
                ElementManager.Instance.OnActiveElementChanged -= HandleActiveChanged;
                ElementManager.Instance.OnElementLost -= HandleLost;
            }
        }

        private void InitialiseSlots()
        {
            foreach (var slot in slots)
            {
                bool absorbed = ElementManager.Instance != null &&
                                ElementManager.Instance.HasElement(slot.element);
                SetSlotAbsorbed(slot, absorbed);

                // Set icon from registry
                if (registry != null)
                {
                    var data = registry.Get(slot.element);
                    if (data?.icon != null && slot.iconImage != null)
                        slot.iconImage.sprite = data.icon;
                }

                bool isActive = ElementManager.Instance != null &&
                                ElementManager.Instance.ActiveElement == slot.element;
                SetSlotActive(slot, isActive);
            }
        }

        // ── Event Handlers ────────────────────────────────────────
        private void HandleAbsorbed(ElementType element)
        {
            var slot = GetSlot(element);
            if (slot == null) return;
            SetSlotAbsorbed(slot, true);
            StartCoroutine(PunchScale(slot.backgroundImage?.transform));
        }

        private void HandleActiveChanged(ElementType element)
        {
            foreach (var slot in slots)
                SetSlotActive(slot, slot.element == element);
        }

        private void HandleLost(ElementType element)
        {
            var slot = GetSlot(element);
            if (slot != null) SetSlotAbsorbed(slot, false);
        }

        // ── Helpers ───────────────────────────────────────────────
        private ElementSlot GetSlot(ElementType element) =>
            slots.Find(s => s.element == element);

        private void SetSlotAbsorbed(ElementSlot slot, bool absorbed)
        {
            if (slot.lockOverlay != null) slot.lockOverlay.gameObject.SetActive(!absorbed);
            if (slot.iconImage != null) slot.iconImage.color = absorbed ? Color.white : lockedSlotColor;
        }

        private void SetSlotActive(ElementSlot slot, bool active)
        {
            if (slot.activeIndicator != null)
                slot.activeIndicator.SetActive(active);

            if (slot.backgroundImage != null)
                slot.backgroundImage.color = active ? activeSlotColor : inactiveSlotColor;
        }

        private IEnumerator PunchScale(Transform t)
        {
            if (t == null) yield break;
            Vector3 original = t.localScale;
            t.localScale = original * punchScale;
            float elapsed = 0f;
            while (elapsed < punchDuration)
            {
                elapsed += Time.deltaTime;
                t.localScale = Vector3.Lerp(original * punchScale, original, elapsed / punchDuration);
                yield return null;
            }
            t.localScale = original;
        }
    }
}
