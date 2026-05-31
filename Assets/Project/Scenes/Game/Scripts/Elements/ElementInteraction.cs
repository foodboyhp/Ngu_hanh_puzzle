// ============================================================
//  ElementInteraction.cs
//  Encodes the Chinese Five Element cycles and resolves
//  what happens when two elements meet in the world.
//
//  Generating Cycle (相生 xiāng shēng):
//    Water → Wood → Fire → Earth → Metal → Water
//
//  Overcoming Cycle (相克 xiāng kè):
//    Water > Fire > Metal > Wood > Earth > Water
//
//  Usage:
//    var result = ElementInteraction.GetResult(ElementType.Thuy, ElementType.Hoa);
//    // → InteractionResult.Overcoming  (Water beats Fire)
//
//  Place in: Assets/Scripts/Elements/
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace FiveElements
{
    /// <summary>
    /// Defines what happens when two elements interact.
    /// All logic is static — no MonoBehaviour needed.
    /// </summary>
    public static class ElementInteraction
    {
        // ── Generating Cycle (A generates B) ──────────────────────
        // Water feeds Wood, Wood feeds Fire, etc.
        private static readonly Dictionary<ElementType, ElementType> GeneratingCycle =
            new Dictionary<ElementType, ElementType>
            {
                { ElementType.Water, ElementType.Wood  },  // Water → Wood
                { ElementType.Wood,  ElementType.Fire  },  // Wood  → Fire
                { ElementType.Fire,  ElementType.Earth  },  // Fire  → Earth
                { ElementType.Earth,  ElementType.Metal  },  // Earth → Metal
                { ElementType.Metal,  ElementType.Water },  // Metal → Water
            };

        // ── Overcoming Cycle (A overcomes B) ──────────────────────
        // Water extinguishes Fire, Fire melts Metal, etc.
        private static readonly Dictionary<ElementType, ElementType> OvercomingCycle =
            new Dictionary<ElementType, ElementType>
            {
                { ElementType.Water, ElementType.Fire  },  // Water  > Fire
                { ElementType.Fire,  ElementType.Metal  },  // Fire   > Metal
                { ElementType.Metal,  ElementType.Wood  },  // Metal  > Wood
                { ElementType.Wood,  ElementType.Earth  },  // Wood   > Earth
                { ElementType.Earth,  ElementType.Water },  // Earth  > Water
            };

        // ── Combo Definitions ─────────────────────────────────────
        // When two elements are used together they can produce a named combo effect.
        private static readonly Dictionary<(ElementType, ElementType), ComboEffect> Combos =
            new Dictionary<(ElementType, ElementType), ComboEffect>
            {
                // Generating pairs
                { (ElementType.Water, ElementType.Wood), new ComboEffect(
                    "Torrential Growth",
                    "Water feeds the roots — vines grow twice as long and last twice as long.",
                    ComboType.Generating) },

                { (ElementType.Wood, ElementType.Fire), new ComboEffect(
                    "Wildfire",
                    "Wood fuels fire — ignite spreads to adjacent objects automatically.",
                    ComboType.Generating) },

                { (ElementType.Fire, ElementType.Earth), new ComboEffect(
                    "Scorched Earth",
                    "Fire bakes earth — tremor range doubles and leaves burning ground.",
                    ComboType.Generating) },

                { (ElementType.Earth, ElementType.Metal), new ComboEffect(
                    "Forged Iron",
                    "Earth yields metal — magnetic pull also drags stone objects.",
                    ComboType.Generating) },

                { (ElementType.Metal, ElementType.Water), new ComboEffect(
                    "Crystal Water",
                    "Metal purifies water — projectiles pierce through obstacles.",
                    ComboType.Generating) },

                // Overcoming pairs
                { (ElementType.Water, ElementType.Fire), new ComboEffect(
                    "Steam Cloud",
                    "Water meets fire — creates a blinding steam screen for 3 seconds.",
                    ComboType.Overcoming) },

                { (ElementType.Fire, ElementType.Metal), new ComboEffect(
                    "Molten Metal",
                    "Fire melts metal — magnetic objects become slowed and moldable.",
                    ComboType.Overcoming) },

                { (ElementType.Metal, ElementType.Wood), new ComboEffect(
                    "Severed Roots",
                    "Metal cuts wood — instantly destroys all active vines on screen.",
                    ComboType.Overcoming) },

                { (ElementType.Wood, ElementType.Earth), new ComboEffect(
                    "Deep Root",
                    "Wood cracks earth — tremor reveals hidden underground passages.",
                    ComboType.Overcoming) },

                { (ElementType.Earth, ElementType.Water), new ComboEffect(
                    "Mud Trap",
                    "Earth absorbs water — projectiles become sticky mud that slows enemies.",
                    ComboType.Overcoming) },
            };

        // ──────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────

        /// <summary>
        /// Determine the relationship between element A and element B.
        /// </summary>
        public static InteractionResult GetResult(ElementType a, ElementType b)
        {
            if (a == b || a == ElementType.None || b == ElementType.None)
                return InteractionResult.Neutral;

            if (GeneratingCycle.TryGetValue(a, out var generated) && generated == b)
                return InteractionResult.Generating;

            if (OvercomingCycle.TryGetValue(a, out var overcome) && overcome == b)
                return InteractionResult.Overcoming;

            return InteractionResult.Neutral;
        }

        /// <summary>
        /// Returns the element that A generates in the cycle, or None.
        /// </summary>
        public static ElementType GetGenerated(ElementType a) =>
            GeneratingCycle.TryGetValue(a, out var result) ? result : ElementType.None;

        /// <summary>
        /// Returns the element that A overcomes, or None.
        /// </summary>
        public static ElementType GetOvercome(ElementType a) =>
            OvercomingCycle.TryGetValue(a, out var result) ? result : ElementType.None;

        /// <summary>
        /// Returns the element that overcomes A, or None.
        /// </summary>
        public static ElementType GetWeakness(ElementType a)
        {
            foreach (var kvp in OvercomingCycle)
                if (kvp.Value == a) return kvp.Key;
            return ElementType.None;
        }

        /// <summary>
        /// Try to get a named combo for combining elements A and B.
        /// Order-independent: (Thuy, Hoa) == (Hoa, Thuy).
        /// </summary>
        public static bool TryGetCombo(ElementType a, ElementType b, out ComboEffect combo)
        {
            if (Combos.TryGetValue((a, b), out combo)) return true;
            if (Combos.TryGetValue((b, a), out combo)) return true;
            combo = null;
            return false;
        }

        /// <summary>
        /// Trigger a combo between two active elements on the player.
        /// Returns the ComboEffect if one exists, null otherwise.
        /// </summary>
        public static ComboEffect TriggerCombo(ElementType a, ElementType b,
                                                PlayerController player, Vector3 worldPos)
        {
            if (!TryGetCombo(a, b, out var combo)) return null;

            Debug.Log($"[ElementInteraction] COMBO: {combo.Name} — {combo.Description}");

            // Spawn combo VFX at position using a pooled system if available
            combo.Execute(player, worldPos);
            return combo;
        }

        /// <summary>
        /// Apply elemental damage modifiers.
        /// Overcoming deals 2× damage; generating deals 1× with a bonus effect.
        /// </summary>
        public static float GetDamageMultiplier(ElementType attacker, ElementType defender)
        {
            var result = GetResult(attacker, defender);
            return result switch
            {
                InteractionResult.Overcoming => 2.0f,
                InteractionResult.Generating => 1.2f,
                _ => 1.0f
            };
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Supporting Types
    // ──────────────────────────────────────────────────────────────

    public enum ComboType { Generating, Overcoming }

    [System.Serializable]
    public class ComboEffect
    {
        public string Name;
        public string Description;
        public ComboType Type;

        // Optional: prefab/callback for the visual effect
        public System.Action<PlayerController, Vector3> OnExecute;

        public ComboEffect(string name, string description, ComboType type,
                           System.Action<PlayerController, Vector3> onExecute = null)
        {
            Name = name;
            Description = description;
            Type = type;
            OnExecute = onExecute;
        }

        public void Execute(PlayerController player, Vector3 worldPos)
        {
            OnExecute?.Invoke(player, worldPos);
        }
    }
}