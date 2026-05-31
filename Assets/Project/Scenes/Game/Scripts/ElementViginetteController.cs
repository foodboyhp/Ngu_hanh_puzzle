using System.Collections;
using UnityEngine;

namespace FiveElements
{
    // ══════════════════════════════════════════════════════════════
    //  ElementVignetteController
    // ══════════════════════════════════════════════════════════════
    /// <summary>
    /// Controls a full-screen UI vignette Image that tints to the
    /// active element's color. Place an Image component
    /// stretched over the full canvas and assign it here.
    /// </summary>
    public class ElementVignetteController : MonoBehaviour
    {
        private static ElementVignetteController _instance;

        [SerializeField] private UnityEngine.UI.Image vignetteImage;
        [SerializeField] private ElementRegistry registry;
        [SerializeField] private float fadeDuration = 0.4f;
        [SerializeField][Range(0f, 1f)] private float vignetteAlpha = 0.15f;

        private Coroutine _fadeRoutine;

        private void Awake() => _instance = this;

        public static void SetElement(ElementType element)
        {
            _instance?.ApplyElement(element);
        }

        private void ApplyElement(ElementType element)
        {
            if (vignetteImage == null || registry == null) return;

            var data = registry.Get(element);
            if (data == null) return;

            Color target = data.primaryColor;
            target.a = vignetteAlpha;

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeToColor(target));
        }

        private IEnumerator FadeToColor(Color target)
        {
            Color start = vignetteImage.color;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                vignetteImage.color = Color.Lerp(start, target, elapsed / fadeDuration);
                yield return null;
            }
            vignetteImage.color = target;
        }
    }
}
