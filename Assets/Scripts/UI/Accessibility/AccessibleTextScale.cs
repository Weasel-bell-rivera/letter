using UnityEngine;
using UnityEngine.UI;

namespace W1.Accessibility.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    public sealed class AccessibleTextScale : MonoBehaviour
    {
        [SerializeField, Min(1)] private int baseFontSize;
        private Text target;
        private AccessibilityPreferencesService service;

        private void Awake()
        {
            target = GetComponent<Text>();
            if (baseFontSize <= 0)
                baseFontSize = Mathf.Max(1, target.fontSize);
        }

        private void OnEnable()
        {
            service = AccessibilityPreferencesService.Instance;
            service.Changed += Apply;
            Apply(service.Current);
        }

        private void OnDisable()
        {
            if (service != null)
                service.Changed -= Apply;
            service = null;
        }

        public void SetBaseFontSize(int value)
        {
            baseFontSize = Mathf.Max(1, value);
            if (isActiveAndEnabled && service != null)
                Apply(service.Current);
        }

        private void Apply(AccessibilityPreferences preferences)
        {
            if (target == null)
                target = GetComponent<Text>();
            target.fontSize = Mathf.Max(1, Mathf.RoundToInt(baseFontSize * preferences.TextScaleMultiplier));
        }
    }
}
