using UnityEngine;

namespace W1.Accessibility.UI
{
    public static class ResponsivePanelSizing
    {
        public static Vector2 Calculate(int screenWidth, int screenHeight, Rect safeAreaPixels,
            Vector2 maximumLogicalSize, float logicalMargin)
        {
            if (screenWidth <= 0 || screenHeight <= 0)
                return Vector2.zero;
            float widthScale = screenWidth / ResponsiveCanvasScaler.ReferenceResolution.x;
            float heightScale = screenHeight / ResponsiveCanvasScaler.ReferenceResolution.y;
            float canvasScale = Mathf.Sqrt(Mathf.Max(.0001f, widthScale * heightScale));
            float availableWidth = Mathf.Max(1f, safeAreaPixels.width / canvasScale - logicalMargin * 2f);
            float availableHeight = Mathf.Max(1f, safeAreaPixels.height / canvasScale - logicalMargin * 2f);
            return new Vector2(Mathf.Min(maximumLogicalSize.x, availableWidth),
                Mathf.Min(maximumLogicalSize.y, availableHeight));
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ResponsivePanelFitter : MonoBehaviour
    {
        [SerializeField] private Vector2 maximumLogicalSize = new(1040f, 1000f);
        [SerializeField, Min(0f)] private float logicalMargin = 32f;
        private RectTransform target;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        public void Configure(Vector2 maximumSize, float margin = 32f)
        {
            maximumLogicalSize = maximumSize;
            logicalMargin = Mathf.Max(0f, margin);
            Refresh();
        }

        private void Awake() => target = GetComponent<RectTransform>();
        private void OnEnable() => Refresh();

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
                Refresh();
        }

        public void Refresh()
        {
            if (target == null)
                target = GetComponent<RectTransform>();
            lastSafeArea = Screen.safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            target.sizeDelta = ResponsivePanelSizing.Calculate(Screen.width, Screen.height,
                Screen.safeArea, maximumLogicalSize, logicalMargin);
        }
    }
}
