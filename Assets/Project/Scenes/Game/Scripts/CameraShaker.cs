using System.Collections;
using UnityEngine;

namespace FiveElements
{

    // ══════════════════════════════════════════════════════════════
    //  CameraShaker
    // ══════════════════════════════════════════════════════════════
    /// <summary>
    /// Attach to the Camera GameObject alongside CameraController.
    /// EarthAbility calls the static ShakeIfPresent() helper.
    /// </summary>
    public class CameraShaker : MonoBehaviour
    {
        private static CameraShaker _instance;

        [Header("Defaults")]
        [SerializeField] private float defaultDuration = 0.25f;
        [SerializeField] private float defaultMagnitude = 0.18f;

        private void Awake() => _instance = this;

        // ── Static convenience ────────────────────────────────────
        public static void ShakeIfPresent(float duration = -1f, float magnitude = -1f)
        {
            if (_instance == null) return;
            _instance.Shake(
                duration < 0 ? _instance.defaultDuration : duration,
                magnitude < 0 ? _instance.defaultMagnitude : magnitude);
        }

        // ── Public ────────────────────────────────────────────────
        public void Shake(float duration, float magnitude)
        {
            StopAllCoroutines();
            StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        // ── Private ───────────────────────────────────────────────
        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            Vector3 origin = transform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float dampened = Mathf.Lerp(magnitude, 0f, progress); // fade out

                float offsetX = Random.Range(-1f, 1f) * dampened;
                float offsetY = Random.Range(-1f, 1f) * dampened;
                transform.localPosition = origin + new Vector3(offsetX, offsetY, 0f);

                yield return null;
            }

            transform.localPosition = origin;
        }
    }

}
