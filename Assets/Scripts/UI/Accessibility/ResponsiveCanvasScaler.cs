using UnityEngine;
using UnityEngine.UI;

namespace W1.Accessibility.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class ResponsiveCanvasScaler : MonoBehaviour
    {
        public static readonly Vector2 ReferenceResolution = new(1920f, 1080f);
        private CanvasScaler scaler;

        private void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            ApplyContract();
        }

        private void OnValidate()
        {
            if (scaler == null)
                scaler = GetComponent<CanvasScaler>();
            ApplyContract();
        }

        public void ApplyContract()
        {
            if (scaler == null)
                return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
        }
    }
}
