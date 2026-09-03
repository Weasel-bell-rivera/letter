using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using W1.Accessibility;
using W1.Accessibility.UI;

public sealed class AccessibilityFoundationTests
{
    [TestCase(TextScalePreset.Percent100, 1f)]
    [TestCase(TextScalePreset.Percent125, 1.25f)]
    [TestCase(TextScalePreset.Percent150, 1.5f)]
    public void TextScalePresets_HaveApprovedMultipliers(TextScalePreset preset, float expected)
    {
        AccessibilityPreferences value = new(preset, false, false);
        Assert.That(value.TextScaleMultiplier, Is.EqualTo(expected));
    }

    [Test]
    public void InvalidTextScale_FallsBackToOneHundredPercent()
    {
        AccessibilityPreferences value = new((TextScalePreset)999, true, true);
        Assert.That(value.TextScale, Is.EqualTo(TextScalePreset.Percent100));
        Assert.That(value.HighContrast, Is.True);
        Assert.That(value.ReducedMotion, Is.True);
    }

    [Test]
    public void Defaults_AreSafeAndOptIn()
    {
        Assert.That(AccessibilityPreferences.Default.TextScale, Is.EqualTo(TextScalePreset.Percent100));
        Assert.That(AccessibilityPreferences.Default.HighContrast, Is.False);
        Assert.That(AccessibilityPreferences.Default.ReducedMotion, Is.False);
    }

