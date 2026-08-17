using System;
using UnityEngine;

namespace Code.Gameplay
{
    public class ArenaSceneReference : MonoBehaviour
    {
        [SerializeField] private UnitView _unitViewPrefab;
        [SerializeField] private Transform _unitViewRoot;

        public UnitView UnitViewPrefab =>
            _unitViewPrefab != null
                ? _unitViewPrefab
                : throw new InvalidOperationException(
                    "UnitView prefab is not assigned.");

        public Transform UnitViewRoot =>
            _unitViewRoot != null
                ? _unitViewRoot
                : transform;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_unitViewPrefab == null)
            {
                Debug.LogWarning(
                    "[ArenaSandboxSceneReferences] " +
                    "UnitView prefab is not assigned.",
                    this);
            }
        }
#endif
    }
}
