using System.Collections.Generic;
using System;

namespace Code.Gameplay
{
    public sealed class ModifierSet
    {
        private const int InitialCapacity = 10;

        private readonly List<StatModifier> _modifiers = new(InitialCapacity);

        public int Count => _modifiers.Count;
        public StatModifier this[int index] => _modifiers[index];

        public void Add(StatModifier modifier)
        {
            if(modifier.StatId == StatId.Unknown || modifier.ModType == ModType.None)
            {
                throw new ArgumentException("Modifier cannot have unknown stat and none mod type.");
            }
            _modifiers.Add(modifier);
        }   
        public void AddRange(IReadOnlyList<StatModifier> modifiers)
        {
            if(modifiers == null)
            {
                throw new ArgumentNullException(nameof(modifiers));
            }
            for (var i = 0; i < modifiers.Count; i++)
                Add(modifiers[i]);
        }
        public void Clear()
        {
            _modifiers.Clear();
        }
    }
}
