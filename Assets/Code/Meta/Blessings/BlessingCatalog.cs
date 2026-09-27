using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    [CreateAssetMenu(menuName = "Lupanarium/Blessing Catalog")]
    public sealed class BlessingCatalog : ScriptableObject
    {
        [SerializeField] private BlessingConfig[] _blessings = Array.Empty<BlessingConfig>();
        public IReadOnlyList<BlessingConfig> Blessings => _blessings;
        public bool Contains(BlessingConfig blessing) => blessing != null && Array.IndexOf(_blessings, blessing) >= 0;
    }
}
