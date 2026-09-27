using System;
using System.Linq;
using Code.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Tests
{
    public sealed class ArenaHudLayoutTests
    {
        [Test]
        public void CatalogCoversEveryCombatClassExactlyOnce()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<UnitClassHudCatalog>("Assets/Configs/UI/HealthView.asset");
            var expected = Enum.GetValues(typeof(UnitClassId)).Cast<UnitClassId>().Where(x => x != UnitClassId.None);
            Assert.That(catalog.Entries.Select(x => x.ClassId), Is.EquivalentTo(expected));
        }

        [Test]
        public void ArenaHeaderAndRosterScrollKeepTheirReferences()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/3.Arena.unity");
            try
            {
                T Find<T>() where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).Single();
                UnityEngine.Object Ref(UnityEngine.Object target, string field) => new SerializedObject(target).FindProperty(field).objectReferenceValue;
                var hud = Find<RunHudView>();
                var button = (Button)Ref(hud, "_returnToLupanariumButton");
                Assert.That(button.transform.parent, Is.SameAs(((TMPro.TMP_Text)Ref(hud, "_waveLabel")).transform.parent));
                var roster = Find<SquadSelectionView>();
                var content = (Transform)Ref(roster, "_cardsRoot");
                var scroll = content.GetComponentInParent<ScrollRect>();
                Assert.That(scroll.content, Is.SameAs(content));
                Assert.That(scroll.movementType, Is.EqualTo(ScrollRect.MovementType.Clamped));
                var card = Ref(roster, "_cardPrefab");
                foreach (var field in new[] { "_descriptionLabel", "_healthLabel", "_damageLabel", "_speedLabel" })
                    Assert.That(Ref(card, field), Is.Not.Null, field);
                var row = Ref(Find<BattleHealthHudView>(), "_rowPrefab");
                Assert.That(Ref(row, "_statsLabel"), Is.Not.Null);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
