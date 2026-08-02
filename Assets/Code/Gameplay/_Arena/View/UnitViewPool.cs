using System;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Code.Gameplay
{
    public sealed class UnitViewPool : IDisposable
    {
        private const int DefaultCapacity = 256;
        private const int MaximumPoolSize = 1200;

        private readonly ArenaSceneReference _references;
        private readonly ObjectPool<UnitView> _pool;

        public int CountAll => _pool.CountAll;
        public int CountActive => _pool.CountActive;
        public int CountInactive => _pool.CountInactive;

        public UnitViewPool(
            ArenaSceneReference references)
        {
            _references = references ??
                throw new ArgumentNullException(nameof(references));

            _pool = new ObjectPool<UnitView>(
                createFunc: CreateView,
                actionOnGet: OnGetView,
                actionOnRelease: OnReleaseView,
                actionOnDestroy: OnDestroyView,
                collectionCheck: true,
                defaultCapacity: DefaultCapacity,
                maxSize: MaximumPoolSize);
        }

        public UnitView Get(UnitRuntime runtime)
        {
            if (runtime == null)
                throw new ArgumentNullException(nameof(runtime));

            UnitView view = _pool.Get();

            try
            {
                view.Bind(runtime);
                return view;
            }
            catch
            {
                _pool.Release(view);
                throw;
            }
        }

        public void Release(UnitView view)
        {
            if (view == null)
                return;

            _pool.Release(view);
        }

        public void Dispose()
        {
            _pool.Clear();
        }

        private UnitView CreateView()
        {
            UnitView view = Object.Instantiate(
                _references.UnitViewPrefab,
                _references.UnitViewRoot);

            view.gameObject.SetActive(false);

            return view;
        }

        private static void OnGetView(UnitView view)
        {
            view.gameObject.SetActive(true);
        }

        private static void OnReleaseView(UnitView view)
        {
            view.Unbind();
            view.gameObject.SetActive(false);
        }

        private static void OnDestroyView(UnitView view)
        {
            if (view != null)
                Object.Destroy(view.gameObject);
        }
    }
}
