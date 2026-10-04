using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Code.Gameplay;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using L10n = Code.Gameplay.L10n;

namespace Code.Editor
{
    // Explicitly armed Editor-only smoke check. Never included in a player build.
    [InitializeOnLoad]
    public static class LocalizationPlayProbe
    {
        static LocalizationPlayProbe() { EditorApplication.update += Tick; }
        private static void Tick()
        {
            if (!SessionState.GetBool("LocalizationQA.armed", false) || !EditorApplication.isPlaying || !L10n.IsReady) return;
            SessionState.SetBool("LocalizationQA.armed", false);
            var report = new List<string>();
            try
            {
                var view = UnityEngine.Object.FindFirstObjectByType<MainMenuView>();
                var settings = (GameObject)new SerializedObject(view).FindProperty("_settingsPanel").objectReferenceValue;
                settings.SetActive(true);
                var controls = UnityEngine.Object.FindFirstObjectByType<LanguageSettingsView>();
                var collection = LocalizationEditorSettings.GetStringTableCollection("GameText");
                foreach (string code in new[] { "ru", "en", "tr" })
                {
                    string field = code == "ru" ? "_russian" : code == "en" ? "_english" : "_turkish";
                    ((UnityEngine.UI.Button)new SerializedObject(controls).FindProperty(field).objectReferenceValue).onClick.Invoke();
                    var table = collection.GetTable(new LocaleIdentifier(code)) as StringTable;
                    if (PlayerPrefs.GetString(L10n.PreferenceKey) != code) throw new Exception("Choice was not saved.");
                    int count = 0;
                    foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Configs" }))
                    {
                        var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                        var serialized = new SerializedObject(asset);
                        foreach (var pair in new[] { ("_displayName", "DisplayName"), ("_description", "Description") })
                        {
                            var source = serialized.FindProperty(pair.Item1);
                            var property = asset.GetType().GetProperty(pair.Item2);
                            if (source == null || property == null || string.IsNullOrWhiteSpace(source.stringValue)) continue;
                            var actual = property.GetValue(asset) as string;
                            var expected = table.GetEntry(source.stringValue)?.Value;
                            if (actual != expected || string.IsNullOrEmpty(actual)) throw new Exception(asset.name + ": " + pair.Item2 + " = " + actual);
                            count++;
                        }
                        if (asset is UnitClassHudCatalog hud)
                            foreach (var entry in hud.Entries)
                            {
                                if (string.IsNullOrEmpty(entry.DisplayName)) throw new Exception("Missing unit name");
                                if (code != "ru" && System.Text.RegularExpressions.Regex.IsMatch(entry.DisplayName + entry.Description, "[А-Яа-я]"))
                                    throw new Exception("Untranslated unit: " + entry.ClassId);
                            }
                    }
                    report.Add(code + ": " + count + " config getters and all unit class names/descriptions translated; preference saved.");
                    report.Add(string.Join(" | ", view.GetComponentsInChildren<TMP_Text>().Select(t => t.text)));
                }
                L10n.UseAutomaticLanguage();
                if (!L10n.IsAutomatic) throw new Exception("Auto setting was not restored.");
                report.Add("PASS: menu opened directly, all 3 language buttons, content getters, automatic mode.");
            }
            catch (Exception e) { report.Add("FAIL: " + e); }
            File.WriteAllLines("Logs/localization-play-check.txt", report);
        }
    }
}
