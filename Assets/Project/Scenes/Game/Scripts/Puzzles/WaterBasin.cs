using UnityEngine;


namespace FiveElements
{
    /// <summary>
    /// A water-basin puzzle object. Fills when Water is applied,
    /// freezes when Water+Metal combo fires.
    /// </summary>
    public class WaterBasin : PuzzleObject, IFreezable
    {
        [Header("Water Basin")]
        [SerializeField] private SpriteRenderer waterRenderer;
        [SerializeField] private Color emptyColor = Color.gray;
        [SerializeField] private Color filledColor = new Color(0.2f, 0.5f, 1f, 0.8f);
        [SerializeField] private Color frozenColor = new Color(0.8f, 0.95f, 1f, 0.9f);
        [SerializeField] private AudioClip fillSound;
        [SerializeField] private AudioClip freezeSound;

        private bool _isFilled = false;
        private bool _isFrozen = false;

        public bool IsFilled => _isFilled;
        public bool IsFrozen => _isFrozen;

        protected override void Start()
        {
            base.Start();
            if (waterRenderer) waterRenderer.color = emptyColor;
        }

        protected override void OnActivate(ElementType element)
        {
            if (element == ElementType.Water)
            {
                _isFilled = true;
                if (waterRenderer) waterRenderer.color = filledColor;
                if (fillSound) AudioSource.PlayClipAtPoint(fillSound, transform.position);
            }
        }

        protected override void HandleCombo(ComboEffect combo, ElementType applied)
        {
            // Crystal Water combo: Metal purifies filled water
            if (_isFilled && combo.Name == "Crystal Water")
                Solve();
        }

        public void Freeze()
        {
            if (!_isFilled) return;
            _isFrozen = true;
            if (waterRenderer) waterRenderer.color = frozenColor;
            if (freezeSound) AudioSource.PlayClipAtPoint(freezeSound, transform.position);
            // Make it walkable — enable a platform collider
            var platformCollider = GetComponent<PlatformEffector2D>();
            if (platformCollider != null) platformCollider.enabled = true;
        }

        public void Unfreeze()
        {
            _isFrozen = false;
            if (waterRenderer) waterRenderer.color = _isFilled ? filledColor : emptyColor;
            var platformCollider = GetComponent<PlatformEffector2D>();
            if (platformCollider != null) platformCollider.enabled = false;
        }

        public override void Reset()
        {
            base.Reset();
            _isFilled = false;
            _isFrozen = false;
            if (waterRenderer) waterRenderer.color = emptyColor;
        }
    }
}
