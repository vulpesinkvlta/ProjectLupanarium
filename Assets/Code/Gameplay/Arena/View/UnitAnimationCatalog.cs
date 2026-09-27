using System;
using UnityEngine;

namespace Code.Gameplay
{
    [CreateAssetMenu(fileName = "UnitAnimations", menuName = "Gladiator Game/Units/Sprite Animations")]
    public sealed class UnitAnimationCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public UnitClassId ClassId;
            [Tooltip("Общий цикл для ожидания и ходьбы. Кадры по порядку.")]
            public Sprite[] IdleWalk = Array.Empty<Sprite>();
            [Tooltip("Атака: замах и удар. Проигрывается один раз на атаку.")]
            public Sprite[] Attack = Array.Empty<Sprite>();
            [Min(1)] public float FramesPerSecond = 8;
            [Tooltip("Масштаб спрайтов относительно общих визуальных настроек.")]
            [Min(.01f)] public float Scale = 1;
            public Vector2 Offset;
            [Tooltip("В исходных кадрах персонаж смотрит вправо.")]
            public bool FacesRight = true;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        public Entry Find(UnitClassId classId)
        {
            foreach (var entry in _entries)
                if (entry != null && entry.ClassId == classId) return entry;
            return null;
        }
    }
}
