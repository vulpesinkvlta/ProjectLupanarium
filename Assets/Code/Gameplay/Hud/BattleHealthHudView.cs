using System;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Code.Gameplay
{
    public sealed class BattleHealthHudView : MonoBehaviour
    {
        private const int InitialRowCapacity = 12;
        private const int MaximumRowCapacity = 64;

        private const int InitialSegmentCapacity = 256;
        private const int MaximumSegmentCapacity = 1500;

        [Header("Configuration")]
        [SerializeField]
        private UnitClassHudCatalog _catalog;

        [Header("Roots")]
        [SerializeField]
        private Transform _playerRowsRoot;

        [SerializeField]
        private Transform _enemyRowsRoot;

        [SerializeField]
        private Transform _poolRoot;

        [Header("Prefabs")]
        [SerializeField]
        private HealthClassRowView _rowPrefab;

        [SerializeField]
        private UnitHealthSegmentView _segmentPrefab;

        private ObjectPool<HealthClassRowView> _rowPool;
        private ObjectPool<UnitHealthSegmentView> _segmentPool;

        public UnitClassHudCatalog Catalog =>
            _catalog != null
                ? _catalog
                : throw new InvalidOperationException(
                    "Unit class HUD catalog is not assigned.");

        private void Awake()
        {
            ValidateReferences();
            CreatePools();
        }

        public HealthClassRowView AcquireRow(
            TeamId team,
            UnitClassHudCatalog.Entry presentation)
        {
            HealthClassRowView row =
                _rowPool.Get();

            Transform targetRoot =
                team == TeamId.Player
                    ? _playerRowsRoot
                    : _enemyRowsRoot;

            row.transform.SetParent(
                targetRoot,
                worldPositionStays: false);

            row.Bind(
                team,
                presentation);

            return row;
        }

        public UnitHealthSegmentView AcquireSegment(
            HealthClassRowView row,
            int unitId)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            UnitHealthSegmentView segment =
                _segmentPool.Get();

            segment.transform.SetParent(
                row.SegmentsRoot,
                worldPositionStays: false);

            segment.Bind(unitId);

            return segment;
        }

        public void ReleaseSegment(
            UnitHealthSegmentView segment)
        {
            if (segment != null)
                _segmentPool.Release(segment);
        }

        public void ReleaseRow(
            HealthClassRowView row)
        {
            if (row != null)
                _rowPool.Release(row);
        }

        private void CreatePools()
        {
            _rowPool =
                new ObjectPool<HealthClassRowView>(
                    createFunc: CreateRow,
                    actionOnGet: OnGetRow,
                    actionOnRelease: OnReleaseRow,
                    actionOnDestroy: OnDestroyRow,
                    collectionCheck: true,
                    defaultCapacity: InitialRowCapacity,
                    maxSize: MaximumRowCapacity);

            _segmentPool =
                new ObjectPool<UnitHealthSegmentView>(
                    createFunc: CreateSegment,
                    actionOnGet: OnGetSegment,
                    actionOnRelease: OnReleaseSegment,
                    actionOnDestroy: OnDestroySegment,
                    collectionCheck: true,
                    defaultCapacity: InitialSegmentCapacity,
                    maxSize: MaximumSegmentCapacity);
        }

        private HealthClassRowView CreateRow()
        {
            HealthClassRowView row =
                Object.Instantiate(
                    _rowPrefab,
                    GetPoolRoot());

            row.gameObject.SetActive(false);
            return row;
        }

        private UnitHealthSegmentView CreateSegment()
        {
            UnitHealthSegmentView segment =
                Object.Instantiate(
                    _segmentPrefab,
                    GetPoolRoot());

            segment.gameObject.SetActive(false);
            return segment;
        }

        private static void OnGetRow(
            HealthClassRowView row)
        {
            row.gameObject.SetActive(true);
        }

        private void OnReleaseRow(
            HealthClassRowView row)
        {
            row.Unbind();

            row.transform.SetParent(
                GetPoolRoot(),
                worldPositionStays: false);

            row.gameObject.SetActive(false);
        }

        private static void OnDestroyRow(
            HealthClassRowView row)
        {
            if (row != null)
                Object.Destroy(row.gameObject);
        }

        private static void OnGetSegment(
            UnitHealthSegmentView segment)
        {
            segment.gameObject.SetActive(true);
        }

        private void OnReleaseSegment(
            UnitHealthSegmentView segment)
        {
            segment.Unbind();

            segment.transform.SetParent(
                GetPoolRoot(),
                worldPositionStays: false);

            segment.gameObject.SetActive(false);
        }

        private static void OnDestroySegment(
            UnitHealthSegmentView segment)
        {
            if (segment != null)
                Object.Destroy(segment.gameObject);
        }

        private Transform GetPoolRoot()
        {
            return _poolRoot != null
                ? _poolRoot
                : transform;
        }

        private void ValidateReferences()
        {
            if (_catalog == null)
                throw new InvalidOperationException(
                    "HUD catalog is not assigned.");

            if (_playerRowsRoot == null)
                throw new InvalidOperationException(
                    "Player rows root is not assigned.");

            if (_enemyRowsRoot == null)
                throw new InvalidOperationException(
                    "Enemy rows root is not assigned.");

            if (_rowPrefab == null)
                throw new InvalidOperationException(
                    "Health class row prefab is not assigned.");

            if (_segmentPrefab == null)
                throw new InvalidOperationException(
                    "Health segment prefab is not assigned.");
        }

        private void OnDestroy()
        {
            _segmentPool?.Clear();
            _rowPool?.Clear();
        }
    }
}
