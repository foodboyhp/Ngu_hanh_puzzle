// ============================================================
//  ElementType.cs
//  Shared enums and data structures used across all systems.
//  Place in: Assets/Scripts/Elements/
// ============================================================

using UnityEngine;

namespace FiveElements
{
    /// <summary>
    /// The five elements of Chinese cosmology (Ngũ Hành / 五行).
    /// Order matches the Generating Cycle: Water → Wood → Fire → Earth → Metal → Water
    /// </summary>
    public enum ElementType
    {
        None = 0,
        Water = 1,   // 水 Water  — first element, player starts with this
        Wood = 2,   // 木 Wood
        Fire = 3,   // 火 Fire
        Earth = 4,   // 土 Earth
        Metal = 5    // 金 Metal
    }

    /// <summary>
    /// Result when two elements interact (generating or overcoming cycle).
    /// </summary>
    public enum InteractionResult
    {
        None,
        Generating,     // 相生 — synergistic / combo
        Overcoming,     // 相克 — opposing / cancels or transforms
        Neutral
    }

    /// <summary>
    /// Lightweight data bundle describing one element's meta-info.
    /// Populated via ElementRegistry ScriptableObject.
    /// </summary>
    [System.Serializable]
    public class ElementData
    {
        public ElementType type;
        public string displayName;          // e.g. "(水)"
        public string description;
        public Color primaryColor;         // used for UI, VFX tints
        public Color secondaryColor;
        public Sprite icon;                 // HUD icon
        public AudioClip absorbSound;       // played when player absorbs this element
        public AudioClip activateSound;     // played when ability is used
        public GameObject absorbVFXPrefab;  // particle burst on absorption
        public GameObject abilityVFXPrefab; // visual when ability fires
    }
}