using UnityEngine;
using UnityEngine.UI;

namespace W1.Accessibility.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public sealed class HighContrastGraphic : MonoBehaviour
    {
        [SerializeField] private Color standardColor = Color.white;
        [SerializeField] private Color highContrastColor = Color.white;
        private Graphic target;
        private AccessibilityPreferencesService service;

        public void Configure(Color standard, Color highContrast)
        {
            standardColor = standard;
            highContrastColor = highContrast;
            if (target == null)
                target = GetComponent<Graphic>();
            if (service != null)
                Apply(service.Current);
        }

        private void Awake() => target = GetComponent<Graphic>();

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

        private void Apply(AccessibilityPreferences preferences)
        {
            if (target == null)
                target = GetComponent<Graphic>();
            target.color = preferences.HighContrast ? highContrastColor : standardColor;
        }
    }
}
