using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Gameplay
{
    public sealed class HealthClassRowView : MonoBehaviour
    {
        [Header("Class")]
        [SerializeField]
        private Image _classIcon;

        [SerializeField]
        private TMP_Text _classNameLabel;

        [SerializeField]
        private TMP_Text _countLabel;

        [Header("Segments")]
        [SerializeField]
        private Transform _segmentsRoot;

        [SerializeField]
        private GridLayoutGroup _segmentsGrid;

        public TeamId Team { get; private set; }
        public UnitClassId ClassId { get; private set; }

        public Transform SegmentsRoot =>
            _segmentsRoot;

        public void Bind(
            TeamId team,
            UnitClassHudCatalog.Entry presentation)
        {
            Team = team;
            ClassId = presentation.ClassId;

            gameObject.name =
                $"HealthRow_{team}_{ClassId}";

            _classNameLabel.text =
                presentation.DisplayName;

            if (_classIcon != null)
            {
                _classIcon.sprite =
                    presentation.Icon;

                _classIcon.color =
                    presentation.AccentColor;

                _classIcon.enabled =
                    presentation.Icon != null;
            }

            ConfigureAlignment(team);
        }

        public void SetCount(
            int alive,
            int total)
        {
            if (_countLabel == null)
                return;

            _countLabel.text =
                $"{alive}/{total}";
        }

        public void Unbind()
        {
            Team = default;
            ClassId = UnitClassId.None;

            if (_classNameLabel != null)
                _classNameLabel.text = string.Empty;

            if (_countLabel != null)
                _countLabel.text = string.Empty;

            if (_classIcon != null)
            {
                _classIcon.sprite = null;
                _classIcon.enabled = false;
            }

            gameObject.name =
                nameof(HealthClassRowView);
        }

        private void ConfigureAlignment(
            TeamId team)
        {
            bool isPlayer =
                team == TeamId.Player;

            if (_segmentsGrid != null)
            {
                _segmentsGrid.startCorner =
                    isPlayer
                        ? GridLayoutGroup.Corner.UpperLeft
                        : GridLayoutGroup.Corner.UpperRight;

                _segmentsGrid.childAlignment =
                    isPlayer
                        ? TextAnchor.UpperLeft
                        : TextAnchor.UpperRight;
            }

            if (_classNameLabel != null)
            {
                _classNameLabel.alignment =
                    isPlayer
                        ? TextAlignmentOptions.Left
                        : TextAlignmentOptions.Right;
            }

            if (_countLabel != null)
            {
                _countLabel.alignment =
                    isPlayer
                        ? TextAlignmentOptions.Left
                        : TextAlignmentOptions.Right;
            }
        }
    }
}
