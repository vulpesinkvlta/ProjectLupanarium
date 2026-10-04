using System.Linq;
using System.Text.RegularExpressions;
using Code.Gameplay;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;

namespace Code.Tests
{
    public sealed class LocalizationTests
    {
        [TestCase("ru", "ru")][TestCase("tr-TR", "tr")][TestCase("en_US", "en")]
        [TestCase("RU", "ru")][TestCase("de", "en")][TestCase(null, "en")]
        public void SdkLanguageHasSupportedFallback(string input, string expected) =>
            Assert.That(Code.Gameplay.L10n.NormalizeLanguage(input), Is.EqualTo(expected));

        [Test]
        public void AllTranslationsExistAndPreserveFormatArgumentsAndMarkup()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection("GameText");
            Assert.That(collection, Is.Not.Null);
            string[] Markers(string s) => Regex.Matches(s, @"\{\d+(?::[^}]+)?\}|</?[^>]+>").Cast<Match>().Select(x => x.Value).OrderBy(x => x).ToArray();
            foreach (var entry in collection.SharedData.Entries)
            {
                var source = (collection.GetTable(new LocaleIdentifier("ru")) as StringTable).GetEntry(entry.Id).Value;
                foreach (string code in new[] { "ru", "en", "tr" })
                {
                    var table = collection.GetTable(new LocaleIdentifier(code)) as StringTable;
                    var translated = table?.GetEntry(entry.Id);
                    Assert.That(translated?.Value, Is.Not.Null.And.Not.Empty, code + ": " + entry.Key);
                    Assert.That(Markers(translated.Value), Is.EqualTo(Markers(source)), code + ": " + entry.Key);
                    if (entry.Key.Contains("{") || entry.Key.Contains("<"))
                        Assert.That(Markers(translated.Value), Is.EqualTo(Markers(entry.Key)), code + ": " + entry.Key);
                    Assert.That(translated.IsSmart, Is.False);
                }
            }
        }

        [Test]
        public void FontContainsEveryTranslatedCharacter()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Localization/Fonts/GameFont.asset");
            var collection = LocalizationEditorSettings.GetStringTableCollection("GameText");
            var chars = collection.StringTables.SelectMany(t => t.Values).SelectMany(e => Regex.Replace(e.Value, "<[^>]+>", "")).Where(c => !char.IsControl(c)).Distinct();
            // Unity may clear a dynamic atlas after Play Mode or a build. Validate the source font,
            // rather than assuming every glyph happens to remain cached from the previous session.
            font.TryAddCharacters(new string(chars.ToArray()), out string missing);
            Assert.That(missing, Is.Null.Or.Empty, "Unsupported glyphs in translated content");
        }

        [Test]
        public void EveryAuthoredContentNameAndDescriptionHasAllThreeTranslations()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection("GameText");
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Configs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var property = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(path)).GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.String ||
                        (property.name != "_displayName" && property.name != "_description") || string.IsNullOrWhiteSpace(property.stringValue)) continue;
                    foreach (string code in new[] { "ru", "en", "tr" })
                    {
                        var table = collection.GetTable(new LocaleIdentifier(code)) as StringTable;
                        Assert.That(table.GetEntry(property.stringValue)?.Value, Is.Not.Null.And.Not.Empty, path + " / " + code);
                    }
                    count++;
                }
            }
            Assert.That(count, Is.GreaterThan(0));
        }

        [TestCase("1.MainMenu")][TestCase("2.Base")][TestCase("3.Arena")]
        public void StaticBindingsWorkInEditorAndNeverOverwritePresenterLabels(string sceneName)
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/" + sceneName + ".unity");
            try
            {
                var components = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
                var bindings = components.OfType<LocalizeStringEvent>().ToArray();
                Assert.That(bindings.Length, Is.GreaterThan(0));
                foreach (var binding in bindings)
                {
                    Assert.That(binding.OnUpdateString.GetPersistentTarget(0), Is.SameAs(binding.GetComponent<TMP_Text>()));
                    Assert.That(binding.OnUpdateString.GetPersistentMethodName(0), Is.EqualTo("set_text"));
                    Assert.That(binding.OnUpdateString.GetPersistentListenerState(0), Is.EqualTo(UnityEngine.Events.UnityEventCallState.EditorAndRuntime));
                    binding.OnUpdateString.Invoke("test");
                    Assert.That(binding.GetComponent<TMP_Text>().text, Is.EqualTo("test"));
                }
                foreach (var view in components.Where(c => c != null && c.GetType().Namespace == "Code.Gameplay"))
                {
                    var property = new SerializedObject(view).GetIterator();
                    while (property.NextVisible(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is TMP_Text label)
                            Assert.That(label.GetComponent<LocalizeStringEvent>(), Is.Null, sceneName + " :: " + view.GetType().Name + "." + property.name);
                }
                if (sceneName == "3.Arena")
                {
                    var speed = components.OfType<BattleSpeedView>().Single();
                    Assert.That(speed.gameObject.activeSelf, Is.True);
                    var button = new SerializedObject(speed).FindProperty("_button").objectReferenceValue as UnityEngine.UI.Button;
                    Assert.That(button, Is.Not.Null);
                    Assert.That(button.gameObject, Is.Not.SameAs(speed.gameObject));
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
