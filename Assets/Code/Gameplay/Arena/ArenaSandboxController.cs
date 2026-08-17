using System;
using UnityEngine;

namespace Code.Gameplay
{
    public sealed class ArenaSandboxController
    {
        private readonly ArenaSimulation _simulation;
        private readonly UnitSpawner _unitSpawner;

        public ArenaSandboxController(
            ArenaSimulation simulation,
            UnitSpawner unitSpawner)
        {
            _simulation = simulation ??
                throw new ArgumentNullException(nameof(simulation));

            _unitSpawner = unitSpawner ??
                throw new ArgumentNullException(nameof(unitSpawner));
        }

        public void SpawnBattle(int unitsPerTeam)
        {
            _simulation.Stop();

            _unitSpawner.ClearAll();
            _simulation.Reset();

            _unitSpawner.SpawnBattle(unitsPerTeam);

            _simulation.Start();
        }

        public void ClearBattle()
        {
            _simulation.Stop();

            _unitSpawner.ClearAll();
            _simulation.Reset();

            _simulation.Start();
        }
    }
}
