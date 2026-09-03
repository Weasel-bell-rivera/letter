using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using W1.Accessibility.UI;

public sealed class PauseMenuView
{
    private static readonly Color OverlayColor = new(0.015f, 0.02f, 0.03f, 0.88f);
    private static readonly Color PanelColor = new(0.07f, 0.085f, 0.1f, 0.98f);
    private static readonly Color ButtonColor = new(0.14f, 0.17f, 0.2f, 1f);
    private static readonly Color HighlightColor = new(0.28f, 0.55f, 0.62f, 1f);
    private static readonly Color TextColor = new(0.93f, 0.95f, 0.96f, 1f);

    private readonly GameObject root;
    private readonly GameObject mainPanel;
    private readonly GameObject settingsPanel;
    private readonly GameObject quitPanel;
    private readonly Text quitTitle;
    private readonly Text quitMessage;
    private readonly EventSystem eventSystem;
    private readonly InputSystemUIInputModule inputModule;
    private readonly Dictionary<Text, string> localizedLabels = new();
    private InputActionReference pointReference;
    private InputActionReference clickReference;
    private InputActionReference scrollReference;
    private InputActionReference navigateReference;
    private InputActionReference confirmReference;
    private InputActionReference cancelReference;

    public Button ResumeButton { get; }
    public Button SettingsButton { get; }
    public Button RestartButton { get; }
    public Button QuitButton { get; }
    public Button ConfirmQuitButton { get; }
    public Button CancelQuitButton { get; }
    public Button SettingsBackButton { get; }
    public RectTransform SettingsContent { get; }
    public ScrollRect SettingsScrollRect { get; }
    public bool IsVisible => root.activeSelf;

