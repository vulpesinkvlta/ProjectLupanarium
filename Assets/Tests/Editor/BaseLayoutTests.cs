using System;
using System.Linq;
using Code.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Tests
{
    public sealed class BaseLayoutTests
    {
        private Scene _scene;

        [SetUp]
        public void OpenIsolatedScene()
        {
            _scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/2.Base.unity");
        }

        [TearDown]
        public void CloseIsolatedScene() => EditorSceneManager.ClosePreviewScene(_scene);

        private T Find<T>() where T : Component => _scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<T>(true)).Single();

        private static UnityEngine.Object Reference(UnityEngine.Object target, string field) =>
            new SerializedObject(target).FindProperty(field).objectReferenceValue;

        [Test]
        public void AllListsHaveSeparateViewportAndClampedContent()
        {
            var scrolls = _scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<UnityEngine.UI.ScrollRect>(true)).ToArray();
            Assert.That(scrolls.Length, Is.EqualTo(3));
            foreach (var scroll in scrolls)
            {
                Assert.That(scroll.movementType, Is.EqualTo(UnityEngine.UI.ScrollRect.MovementType.Clamped));
                Assert.That(scroll.horizontal, Is.False);
                Assert.That(scroll.content, Is.Not.SameAs(scroll.viewport));
                Assert.That(scroll.content, Is.Not.SameAs(scroll.transform));
                Assert.That(scroll.content.parent, Is.SameAs(scroll.viewport));
                Assert.That(scroll.viewport.GetComponent<UnityEngine.UI.Mask>(), Is.Not.Null);
                var fitter = scroll.content.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                Assert.That(fitter.verticalFit, Is.EqualTo(UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize));
                var layout = scroll.content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                Assert.That(layout.spacing, Is.GreaterThanOrEqualTo(12));
                Assert.That(layout.padding.top, Is.GreaterThan(0));
                Assert.That(layout.childForceExpandHeight, Is.False);
            }
        }

        [Test]
        public void BaseStartsWithAllPanelsHiddenAndThreeTabs()
        {
            var nav = Find<BasePanelNavigation>();
            var data = new SerializedObject(nav);
            Assert.That(((GameObject)Reference(nav, "_panelLayer")).activeSelf, Is.False);
            Assert.That(data.FindProperty("_tabs").arraySize, Is.EqualTo(3));
            var panels = data.FindProperty("_panels");
            Assert.That(panels.arraySize, Is.EqualTo(3));
            for (var i = 0; i < panels.arraySize; i++)
                Assert.That(((GameObject)panels.GetArrayElementAtIndex(i).objectReferenceValue).activeSelf, Is.False);
            Assert.That(Find<UnityEngine.UI.CanvasScaler>().uiScaleMode,
                Is.EqualTo(UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void PanelSwitchesExclusivelyAndRepeatedClickCloses(int index)
        {
            var nav = Find<BasePanelNavigation>();
            nav.TogglePanel((index + 1) % 3);
            nav.TogglePanel(index);
            Assert.That(nav.OpenPanelIndex, Is.EqualTo(index));
            var panels = new SerializedObject(nav).FindProperty("_panels");
            for (var i = 0; i < panels.arraySize; i++)
                Assert.That(((GameObject)panels.GetArrayElementAtIndex(i).objectReferenceValue).activeSelf,
                    Is.EqualTo(i == index));
            nav.TogglePanel(index);
            Assert.That(nav.OpenPanelIndex, Is.EqualTo(-1));
            Assert.That(((GameObject)Reference(nav, "_panelLayer")).activeSelf, Is.False);
        }

        [Test]
        public void SquadShowsEveryFighterAndReusesViews()
        {
            var view = Find<BaseSquadView>();
            var catalog = AssetDatabase.LoadAssetAtPath<RosterCatalog>("Assets/Configs/Run/RosterCatalog.asset");
            var squad = new[] { new SquadEntry(catalog.Entries[0].Unit, 3), new SquadEntry(catalog.Entries[1].Unit, 2) };
            view.Refresh(squad, null);
            var root = (Transform)Reference(view, "_unitsRoot");
            Assert.That(view.DisplayedUnitCount, Is.EqualTo(5));
            Assert.That(root.childCount, Is.EqualTo(5));
            view.Refresh(squad, null);
            Assert.That(root.childCount, Is.EqualTo(5), "Refresh must not duplicate fighters.");
            view.Refresh(new[] { new SquadEntry(catalog.Entries[0].Unit, 1) }, null);
            Assert.That(root.Cast<Transform>().Count(t => t.gameObject.activeSelf), Is.EqualTo(1));
        }

        [Test]
        public void SquadPresenterShowsOnlyCurrentActiveRun()
        {
            var run = new RunState(AssetDatabase.LoadAssetAtPath<RunConfig>("Assets/Configs/Run/RunConfig.asset"));
            var catalog = AssetDatabase.LoadAssetAtPath<RosterCatalog>("Assets/Configs/Run/RosterCatalog.asset");
            var view = Find<BaseSquadView>();
            using var presenter = new BaseSquadPresenter(run, null, view);
            presenter.Start();
            Assert.That(view.DisplayedUnitCount, Is.Zero);
            Assert.That(((GameObject)Reference(view, "_emptyState")).activeSelf, Is.True);
            run.AddUnits(catalog.Entries[0].Unit, 6);
            run.SetProgress(BattleFlowState.Preparation, Array.Empty<UpgradeConfig>(), Array.Empty<ContractOffer>());
            Assert.That(view.DisplayedUnitCount, Is.EqualTo(6));
            run.SetProgress(BattleFlowState.Defeat, Array.Empty<UpgradeConfig>(), Array.Empty<ContractOffer>());
            Assert.That(view.DisplayedUnitCount, Is.Zero);
            Assert.That(((GameObject)Reference(view, "_emptyState")).activeSelf, Is.True);
        }

        [TestCase("Assets/Prefabs/Base/UI/RosterView.prefab")]
        [TestCase("Assets/Prefabs/Base/UI/SchoolBuildingView.prefab")]
        [TestCase("Assets/Prefabs/Base/UI/ItemsRow.prefab")]
        public void CardsHaveExplicitHeightAndReplaceableIconSlot(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight, Is.EqualTo(126));
            Assert.That(prefab.transform.Find("IconFrame"), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<UnityEngine.UI.Button>(true).Length, Is.EqualTo(1));
            Assert.That(prefab.GetComponentsInChildren<TMPro.TMP_Text>(true).All(t => t.font != null), Is.True);
        }
    }
}
