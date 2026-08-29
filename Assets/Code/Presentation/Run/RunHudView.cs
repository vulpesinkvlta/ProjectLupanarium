using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Gameplay
{
    /// <summary>
    /// Постоянный HUD забега: волна, золото, размер отряда,
    /// кнопка старта боя и экран поражения.
    /// </summary>
    public sealed class RunHudView : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] private TMP_Text _waveLabel;
        [SerializeField] private TMP_Text _goldLabel;
        [SerializeField] private TMP_Text _squadLabel;

        [Header("Preparation")]
        [SerializeField] private GameObject _preparationRoot;
        [SerializeField] private Button _fightButton;

        [Header("Formation (необязательно)")]
        [SerializeField] private TMP_Text _formationLabel;
        [SerializeField] private Button _nextFormationButton;
        [SerializeField] private Button _prevFormationButton;

        [Header("Defeat")]
        [SerializeField] private GameObject _defeatRoot;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _returnToLupanariumButton;
        [SerializeField] private TMP_Text _defeatLabel;

        public event Action FightRequested;
        public event Action RestartRequested;
        public event Action ReturnToLupanariumRequested;

        /// <summary>Игрок листает строй. Аргумент — направление, +1 или -1.</summary>
        public event Action<int> FormationCycleRequested;

        public void SetRunInfo(int waveNumber, int gold, int squadSize)
        {
            if (_waveLabel != null)
                _waveLabel.text = $"Волна {waveNumber}";

            if (_goldLabel != null)
                _goldLabel.text = $"Золото: {gold}";

            if (_squadLabel != null)
                _squadLabel.text = $"Отряд: {squadSize}";
        }

        public void SetFormation(string formationName)
        {
            if (_formationLabel != null)
            {
                _formationLabel.text =
                    string.IsNullOrEmpty(formationName)
                        ? "Строй: без строя"
                        : $"Строй: {formationName}";
            }
        }

        public void SetState(BattleFlowState state)
        {
            if (_preparationRoot != null)
            {
                _preparationRoot.SetActive(
                    state == BattleFlowState.Preparation);
            }

            if (_defeatRoot != null)
                _defeatRoot.SetActive(state == BattleFlowState.Defeat);
        }

        public void SetDefeatText(int waveNumber)
        {
            if (_defeatLabel != null)
                _defeatLabel.text = $"Поражение на волне {waveNumber}";
        }

        private void Awake()
        {
            if (_fightButton != null)
                _fightButton.onClick.AddListener(OnFightClicked);

            if (_restartButton != null)
                _restartButton.onClick.AddListener(OnRestartClicked);

            if (_nextFormationButton != null)
                _nextFormationButton.onClick.AddListener(OnNextFormationClicked);

            if (_prevFormationButton != null)
                _prevFormationButton.onClick.AddListener(OnPrevFormationClicked);

            if (_returnToLupanariumButton != null)
                _returnToLupanariumButton.onClick.AddListener(OnReturnClicked);
        }

        private void OnDestroy()
        {
            if (_fightButton != null)
                _fightButton.onClick.RemoveListener(OnFightClicked);

            if (_restartButton != null)
                _restartButton.onClick.RemoveListener(OnRestartClicked);

            if (_nextFormationButton != null)
                _nextFormationButton.onClick.RemoveListener(OnNextFormationClicked);

            if (_prevFormationButton != null)
                _prevFormationButton.onClick.RemoveListener(OnPrevFormationClicked);

            if (_returnToLupanariumButton != null)
                _returnToLupanariumButton.onClick.RemoveListener(OnReturnClicked);
        }

        private void OnFightClicked()
        {
            FightRequested?.Invoke();
        }

        private void OnRestartClicked()
        {
            RestartRequested?.Invoke();
        }

        private void OnReturnClicked()
        {
            ReturnToLupanariumRequested?.Invoke();
        }

        private void OnNextFormationClicked()
        {
            FormationCycleRequested?.Invoke(1);
        }

        private void OnPrevFormationClicked()
        {
            FormationCycleRequested?.Invoke(-1);
        }
    }
}
