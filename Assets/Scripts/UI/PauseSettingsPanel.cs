using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using W1.Accessibility;
using W1.Accessibility.UI;

public sealed class PauseSettingsPanel : IDisposable
{
    private readonly Button displayModeButton;
    private readonly Button resolutionButton;
    private readonly Button qualityButton;
    private readonly Button languageButton;
    private readonly Button textScaleButton;
    private readonly Button highContrastButton;
    private readonly Button reducedMotionButton;
    private readonly Slider masterSlider;
    private readonly Slider musicSlider;
    private readonly Slider sfxSlider;
    private readonly Text masterValue;
    private readonly Text musicValue;
    private readonly Text sfxValue;
    private readonly Text masterLabel;
    private readonly Text musicLabel;
    private readonly Text sfxLabel;
    private readonly Text statusText;
    private readonly Button defaultsButton;
    private readonly Button applyButton;
    private SettingsService service;
    private AccessibilityPreferencesService accessibilityService;
    private AccessibilityPreferences accessibilityDraft = AccessibilityPreferences.Default;
    private AccessibilityPreferences originalAccessibility;
    private SettingsSnapshot originalSettings;
    private IReadOnlyList<DisplayResolutionOption> resolutions = Array.Empty<DisplayResolutionOption>();
    private IReadOnlyList<string> qualityNames = Array.Empty<string>();
    private bool editing;
    private bool refreshing;
    private string statusLocalizationKey;

    public event Action ApplyRequested;
    public GameObject FirstSelection => displayModeButton.gameObject;

