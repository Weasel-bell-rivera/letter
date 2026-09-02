using System;

namespace W1.Accessibility.UI
{
    public readonly struct ResolutionValidationProfile : IEquatable<ResolutionValidationProfile>
    {
        public int Width { get; }
        public int Height { get; }
        public string Label { get; }
        public float AspectRatio => (float)Width / Height;

        public ResolutionValidationProfile(int width, int height, string label)
        {
            Width = width;
            Height = height;
            Label = label;
        }

        public bool Equals(ResolutionValidationProfile other) => Width == other.Width && Height == other.Height;
        public override bool Equals(object obj) => obj is ResolutionValidationProfile other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Width, Height);
    }

    public static class ResolutionValidationMatrix
    {
        public static readonly ResolutionValidationProfile[] Profiles =
        {
            new(1280, 720, "16:9 low-resolution validation profile"),
            new(1920, 1080, "16:9 reference profile"),
            new(2560, 1440, "16:9 high-resolution validation profile"),
            new(2560, 1080, "ultrawide 21:9 validation profile"),
            new(3440, 1440, "ultrawide 21:9 high-resolution validation profile"),
            new(1920, 1200, "16:10 validation profile"),
            new(1024, 768, "4:3 validation profile")
        };
    }
}
