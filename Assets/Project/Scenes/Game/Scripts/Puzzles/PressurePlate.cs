using UnityEngine;

namespace FiveElements
{

    /// <summary>
    /// A pressure plate activated by the Earth tremor or a heavy object.
    /// </summary>
    public class PressurePlate : PuzzleObject
    {
        [Header("Pressure Plate")]
        [SerializeField] private float pressDepth = 0.1f;
        [SerializeField] private AudioClip pressSound;
        [SerializeField] private AudioClip releaseSound;

        private Vector3 _restPosition;

        protected override void Start()
        {
            base.Start();
            _restPosition = transform.localPosition;
        }

        protected override void OnActivate(ElementType element)
        {
            transform.localPosition = _restPosition - new Vector3(0, pressDepth, 0);
            if (pressSound != null) AudioSource.PlayClipAtPoint(pressSound, transform.position);
        }

        protected override void OnDeactivate()
        {
            transform.localPosition = _restPosition;
            if (releaseSound != null) AudioSource.PlayClipAtPoint(releaseSound, transform.position);
        }

        // Physical objects can also step on this
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.CompareTag("HeavyObject"))
                ForceActivate();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.CompareTag("HeavyObject"))
                Reset();
        }
    }
}