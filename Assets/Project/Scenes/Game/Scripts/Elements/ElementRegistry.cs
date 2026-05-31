// ============================================================
//  ElementRegistry.cs
//  ScriptableObject asset that holds all ElementData entries.
//  Create via: Assets → Create → FiveElements → Element Registry
//  Place in: Assets/Scripts/Elements/
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace FiveElements
{
    [CreateAssetMenu(fileName = "ElementRegistry", menuName = "FiveElements/Element Registry")]
    public class ElementRegistry : ScriptableObject
    {
        [Tooltip("Populate one entry per element. Order does not matter.")]
        [SerializeField] private List<ElementData> elements = new List<ElementData>();

        // Quick-lookup dictionary built at runtime
        private Dictionary<ElementType, ElementData> _lookup;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<ElementType, ElementData>();
            foreach (var data in elements)
                if (data.type != ElementType.None)
                    _lookup[data.type] = data;
        }

        /// <summary>Returns the data for a given element, or null if not found.</summary>
        public ElementData Get(ElementType type)
        {
            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(type, out var data) ? data : null;
        }

        /// <summary>Returns all registered ElementData entries.</summary>
        public IReadOnlyList<ElementData> All => elements;
    }
}