    [Test]
    public void SafeAreaFitter_ConvertsPixelsToNormalizedAnchors()
    {
        GameObject host = new("Safe Area", typeof(RectTransform), typeof(SafeAreaFitter));
        try
        {
            SafeAreaFitter fitter = host.GetComponent<SafeAreaFitter>();
            fitter.Apply(new Rect(100f, 50f, 800f, 400f), 1000, 500);
            RectTransform rect = host.GetComponent<RectTransform>();
            Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(.1f, .1f)));
            Assert.That(rect.anchorMax, Is.EqualTo(new Vector2(.9f, .9f)));
            Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero));
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void ResponsiveCanvasScaler_AppliesSharedContract()
    {
        GameObject host = new("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(ResponsiveCanvasScaler));
        try
        {
            ResponsiveCanvasScaler responsive = host.GetComponent<ResponsiveCanvasScaler>();
            responsive.ApplyContract();
            CanvasScaler scaler = host.GetComponent<CanvasScaler>();
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(.5f));
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void ValidationMatrix_CoversRequiredAspectRatios()
    {
        ResolutionValidationProfile[] expected =
        {
            new(1280, 720, string.Empty),
            new(1920, 1080, string.Empty),
            new(2560, 1440, string.Empty),
            new(2560, 1080, string.Empty),
            new(3440, 1440, string.Empty),
            new(1920, 1200, string.Empty),
            new(1024, 768, string.Empty)
        };

        Assert.That(ResolutionValidationMatrix.Profiles, Has.Length.EqualTo(expected.Length));
        CollectionAssert.AreEqual(expected, ResolutionValidationMatrix.Profiles);
    }

    [Test]
    public void NonColorStateCue_DoesNotChangeSelectableInteractivity()
    {
        GameObject host = new("Cue", typeof(RectTransform), typeof(Button), typeof(NonColorStateCue));
        try
        {
            Button button = host.GetComponent<Button>();
            button.interactable = true;
            host.GetComponent<NonColorStateCue>().SetState(false);
            Assert.That(button.interactable, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void SelectableFocusCue_ShowsAndHidesShapeMarker()
    {
        GameObject host = new("Selectable", typeof(RectTransform), typeof(Button), typeof(SelectableFocusCue));
        GameObject marker = new("Marker", typeof(RectTransform), typeof(Image));
        marker.transform.SetParent(host.transform, false);
        try
        {
            SelectableFocusCue cue = host.GetComponent<SelectableFocusCue>();
            cue.Configure(marker);
            Assert.That(marker.activeSelf, Is.False);
            cue.OnSelect(null);
            Assert.That(marker.activeSelf, Is.True);
            cue.OnDeselect(null);
            Assert.That(marker.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void PreferencesService_PersistsBeforePublishingCurrentState()
    {
        string source = File.ReadAllText(Path.Combine(Application.dataPath,
            "Scripts/Accessibility/AccessibilityPreferencesService.cs"));
        int write = source.IndexOf("store.TryWrite(sanitized", System.StringComparison.Ordinal);
        int publish = source.IndexOf("Current = sanitized", System.StringComparison.Ordinal);
        Assert.That(write, Is.GreaterThanOrEqualTo(0));
        Assert.That(publish, Is.GreaterThan(write));
        StringAssert.Contains("if (!persisted)", source.Substring(write, publish - write));
    }

    [Test]
    public void AuditedPresentationLoops_ConsumeReducedMotionPolicy()
    {
        string[] relativePaths =
        {
            "Scripts/Gameplay/WindColumnVisual2D.cs",
            "Scripts/Gameplay/ConveyorVisual2D.cs",
            "Scripts/Gameplay/FreezingVisual2D.cs",
            "Scripts/Gameplay/WindTurbineSwitch2D.cs",
            "Scripts/Gameplay/GroundFireThrowerEnemy2D.cs",
            "Scripts/Presentation/AmbientSpritePulse2D.cs",
            "Scripts/Presentation/EruptionPresentation2D.cs",
            "Scripts/Presentation/MirrorCloneReadabilityHalo2D.cs"
        };

        foreach (string path in relativePaths)
        {
            string source = File.ReadAllText(Path.Combine(Application.dataPath, path));
            StringAssert.Contains("AccessibilityMotionPolicy", source, path);
        }
    }

    [Test]
    public void SettingsPanelGeometry_FitsEveryRequiredResolution()
    {
        foreach (ResolutionValidationProfile profile in ResolutionValidationMatrix.Profiles)
        {
            Rect safeArea = new(0f, 0f, profile.Width, profile.Height);
            Vector2 size = ResponsivePanelSizing.Calculate(profile.Width, profile.Height, safeArea,
                new Vector2(1040f, 1000f), 32f);
            float canvasScale = Mathf.Sqrt(
                profile.Width / ResponsiveCanvasScaler.ReferenceResolution.x *
                profile.Height / ResponsiveCanvasScaler.ReferenceResolution.y);
            float availableWidth = safeArea.width / canvasScale - 64f;
            float availableHeight = safeArea.height / canvasScale - 64f;
            Assert.That(size.x, Is.Positive, profile.Label);
            Assert.That(size.y, Is.Positive, profile.Label);
            Assert.That(size.x, Is.LessThanOrEqualTo(availableWidth + .01f), profile.Label);
            Assert.That(size.y, Is.LessThanOrEqualTo(availableHeight + .01f), profile.Label);
        }
    }

    [Test]
    public void UltrawideSettingsPanel_ShrinksBelowLegacyFixedHeight()
    {
        Vector2 size = ResponsivePanelSizing.Calculate(2560, 1080,
            new Rect(0f, 0f, 2560f, 1080f), new Vector2(1040f, 1000f), 32f);
        Assert.That(size.y, Is.LessThan(1000f));
    }

    [Test]
    public void StaticNonColorHazardCues_ArePresentInAuditedSources()
    {
        string lava = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Gameplay/RisingLava2D.cs"));
        StringAssert.Contains("CurrentPhase == Phase.Warning", lava);
        StringAssert.DoesNotContain("Lava Warning Shape Cue", lava);
        string conveyor = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Gameplay/ConveyorVisual2D.cs"));
        StringAssert.Contains("indicatorRoot.gameObject.SetActive(active)", conveyor);
        string wind = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Gameplay/WindColumnVisual2D.cs"));
        StringAssert.Contains("ApplyReducedMotionShape", wind);
        string snowmanGate = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Gameplay/SnowmanGate2D.cs"));
        StringAssert.Contains("Waiting X Shape", snowmanGate);
        StringAssert.Contains("Satisfied Check Shape", snowmanGate);
    }
}
