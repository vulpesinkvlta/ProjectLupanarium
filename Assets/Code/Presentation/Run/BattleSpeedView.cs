using TMPro;
using UnityEngine;
using VContainer;

namespace Code.Gameplay
{
    public sealed class BattleSpeedView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button _button;
        [SerializeField] private TMP_Text _label;
        private ArenaSimulationClock _clock;
        private RunState _run;
        [Inject] public void Construct(ArenaSimulationClock clock, RunState run) { _clock=clock; _run=run; }
        private void Awake() => _button.onClick.AddListener(Cycle);
        private void OnDestroy() => _button.onClick.RemoveListener(Cycle);
        private void Cycle()
        {
            if(_run?.Phase!=BattleFlowState.Fighting || Time.timeScale<=0 || _clock.IsPaused)return;
            _clock.SetSpeed(NextSpeed(_clock.SimulationSpeed));
        }
        public static float NextSpeed(float current) => current<2 ? 2 : current<4 ? 4 : 1;
        private void Update()
        {
            bool fighting=_run?.Phase==BattleFlowState.Fighting;
            _button.gameObject.SetActive(fighting);
            if(!fighting)return;
            _button.interactable=Time.timeScale>0 && !_clock.IsPaused;
            _label.text=L10n.Format("Скорость боя ×{0}",_clock.SimulationSpeed);
        }
    }
}
