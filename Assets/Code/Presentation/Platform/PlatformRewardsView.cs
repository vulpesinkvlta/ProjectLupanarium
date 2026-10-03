using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Code.Gameplay
{
    public sealed class PlatformRewardsView : MonoBehaviour
    {
        [SerializeField] private bool _base;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _toggle;
        [SerializeField] private Button _money, _supplies, _heal, _rest, _battle;
        [SerializeField] private TMP_Text _status;
        private PlatformRewardsService _service;
        private RunState _run;
        private LupanariumState _school;
        [Inject] public void Construct(PlatformRewardsService service, RunState run, LupanariumState school)
        { _service = service; _run = run; _school = school; }
        private void Start()
        {
            if (_toggle != null) _toggle.onClick.AddListener(() => _panel.SetActive(!_panel.activeSelf));
            if (_money != null) _money.onClick.AddListener(() => _service.Request(AdRewardKind.Money));
            if (_supplies != null) _supplies.onClick.AddListener(() => _service.Request(AdRewardKind.Supplies));
            if (_heal != null) _heal.onClick.AddListener(() => _service.Request(AdRewardKind.Heal));
            if (_battle != null) _battle.onClick.AddListener(() => _service.Request(AdRewardKind.BattleBonus));
            if (_rest != null) _rest.onClick.AddListener(() => _run.TryFinishRest(PlatformRewardsService.Now));
            if (_base) _panel.SetActive(false);
        }
        private void Update()
        {
            if (_service == null) return;
            if (_base)
            {
                Set(_money, AdRewardKind.Money, L10n.Text("Реклама: +50 денариев"));
                Set(_supplies, AdRewardKind.Supplies, L10n.Text("Реклама: +1 ") + (_service.Supply != null ? _service.Supply.DisplayName : L10n.Text("расходник")));
                Set(_heal, AdRewardKind.Heal, L10n.Text("Реклама: восстановить весь отряд"));
                long rest = System.Math.Max(0, _run.RestReadyAt - PlatformRewardsService.Now);
                _rest.interactable = _run.IsActive && _run.HasWounded && _run.RestReadyAt > 0 && rest == 0;
                _rest.GetComponentInChildren<TMP_Text>().text = rest > 0 ? L10n.F($"Бесплатный отдых: {rest / 60}:{rest % 60:00}") : L10n.Text("Восстановить отряд бесплатно");
                _status.text = !_run.IsActive ? L10n.Text("Нет активного отряда") : !_run.HasWounded ? L10n.Text("Все бойцы здоровы") : L10n.F($"Ранения сохраняются между боями.\nБез сознания: {_run.DownedCount}. Отдых — 2 минуты.");
                if (_service.AdsVisible && _service.Bridge != null && !string.IsNullOrEmpty(_service.Bridge.Status))
                    _status.text += "\n" + _service.Bridge.Status;
            }
            else
            {
                bool visible = _service.AdsVisible && _school.PendingBattleBonus > 0 &&
                    (_run.Phase == BattleFlowState.BattleSummary || _run.Phase == BattleFlowState.Reward || _run.Phase == BattleFlowState.Defeat);
                _panel.SetActive(visible);
                if (visible)
                {
                    Set(_battle, AdRewardKind.BattleBonus, L10n.F($"Реклама: ещё +{_school.PendingBattleBonus} денариев за бой"));
                    _status.text = _service.Bridge?.Status;
                }
            }
        }
        private void Set(Button button, AdRewardKind kind, string label)
        {
            if (button == null) return;
            button.gameObject.SetActive(_service.AdsVisible);
            button.interactable = _service.CanReward(kind);
            long seconds = _service.Remaining(kind);
            button.GetComponentInChildren<TMP_Text>().text = label + (seconds > 0 ? $" • {seconds / 60}:{seconds % 60:00}" : "");
        }
    }
}
