using System.Collections;
using TMPro;
using UnityEngine;
namespace FiveElements
{
    // ══════════════════════════════════════════════════════════════
    //  ComboFeedbackUI  —  banner that fades in/out on combo
    // ══════════════════════════════════════════════════════════════
    public class ComboFeedbackUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI comboNameText;
        [SerializeField] private TextMeshProUGUI comboDescText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float displayDuration = 2.5f;
        [SerializeField] private float fadeDuration = 0.3f;

        private Coroutine _displayRoutine;

        public void ShowCombo(ComboEffect combo)
        {
            if (combo == null) return;
            if (_displayRoutine != null) StopCoroutine(_displayRoutine);
            _displayRoutine = StartCoroutine(DisplayRoutine(combo));
        }

        private IEnumerator DisplayRoutine(ComboEffect combo)
        {
            if (comboNameText != null) comboNameText.text = combo.Name;
            if (comboDescText != null) comboDescText.text = combo.Description;

            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                if (canvasGroup != null) canvasGroup.alpha = elapsed / fadeDuration;
                yield return null;
            }
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            yield return new WaitForSeconds(displayDuration);

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                if (canvasGroup != null) canvasGroup.alpha = 1f - elapsed / fadeDuration;
                yield return null;
            }
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }
    }
}