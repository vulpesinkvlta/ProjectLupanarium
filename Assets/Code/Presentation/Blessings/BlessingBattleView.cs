using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Code.Gameplay
{
    public sealed class BlessingBattleView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Transform _cardsRoot;
        [SerializeField] private BlessingCardView _cardPrefab;
        [SerializeField] private TMP_Text _hint;
        [SerializeField] private UnityEngine.UI.Button _cancelButton;
        private readonly List<BlessingCardView> _cards = new();
        private readonly List<RaycastResult> _hits = new();
        private BlessingConfig _selected;
        private LineRenderer _ring;
        private Material _material;
        private Camera _camera;
        private int _selectedFrame;
        private float _flashUntil;
        public Func<Vector2, bool> ValidatePoint;
        public event Action<BlessingConfig> SelectionRequested;
        public event Action<Vector2> CastRequested;

        private void Awake()
        {
            _camera = Camera.main;
            _cancelButton.onClick.AddListener(Cancel);
            var go = new GameObject("BlessingAreaPreview");
            _ring = go.AddComponent<LineRenderer>();
            _material = new Material(Shader.Find("Sprites/Default"));
            _ring.sharedMaterial = _material;
            _ring.useWorldSpace = true;
            _ring.loop = true;
            _ring.positionCount = 64;
            _ring.widthMultiplier = .06f;
            _ring.sortingOrder = 1000;
            _ring.enabled = false;
            _root.SetActive(false);
        }

        public void Build(BlessingCatalog catalog)
        {
            foreach (var config in catalog.Blessings)
            {
                if (config == null) continue;
                var card = Instantiate(_cardPrefab, _cardsRoot);
                card.Bind(config); card.Clicked += Select; _cards.Add(card);
            }
        }
        private void Select(BlessingConfig config) => SelectionRequested?.Invoke(config);
        public void Refresh(LupanariumState school, bool fighting, bool available)
        {
            _root.SetActive(fighting);
            foreach (var card in _cards) card.Refresh(school, false, available);
            if (!available) Cancel();
        }
        public void SelectTarget(BlessingConfig config)
        {
            _selected = config;
            _selectedFrame = Time.frameCount;
            _flashUntil = 0;
            _hint.text = $"{config.DisplayName}: выберите круг на арене. Действует на обе стороны!";
            _cancelButton.gameObject.SetActive(true);
        }
        public void Cancel()
        {
            _selected = null;
            _cancelButton.gameObject.SetActive(false);
            _hint.text = "Выберите благословление, затем место на арене";
            if (_ring != null && Time.unscaledTime >= _flashUntil) _ring.enabled = false;
        }
        public void ShowCast(Vector2 point, BlessingConfig config)
        {
            Cancel();
            DrawRing(point, config.Radius, config.Color);
            _flashUntil = Time.unscaledTime + .6f;
        }
        private void Update()
        {
            if (_selected == null)
            {
                if (_ring != null && Time.unscaledTime >= _flashUntil) _ring.enabled = false;
                return;
            }
            if ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)) { Cancel(); return; }
            if (_camera == null || Pointer.current == null) return;
            var pointer = Pointer.current;
            Vector2 screen = pointer.position.ReadValue();
            var ray = _camera.ScreenPointToRay(screen);
            bool onPlane = new Plane(Vector3.forward, Vector3.zero).Raycast(ray, out float distance);
            Vector2 world = ray.GetPoint(distance);
            bool overUI = false;
            if (EventSystem.current != null)
            {
                _hits.Clear();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, _hits);
                overUI = _hits.Count > 0;
            }
            bool valid = onPlane && !overUI && ValidatePoint != null && ValidatePoint(world);
            if (onPlane && !overUI) DrawRing(world, _selected.Radius, valid ? _selected.Color : Color.red);
            else _ring.enabled = false;
            if (valid && Time.frameCount > _selectedFrame && pointer.press.wasPressedThisFrame)
                CastRequested?.Invoke(world);
        }
        private void DrawRing(Vector2 point, float radius, Color color)
        {
            _ring.enabled = true;
            _ring.startColor = _ring.endColor = color;
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2 / 64;
                _ring.SetPosition(i, new Vector3(point.x + Mathf.Cos(angle) * radius, point.y + Mathf.Sin(angle) * radius, -.1f));
            }
        }
        private void OnDestroy()
        {
            _cancelButton.onClick.RemoveListener(Cancel);
            foreach (var card in _cards) if (card != null) card.Clicked -= Select;
            if (_ring != null) Destroy(_ring.gameObject);
            if (_material != null) Destroy(_material);
        }
    }
}
