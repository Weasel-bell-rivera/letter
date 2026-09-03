using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[DefaultExecutionOrder(-11000)]
public sealed class SaveFlowController : MonoBehaviour
{
    private enum ViewMode { Hidden, Title, NewGameConfirmation, SaveFailure }

    private static SaveFlowController instance;
    private SaveService save;
    private GameObject overlay;
    private GameObject indicatorRoot;
    private GameObject panel;
    private Text title;
    private Text message;
    private Text saveIndicator;
    private Button primaryButton;
    private Button secondaryButton;
    private EventSystem eventSystem;
    private InputSystemUIInputModule inputModule;
    private InputActionAsset uiActions;
    private InputActionReference pointReference;
    private InputActionReference clickReference;
    private InputActionReference moveReference;
    private InputActionReference submitReference;
    private InputActionReference cancelReference;
    private InputActionReference borrowedPointReference;
    private InputActionReference borrowedClickReference;
    private InputActionReference borrowedMoveReference;
    private InputActionReference borrowedSubmitReference;
    private InputActionReference borrowedCancelReference;
    private bool borrowedInputModuleEnabled;
    private bool ownsInputHost;
    private bool inputConfigured;
    private bool cancelRequested;
    private ViewMode mode;
    private float indicatorUntil;
    private PlayerController2D suspendedPlayer;
    private MirrorPlayer2D suspendedMirror;
    private bool suspendedPlayerControl;
    private bool suspendedMirrorEnabled;
    private float suspendedTimeScale;
    private bool failureSuspended;
    private bool suspendedFromPauseMenu;

