using UnityEngine;

namespace Code.Gameplay
{
    /// <summary>
    /// Геометрия строя без привязки к ассету.
    ///
    /// Отделена от FormationConfig намеренно: солвер не должен зависеть
    /// от ScriptableObject, иначе слотовую математику невозможно покрыть
    /// тестами вне Unity — а именно в ней проще всего ошибиться незаметно.
    /// </summary>
    public readonly struct FormationShape
    {
        public FormationLayout Layout { get; }

        public int Rows { get; }
        public int Columns { get; }

        public float RankSpacing { get; }
        public float FileSpacing { get; }

        public FormationShape(
            FormationLayout layout,
            int rows,
            int columns,
            float rankSpacing,
            float fileSpacing)
        {
            Layout = layout;
            Rows = Mathf.Max(1, rows);
            Columns = Mathf.Max(1, columns);
            RankSpacing = Mathf.Max(0.1f, rankSpacing);
            FileSpacing = Mathf.Max(0.1f, fileSpacing);
        }
    }
}
