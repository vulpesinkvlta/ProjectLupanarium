using UnityEngine;

namespace Code.Gameplay
{
    public enum UnitClassId : byte
    {
        None = 0,

        Murmillo = 1,
        Retiarius = 2,
        Secutor = 3,
        Thraex = 4,
        Hoplomachus = 5,

        // Звери выступают на стороне арены против гладиаторов.
        Lion = 6,
        Wolf = 7,

        // Временные юниты

        Tirpplet = 8,
        Tralalaela = 9,
        Bobrtito = 10,
        Lirililarila = 11,
        TrippiTroppa = 12,
        ShammerHark = 13,
        Skibidito = 14,
        Petushara = 15,
        Bullshitto = 16,
    }
}