    public static SaveFlowController Instance => instance;
    public bool IsTitleVisible => mode == ViewMode.Title || mode == ViewMode.NewGameConfirmation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject host = new("Save Flow");
        host.AddComponent<SaveFlowController>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        save = SaveService.Instance;
        BuildView();
        save.SaveStarted += OnSaveStarted;
        save.SaveCompleted += OnSaveCompleted;
        save.PersistentSaveFailure += OnPersistentSaveFailure;
        LocalizationService.LocaleChanged += RefreshText;
    }

    private void Start()
    {
        if (!save.StartupFlowSuppressed) ShowTitle();
    }

    private void Update()
    {
        if (save.StartupFlowSuppressed && mode != ViewMode.Hidden)
        {
            HideOverlay();
            return;
        }
        if (!save.StartupFlowSuppressed && mode == ViewMode.Hidden &&
            save.GameplayAuthorized && save.HasPersistentSaveFailure)
        {
            ShowSaveFailure();
            return;
        }
        if (cancelRequested)
        {
            cancelRequested = false;
            if (mode == ViewMode.NewGameConfirmation) ShowTitle();
        }
        if (saveIndicator != null && saveIndicator.gameObject.activeSelf && Time.unscaledTime >= indicatorUntil)
            saveIndicator.gameObject.SetActive(false);
    }

    public void ShowTitle()
    {
        save.ReturnToTitleState();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        mode = ViewMode.Title;
        ConfigureInput();
        overlay.SetActive(true);
        title.text = LocalizationService.Get("save_flow.title");
        message.text = MessageForLoadOutcome(save.LastLoadOutcome);
        ConfigureButton(primaryButton, LocalizationService.Get("save_flow.continue"), ContinueGame,
            save.CanContinue);
        ConfigureButton(secondaryButton, LocalizationService.Get("save_flow.new_game"), RequestNewGame, true);
        Select(save.CanContinue ? primaryButton : secondaryButton);
    }

    private void RequestNewGame()
    {
        if (save.RequiresNewGameConfirmation)
        {
            mode = ViewMode.NewGameConfirmation;
            title.text = LocalizationService.Get("save_flow.new_game_confirm_title");
            message.text = LocalizationService.Get("save_flow.new_game_confirm_message");
            ConfigureButton(primaryButton, LocalizationService.Get("save_flow.confirm"), ConfirmNewGame, true);
            ConfigureButton(secondaryButton, LocalizationService.Get("quit.cancel"), ShowTitle, true);
            Select(secondaryButton);
            return;
        }
        ConfirmNewGame();
    }

    private void ConfirmNewGame()
    {
        if (save.StartNewGame(true))
        {
            HideOverlay();
            return;
        }
        ShowOperationError();
    }

    private void ContinueGame()
    {
        if (save.ContinueGame())
        {
            HideOverlay();
            return;
        }
        ShowOperationError();
    }

    private void RetrySave()
    {
        save.RetrySave();
        if (!save.HasUnsavedChanges)
        {
            ReleaseFailureSuspension(true);
            HideOverlay();
            return;
        }
        ShowSaveFailure();
    }

    private void RetryThenReturnToTitle()
    {
        save.TryPrepareForTitle();
        ReleaseFailureSuspension(false);
        PauseMenuController.Instance?.CloseForTitle();
        ShowTitle();
    }

    private void ShowSaveFailure()
    {
        if (!save.GameplayAuthorized) return;
        SuspendForFailure();
        mode = ViewMode.SaveFailure;
        ConfigureInput();
        overlay.SetActive(true);
        title.text = LocalizationService.Get("save_flow.save_failed_title");
        message.text = LocalizationService.Get("save_flow.save_failed_message");
        ConfigureButton(primaryButton, LocalizationService.Get("save_flow.retry"), RetrySave, true);
        ConfigureButton(secondaryButton, LocalizationService.Get("save_flow.return_title"),
            RetryThenReturnToTitle, true);
        Select(primaryButton);
    }

    private void ShowOperationError()
    {
        mode = ViewMode.Title;
        title.text = LocalizationService.Get("save_flow.error_title");
        message.text = LocalizationService.Get("save_flow.error_message");
        ConfigureButton(primaryButton, LocalizationService.Get("save_flow.continue"), ContinueGame,
            save.CanContinue);
        ConfigureButton(secondaryButton, LocalizationService.Get("save_flow.new_game"), RequestNewGame, true);
        Select(save.CanContinue ? primaryButton : secondaryButton);
    }

    private void OnSaveStarted()
    {
        saveIndicator.text = LocalizationService.Get("save_flow.saving");
        saveIndicator.gameObject.SetActive(true);
        indicatorUntil = Time.unscaledTime + .5f;
    }

    private void OnSaveCompleted(bool success)
    {
        if (!success) return;
        saveIndicator.text = LocalizationService.Get("save_flow.saved");
        saveIndicator.gameObject.SetActive(true);
        indicatorUntil = Time.unscaledTime + .75f;
    }

    private void OnPersistentSaveFailure(string error) => ShowSaveFailure();

    private void RefreshText()
    {
        saveIndicator.text = LocalizationService.Get("save_flow.saving");
        switch (mode)
        {
            case ViewMode.Title:
                ShowTitle();
                break;
            case ViewMode.NewGameConfirmation:
                RequestNewGame();
                break;
            case ViewMode.SaveFailure:
                ShowSaveFailure();
                break;
        }
    }

    private string MessageForLoadOutcome(SaveService.LoadOutcome outcome)
    {
        return outcome switch
        {
            SaveService.LoadOutcome.NoProfile => LocalizationService.Get("save_flow.no_profile"),
            SaveService.LoadOutcome.BackupRecovery => LocalizationService.Get("save_flow.backup_recovered"),
            SaveService.LoadOutcome.CorruptBlocked => LocalizationService.Get("save_flow.corrupt_blocked"),
            SaveService.LoadOutcome.UnsupportedFutureVersion =>
                LocalizationService.Get("save_flow.future_version"),
            _ => LocalizationService.Get("save_flow.ready")
        };
    }

    private void HideOverlay()
    {
        mode = ViewMode.Hidden;
        overlay.SetActive(false);
        if (eventSystem != null) eventSystem.SetSelectedGameObject(null);
        if (inputModule != null) inputModule.enabled = false;
        DisposeInputActions();
    }

    private void SuspendForFailure()
    {
        if (failureSuspended) return;
        failureSuspended = true;
        suspendedTimeScale = Time.timeScale;
        suspendedFromPauseMenu = PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused;
        suspendedPlayer = FindAnyObjectByType<PlayerController2D>();
        suspendedMirror = suspendedPlayer != null ? suspendedPlayer.GetComponent<MirrorPlayer2D>() : null;
        suspendedPlayerControl = suspendedPlayer != null && suspendedPlayer.ControlEnabled;
        suspendedMirrorEnabled = suspendedMirror != null && suspendedMirror.enabled;
        suspendedPlayer?.SetControlEnabled(false);
        if (suspendedMirror != null) suspendedMirror.enabled = false;
        save.SetGameplayPaused(true);
        Time.timeScale = 0f;
    }

    private void ReleaseFailureSuspension(bool resumeGameplay)
    {
        if (!failureSuspended) return;
        Time.timeScale = suspendedTimeScale;
        if (resumeGameplay && !suspendedFromPauseMenu)
        {
            if (suspendedMirror != null) suspendedMirror.enabled = suspendedMirrorEnabled;
            if (suspendedPlayer != null) suspendedPlayer.SetControlEnabled(suspendedPlayerControl);
            save.SetGameplayPaused(false);
        }
        else if (resumeGameplay)
        {
            save.SetGameplayPaused(true);
        }
        suspendedPlayer = null;
        suspendedMirror = null;
        suspendedFromPauseMenu = false;
        failureSuspended = false;
    }

    private void BuildView()
    {
        overlay = Create("Save Flow Overlay", transform, typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        Stretch(overlayRect);
        overlay.GetComponent<Image>().color = new Color(.015f, .02f, .03f, .96f);
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue - 1;
        CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;

        panel = Create("Save Flow Panel", overlay.transform, typeof(RectTransform), typeof(Image),
            typeof(VerticalLayoutGroup));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, .5f);
        panelRect.pivot = new Vector2(.5f, .5f);
        panelRect.sizeDelta = new Vector2(760f, 560f);
        panel.GetComponent<Image>().color = new Color(.07f, .085f, .1f, .98f);
        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(60, 60, 48, 42);
        layout.spacing = 22f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        title = AddText(panel.transform, "Title", 46, 82f, TextAnchor.MiddleCenter);
        message = AddText(panel.transform, "Message", 28, 180f, TextAnchor.MiddleCenter);
        primaryButton = AddButton(panel.transform, "Primary");
        secondaryButton = AddButton(panel.transform, "Secondary");

        indicatorRoot = Create("Save Indicator Canvas", transform, typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler));
        Stretch(indicatorRoot.GetComponent<RectTransform>());
        Canvas indicatorCanvas = indicatorRoot.GetComponent<Canvas>();
        indicatorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        indicatorCanvas.sortingOrder = short.MaxValue - 2;
        CanvasScaler indicatorScaler = indicatorRoot.GetComponent<CanvasScaler>();
        indicatorScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        indicatorScaler.referenceResolution = new Vector2(1920f, 1080f);
        indicatorScaler.matchWidthOrHeight = .5f;

        saveIndicator = AddText(indicatorRoot.transform, "Save Indicator", 24, 48f,
            TextAnchor.MiddleCenter);
        RectTransform indicatorRect = saveIndicator.rectTransform;
        indicatorRect.anchorMin = indicatorRect.anchorMax = new Vector2(1f, 1f);
        indicatorRect.pivot = new Vector2(1f, 1f);
        indicatorRect.anchoredPosition = new Vector2(-36f, -30f);
        indicatorRect.sizeDelta = new Vector2(220f, 48f);
        saveIndicator.gameObject.SetActive(false);
        overlay.SetActive(false);
    }

    private void ConfigureInput()
    {
        DisposeInputActions();
        inputModule = FindAnyObjectByType<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            GameObject inputHost = Create("Save Flow Event System", transform,
                typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem = inputHost.GetComponent<EventSystem>();
            inputModule = inputHost.GetComponent<InputSystemUIInputModule>();
            ownsInputHost = true;
        }
        else
        {
            eventSystem = inputModule.GetComponent<EventSystem>();
            ownsInputHost = false;
            borrowedPointReference = inputModule.point;
            borrowedClickReference = inputModule.leftClick;
            borrowedMoveReference = inputModule.move;
            borrowedSubmitReference = inputModule.submit;
            borrowedCancelReference = inputModule.cancel;
            borrowedInputModuleEnabled = inputModule.enabled;
        }

        uiActions = ScriptableObject.CreateInstance<InputActionAsset>();
        InputActionMap map = uiActions.AddActionMap("SaveFlowUI");
        InputAction point = map.AddAction("Point", InputActionType.PassThrough, "<Pointer>/position");
        InputAction click = map.AddAction("Click", InputActionType.PassThrough, "<Pointer>/press");
        InputAction move = map.AddAction("Navigate", InputActionType.PassThrough);
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/s").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/a").With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d").With("Right", "<Keyboard>/rightArrow");
        InputAction submit = map.AddAction("Submit", InputActionType.Button, "<Keyboard>/enter");
        submit.AddBinding("<Keyboard>/space");
        InputAction cancel = map.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
        cancel.performed += OnCancelPerformed;
        pointReference = InputActionReference.Create(point);
        clickReference = InputActionReference.Create(click);
        moveReference = InputActionReference.Create(move);
        submitReference = InputActionReference.Create(submit);
        cancelReference = InputActionReference.Create(cancel);
        inputModule.point = pointReference;
        inputModule.leftClick = clickReference;
        inputModule.move = moveReference;
        inputModule.submit = submitReference;
        inputModule.cancel = cancelReference;
        map.Enable();
        inputModule.enabled = true;
        inputConfigured = true;
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        if (context.performed) cancelRequested = true;
    }

    private void DisposeInputActions()
    {
        if (inputConfigured && inputModule != null)
        {
            inputModule.point = ownsInputHost ? null : borrowedPointReference;
            inputModule.leftClick = ownsInputHost ? null : borrowedClickReference;
            inputModule.move = ownsInputHost ? null : borrowedMoveReference;
            inputModule.submit = ownsInputHost ? null : borrowedSubmitReference;
            inputModule.cancel = ownsInputHost ? null : borrowedCancelReference;
            inputModule.enabled = ownsInputHost ? false : borrowedInputModuleEnabled;
        }
        DestroyReference(ref pointReference);
        DestroyReference(ref clickReference);
        DestroyReference(ref moveReference);
        DestroyReference(ref submitReference);
        DestroyReference(ref cancelReference);
        if (uiActions != null) Destroy(uiActions);
        uiActions = null;
        borrowedPointReference = null;
        borrowedClickReference = null;
        borrowedMoveReference = null;
        borrowedSubmitReference = null;
        borrowedCancelReference = null;
        inputConfigured = false;
    }

    private static void DestroyReference(ref InputActionReference reference)
    {
        if (reference != null) UnityEngine.Object.Destroy(reference);
        reference = null;
    }

    private void Select(Button button)
    {
        if (eventSystem == null || button == null) return;
        eventSystem.SetSelectedGameObject(null);
        eventSystem.SetSelectedGameObject(button.gameObject);
    }

    private static void ConfigureButton(Button button, string label, UnityEngine.Events.UnityAction action,
        bool interactable)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
        button.interactable = interactable;
        button.GetComponentInChildren<Text>().text = label;
    }

    private static Text AddText(Transform parent, string name, int fontSize, float minHeight,
        TextAnchor alignment)
    {
        GameObject host = Create(name, parent, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        Text text = host.GetComponent<Text>();
        text.font = LocalizedFontProvider.GetFont();
        text.fontSize = fontSize;
        text.color = new Color(.93f, .95f, .96f, 1f);
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        host.GetComponent<LayoutElement>().minHeight = minHeight;
        return text;
    }

    private static Button AddButton(Transform parent, string name)
    {
        GameObject host = Create(name, parent, typeof(RectTransform), typeof(Image), typeof(Button),
            typeof(LayoutElement));
        host.GetComponent<LayoutElement>().minHeight = 72f;
        Image image = host.GetComponent<Image>();
        image.color = new Color(.14f, .17f, .2f, 1f);
        Button button = host.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(.28f, .55f, .62f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(.2f, .42f, .48f, 1f);
        colors.disabledColor = new Color(.1f, .11f, .12f, .65f);
        button.colors = colors;
        Text label = AddText(host.transform, "Label", 30, 60f, TextAnchor.MiddleCenter);
        Stretch(label.rectTransform);
        return button;
    }

    private static GameObject Create(string name, Transform parent, params Type[] components)
    {
        GameObject result = new(name, components);
        if (parent != null) result.transform.SetParent(parent, false);
        return result;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        if (save != null)
        {
            save.SaveStarted -= OnSaveStarted;
            save.SaveCompleted -= OnSaveCompleted;
            save.PersistentSaveFailure -= OnPersistentSaveFailure;
        }
        LocalizationService.LocaleChanged -= RefreshText;
        DisposeInputActions();
        instance = null;
    }
}
