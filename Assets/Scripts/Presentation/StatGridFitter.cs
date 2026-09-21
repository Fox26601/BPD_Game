using UnityEngine;
using UnityEngine.UI;

namespace BPD.Presentation
{
    /// <summary>
    /// Keeps a 2-column stats grid cell size matched to the parent width.
    /// </summary>
    public sealed class StatGridFitter : MonoBehaviour
    {
        public const float MinCellHeight = 56f;

        GridLayoutGroup _grid;
        float _lastWidth = -1f;
        float _lastHeight = -1f;

        public void Bind(GridLayoutGroup grid)
        {
            _grid = grid;
        }

        void LateUpdate()
        {
            if (_grid == null)
            {
                return;
            }

            var rt = (RectTransform)_grid.transform;
            float width = rt.rect.width;
            float height = rt.rect.height;
            if (width < 1f)
            {
                return;
            }

            if (Mathf.Abs(width - _lastWidth) < 0.5f && Mathf.Abs(height - _lastHeight) < 0.5f)
            {
                return;
            }

            _lastWidth = width;
            _lastHeight = height;
            float spacing = _grid.spacing.x;
            float pad = _grid.padding.horizontal;
            float cellW = (width - pad - spacing) * 0.5f;
            float cellH = Mathf.Max(MinCellHeight, Mathf.Min(height * 0.48f, 72f));
            _grid.cellSize = new Vector2(Mathf.Max(160f, cellW), cellH);
        }
    }
}
