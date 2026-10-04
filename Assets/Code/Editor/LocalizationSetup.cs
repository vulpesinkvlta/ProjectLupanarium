using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Code.Gameplay;
using L10n = Code.Gameplay.L10n;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Code.Editor
{
    public static class LocalizationSetup
    {
        public const string SourcePath = "Tools/Localization/translations.json";
        [Serializable] public sealed class Source { public Entry[] entries; }
        [Serializable] public sealed class Entry { public string key, ru, en, tr, context; }
        private static readonly string[] Languages = { "ru", "en", "tr" };
        private static readonly string[] Scenes = { "Assets/Scenes/1.MainMenu.unity", "Assets/Scenes/2.Base.unity", "Assets/Scenes/3.Arena.unity" };

        [MenuItem("Gladiator Game/Localization/Import translations JSON")]
        public static void Import()
        {
            Directory.CreateDirectory("Assets/Localization/Tables");
            Directory.CreateDirectory("Assets/Localization/Locales");
            AssetDatabase.Refresh();
            var settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>("Assets/Localization/LocalizationSettings.asset");
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                AssetDatabase.CreateAsset(settings, "Assets/Localization/LocalizationSettings.asset");
            }
            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            foreach (string code in Languages)
            {
                if (LocalizationEditorSettings.GetLocales().Any(x => x.Identifier.Code == code)) continue;
                var locale = Locale.CreateLocale(code);
                AssetDatabase.CreateAsset(locale, $"Assets/Localization/Locales/{code}.asset");
                LocalizationEditorSettings.AddLocale(locale);
            }
            var collection = LocalizationEditorSettings.GetStringTableCollection(L10n.TableName)
                ?? LocalizationEditorSettings.CreateStringTableCollection(L10n.TableName, "Assets/Localization/Tables");
            var source = JsonUtility.FromJson<Source>(File.ReadAllText(SourcePath));
            foreach (string code in Languages)
            {
                var table = collection.GetTable(new LocaleIdentifier(code)) as StringTable;
                if (table == null) table = collection.AddNewTable(new LocaleIdentifier(code)) as StringTable;
                foreach (var entry in source.entries)
                {
                    string value = code == "ru" ? entry.ru : code == "tr" ? entry.tr : entry.en;
                    if (string.IsNullOrEmpty(value)) throw new InvalidDataException($"Missing {code}: {entry.key}");
                    table.AddEntry(entry.key, value).IsSmart = false; // Runtime templates use string.Format, never Smart Format.
                }
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(collection);
            EditorUtility.SetDirty(collection.SharedData);
            LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(typeof(LocalizationSetup), collection);
            AssetDatabase.SaveAssets();
            Debug.Log($"Imported {source.entries.Length} translations in each of ru/en/tr.");
        }

        [MenuItem("Gladiator Game/Localization/Set up UI and battle speed")]
        public static void SetUp()
        {
            Import();
            var font = CreateFont();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var report = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try { Localize(root, font, report, path); PrefabUtility.SaveAsPrefabAsset(root, path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach (string path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                if (path.Contains("3.Arena")) CreateSpeedControl(font);
                if (path.Contains("1.MainMenu")) CreateLanguageControls(scene, font);
                var roots = scene.GetRootGameObjects();
                foreach (var root in roots) Localize(root, font, report, path, roots);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            File.WriteAllLines("Tools/Localization/ui-binding-report.txt", report);
            AssetDatabase.SaveAssets();
            Debug.Log("UI localization and battle speed are configured. See Tools/Localization/ui-binding-report.txt.");
        }

        private static TMP_FontAsset CreateFont()
        {
            const string path = "Assets/Localization/Fonts/GameFont.asset";
            Directory.CreateDirectory("Assets/Localization/Fonts");
            AssetDatabase.Refresh();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
                if (source == null) throw new InvalidOperationException("Liberation Sans source font is missing.");
                font = TMP_FontAsset.CreateFontAsset(source, 64, 8, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048);
                font.name = "GameFont";
                font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                font.isMultiAtlasTexturesEnabled = true;
                AssetDatabase.CreateAsset(font, path);
                string text = string.Concat(JsonUtility.FromJson<Source>(File.ReadAllText(SourcePath)).entries.Select(e => e.ru + e.en + e.tr));
                font.TryAddCharacters(new string(text.Where(c => !char.IsControl(c)).Distinct().ToArray()), out string missing);
                if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("Font missing: " + missing);
                foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
                font.material.mainTexture = font.atlasTexture;
                AssetDatabase.AddObjectToAsset(font.material, font);
                EditorUtility.SetDirty(font);
            }
            var assets = LocalizationEditorSettings.GetAssetTableCollection("GameAssets")
                ?? LocalizationEditorSettings.CreateAssetTableCollection("GameAssets", "Assets/Localization/Tables");
            var addressables = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            string fontGuid = AssetDatabase.AssetPathToGUID(path);
            addressables.CreateOrMoveEntry(fontGuid, addressables.DefaultGroup);
            foreach (string code in Languages)
            {
                var table = assets.GetTable(new LocaleIdentifier(code)) as AssetTable;
                if (table == null) table = assets.AddNewTable(new LocaleIdentifier(code)) as AssetTable;
                table.AddEntry("font.ui", fontGuid);
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(assets); EditorUtility.SetDirty(assets.SharedData);
            LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(typeof(LocalizationSetup), assets);
            AssetDatabase.SaveAssets();
            if (!AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().Any()) throw new InvalidDataException("Font material was not saved.");
            return font;
        }

        private static void Localize(GameObject root, TMP_FontAsset font, List<string> report, string path, GameObject[] scope = null)
        {
            var dynamicLabels = new HashSet<TMP_Text>();
            // Labels referenced by presenters are refreshed from localized data. They must not receive
            // delayed static events that overwrite a price, unit name or health value after Bind().
            foreach (var behaviour in (scope ?? new[] { root }).SelectMany(x => x.GetComponentsInChildren<MonoBehaviour>(true)))
            {
                if (behaviour == null || behaviour.GetType().Namespace != "Code.Gameplay") continue;
                var serialized = new SerializedObject(behaviour);
                var property = serialized.GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (property.objectReferenceValue is TMP_Text label) dynamicLabels.Add(label);
                    if (property.objectReferenceValue is TMP_FontAsset) property.objectReferenceValue = font;
                    if (property.objectReferenceValue is UnityEngine.UI.Button button &&
                        ((behaviour is LupanariumView && property.name == "_startRunButton") ||
                         (behaviour is PlatformRewardsView && property.name != "_toggle") || behaviour is LanguageSettingsView))
                        foreach (var child in button.GetComponentsInChildren<TMP_Text>(true)) dynamicLabels.Add(child);
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            var collection = LocalizationEditorSettings.GetStringTableCollection(L10n.TableName);
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                label.font = font;
                label.fontSharedMaterial = font.material;
                // Keep the designed size where it fits, but accommodate longer Turkish captions.
                if (label is TextMeshProUGUI)
                {
                    label.fontSizeMax = Mathf.Max(14, label.enableAutoSizing ? label.fontSizeMax : label.fontSize);
                    label.enableAutoSizing = true;
                    label.fontSizeMin = Mathf.Min(label.fontSizeMax, Mathf.Max(10, label.fontSizeMax * .65f));
                }
                else label.enableAutoSizing = false; // World-space unit labels use world units, not UI pixel sizes.
                var binding = label.GetComponent<LocalizeStringEvent>();
                if (dynamicLabels.Contains(label))
                {
                    if (binding != null) UnityEngine.Object.DestroyImmediate(binding);
                    report.Add("DYNAMIC " + path + " :: " + label.name + " :: " + label.text.Replace('\n', ' '));
                    continue;
                }
                string key = binding != null ? binding.StringReference.TableEntryReference.Key : label.text;
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (collection.SharedData.GetEntry(key) == null)
                {
                    report.Add("UNMATCHED " + path + " :: " + label.name + " :: " + key.Replace('\n', ' '));
                    continue;
                }
                if (binding == null) binding = label.gameObject.AddComponent<LocalizeStringEvent>();
                binding.StringReference = new LocalizedString(L10n.TableName, key);
                while (binding.OnUpdateString.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(binding.OnUpdateString, 0);
                var setter = (UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), label, "set_text");
                UnityEventTools.AddPersistentListener(binding.OnUpdateString, setter);
                binding.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
                binding.RefreshString();
                report.Add("BOUND " + path + " :: " + label.name + " :: " + key.Replace('\n', ' '));
            }
        }

        private static void CreateSpeedControl(TMP_FontAsset font)
        {
            var root = GameObject.Find("BattleSpeedCanvas");
            if (root != null) { EnsureSafeArea(root); return; }
            root = new GameObject("BattleSpeedCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster), typeof(BattleSpeedView));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 25;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            var button = new GameObject("Speed", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            button.transform.SetParent(root.transform, false);
            var rect = button.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0, 0);
            rect.anchoredPosition = new Vector2(145, 42); rect.sizeDelta = new Vector2(230, 48);
            button.GetComponent<UnityEngine.UI.Image>().color = new Color(.24f, .20f, .16f);
            var text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); text.transform.SetParent(button.transform, false);
            var tr = text.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(10, 3); tr.offsetMax = new Vector2(-10, -3);
            var label = text.GetComponent<TextMeshProUGUI>(); label.font = font; label.fontSize = 22; label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(.97f, .91f, .78f); label.raycastTarget = false; label.text = "Скорость боя ×1";
            var serialized = new SerializedObject(root.GetComponent<BattleSpeedView>());
            serialized.FindProperty("_button").objectReferenceValue = button.GetComponent<UnityEngine.UI.Button>();
            serialized.FindProperty("_label").objectReferenceValue = label; serialized.ApplyModifiedPropertiesWithoutUndo();
            EnsureSafeArea(root);
        }

        private static void CreateLanguageControls(UnityEngine.SceneManagement.Scene scene, TMP_FontAsset font)
        {
            var view = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MainMenuView>(true)).Single();
            if (view.GetComponent<LanguageSettingsView>() != null) return;
            var menu = new SerializedObject(view);
            var settings = (GameObject)menu.FindProperty("_settingsPanel").objectReferenceValue;
            var panel = settings.transform.Find("Panel") as RectTransform;
            panel.sizeDelta = new Vector2(720, 520);
            var title = panel.Find("Title") as RectTransform;
            title.anchoredPosition = new Vector2(0, 205);
            var hint = panel.Find("Placeholder").GetComponent<TMP_Text>();
            var binding = hint.GetComponent<LocalizeStringEvent>();
            if (binding != null) UnityEngine.Object.DestroyImmediate(binding);
            hint.rectTransform.anchoredPosition = new Vector2(0, -80);
            hint.rectTransform.sizeDelta = new Vector2(630, 80);
            hint.text = "settings.language.hint";
            var heading = UnityEngine.Object.Instantiate(hint, panel);
            heading.name = "LanguageHeading"; heading.rectTransform.anchoredPosition = new Vector2(0, 140);
            heading.rectTransform.sizeDelta = new Vector2(600, 40); heading.text = "settings.language.title";
            var close = (UnityEngine.UI.Button)menu.FindProperty("_closeSettingsButton").objectReferenceValue;
            (close.transform as RectTransform).anchoredPosition = new Vector2(0, -195);
            UnityEngine.UI.Button Choice(string name, string caption, Vector2 position, float width)
            {
                var button = UnityEngine.Object.Instantiate(close, panel);
                button.name = name; button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                var rect = button.transform as RectTransform; rect.anchoredPosition = position; rect.sizeDelta = new Vector2(width, 58);
                var label = button.GetComponentInChildren<TMP_Text>(true);
                var localized = label.GetComponent<LocalizeStringEvent>();
                if (localized != null) UnityEngine.Object.DestroyImmediate(localized);
                label.text = caption; label.font = font;
                return button;
            }
            var russian = Choice("Russian", "Русский", new Vector2(-205, 70), 195);
            var english = Choice("English", "English", new Vector2(0, 70), 195);
            var turkish = Choice("Turkish", "Türkçe", new Vector2(205, 70), 195);
            var automatic = Choice("Automatic", "settings.language.auto", new Vector2(0, 0), 605);
            var component = view.gameObject.AddComponent<LanguageSettingsView>();
            var serialized = new SerializedObject(component);
            serialized.FindProperty("_russian").objectReferenceValue = russian;
            serialized.FindProperty("_english").objectReferenceValue = english;
            serialized.FindProperty("_turkish").objectReferenceValue = turkish;
            serialized.FindProperty("_automatic").objectReferenceValue = automatic;
            serialized.FindProperty("_heading").objectReferenceValue = heading;
            serialized.FindProperty("_hint").objectReferenceValue = hint;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureSafeArea(GameObject root)
        {
            var oldFitter = root.GetComponent<SafeAreaFitter>();
            if (oldFitter != null) UnityEngine.Object.DestroyImmediate(oldFitter);
            if (root.transform.Find("SafeArea") != null) return;
            var safe = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            safe.transform.SetParent(root.transform, false);
            var rect = safe.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                if (child != safe.transform) child.SetParent(safe.transform, false);
        }

        [MenuItem("Gladiator Game/Localization/Validate tables")]
        public static void Validate()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(L10n.TableName);
            if (collection == null) throw new InvalidDataException("GameText is missing.");
            int count = 0;
            foreach (var key in collection.SharedData.Entries)
                foreach (string code in Languages)
                {
                    var table = collection.GetTable(new LocaleIdentifier(code)) as StringTable;
                    if (string.IsNullOrEmpty(table?.GetEntry(key.Id)?.Value)) throw new InvalidDataException($"Missing {code}: {key.Key}");
                    count++;
                }
            Debug.Log($"Localization validation passed: {count} non-empty values.");
        }
    }
}
