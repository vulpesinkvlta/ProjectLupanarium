using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class DamageBuffer
    {
        private const int InitialCapacity = 512;

        private readonly List<DamageRequest> _requests =
            new(InitialCapacity);

        public IReadOnlyList<DamageRequest> Requests =>
            _requests;

        public int Count => _requests.Count;

        public void Add(DamageRequest request)
        {
            _requests.Add(request);
        }

        public void Clear()
        {
            _requests.Clear();
        }
    }
}
