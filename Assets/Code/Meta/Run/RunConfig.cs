using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Стартовые условия забега. Отдельный ассет, чтобы баланс старта
    /// правился без захода в код.
    /// </summary>
    [CreateAssetMenu(
        fileName = "RunConfig",
        menuName = "Gladiator Game/Run/Run Config")]
    public sealed class RunConfig : ScriptableObject
    {
        [Header("Starting squad")]
        [SerializeField] private SquadEntry[] _startingSquad;

        [Header("Formation")]
        [SerializeField] private FormationConfig _defaultFormation;

        [Header("Economy")]
        [SerializeField, Min(0)] private int _startingGold;

        public IReadOnlyList<SquadEntry> StartingSquad => _startingSquad;
        public int StartingGold => _startingGold;

        public FormationConfig DefaultFormation => _defaultFormation;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_startingSquad == null || _startingSquad.Length == 0)
            {
                Debug.LogWarning(
                    $"RunConfig '{name}': стартовый отряд пуст, " +
                    $"забег начнётся без единого гладиатора.",
                    this);
            }
        }
#endif
    }
}
