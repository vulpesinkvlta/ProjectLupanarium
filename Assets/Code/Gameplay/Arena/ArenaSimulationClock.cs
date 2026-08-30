using System;
using UnityEngine;
using VContainer.Unity;

namespace Code.Gameplay
{
    public sealed class ArenaSimulationClock : IStartable, ITickable
    {
        private const float SimulationFrequency = 30f;
        private const float TickInterval = 1f / SimulationFrequency;

        // Не позволяем одному зависшему кадру породить
        // огромную очередь симуляционных тиков.
        private const float MaxFrameDelta = 0.1f;
        private const int MaxTicksPerFrame = 12;

        private readonly ArenaSimulation _simulation;

        private float _accumulator;
        private float _simulationSpeed = 1f;

        private bool _isPaused;
        private bool _hasReportedSuccessfulTick;

        public float SimulationSpeed => _simulationSpeed;
        public float InterpolationAlpha { get; private set; }
        public bool IsPaused => _isPaused;

        private readonly UnitViewSynchronizer _viewSynchronizer;

        public ArenaSimulationClock(
            ArenaSimulation simulation,
            UnitViewSynchronizer viewSynchronizer)
        {
            _simulation = simulation ??
                throw new ArgumentNullException(nameof(simulation));

            _viewSynchronizer = viewSynchronizer ??
                throw new ArgumentNullException(
                    nameof(viewSynchronizer));
        }

        public void Start()
        {
            _simulation.Start();

            Debug.Log(
                $"[ArenaSandbox] Simulation started. " +
                $"Frequency: {SimulationFrequency} Hz.");
        }

        public void Tick()
        {
            if (_isPaused)
            {
                InterpolationAlpha = 0f;
                return;
            }

            float frameDelta =
                Mathf.Min(Time.unscaledDeltaTime, MaxFrameDelta) *
                _simulationSpeed;

            _accumulator += frameDelta;

            var executedTicks = 0;

            while (_accumulator >= TickInterval &&
                   executedTicks < MaxTicksPerFrame)
            {
                _simulation.Tick(TickInterval);

                _accumulator -= TickInterval;
                executedTicks++;
            }

            if (executedTicks == MaxTicksPerFrame &&
                _accumulator >= TickInterval)
            {
                _accumulator %= TickInterval;
            }

            InterpolationAlpha =
                Mathf.Clamp01(_accumulator / TickInterval);
            // Визуальные эффекты идут по тому же времени, что и симуляция:
            // на ускоренной перемотке угасание трупов ускоряется вместе
            // с боем, а не тянется в реальном темпе.
            _viewSynchronizer.UpdateViews(
                InterpolationAlpha,
                frameDelta);

            ReportSuccessfulSimulationStart();
        }

        public void SetSpeed(float multiplier)
        {
            if (multiplier <= 0f || multiplier > 8f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(multiplier),
                    "Simulation speed must be greater than zero " +
                    "and no greater than x8.");
            }

            _simulationSpeed = multiplier;
        }

        public void Pause()
        {
            _isPaused = true;
        }

        public void Resume()
        {
            _isPaused = false;
        }

        private void ReportSuccessfulSimulationStart()
        {
            if (_hasReportedSuccessfulTick)
                return;

            if (_simulation.TickIndex < 30)
                return;

            _hasReportedSuccessfulTick = true;

            Debug.Log(
                $"[ArenaSandbox] Fixed simulation tick works. " +
                $"Tick: {_simulation.TickIndex}, " +
                $"simulation time: " +
                $"{_simulation.ElapsedSimulationTime:F2} sec.");
        }
    }
}