    public PauseSettingsPanel(RectTransform parent, ScrollRect scrollRect)
    {
        displayModeButton = PauseMenuView.AddButton(parent, "Display Mode", LocalizationService.Get("settings.display_mode"));
        TrackSelection(displayModeButton, scrollRect);
        displayModeButton.GetComponent<LayoutElement>().minHeight = 88f;
        resolutionButton = PauseMenuView.AddButton(parent, "Resolution", LocalizationService.Get("settings.resolution"));
        TrackSelection(resolutionButton, scrollRect);
        resolutionButton.GetComponent<LayoutElement>().minHeight = 88f;
        qualityButton = PauseMenuView.AddButton(parent, "Quality", LocalizationService.Get("settings.quality"));
        TrackSelection(qualityButton, scrollRect);
        qualityButton.GetComponent<LayoutElement>().minHeight = 88f;
        languageButton = PauseMenuView.AddButton(parent, "Language", LocalizationService.Get("settings.language"));
        TrackSelection(languageButton, scrollRect);
        languageButton.GetComponent<LayoutElement>().minHeight = 88f;
        textScaleButton = PauseMenuView.AddButton(parent, "Text Scale", LocalizationService.Get("settings.text_scale"));
        TrackSelection(textScaleButton, scrollRect);
        highContrastButton = PauseMenuView.AddButton(parent, "High Contrast", LocalizationService.Get("settings.high_contrast"));
        TrackSelection(highContrastButton, scrollRect);
        reducedMotionButton = PauseMenuView.AddButton(parent, "Reduced Motion", LocalizationService.Get("settings.reduced_motion"));
        TrackSelection(reducedMotionButton, scrollRect);

        masterSlider = AddVolumeControl(parent, "Master Volume", LocalizationService.Get("settings.master_volume"), out masterLabel, out masterValue);
        TrackSelection(masterSlider, scrollRect);
        musicSlider = AddVolumeControl(parent, "Music Volume", LocalizationService.Get("settings.music_volume"), out musicLabel, out musicValue);
        TrackSelection(musicSlider, scrollRect);
        sfxSlider = AddVolumeControl(parent, "SFX Volume", LocalizationService.Get("settings.sfx_volume"), out sfxLabel, out sfxValue);
        TrackSelection(sfxSlider, scrollRect);

        defaultsButton = PauseMenuView.AddButton(parent, "Restore Defaults", LocalizationService.Get("settings.restore_defaults"));
        TrackSelection(defaultsButton, scrollRect);
        defaultsButton.GetComponent<LayoutElement>().minHeight = 88f;
        applyButton = PauseMenuView.AddButton(parent, "Apply", LocalizationService.Get("settings.apply"));
        TrackSelection(applyButton, scrollRect);
        applyButton.GetComponent<LayoutElement>().minHeight = 88f;
        statusText = PauseMenuView.AddLabel(parent, "Settings Status", string.Empty, 18,
            TextAnchor.MiddleCenter);
        statusText.color = new Color(.92f, .55f, .5f, 1f);
        statusText.GetComponent<HighContrastGraphic>().Configure(
            new Color(.92f, .55f, .5f, 1f), new Color(1f, .9f, .15f, 1f));
        statusText.GetComponent<LayoutElement>().minHeight = 42f;

        displayModeButton.onClick.AddListener(CycleDisplayMode);
        resolutionButton.onClick.AddListener(CycleResolution);
        qualityButton.onClick.AddListener(CycleQuality);
        languageButton.onClick.AddListener(CycleLanguage);
        textScaleButton.onClick.AddListener(CycleTextScale);
        highContrastButton.onClick.AddListener(CycleHighContrast);
        reducedMotionButton.onClick.AddListener(CycleReducedMotion);
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSfxVolume);
        defaultsButton.onClick.AddListener(RestoreDefaults);
        applyButton.onClick.AddListener(() => ApplyRequested?.Invoke());
        LocalizationService.LocaleChanged += RefreshLocalizedText;
        RefreshLocalizedText();
    }

    public bool BeginEdit()
    {
        SettingsService next = SettingsService.Instance;
        AccessibilityPreferencesService nextAccessibility = AccessibilityPreferencesService.Instance;
        if (!SettingsService.IsReady || next == null || !AccessibilityPreferencesService.IsReady || nextAccessibility == null)
            return false;
        BindService(next);
        accessibilityService = nextAccessibility;
        accessibilityDraft = accessibilityService.Current;
        originalAccessibility = accessibilityDraft;
        service.BeginEdit();
        originalSettings = service.AppliedSettings;
        resolutions = service.SupportedResolutions ?? Array.Empty<DisplayResolutionOption>();
        qualityNames = service.QualityLevelNames ?? Array.Empty<string>();
        editing = true;
        statusLocalizationKey = null;
        statusText.text = string.Empty;
        RefreshFromDraft();
        return true;
    }

    public bool ApplyEdit()
    {
        if (!editing || service == null) return false;
        if (accessibilityService == null || !accessibilityService.Apply(accessibilityDraft))
        {
            ShowApplyFailure();
            return false;
        }

        if (service.Apply())
        {
            editing = false;
            statusLocalizationKey = null;
            statusText.text = string.Empty;
            return true;
        }

        // The display service currently exposes an atomic per-file Apply rather than a
        // cross-service transaction. Restore both runtime snapshots and persistence when
        // its second-stage commit fails so Apply/Cancel never report a partial success.
        if (!accessibilityService.Apply(originalAccessibility))
            accessibilityService.RestoreRuntimeSnapshotAfterFailedTransaction(originalAccessibility);
        RestoreOriginalSettings();
        accessibilityDraft = originalAccessibility;
        ShowApplyFailure();
        return false;
    }

    public void CancelEdit()
    {
        if (!editing || service == null) return;
        service.Cancel();
        if (accessibilityService != null)
            accessibilityDraft = accessibilityService.Current;
        editing = false;
        statusLocalizationKey = null;
        statusText.text = string.Empty;
    }

    public void Dispose()
    {
        CancelEdit();
        LocalizationService.LocaleChanged -= RefreshLocalizedText;
        BindService(null);
    }

    private void BindService(SettingsService next)
    {
        if (service == next) return;
        if (service != null)
        {
            service.DraftChanged -= RefreshFromDraft;
            service.AppliedChanged -= OnAppliedChanged;
        }

        service = next;
        if (service != null)
        {
            service.DraftChanged += RefreshFromDraft;
            service.AppliedChanged += OnAppliedChanged;
        }
    }

    private void OnAppliedChanged()
    {
        if (editing) RefreshFromDraft();
    }

    private void CycleDisplayMode()
    {
        if (!editing || service == null) return;
        GameDisplayMode next = service.DraftSettings.DisplayMode == GameDisplayMode.Fullscreen
            ? GameDisplayMode.Windowed
            : GameDisplayMode.Fullscreen;
        service.SetDraftDisplayMode(next);
    }

    private void CycleResolution()
    {
        if (!editing || service == null || resolutions.Count == 0) return;
        int current = IndexOfResolution(service.DraftSettings.Resolution);
        service.SetDraftResolution(resolutions[(current + 1 + resolutions.Count) % resolutions.Count]);
    }

    private void CycleQuality()
    {
        if (!editing || service == null || qualityNames.Count == 0) return;
        int next = (Mathf.Clamp(service.DraftSettings.QualityLevel, 0, qualityNames.Count - 1) + 1) %
                   qualityNames.Count;
        service.SetDraftQualityLevel(next);
    }

    private void CycleLanguage()
    {
        SetLocale(LocalizationService.CurrentLocale == LocalizationService.English
            ? LocalizationService.SimplifiedChinese
            : LocalizationService.English);
    }

    public bool SetLocale(string locale) => LocalizationService.SetLocale(locale);

    private void CycleTextScale()
    {
        if (!editing) return;
        TextScalePreset next = accessibilityDraft.TextScale switch
        {
            TextScalePreset.Percent100 => TextScalePreset.Percent125,
            TextScalePreset.Percent125 => TextScalePreset.Percent150,
            _ => TextScalePreset.Percent100
        };
        accessibilityDraft = accessibilityDraft.WithTextScale(next);
        RefreshAccessibilityDraft();
    }

    private void CycleHighContrast()
    {
        if (!editing) return;
        accessibilityDraft = accessibilityDraft.WithHighContrast(!accessibilityDraft.HighContrast);
        RefreshAccessibilityDraft();
    }

    private void CycleReducedMotion()
    {
        if (!editing) return;
        accessibilityDraft = accessibilityDraft.WithReducedMotion(!accessibilityDraft.ReducedMotion);
        RefreshAccessibilityDraft();
    }

    private void SetMasterVolume(float value)
    {
        if (!refreshing && editing && service != null) service.SetDraftMasterVolume(value);
    }

    private void SetMusicVolume(float value)
    {
        if (!refreshing && editing && service != null) service.SetDraftMusicVolume(value);
    }

    private void SetSfxVolume(float value)
    {
        if (!refreshing && editing && service != null) service.SetDraftSfxVolume(value);
    }

    private void RestoreDefaults()
    {
        if (!editing || service == null) return;
        service.RestoreDefaults();
        accessibilityDraft = AccessibilityPreferences.Default;
        RefreshAccessibilityDraft();
        statusLocalizationKey = "settings.defaults_previewed";
        statusText.text = LocalizationService.Get(statusLocalizationKey);
    }

    private void RefreshFromDraft()
    {
        if (!editing || service == null) return;
        refreshing = true;
        SettingsSnapshot draft = service.DraftSettings;
        SetButtonText(displayModeButton, LocalizationService.Get("settings.display_mode"), draft.DisplayMode == GameDisplayMode.Fullscreen
            ? LocalizationService.Get("settings.fullscreen")
            : LocalizationService.Get("settings.windowed"));
        SetButtonText(resolutionButton, LocalizationService.Get("settings.resolution"), ResolutionLabel(draft.Resolution));
        string quality = qualityNames.Count > 0
            ? qualityNames[Mathf.Clamp(draft.QualityLevel, 0, qualityNames.Count - 1)]
            : LocalizationService.Get("settings.unavailable");
        SetButtonText(qualityButton, LocalizationService.Get("settings.quality"), LocalizeQualityName(quality));
        SetButtonText(languageButton, LocalizationService.Get("settings.language"),
            LocalizationService.Get(LocalizationService.CurrentLocale == LocalizationService.English
                ? "locale.english"
                : "locale.simplified_chinese"));
        RefreshAccessibilityDraft();
        masterSlider.SetValueWithoutNotify(draft.MasterVolume);
        musicSlider.SetValueWithoutNotify(draft.MusicVolume);
        sfxSlider.SetValueWithoutNotify(draft.SfxVolume);
        masterValue.text = Percentage(draft.MasterVolume);
        musicValue.text = Percentage(draft.MusicVolume);
        sfxValue.text = Percentage(draft.SfxVolume);
        refreshing = false;
    }

    private int IndexOfResolution(DisplayResolutionOption target)
    {
        for (int i = 0; i < resolutions.Count; i++)
            if (resolutions[i].Equals(target)) return i;
        return -1;
    }

    private static string ResolutionLabel(DisplayResolutionOption option)
        => string.IsNullOrWhiteSpace(option.Label) ? $"{option.Width} × {option.Height}" : option.Label;

    private static string Percentage(float value) => $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";

    private void RefreshAccessibilityDraft()
    {
        SetButtonText(textScaleButton, LocalizationService.Get("settings.text_scale"),
            $"{(int)accessibilityDraft.TextScale}%");
        SetButtonText(highContrastButton, LocalizationService.Get("settings.high_contrast"),
            LocalizationService.Get(accessibilityDraft.HighContrast ? "settings.on" : "settings.off"));
        SetButtonText(reducedMotionButton, LocalizationService.Get("settings.reduced_motion"),
            LocalizationService.Get(accessibilityDraft.ReducedMotion ? "settings.on" : "settings.off"));
    }

    private static void SetButtonText(Button button, string field, string value)
    {
        Text label = button.GetComponentInChildren<Text>();
        if (label != null) label.text = LocalizationService.Format("settings.field_value", field, value);
    }

    private static Slider AddVolumeControl(Transform parent, string name, string label,
        out Text headingLabel, out Text valueLabel)
    {
        GameObject group = PauseMenuView.CreateUiObject(name, parent, typeof(RectTransform),
            typeof(VerticalLayoutGroup), typeof(LayoutElement));
        VerticalLayoutGroup layout = group.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        group.GetComponent<LayoutElement>().minHeight = 112f;

        GameObject heading = PauseMenuView.CreateUiObject("Heading", group.transform,
            typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        HorizontalLayoutGroup headingLayout = heading.GetComponent<HorizontalLayoutGroup>();
        headingLayout.childControlWidth = true;
        headingLayout.childForceExpandWidth = true;
        heading.GetComponent<LayoutElement>().minHeight = 52f;
        headingLabel = PauseMenuView.AddLabel(heading.transform, "Label", label, 21);
        valueLabel = PauseMenuView.AddLabel(heading.transform, "Value", "100%", 21, TextAnchor.MiddleRight);

        GameObject sliderObject = PauseMenuView.CreateUiObject("Slider", group.transform,
            typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
        sliderObject.GetComponent<LayoutElement>().preferredHeight = 46f;
        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        GameObject background = PauseMenuView.CreateUiObject("Background", sliderObject.transform,
            typeof(RectTransform), typeof(Image));
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, .35f);
        backgroundRect.anchorMax = new Vector2(1f, .65f);
        backgroundRect.offsetMin = new Vector2(8f, 0f);
        backgroundRect.offsetMax = new Vector2(-8f, 0f);
        background.GetComponent<Image>().color = new Color(.13f, .15f, .17f, 1f);
        background.AddComponent<HighContrastGraphic>().Configure(
            new Color(.13f, .15f, .17f, 1f), Color.black);

        GameObject fillArea = PauseMenuView.CreateUiObject("Fill Area", sliderObject.transform,
            typeof(RectTransform));
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, .35f);
        fillAreaRect.anchorMax = new Vector2(1f, .65f);
        fillAreaRect.offsetMin = new Vector2(8f, 0f);
        fillAreaRect.offsetMax = new Vector2(-8f, 0f);
        GameObject fill = PauseMenuView.CreateUiObject("Fill", fillArea.transform,
            typeof(RectTransform), typeof(Image));
        PauseMenuView.Stretch(fill.GetComponent<RectTransform>());
        fill.GetComponent<Image>().color = new Color(.28f, .66f, .72f, 1f);
        fill.AddComponent<HighContrastGraphic>().Configure(
            new Color(.28f, .66f, .72f, 1f), Color.white);

        GameObject handleArea = PauseMenuView.CreateUiObject("Handle Slide Area", sliderObject.transform,
            typeof(RectTransform));
        PauseMenuView.Stretch(handleArea.GetComponent<RectTransform>());
        GameObject handle = PauseMenuView.CreateUiObject("Handle", handleArea.transform,
            typeof(RectTransform), typeof(Image));
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(28f, 40f);
        handle.GetComponent<Image>().color = new Color(.88f, .94f, .95f, 1f);
        handle.AddComponent<HighContrastGraphic>().Configure(
            new Color(.88f, .94f, .95f, 1f), new Color(1f, .9f, .15f, 1f));

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.value = 1f;
        PauseMenuView.AddFocusCue(slider);
        return slider;
    }

    private void RefreshLocalizedText()
    {
        SetPlainButtonText(defaultsButton, LocalizationService.Get("settings.restore_defaults"));
        SetPlainButtonText(applyButton, LocalizationService.Get("settings.apply"));
        masterLabel.text = LocalizationService.Get("settings.master_volume");
        musicLabel.text = LocalizationService.Get("settings.music_volume");
        sfxLabel.text = LocalizationService.Get("settings.sfx_volume");
        if (!string.IsNullOrEmpty(statusLocalizationKey))
            statusText.text = statusLocalizationKey == "settings.apply_failed"
                ? WarningStatus(statusLocalizationKey)
                : LocalizationService.Get(statusLocalizationKey);
        if (editing) RefreshFromDraft();
        else
        {
            SetPlainButtonText(displayModeButton, LocalizationService.Get("settings.display_mode"));
            SetPlainButtonText(resolutionButton, LocalizationService.Get("settings.resolution"));
            SetPlainButtonText(qualityButton, LocalizationService.Get("settings.quality"));
            SetButtonText(languageButton, LocalizationService.Get("settings.language"),
                LocalizationService.Get(LocalizationService.CurrentLocale == LocalizationService.English
                    ? "locale.english"
                    : "locale.simplified_chinese"));
            RefreshAccessibilityDraft();
        }
    }

    private static void SetPlainButtonText(Button button, string value)
    {
        Text label = button.GetComponentInChildren<Text>();
        if (label != null) label.text = value;
    }

    private static string LocalizeQualityName(string quality)
    {
        return quality switch
        {
            "Very Low" => LocalizationService.Get("quality.very_low"),
            "Low" => LocalizationService.Get("quality.low"),
            "Medium" => LocalizationService.Get("quality.medium"),
            "High" => LocalizationService.Get("quality.high"),
            "Very High" => LocalizationService.Get("quality.very_high"),
            "Ultra" => LocalizationService.Get("quality.ultra"),
            _ => quality
        };
    }

    private static string WarningStatus(string localizationKey) => $"⚠ {LocalizationService.Get(localizationKey)}";

    private void ShowApplyFailure()
    {
        statusLocalizationKey = "settings.apply_failed";
        statusText.text = WarningStatus(statusLocalizationKey);
    }

    private void RestoreOriginalSettings()
    {
        if (service == null || originalSettings == null)
            return;
        service.BeginEdit();
        service.SetDraftDisplayMode(originalSettings.DisplayMode);
        service.SetDraftResolution(originalSettings.Resolution);
        service.SetDraftQualityLevel(originalSettings.QualityLevel);
        service.SetDraftMasterVolume(originalSettings.MasterVolume);
        service.SetDraftMusicVolume(originalSettings.MusicVolume);
        service.SetDraftSfxVolume(originalSettings.SfxVolume);
        service.Apply();
        service.BeginEdit();
    }

    private static void TrackSelection(Selectable selectable, ScrollRect scrollRect)
    {
        PauseMenuScrollIntoView tracker = selectable.gameObject.AddComponent<PauseMenuScrollIntoView>();
        tracker.Configure(scrollRect);
    }
}
