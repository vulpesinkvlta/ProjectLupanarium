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

        [Header("Defeat")]
        [SerializeField] private GameObject _defeatRoot;
        [SerializeField] private Button _restartButton;
        [SerializeField] private TMP_Text _defeatLabel;

        public event Action FightRequested;
        public event Action RestartRequested;

        public void SetRunInfo(int waveNumber, int gold, int squadSize)
        {
            if (_waveLabel != null)
                _waveLabel.text = $"Волна {waveNumber}";

            if (_goldLabel != null)
                _goldLabel.text = $"Золото: {gold}";

            if (_squadLabel != null)
                _squadLabel.text = $"Отряд: {squadSize}";
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
        }

        private void OnDestroy()
        {
            if (_fightButton != null)
                _fightButton.onClick.RemoveListener(OnFightClicked);

            if (_restartButton != null)
                _restartButton.onClick.RemoveListener(OnRestartClicked);
        }

        private void OnFightClicked()
        {
            FightRequested?.Invoke();
        }

        private void OnRestartClicked()
        {
            RestartRequested?.Invoke();
        }
    }
}
