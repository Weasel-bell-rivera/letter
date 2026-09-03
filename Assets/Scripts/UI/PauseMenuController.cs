using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class PauseMenuController : MonoBehaviour
{
    public enum PanelState
    {
        Closed,
        Main,
        Settings,
        QuitConfirmation
    }

    private static PauseMenuController instance;
    private PauseMenuView view;
    private PauseSettingsPanel settingsPanel;
    private PlayerInput playerInput;
    private PlayerController2D playerController;
    private MirrorPlayer2D mirrorController;
    private InputAction pauseAction;
    private InputAction cancelAction;
    private string previousActionMap;
    private bool previousPlayerControl;
    private bool previousMirrorEnabled;
    private float previousTimeScale;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLockMode;
    private bool paused;
    private bool applicationQuitting;
    private bool openRequested;
    private bool cancelRequested;
    private bool resumeRequested;
    private bool restartRequested;
    private bool quitSaveFailed;

    public static PauseMenuController Instance => instance;
    public bool IsPaused => paused;
    public PanelState CurrentPanel { get; private set; } = PanelState.Closed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateProjectPauseMenu()
    {
        if (instance != null) return;
        GameObject root = new("Project Pause Menu");
        root.AddComponent<PauseMenuController>();
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
        view = new PauseMenuView(transform);
        settingsPanel = new PauseSettingsPanel(view.SettingsContent, view.SettingsScrollRect);
        BindButtons();
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void Start() => EnsureInputSource();

    private void LateUpdate()
    {
        EnsureInputSource();
        ProcessDeferredInputTransitions();
        if (!paused) return;

        ActivateUiInput();
        if (playerController != null && playerController.ControlEnabled)
            playerController.SetControlEnabled(false);
        if (mirrorController != null && mirrorController.enabled)
            mirrorController.enabled = false;
    }

    private void BindButtons()
    {
        view.ResumeButton.onClick.AddListener(ResumeGame);
        view.SettingsButton.onClick.AddListener(OpenSettings);
        view.RestartButton.onClick.AddListener(RestartRoom);
        view.QuitButton.onClick.AddListener(OpenQuitConfirmation);
        view.ConfirmQuitButton.onClick.AddListener(ConfirmQuit);
        view.CancelQuitButton.onClick.AddListener(CancelQuitOrReturnTitle);
        view.SettingsBackButton.onClick.AddListener(CancelSettings);
        settingsPanel.ApplyRequested += ApplySettings;
    }

    private void EnsureInputSource()
    {
        if (playerInput != null && playerInput.isActiveAndEnabled) return;

        PlayerInput next = FindAnyObjectByType<PlayerInput>();
        if (next == playerInput) return;
        UnbindInputSource();
        if (next == null) return;

        playerInput = next;
        playerController = next.GetComponent<PlayerController2D>();
        mirrorController = next.GetComponent<MirrorPlayer2D>();
        InputActionAsset actions = next.actions;
        pauseAction = actions?.FindAction("Player/Pause", false);
        cancelAction = actions?.FindAction("UI/Cancel", false);
        if (pauseAction != null) pauseAction.performed += OnPausePerformed;
        if (cancelAction != null) cancelAction.performed += OnCancelPerformed;
        view.ConfigureInput(actions);

        if (paused)
            ActivateUiInput();
    }

    private void UnbindInputSource()
    {
        if (pauseAction != null) pauseAction.performed -= OnPausePerformed;
        if (cancelAction != null) cancelAction.performed -= OnCancelPerformed;
        pauseAction = null;
        cancelAction = null;
        playerInput = null;
        playerController = null;
        mirrorController = null;
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (!context.performed || paused) return;
        openRequested = true;
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed || !paused) return;
        cancelRequested = true;
    }

    public void OpenPauseMenu()
    {
        if (paused) return;
        EnsureInputSource();
        if (!CanActivateUiInput())
        {
            Debug.LogError("Pause menu requires Player/Pause and the complete keyboard/mouse UI action map.", this);
            return;
        }

        paused = true;
        previousTimeScale = Time.timeScale;
        previousCursorVisible = Cursor.visible;
        previousCursorLockMode = Cursor.lockState;
        previousActionMap = playerInput.currentActionMap != null
            ? playerInput.currentActionMap.name
            : "Player";
        previousPlayerControl = playerController != null && playerController.ControlEnabled;
        previousMirrorEnabled = mirrorController != null && mirrorController.enabled;

        if (playerController != null) playerController.SetControlEnabled(false);
        if (mirrorController != null) mirrorController.enabled = false;
        SaveService.Instance.SetGameplayPaused(true);
        ActivateUiInput();
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        CurrentPanel = PanelState.Main;
        view.ShowMain();
    }

    public void ResumeGame()
    {
        if (!paused) return;
        resumeRequested = true;
    }

    public void CloseForTitle()
    {
        if (!paused) return;
        settingsPanel.CancelEdit();
        RestoreGameplayState();
    }

    public void OpenSettings()
    {
        if (!paused || CurrentPanel != PanelState.Main) return;
        if (!settingsPanel.BeginEdit())
        {
            Debug.LogError("SettingsService is not ready; the Settings panel cannot be opened.", this);
            return;
        }

        CurrentPanel = PanelState.Settings;
        view.ShowSettings(settingsPanel.FirstSelection);
    }

    public void OpenQuitConfirmation()
    {
        if (!paused || CurrentPanel != PanelState.Main) return;
        quitSaveFailed = false;
        CurrentPanel = PanelState.QuitConfirmation;
        view.ShowQuitConfirmation();
    }

    public void RestartRoom()
    {
        if (!paused) return;
        restartRequested = true;
    }

    private void RestartRoomNow()
    {
        RoomResetSystem reset = FindAnyObjectByType<RoomResetSystem>();
        if (reset == null)
        {
            Debug.LogError("Restart Room requires the active room's RoomResetSystem.", this);
            return;
        }

        settingsPanel.CancelEdit();
        RestoreGameplayState();
        reset.ResetRoom();
    }

    private void ResumeGameNow()
    {
        if (!paused) return;
        settingsPanel.CancelEdit();
        RestoreGameplayState();
    }

    public void ConfirmQuit()
    {
        if (!paused || CurrentPanel != PanelState.QuitConfirmation) return;
        SaveService save = SaveService.Instance;
        if (SaveService.IsReady && save.TryPrepareForQuit())
        {
            Application.Quit();
            return;
        }

        quitSaveFailed = true;
        view.ShowQuitSaveFailure(save.LastWriteError);
    }

    public void CancelOrBack()
    {
        if (!paused) return;
        switch (CurrentPanel)
        {
            case PanelState.Settings:
                CancelSettings();
                break;
            case PanelState.QuitConfirmation:
                CancelQuitOrReturnTitle();
                break;
            default:
                ResumeGame();
                break;
        }
    }

    private void ApplySettings()
    {
        if (CurrentPanel != PanelState.Settings) return;
        if (settingsPanel.ApplyEdit()) ReturnToMain();
    }

    private void CancelSettings()
    {
        if (!paused || CurrentPanel != PanelState.Settings) return;
        settingsPanel.CancelEdit();
        ReturnToMain();
    }

    private void ReturnToMain()
    {
        if (!paused) return;
        quitSaveFailed = false;
        CurrentPanel = PanelState.Main;
        view.ShowMain();
    }

    private void CancelQuitOrReturnTitle()
    {
        if (!quitSaveFailed)
        {
            ReturnToMain();
            return;
        }

        SaveService.Instance.TryPrepareForTitle();
        RestoreGameplayState();
        SaveFlowController.Instance?.ShowTitle();
        quitSaveFailed = false;
    }

    private bool CanActivateUiInput()
    {
        InputActionAsset actions = playerInput != null ? playerInput.actions : null;
        return pauseAction != null && cancelAction != null &&
               actions?.FindAction("UI/Navigate", false) != null &&
               actions.FindAction("UI/Confirm", false) != null &&
               actions.FindAction("UI/Point", false) != null &&
               actions.FindAction("UI/Click", false) != null;
    }

    private void ActivateUiInput()
    {
        if (playerInput == null) return;
        if (playerInput.currentActionMap == null || playerInput.currentActionMap.name != "UI")
            playerInput.SwitchCurrentActionMap("UI");
        view.SetInputEnabled(true);
    }

    private void ProcessDeferredInputTransitions()
    {
        if (openRequested)
        {
            openRequested = false;
            OpenPauseMenu();
        }

        if (cancelRequested)
        {
            cancelRequested = false;
            CancelOrBack();
        }

        if (restartRequested)
        {
            restartRequested = false;
            RestartRoomNow();
        }
        else if (resumeRequested)
        {
            resumeRequested = false;
            ResumeGameNow();
        }
    }

    private void RestoreGameplayState()
    {
        paused = false;
        CurrentPanel = PanelState.Closed;
        view.Hide();
        view.SetInputEnabled(false);

        if (playerInput != null && playerInput.isActiveAndEnabled)
        {
            string map = string.IsNullOrWhiteSpace(previousActionMap) ? "Player" : previousActionMap;
            if (playerInput.actions?.FindActionMap(map, false) != null)
                playerInput.SwitchCurrentActionMap(map);
            else if (playerInput.actions?.FindActionMap("Player", false) != null)
                playerInput.SwitchCurrentActionMap("Player");
        }

        if (mirrorController != null) mirrorController.enabled = previousMirrorEnabled;
        if (playerController != null) playerController.SetControlEnabled(previousPlayerControl);
        Time.timeScale = previousTimeScale;
        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;
        if (SaveService.IsReady) SaveService.Instance.SetGameplayPaused(false);
        previousActionMap = null;
    }

    private void OnActiveSceneChanged(Scene previous, Scene next)
    {
        if (paused)
        {
            settingsPanel.CancelEdit();
            RestoreGameplayState();
        }
        else
        {
            CurrentPanel = PanelState.Closed;
            view.Hide();
        }

        UnbindInputSource();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus || !paused) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ActivateUiInput();
        switch (CurrentPanel)
        {
            case PanelState.Settings:
                view.ShowSettings(settingsPanel.FirstSelection);
                break;
            case PanelState.QuitConfirmation:
                view.ShowQuitConfirmation();
                break;
            default:
                view.ReselectMain();
                break;
        }
    }

    private void OnApplicationQuit() => applicationQuitting = true;

    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        UnbindInputSource();
        settingsPanel?.Dispose();
        view?.Dispose();
        if (paused)
        {
            Time.timeScale = previousTimeScale;
            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisible;
            if (!applicationQuitting && SaveService.IsReady)
                SaveService.Instance.SetGameplayPaused(false);
        }
        instance = null;
    }
}
