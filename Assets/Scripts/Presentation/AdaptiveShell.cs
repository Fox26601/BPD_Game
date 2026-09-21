using UnityEngine;
using UnityEngine.UI;

namespace BPD.Presentation
{
    /// <summary>
    /// Keeps HUD inside Screen.safeArea and notifies listeners when portrait/landscape flips.
    /// </summary>
    public sealed class AdaptiveShell : MonoBehaviour
    {
        public const float PortraitBreakpoint = 1f;

        RectTransform _safeArea;
        Rect _lastSafeArea;
        Vector2Int _lastScreen;
        bool _wasPortrait = true;

        public bool IsPortrait { get; private set; } = true;
        public event System.Action<bool> OrientationChanged;

        public void Bind(RectTransform safeArea)
        {
            _safeArea = safeArea;
            ApplySafeArea(force: true);
        }

        void LateUpdate()
        {
            ApplySafeArea(force: false);
        }

        void ApplySafeArea(bool force)
        {
            if (_safeArea == null)
            {
                return;
            }

            var safe = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (!force && safe == _lastSafeArea && size == _lastScreen)
            {
                return;
            }

            _lastSafeArea = safe;
            _lastScreen = size;

            var anchorMin = safe.position;
            var anchorMax = safe.position + safe.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
            _safeArea.anchorMin = anchorMin;
            _safeArea.anchorMax = anchorMax;
            _safeArea.offsetMin = Vector2.zero;
            _safeArea.offsetMax = Vector2.zero;

            bool portrait = Screen.height >= Screen.width * PortraitBreakpoint;
            IsPortrait = portrait;
            if (force || portrait != _wasPortrait)
            {
                _wasPortrait = portrait;
                OrientationChanged?.Invoke(portrait);
            }
        }
    }
}
