using UnityEngine;

namespace W1.Accessibility.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform target;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake() => target = GetComponent<RectTransform>();
        private void OnEnable() => Refresh();

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
                Refresh();
        }

        public void Refresh() => Apply(Screen.safeArea, Screen.width, Screen.height);

        public void Apply(Rect safeArea, int screenWidth, int screenHeight)
        {
            if (target == null)
                target = GetComponent<RectTransform>();
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(screenWidth, screenHeight);
            if (screenWidth <= 0 || screenHeight <= 0)
                return;

            Vector2 min = new(safeArea.xMin / screenWidth, safeArea.yMin / screenHeight);
            Vector2 max = new(safeArea.xMax / screenWidth, safeArea.yMax / screenHeight);
            target.anchorMin = new Vector2(Mathf.Clamp01(min.x), Mathf.Clamp01(min.y));
            target.anchorMax = new Vector2(Mathf.Clamp01(max.x), Mathf.Clamp01(max.y));
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }
    }
}
