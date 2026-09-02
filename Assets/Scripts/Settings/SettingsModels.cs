using System;

public enum GameDisplayMode
{
    Fullscreen = 0,
    Windowed = 1
}

public readonly struct DisplayResolutionOption : IEquatable<DisplayResolutionOption>
{
    public int Width { get; }
    public int Height { get; }
    public string Label => $"{Width} x {Height}";

    public DisplayResolutionOption(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public bool Equals(DisplayResolutionOption other) => Width == other.Width && Height == other.Height;
    public override bool Equals(object obj) => obj is DisplayResolutionOption other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Width, Height);
    public override string ToString() => Label;

    public static bool operator ==(DisplayResolutionOption left, DisplayResolutionOption right) => left.Equals(right);
    public static bool operator !=(DisplayResolutionOption left, DisplayResolutionOption right) => !left.Equals(right);
}

public sealed class SettingsSnapshot
{
    public GameDisplayMode DisplayMode { get; }
    public DisplayResolutionOption Resolution { get; }
    public int QualityLevel { get; }
    public float MasterVolume { get; }
    public float MusicVolume { get; }
    public float SfxVolume { get; }

    internal SettingsSnapshot(GameDisplayMode displayMode, DisplayResolutionOption resolution,
        int qualityLevel, float masterVolume, float musicVolume, float sfxVolume)
    {
        DisplayMode = displayMode;
        Resolution = resolution;
        QualityLevel = qualityLevel;
        MasterVolume = masterVolume;
        MusicVolume = musicVolume;
        SfxVolume = sfxVolume;
    }

    internal SettingsSnapshot WithDisplayMode(GameDisplayMode value) =>
        new(value, Resolution, QualityLevel, MasterVolume, MusicVolume, SfxVolume);

    internal SettingsSnapshot WithResolution(DisplayResolutionOption value) =>
        new(DisplayMode, value, QualityLevel, MasterVolume, MusicVolume, SfxVolume);

    internal SettingsSnapshot WithQualityLevel(int value) =>
        new(DisplayMode, Resolution, value, MasterVolume, MusicVolume, SfxVolume);

    internal SettingsSnapshot WithMasterVolume(float value) =>
        new(DisplayMode, Resolution, QualityLevel, value, MusicVolume, SfxVolume);

    internal SettingsSnapshot WithMusicVolume(float value) =>
        new(DisplayMode, Resolution, QualityLevel, MasterVolume, value, SfxVolume);

    internal SettingsSnapshot WithSfxVolume(float value) =>
        new(DisplayMode, Resolution, QualityLevel, MasterVolume, MusicVolume, value);
}
