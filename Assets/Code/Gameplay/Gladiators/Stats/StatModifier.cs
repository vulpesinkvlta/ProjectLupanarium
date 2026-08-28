using System;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Одно изменение одного стата.
    ///
    /// Структура не readonly и хранит данные в [SerializeField]-полях,
    /// потому что модификаторы авторятся в ScriptableObject'ах: сериализатор
    /// Unity не видит backing-поля авто-свойств и не умеет писать в readonly.
    /// Наружу изменяемость всё равно закрыта — сеттеров нет.
    /// </summary>
    [Serializable]
    public struct StatModifier
    {
        [SerializeField] private StatId _statId;
        [SerializeField] private ModType _modType;
        [SerializeField] private float _value;

        public StatId StatId => _statId;
        public ModType ModType => _modType;
        public float Value => _value;

        public StatModifier(StatId statId, ModType modType, float value)
        {
            _statId = statId;
            _modType = modType;
            _value = value;
        }

        public override string ToString()
        {
            return $"{_statId} {_modType} {_value}";
        }
    }
}
