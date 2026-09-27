using UnityEngine;

namespace Code.Gameplay
{
    public enum BlessingEffect { Heal, Tar, Haste }

    [CreateAssetMenu(menuName = "Lupanarium/Blessing")]
    public sealed class BlessingConfig : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField, Min(1)] private int _price = 50;
        [SerializeField] private BlessingEffect _effect;
        [SerializeField, Min(.1f)] private float _radius = 2;
        [SerializeField, Min(0)] private float _power = 30;
        [SerializeField, Min(.1f)] private float _duration = 5;
        [SerializeField, Range(.05f, 5)] private float _speedFactor = .5f;
        [SerializeField] private Color _color = Color.green;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public int Price => Mathf.Max(1, _price);
        public BlessingEffect Effect => _effect;
        public float Radius => Mathf.Max(.1f, _radius);
        public float Power => Mathf.Max(0, _power);
        public float Duration => Mathf.Max(.1f, _duration);
        public float SpeedFactor => _speedFactor;
        public Color Color => _color;
    }
}
