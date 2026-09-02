using System;
using UnityEngine;

namespace W1.Accessibility
{
    public enum TextScalePreset
    {
        Percent100 = 100,
        Percent125 = 125,
        Percent150 = 150
    }

    [Serializable]
    public struct AccessibilityPreferences : IEquatable<AccessibilityPreferences>
    {
        [SerializeField] private TextScalePreset textScale;
        [SerializeField] private bool highContrast;
        [SerializeField] private bool reducedMotion;

        public static AccessibilityPreferences Default =>
            new(TextScalePreset.Percent100, false, false);

        public TextScalePreset TextScale => textScale;
        public float TextScaleMultiplier => (int)textScale / 100f;
        public bool HighContrast => highContrast;
        public bool ReducedMotion => reducedMotion;

        public AccessibilityPreferences(TextScalePreset textScale, bool highContrast, bool reducedMotion)
        {
            this.textScale = SanitizeTextScale(textScale);
            this.highContrast = highContrast;
            this.reducedMotion = reducedMotion;
        }

        public AccessibilityPreferences WithTextScale(TextScalePreset value) =>
            new(value, highContrast, reducedMotion);

        public AccessibilityPreferences WithHighContrast(bool value) =>
            new(textScale, value, reducedMotion);

        public AccessibilityPreferences WithReducedMotion(bool value) =>
            new(textScale, highContrast, value);

        public static TextScalePreset SanitizeTextScale(TextScalePreset value)
        {
            return value == TextScalePreset.Percent125 || value == TextScalePreset.Percent150
                ? value
                : TextScalePreset.Percent100;
        }

        public bool Equals(AccessibilityPreferences other) =>
            textScale == other.textScale && highContrast == other.highContrast && reducedMotion == other.reducedMotion;

        public override bool Equals(object obj) => obj is AccessibilityPreferences other && Equals(other);
        public override int GetHashCode() => HashCode.Combine((int)textScale, highContrast, reducedMotion);
    }
}
