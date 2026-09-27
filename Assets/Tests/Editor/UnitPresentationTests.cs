using System.Linq;
using Code.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Code.Tests
{
    public sealed class UnitPresentationTests
    {
        private GameObject _instance;
        private UnitView _view;
        private UnitRuntime _runtime;
        private static Object Ref(Object target, string field) => new SerializedObject(target).FindProperty(field).objectReferenceValue;

        [SetUp]
        public void SetUp()
        {
            _instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/UnitView.prefab"));
            _view = _instance.GetComponent<UnitView>();
            var config = AssetDatabase.LoadAssetAtPath<UnitConfig>("Assets/Configs/Gladiators/Murmillo.asset");
            _runtime = new UnitRuntime(1, TeamId.Player, Vector2.zero,
                new UnitDefinition("murmillo", UnitClassId.Murmillo, new UnitStatsBuilder().Build(config, new ModifierSet()), 0, default));
            _view.Bind(_runtime);
        }

        [TearDown] public void TearDown() => Object.DestroyImmediate(_instance);

        [Test]
        public void HoppingMovesOnlyVisualChildAndKeepsGroundSorting()
        {
            var visual = (Transform)Ref(_view, "_visual");
            var original = visual.localPosition;
            _runtime.SetState(UnitState.Moving);
            _runtime.SetPosition(new Vector2(.12f, 0));
            _view.SetVisualPosition(_runtime.Position, true);
            _view.TickVisuals(.1f);
            Assert.That(_view.transform.position, Is.EqualTo((Vector3)_runtime.Position));
            Assert.That(visual.localPosition, Is.Not.EqualTo(original));
            Assert.That(visual.localRotation, Is.Not.EqualTo(Quaternion.identity));
            Assert.That(((SpriteRenderer)Ref(_view, "_spriteRenderer")).sortingOrder, Is.EqualTo(20));
        }

        [Test]
        public void InstantStrikeIsDetectedAndPoolingClearsAttackAndDeath()
        {
            _runtime.BeginWindup(0);
            _runtime.StartAttackCooldown();
            _view.TickVisuals(.01f);
            Assert.That(_view.IsAttackAnimation, Is.True);
            _view.TickVisuals(1);
            Assert.That(_view.IsAttackAnimation, Is.False);
            _view.PlayDeath();
            _view.TickVisuals(.2f);
            _view.Unbind();
            _view.Bind(_runtime);
            Assert.That(_view.IsAttackAnimation, Is.False);
            Assert.That(((SpriteRenderer)Ref(_view, "_spriteRenderer")).color.a, Is.EqualTo(1));
            Assert.That(((Transform)Ref(_view, "_visual")).localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void BaseIdleDoesNotRequireCombatRuntimeOrMoveGroundPosition()
        {
            _view.BindDisplay(UnitClassId.Murmillo, "Мурмиллон", Color.cyan, 2, true);
            var position = _view.transform.position;
            _view.TickVisuals(.2f);
            Assert.That(_view.Runtime, Is.Null);
            Assert.That(_view.transform.position, Is.EqualTo(position));
            Assert.That(((TMPro.TMP_Text)Ref(_view, "_nameLabel")).gameObject.activeSelf, Is.True);
        }

        [Test]
        public void BothScenesReferenceExactlyTheSamePrefab()
        {
            foreach (var path in new[] { "Assets/Scenes/2.Base.unity", "Assets/Scenes/3.Arena.unity" })
            {
                var scene = EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    var references = path.Contains("Base")
                        ? scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BaseSquadView>(true)).Select(v => Ref(v, "_unitPrefab"))
                        : scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ArenaSceneReference>(true)).Select(v => Ref(v, "_unitViewPrefab"));
                    Assert.That(references, Is.Not.Empty);
                    foreach (var reference in references)
                        Assert.That(AssetDatabase.GetAssetPath(reference), Is.EqualTo("Assets/Prefabs/Units/UnitView.prefab"));
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
        }

        [Test]
        public void SpriteClipsSwitchOnStrikeAndUseUntintedArt()
        {
            var catalog = ScriptableObject.CreateInstance<UnitAnimationCatalog>();
            var texture = new Texture2D(4, 4);
            var idle = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * .5f);
            var attack = Sprite.Create(texture, new Rect(2, 0, 2, 2), Vector2.one * .5f);
            try
            {
                var data = new SerializedObject(catalog);
                var entries = data.FindProperty("_entries"); entries.arraySize = 1;
                var entry = entries.GetArrayElementAtIndex(0);
                entry.FindPropertyRelative("ClassId").intValue = (int)UnitClassId.Murmillo;
                entry.FindPropertyRelative("Scale").floatValue = 1;
                entry.FindPropertyRelative("FacesRight").boolValue = true;
                foreach (var field in new[] { "IdleWalk", "Attack" })
                {
                    var frames = entry.FindPropertyRelative(field); frames.arraySize = 1;
                    frames.GetArrayElementAtIndex(0).objectReferenceValue = field == "Attack" ? attack : idle;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                var viewData = new SerializedObject(_view);
                viewData.FindProperty("_animations").objectReferenceValue = catalog;
                viewData.ApplyModifiedPropertiesWithoutUndo();
                _view.Bind(_runtime);
                var renderer = (SpriteRenderer)Ref(_view, "_spriteRenderer");
                Assert.That(renderer.sprite, Is.SameAs(idle));
                Assert.That(renderer.color, Is.EqualTo(Color.white));
                _runtime.StartAttackCooldown();
                _view.TickVisuals(.01f);
                Assert.That(renderer.sprite, Is.SameAs(attack));
                _view.TickVisuals(1);
                Assert.That(renderer.sprite, Is.SameAs(idle));
            }
            finally
            {
                Object.DestroyImmediate(catalog); Object.DestroyImmediate(idle);
                Object.DestroyImmediate(attack); Object.DestroyImmediate(texture);
            }
        }
    }
}
