namespace Code.Gameplay
{
    public enum FormationLayout : byte
    {
        /// <summary>Широкий фронт: много бойцов в ряд, мало рядов.</summary>
        Line = 0,

        /// <summary>Узкая глубокая колонна.</summary>
        Column = 1,

        /// <summary>Компактный квадрат.</summary>
        Box = 2,

        /// <summary>Клин остриём к врагу.</summary>
        Wedge = 3,

        /// <summary>Кольцо, круговая оборона.</summary>
        Circle = 4,

        /// <summary>
        /// Манипулы: отряд разбит на отдельные квадраты, стоящие
        /// в шахматном порядке с промежутками между ними.
        /// </summary>
        Maniple = 5
    }
}
