using System.Collections.Generic;
using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Боевой строй: форма расстановки, бонусы к статам и дистанция,
    /// на которой боец бросает строй и вступает в схватку.
    /// </summary>
    [CreateAssetMenu(
        fileName = "FormationConfig",
        menuName = "Gladiator Game/Formations/Formation")]
    public sealed class FormationConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;

        [Header("Shape")]
        [SerializeField] private FormationLayout _layout = FormationLayout.Box;

        [Tooltip("Рядов в глубину. Используется макетом Line.")]
        [SerializeField, Min(1)] private int _rows = 2;

        [Tooltip("Бойцов в ряд. Используется макетом Column.")]
        [SerializeField, Min(1)] private int _columns = 4;

        [Tooltip("Расстояние между рядами в глубину.")]
        [SerializeField, Min(0.1f)] private float _rankSpacing = 0.8f;

        [Tooltip("Расстояние между бойцами внутри ряда.")]
        [SerializeField, Min(0.1f)] private float _fileSpacing = 0.8f;

        [Header("Behaviour")]
        [Tooltip("Скорость, с которой строй наступает на врага.")]
        [SerializeField, Min(0f)] private float _marchSpeed = 1.5f;

        [Tooltip("На этой дистанции до цели боец покидает строй навсегда.")]
        [SerializeField, Min(0.5f)] private float _breakRange = 2.5f;

        [Header("Bonuses")]
        [SerializeField] private StatModifier[] _modifiers;

        public string Id => _id;
        public Sprite Icon => _icon;
        public string Description => _description;

        public FormationLayout Layout => _layout;

        public int Rows => _rows;
        public int Columns => _columns;

        public float RankSpacing => _rankSpacing;
        public float FileSpacing => _fileSpacing;

        /// <summary>Геометрия строя в виде, независимом от ассета.</summary>
        public FormationShape Shape =>
            new(_layout, _rows, _columns, _rankSpacing, _fileSpacing);

        public float MarchSpeed => _marchSpeed;
        public float BreakRange => _breakRange;

        public IReadOnlyList<StatModifier> Modifiers =>
            _modifiers ?? System.Array.Empty<StatModifier>();

        public string DisplayName =>
            string.IsNullOrWhiteSpace(_displayName)
                ? name
                : _displayName;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                Debug.LogWarning(
                    $"FormationConfig '{name}' has an empty ID.",
                    this);
            }
        }
#endif
    }
}
