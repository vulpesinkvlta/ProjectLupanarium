using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace Code.Gameplay
{
    public sealed class LanguageSettingsView : MonoBehaviour
    {
        [SerializeField] private Button _russian, _english, _turkish, _automatic;
        [SerializeField] private TMP_Text _heading, _hint;
        private void Awake()
        {
            _russian.onClick.AddListener(Russian);
            _english.onClick.AddListener(English);
            _turkish.onClick.AddListener(Turkish);
            _automatic.onClick.AddListener(L10n.UseAutomaticLanguage);
            L10n.LanguageChanged += Refresh;
            Refresh();
            StartCoroutine(L10n.Initialize()); // Also supports opening Menu directly, without Bootstrap.
        }
        private void Russian() => L10n.SetLanguage("ru");
        private void English() => L10n.SetLanguage("en");
        private void Turkish() => L10n.SetLanguage("tr");
        private void Refresh()
        {
            _heading.text = L10n.Text("settings.language.title");
            _hint.text = L10n.Text("settings.language.hint");
            string current = L10n.IsReady ? LocalizationSettings.SelectedLocale.Identifier.Code : "";
            Mark(_russian, !L10n.IsAutomatic && current == "ru");
            Mark(_english, !L10n.IsAutomatic && current == "en");
            Mark(_turkish, !L10n.IsAutomatic && current == "tr");
            Mark(_automatic, L10n.IsAutomatic);
            _automatic.GetComponentInChildren<TMP_Text>(true).text = L10n.Text("settings.language.auto");
        }
        private static void Mark(Button button, bool selected)
        {
            button.interactable = L10n.IsReady;
            var colors = button.colors;
            colors.normalColor = selected ? new Color(.65f,.42f,.20f) : Color.white;
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
        }
        private void OnDestroy()
        {
            L10n.LanguageChanged -= Refresh;
            _russian.onClick.RemoveListener(Russian);
            _english.onClick.RemoveListener(English);
            _turkish.onClick.RemoveListener(Turkish);
            _automatic.onClick.RemoveListener(L10n.UseAutomaticLanguage);
        }
    }
}