    public PauseMenuView(Transform parent)
    {
        root = CreateUiObject("Pause Menu", parent, typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect);
        Image overlay = root.GetComponent<Image>();
        overlay.color = OverlayColor;
        root.AddComponent<HighContrastGraphic>().Configure(OverlayColor, new Color(0f, 0f, 0f, .96f));

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        root.AddComponent<ResponsiveCanvasScaler>().ApplyContract();

        GameObject safeArea = CreateUiObject("Safe Area", root.transform,
            typeof(RectTransform), typeof(SafeAreaFitter));
        Stretch(safeArea.GetComponent<RectTransform>());
        safeArea.GetComponent<SafeAreaFitter>().Refresh();

        GameObject eventObject = CreateUiObject("Pause Event System", parent,
            typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem = eventObject.GetComponent<EventSystem>();
        inputModule = eventObject.GetComponent<InputSystemUIInputModule>();
        inputModule.enabled = false;

        mainPanel = CreatePanel(safeArea.transform, "Main Panel", new Vector2(620f, 660f));
        AddLocalizedTitle(mainPanel.transform, "pause.title");
        ResumeButton = AddLocalizedButton(mainPanel.transform, "Resume", "pause.resume");
        SettingsButton = AddLocalizedButton(mainPanel.transform, "Settings", "pause.settings");
        RestartButton = AddLocalizedButton(mainPanel.transform, "Restart Room", "pause.restart_room");
        QuitButton = AddLocalizedButton(mainPanel.transform, "Quit to Desktop", "pause.quit_desktop");
        AddLocalizedHint(mainPanel.transform, "pause.main_hint");

        settingsPanel = CreatePanel(safeArea.transform, "Settings Panel", new Vector2(1040f, 1000f));
        settingsPanel.AddComponent<ResponsivePanelFitter>().Configure(new Vector2(1040f, 1000f));
        AddLocalizedTitle(settingsPanel.transform, "settings.title");
        GameObject scrollObject = CreateUiObject("Settings Scroll", settingsPanel.transform,
            typeof(RectTransform), typeof(ScrollRect), typeof(LayoutElement));
        LayoutElement scrollLayout = scrollObject.GetComponent<LayoutElement>();
        scrollLayout.minHeight = 260f;
        scrollLayout.flexibleHeight = 1f;
        SettingsScrollRect = scrollObject.GetComponent<ScrollRect>();
        SettingsScrollRect.horizontal = false;
        SettingsScrollRect.vertical = true;
        SettingsScrollRect.movementType = ScrollRect.MovementType.Clamped;
        SettingsScrollRect.scrollSensitivity = 48f;

        GameObject viewport = CreateUiObject("Viewport", scrollObject.transform,
            typeof(RectTransform), typeof(RectMask2D));
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect);
        viewportRect.offsetMax = new Vector2(-30f, 0f);

        GameObject content = CreateUiObject("Settings Content", viewport.transform,
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        SettingsContent = content.GetComponent<RectTransform>();
        SettingsContent.anchorMin = new Vector2(0f, 1f);
        SettingsContent.anchorMax = new Vector2(1f, 1f);
        SettingsContent.pivot = new Vector2(.5f, 1f);
        SettingsContent.anchoredPosition = Vector2.zero;
        SettingsContent.sizeDelta = Vector2.zero;
        VerticalLayoutGroup settingsLayout = content.GetComponent<VerticalLayoutGroup>();
        settingsLayout.padding = new RectOffset(30, 30, 8, 8);
        settingsLayout.spacing = 6f;
        settingsLayout.childAlignment = TextAnchor.UpperCenter;
        settingsLayout.childControlWidth = true;
        settingsLayout.childControlHeight = true;
        settingsLayout.childForceExpandWidth = true;
        settingsLayout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        SettingsScrollRect.viewport = viewportRect;
        SettingsScrollRect.content = SettingsContent;
        SettingsScrollRect.verticalScrollbar = AddScrollbar(scrollObject.transform);
        SettingsScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        SettingsScrollRect.verticalScrollbarSpacing = 8f;
        SettingsBackButton = AddLocalizedButton(settingsPanel.transform, "Cancel", "quit.cancel");
        AddLocalizedHint(settingsPanel.transform, "settings.cancel_hint");

        quitPanel = CreatePanel(safeArea.transform, "Quit Confirmation", new Vector2(680f, 540f));
        quitTitle = AddLocalizedTitle(quitPanel.transform, "quit.title");
        quitMessage = AddLocalizedBody(quitPanel.transform, "quit.save_notice");
        ConfirmQuitButton = AddLocalizedButton(quitPanel.transform, "Confirm Quit", "quit.confirm");
        CancelQuitButton = AddLocalizedButton(quitPanel.transform, "Cancel Quit", "quit.cancel");
        AddLocalizedHint(quitPanel.transform, "quit.cancel_hint");

        LocalizationService.LocaleChanged += RefreshLocalizedText;
        Hide();
    }

    public void ConfigureInput(InputActionAsset actions)
    {
        inputModule.enabled = false;
        DisposeInputReferences();
        if (actions == null) return;

        pointReference = Reference(actions.FindAction("UI/Point", false));
        clickReference = Reference(actions.FindAction("UI/Click", false));
        scrollReference = Reference(actions.FindAction("UI/Scroll", false));
        navigateReference = Reference(actions.FindAction("UI/Navigate", false));
        confirmReference = Reference(actions.FindAction("UI/Confirm", false));
        // Cancel is handled once by PauseMenuController. Supplying the same action to the
        // UI module as well caused duplicate dispatch and action-map mutation during callbacks.
        cancelReference = null;
        inputModule.point = pointReference;
        inputModule.leftClick = clickReference;
        inputModule.scrollWheel = scrollReference;
        inputModule.move = navigateReference;
        inputModule.submit = confirmReference;
        inputModule.cancel = null;
    }

    public void SetInputEnabled(bool enabled) => inputModule.enabled = enabled;

    public void ShowMain()
    {
        root.SetActive(true);
        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
        quitPanel.SetActive(false);
        Select(ResumeButton.gameObject);
    }

    public void ShowSettings(GameObject preferredSelection)
    {
        root.SetActive(true);
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
        quitPanel.SetActive(false);
        Select(preferredSelection != null ? preferredSelection : SettingsBackButton.gameObject);
    }

    public void ShowQuitConfirmation()
    {
        quitTitle.text = LocalizationService.Get("quit.title");
        quitMessage.text = LocalizationService.Get("quit.save_notice");
        SetButtonLabel(ConfirmQuitButton, LocalizationService.Get("quit.confirm"));
        SetButtonLabel(CancelQuitButton, LocalizationService.Get("quit.cancel"));
        root.SetActive(true);
        mainPanel.SetActive(false);
        settingsPanel.SetActive(false);
        quitPanel.SetActive(true);
        Select(CancelQuitButton.gameObject);
    }

    public void ShowQuitSaveFailure(string error)
    {
        quitTitle.text = LocalizationService.Get("quit.save_failed_title");
        quitMessage.text = LocalizationService.Get("save_flow.save_failed_message");
        SetButtonLabel(ConfirmQuitButton, LocalizationService.Get("quit.retry_save"));
        SetButtonLabel(CancelQuitButton, LocalizationService.Get("save_flow.return_title"));
        Select(ConfirmQuitButton.gameObject);
    }

    public void Hide()
    {
        eventSystem.SetSelectedGameObject(null);
        root.SetActive(false);
    }

    public void ReselectMain() => Select(ResumeButton.gameObject);

    public void Dispose()
    {
        LocalizationService.LocaleChanged -= RefreshLocalizedText;
        inputModule.enabled = false;
        DisposeInputReferences();
    }

    private void Select(GameObject target)
    {
        eventSystem.SetSelectedGameObject(null);
        eventSystem.SetSelectedGameObject(target);
    }

    private void DisposeInputReferences()
    {
        inputModule.point = null;
        inputModule.leftClick = null;
        inputModule.scrollWheel = null;
        inputModule.move = null;
        inputModule.submit = null;
        inputModule.cancel = null;
        DestroyReference(ref pointReference);
        DestroyReference(ref clickReference);
        DestroyReference(ref scrollReference);
        DestroyReference(ref navigateReference);
        DestroyReference(ref confirmReference);
        DestroyReference(ref cancelReference);
    }

    private static InputActionReference Reference(InputAction action)
        => action != null ? InputActionReference.Create(action) : null;

    private static void DestroyReference(ref InputActionReference reference)
    {
        if (reference != null) UnityEngine.Object.Destroy(reference);
        reference = null;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        GameObject panel = CreateUiObject(name, parent, typeof(RectTransform), typeof(Image),
            typeof(VerticalLayoutGroup));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        panel.GetComponent<Image>().color = PanelColor;
        panel.AddComponent<HighContrastGraphic>().Configure(PanelColor, new Color(.01f, .01f, .01f, 1f));
        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(54, 54, 42, 36);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return panel;
    }

    public static Text AddLabel(Transform parent, string name, string value, int fontSize,
        TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        GameObject label = CreateUiObject(name, parent, typeof(RectTransform), typeof(Text),
            typeof(LayoutElement));
        Text text = label.GetComponent<Text>();
        text.font = LocalizedFontProvider.GetFont();
        text.text = value;
        text.fontSize = fontSize;
        text.color = TextColor;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        label.GetComponent<LayoutElement>().minHeight = Mathf.Max(42f, fontSize * 1.8f);
        label.AddComponent<AccessibleTextScale>().SetBaseFontSize(fontSize);
        label.AddComponent<HighContrastGraphic>().Configure(TextColor, Color.white);
        return text;
    }

    public static Button AddButton(Transform parent, string name, string label)
    {
        GameObject buttonObject = CreateUiObject(name, parent, typeof(RectTransform), typeof(Image),
            typeof(Button), typeof(LayoutElement));
        buttonObject.GetComponent<Image>().color = ButtonColor;
        buttonObject.AddComponent<HighContrastGraphic>().Configure(ButtonColor, Color.black);
        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = HighlightColor;
        colors.selectedColor = HighlightColor;
        colors.pressedColor = new Color(.2f, .42f, .48f, 1f);
        colors.disabledColor = new Color(.1f, .11f, .12f, .65f);
        button.colors = colors;
        buttonObject.GetComponent<LayoutElement>().minHeight = 88f;
        Text text = AddLabel(buttonObject.transform, "Label", label, 28, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        AddFocusCue(button);
        return button;
    }

    public static void SetButtonLabel(Button button, string label)
    {
        Text text = button != null ? button.GetComponentInChildren<Text>() : null;
        if (text != null) text.text = label;
    }

    public static GameObject CreateUiObject(string name, Transform parent, params Type[] components)
    {
        GameObject result = new(name, components);
        if (parent != null) result.transform.SetParent(parent, false);
        return result;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Text AddTitle(Transform parent, string value)
    {
        Text title = AddLabel(parent, "Title", value, 42, TextAnchor.MiddleCenter);
        title.fontStyle = FontStyle.Bold;
        title.GetComponent<LayoutElement>().minHeight = 74f;
        return title;
    }

    private static Text AddBody(Transform parent, string value)
    {
        Text body = AddLabel(parent, "Message", value, 24, TextAnchor.MiddleCenter);
        body.GetComponent<LayoutElement>().minHeight = 72f;
        return body;
    }

    private static void AddHint(Transform parent, string value)
    {
        Text hint = AddLabel(parent, "Keyboard Hint", value, 19, TextAnchor.MiddleCenter);
        hint.color = new Color(.68f, .73f, .76f, 1f);
        hint.GetComponent<LayoutElement>().minHeight = 46f;
    }

    private Text AddLocalizedLabel(Transform parent, string name, string key, int fontSize,
        TextAnchor alignment)
    {
        Text text = AddLabel(parent, name, LocalizationService.Get(key), fontSize, alignment);
        localizedLabels.Add(text, key);
        return text;
    }

    private Button AddLocalizedButton(Transform parent, string name, string key)
    {
        Button button = AddButton(parent, name, LocalizationService.Get(key));
        localizedLabels.Add(button.GetComponentInChildren<Text>(), key);
        return button;
    }

    private Text AddLocalizedTitle(Transform parent, string key)
    {
        Text title = AddLocalizedLabel(parent, "Title", key, 42, TextAnchor.MiddleCenter);
        title.fontStyle = FontStyle.Bold;
        title.GetComponent<LayoutElement>().minHeight = 74f;
        return title;
    }

    private Text AddLocalizedBody(Transform parent, string key)
    {
        Text body = AddLocalizedLabel(parent, "Message", key, 24, TextAnchor.MiddleCenter);
        body.GetComponent<LayoutElement>().minHeight = 72f;
        return body;
    }

    private void AddLocalizedHint(Transform parent, string key)
    {
        Text hint = AddLocalizedLabel(parent, "Keyboard Hint", key, 19, TextAnchor.MiddleCenter);
        hint.color = new Color(.68f, .73f, .76f, 1f);
        hint.GetComponent<LayoutElement>().minHeight = 46f;
    }

    private void RefreshLocalizedText()
    {
        Font font = LocalizedFontProvider.GetFont();
        foreach (KeyValuePair<Text, string> binding in localizedLabels)
        {
            if (binding.Key == null) continue;
            binding.Key.font = font;
            binding.Key.text = LocalizationService.Get(binding.Value);
        }
    }

    private static Scrollbar AddScrollbar(Transform parent)
    {
        GameObject scrollbarObject = CreateUiObject("Scrollbar", parent, typeof(RectTransform),
            typeof(Image), typeof(Scrollbar));
        RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = Vector2.one;
        scrollbarRect.pivot = new Vector2(1f, 1f);
        scrollbarRect.offsetMin = new Vector2(-22f, 0f);
        scrollbarRect.offsetMax = Vector2.zero;
        Image track = scrollbarObject.GetComponent<Image>();
        track.color = new Color(.1f, .12f, .14f, .85f);

        GameObject slidingArea = CreateUiObject("Sliding Area", scrollbarObject.transform,
            typeof(RectTransform));
        Stretch(slidingArea.GetComponent<RectTransform>());
        GameObject handle = CreateUiObject("Handle", slidingArea.transform,
            typeof(RectTransform), typeof(Image));
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        Stretch(handleRect);
        handle.GetComponent<Image>().color = new Color(.38f, .62f, .67f, 1f);

        Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handle.GetComponent<Image>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.value = 1f;
        AddFocusCue(scrollbar);
        return scrollbar;
    }

    public static void AddFocusCue(Selectable selectable)
    {
        if (selectable == null || selectable.GetComponent<SelectableFocusCue>() != null)
            return;

        GameObject marker = CreateUiObject("Keyboard Focus Marker", selectable.transform,
            typeof(RectTransform), typeof(Image));
        RectTransform markerRect = marker.GetComponent<RectTransform>();
        markerRect.anchorMin = new Vector2(0f, 0f);
        markerRect.anchorMax = new Vector2(0f, 1f);
        markerRect.pivot = new Vector2(0f, .5f);
        markerRect.offsetMin = new Vector2(8f, 8f);
        markerRect.offsetMax = new Vector2(16f, -8f);
        Image image = marker.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;
        selectable.gameObject.AddComponent<SelectableFocusCue>().Configure(marker);
    }
}

public sealed class PauseMenuScrollIntoView : MonoBehaviour, ISelectHandler
{
    private ScrollRect scrollRect;

    public void Configure(ScrollRect target) => scrollRect = target;

    public void OnSelect(BaseEventData eventData)
    {
        if (scrollRect == null || scrollRect.viewport == null || scrollRect.content == null) return;
        RectTransform item = transform as RectTransform;
        if (item == null || !item.IsChildOf(scrollRect.content)) return;

        Canvas.ForceUpdateCanvases();
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scrollRect.viewport, item);
        Rect viewport = scrollRect.viewport.rect;
        Vector2 anchored = scrollRect.content.anchoredPosition;
        if (bounds.min.y < viewport.yMin)
            anchored.y += viewport.yMin - bounds.min.y;
        else if (bounds.max.y > viewport.yMax)
            anchored.y -= bounds.max.y - viewport.yMax;
        scrollRect.content.anchoredPosition = anchored;
    }
}
