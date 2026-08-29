using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Строи, доступные игроку для выбора перед боем.
    /// </summary>
    [CreateAssetMenu(
        fileName = "FormationCatalog",
        menuName = "Gladiator Game/Formations/Formation Catalog")]
    public sealed class FormationCatalog : ScriptableObject
    {
        [SerializeField] private FormationConfig[] _formations;

        public IReadOnlyList<FormationConfig> Formations =>
            _formations ?? System.Array.Empty<FormationConfig>();

        public int Count => Formations.Count;

        /// <summary>
        /// Следующий строй по кругу. Возвращает текущий, если каталог пуст.
        /// </summary>
        public FormationConfig GetNext(FormationConfig current, int direction)
        {
            IReadOnlyList<FormationConfig> formations = Formations;

            if (formations.Count == 0)
                return current;

            int currentIndex = IndexOf(current);

            if (currentIndex < 0)
                return formations[0];

            int step = direction >= 0 ? 1 : -1;

            int nextIndex =
                (currentIndex + step + formations.Count) % formations.Count;

            return formations[nextIndex];
        }

        public int IndexOf(FormationConfig formation)
        {
            IReadOnlyList<FormationConfig> formations = Formations;

            for (var i = 0; i < formations.Count; i++)
            {
                if (formations[i] == formation)
                    return i;
            }

            return -1;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_formations == null || _formations.Length == 0)
            {
                Debug.LogWarning(
                    $"FormationCatalog '{name}': не задано ни одного строя.",
                    this);
            }
        }
#endif
    }
}
