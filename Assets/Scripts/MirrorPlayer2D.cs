using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public sealed class MirrorPlayer2D : MonoBehaviour
{
    public enum MirrorState { Unobtained, Held, Placed }
    public enum PlacementFailure { None, NotUnlocked, AlreadyPlaced, MissingPlayer, Airborne, NoSurface, Blocked }
    [SerializeField] private PlayerController2D player;
    [SerializeField] private BoxCollider2D playerCollider;
    [SerializeField] private GameObject mirrorVisualPrefab;
    [SerializeField] private GameObject heldMirrorVisual;
    [SerializeField] private bool initiallyUnlocked = true;
    private GameObject placedMirror, cloneObject;
    private bool gravityDisabled;
    private bool leftMousePressed, rightMousePressed;
    private InputAction placeAction, recallAction, unpairedRecallAction;
    private int lastPlaceFrame = -1, lastRecallFrame = -1;
    public MirrorState State { get; private set; }
    public PlacementFailure LastFailure { get; private set; }
    public MirrorCloneController2D Clone { get; private set; }
    public GameObject PlacedMirror => placedMirror;
    public GameObject HeldMirrorVisual => heldMirrorVisual;
    public bool RecallInputReady => unpairedRecallAction != null && unpairedRecallAction.enabled;
    public float RecallInputValue => unpairedRecallAction?.ReadValue<float>() ?? 0f;
    private void Awake() => State = initiallyUnlocked || MirrorAbilityState.UnlockedThisRun ? MirrorState.Held : MirrorState.Unobtained;
    private void OnEnable()
    {
        BindInputActions();
        CacheMouseButtonState();
        InputSystem.onEvent += ProcessRawInputEvent;
        InputSystem.onAfterUpdate += ProcessInputAfterUpdate;
    }
    private void Start() { BindInputActions(); RefreshHeldVisual(); }
    private void Update()
    {
        if (placeAction == null || recallAction == null) BindInputActions();
        if (placeAction != null && placeAction.WasPressedThisFrame()) HandlePlaceInput();
        if (recallAction != null && recallAction.WasPressedThisFrame()) HandleRecallInput();
        if (unpairedRecallAction != null && unpairedRecallAction.WasPressedThisFrame()) HandleRecallInput();
    }
    private void BindInputActions()
    {
        PlayerInput input = GetComponent<PlayerInput>();
        if (input == null || input.actions == null) return;
        InputAction nextPlace = input.actions.FindAction("PlaceMirror", false);
        InputAction nextRecall = input.actions.FindAction("RecallMirror", false);
        if (placeAction == nextPlace && recallAction == nextRecall) return;
        if (placeAction != null) placeAction.performed -= OnPlacePerformed;
        if (recallAction != null) recallAction.performed -= OnRecallPerformed;
        placeAction = nextPlace; recallAction = nextRecall;
        if (placeAction != null) placeAction.performed += OnPlacePerformed;
        if (recallAction != null) recallAction.performed += OnRecallPerformed;
        if (unpairedRecallAction != null) { unpairedRecallAction.performed -= OnRecallPerformed; unpairedRecallAction.Dispose(); unpairedRecallAction = null; }
        if (recallAction != null)
        {
            foreach (InputBinding binding in recallAction.bindings)
            {
                if (binding.isComposite || binding.isPartOfComposite || string.IsNullOrWhiteSpace(binding.effectivePath)) continue;
                unpairedRecallAction = new InputAction("RecallMirrorUnpaired", InputActionType.Button, binding.effectivePath);
                unpairedRecallAction.performed += OnRecallPerformed;
                unpairedRecallAction.Enable();
                break;
            }
        }
    }
    private void OnDisable()
    {
        InputSystem.onEvent -= ProcessRawInputEvent;
        InputSystem.onAfterUpdate -= ProcessInputAfterUpdate;
        if (placeAction != null) placeAction.performed -= OnPlacePerformed;
        if (recallAction != null) recallAction.performed -= OnRecallPerformed;
        if (unpairedRecallAction != null) { unpairedRecallAction.performed -= OnRecallPerformed; unpairedRecallAction.Dispose(); }
        placeAction = null; recallAction = null; unpairedRecallAction = null;
        leftMousePressed = false; rightMousePressed = false;
    }
    private void ProcessRawInputEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (device is not Mouse mouse) return;

        if (mouse.leftButton.ReadValueFromEvent(eventPtr, out float nextLeftValue))
        {
            bool nextLeft = nextLeftValue >= InputSystem.settings.defaultButtonPressPoint;
            if (nextLeft && !leftMousePressed) HandlePlaceInput();
            leftMousePressed = nextLeft;
        }

        if (mouse.rightButton.ReadValueFromEvent(eventPtr, out float nextRightValue))
        {
            bool nextRight = nextRightValue >= InputSystem.settings.defaultButtonPressPoint;
            if (nextRight && !rightMousePressed) HandleRecallInput();
            rightMousePressed = nextRight;
        }
    }
    private void ProcessInputAfterUpdate()
    {
        PollMouseButtons();
        if (unpairedRecallAction != null && unpairedRecallAction.WasPressedThisFrame()) HandleRecallInput();
    }
    private void CacheMouseButtonState()
    {
        Mouse mouse = Mouse.current;
        leftMousePressed = mouse != null && mouse.leftButton.isPressed;
        rightMousePressed = mouse != null && mouse.rightButton.isPressed;
    }
    private void PollMouseButtons()
    {
        Mouse mouse = Mouse.current;
        bool nextLeft = mouse != null && mouse.leftButton.isPressed;
        bool nextRight = mouse != null && mouse.rightButton.isPressed;
        if (nextLeft && !leftMousePressed) HandlePlaceInput();
        if (nextRight && !rightMousePressed) HandleRecallInput();
        leftMousePressed = nextLeft;
        rightMousePressed = nextRight;
    }
    public void Configure(PlayerController2D target) { player = target; playerCollider = target.GetComponent<BoxCollider2D>(); }
    public void SetInitiallyUnlocked(bool unlocked) { initiallyUnlocked = unlocked; State = unlocked || MirrorAbilityState.UnlockedThisRun ? MirrorState.Held : MirrorState.Unobtained; RefreshHeldVisual(); }
    public void Unlock() { if (State == MirrorState.Unobtained) State = MirrorState.Held; RefreshHeldVisual(); }
    public void OnPlaceMirror(InputValue value) { if (value.isPressed) HandlePlaceInput(); }
    public void OnRecallMirror(InputValue value) { if (value.isPressed) HandleRecallInput(); }
    private void OnPlacePerformed(InputAction.CallbackContext _) => HandlePlaceInput();
    private void OnRecallPerformed(InputAction.CallbackContext _) => HandleRecallInput();
    private void HandlePlaceInput()
    {
        if (lastPlaceFrame == Time.frameCount) return;
        lastPlaceFrame = Time.frameCount;
        bool placed = TryPlace();
#if UNITY_EDITOR
        Debug.Log($"[MirrorInput] placed={placed} state={State} failure={LastFailure} " +
                  $"grounded={(player != null && player.IsGroundedNow)} frame={Time.frameCount}", this);
#endif
    }
    private void HandleRecallInput() { if (lastRecallFrame == Time.frameCount) return; lastRecallFrame = Time.frameCount; RecallImmediate(); }

    public bool TryPlace()
    {
        LastFailure = PlacementFailure.None;
        if (State == MirrorState.Unobtained) { LastFailure = PlacementFailure.NotUnlocked; return false; }
        if (State == MirrorState.Placed) { LastFailure = PlacementFailure.AlreadyPlaced; return false; }
        if (player == null) { LastFailure = PlacementFailure.MissingPlayer; return false; }
        if (!player.IsGroundedNow) { LastFailure = PlacementFailure.Airborne; return false; }
        float facing = player.FacingRight ? 1f : -1f;
        Bounds pb = playerCollider.bounds;
        RaycastHit2D wallHit = FindSurface(pb.center, Vector2.right * facing, pb.extents.x + .25f, MirrorSurface2D.SurfaceKind.SpecialWall);
        MirrorSurface2D wall = wallHit.collider != null ? wallHit.collider.GetComponent<MirrorSurface2D>() : null;
        if (wall != null && wall.safe && wall.kind == MirrorSurface2D.SurfaceKind.SpecialWall)
            return PlaceWall(wallHit.point, facing, pb);
        RaycastHit2D groundHit = FindSurface(new Vector2(pb.center.x, pb.min.y + .05f), Vector2.down, .2f, MirrorSurface2D.SurfaceKind.Ground);
        MirrorSurface2D ground = groundHit.collider != null ? groundHit.collider.GetComponent<MirrorSurface2D>() : null;
        if (ground == null || !ground.safe || ground.kind != MirrorSurface2D.SurfaceKind.Ground) { LastFailure = PlacementFailure.NoSurface; return false; }
        float axisX = pb.center.x;
        Vector2 clonePosition = pb.center;
        return Spawn(new Vector2(axisX, groundHit.point.y), clonePosition, Vector2.left, Vector2.down, 0f);
    }

    private RaycastHit2D FindSurface(Vector2 origin, Vector2 direction, float distance, MirrorSurface2D.SurfaceKind kind)
    {
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(origin, direction, distance))
        { MirrorSurface2D surface = hit.collider.GetComponent<MirrorSurface2D>(); if (surface != null && surface.kind == kind) return hit; }
        return default;
    }

    private bool PlaceWall(Vector2 point, float facing, Bounds pb)
    {
        float mirrorY = pb.min.y;
        Vector2 clonePosition = new(pb.center.x, mirrorY - pb.extents.y);
        Vector2 moveAxis = facing > 0f ? Vector2.down : Vector2.up;
        Vector2 gravity = facing > 0f ? Vector2.left : Vector2.right;
        return Spawn(new Vector2(point.x, mirrorY), clonePosition, moveAxis, gravity, 90f);
    }

    private bool Spawn(Vector2 mirrorPosition, Vector2 clonePosition, Vector2 moveAxis, Vector2 gravity, float rotation)
    {
        foreach (Collider2D overlap in Physics2D.OverlapBoxAll(clonePosition, playerCollider.size * .95f, 0f))
        {
            if (overlap == playerCollider || overlap.isTrigger || overlap.GetComponent<PlayerController2D>() != null) continue;
            MirrorSurface2D surface = overlap.GetComponent<MirrorSurface2D>();
            if (surface != null && surface.kind == MirrorSurface2D.SurfaceKind.Ground) continue;
            LastFailure = PlacementFailure.Blocked; return false;
        }
        const float mirrorHeight = 2.1f;
        placedMirror = mirrorVisualPrefab != null ? Instantiate(mirrorVisualPrefab) : CreateVisual("Placed Mirror", new Vector2(.18f, mirrorHeight), new Color(.2f,.9f,1f,.7f));
        placedMirror.transform.SetPositionAndRotation(mirrorPosition + Vector2.up * (mirrorHeight * .5f), Quaternion.Euler(0f,0f,rotation));
        foreach (SpriteRenderer renderer in placedMirror.GetComponentsInChildren<SpriteRenderer>()) renderer.sortingOrder = 20;
        cloneObject = new GameObject("MirrorClone"); cloneObject.transform.position = clonePosition;
        Rigidbody2D rb = cloneObject.AddComponent<Rigidbody2D>(); rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        BoxCollider2D box = cloneObject.AddComponent<BoxCollider2D>(); box.size = playerCollider.size;
        GameObject cloneVisual = player.VisualRoot != null ? Instantiate(player.VisualRoot.gameObject, cloneObject.transform) : CreateVisual("Visual", playerCollider.bounds.size, new Color(.3f,.8f,1f,.45f), cloneObject.transform);
        cloneVisual.name = "Visual"; cloneVisual.transform.localPosition = Vector3.zero; cloneVisual.transform.localRotation = Quaternion.identity;
        Vector3 mirroredScale = cloneVisual.transform.localScale; mirroredScale.x = -mirroredScale.x; cloneVisual.transform.localScale = mirroredScale;
        foreach (SpriteRenderer renderer in cloneVisual.GetComponentsInChildren<SpriteRenderer>()) { renderer.sortingOrder = -10; Color c = renderer.color; c.a *= .45f; renderer.color = c; }
        Clone = cloneObject.AddComponent<MirrorCloneController2D>(); Clone.Configure(player, moveAxis, gravity); Clone.SetGravityDisabled(gravityDisabled); Clone.Died += OnCloneDied;
        Physics2D.IgnoreCollision(playerCollider, box, true); State = MirrorState.Placed; RefreshHeldVisual(); return true;
    }

    private static GameObject CreateVisual(string name, Vector2 size, Color color, Transform parent = null)
    { GameObject go = new(name); go.transform.SetParent(parent, false); SpriteRenderer r = go.AddComponent<SpriteRenderer>(); r.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0,0,1,1), new Vector2(.5f,.5f), 1f); r.color = color; go.transform.localScale = size; return go; }
    private void RefreshHeldVisual()
    {
        if (State == MirrorState.Held && heldMirrorVisual == null)
        {
            heldMirrorVisual = CreateVisual("Held Mirror", new Vector2(.16f, 1.15f), new Color(.2f, .9f, 1f, .85f), transform);
            heldMirrorVisual.transform.localPosition = new Vector3(.58f, 0f, 0f);
            SpriteRenderer renderer = heldMirrorVisual.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.sortingOrder = 20;
        }
        if (heldMirrorVisual != null) heldMirrorVisual.SetActive(State == MirrorState.Held);
    }
    private void OnCloneDied() { RecallImmediate(); }
    public void RecallImmediate()
    { if (Clone != null) Clone.Died -= OnCloneDied; if (cloneObject != null) { cloneObject.SetActive(false); Destroy(cloneObject); } if (placedMirror != null) { placedMirror.SetActive(false); Destroy(placedMirror); } cloneObject = null; placedMirror = null; Clone = null; if (State != MirrorState.Unobtained) State = MirrorState.Held; RefreshHeldVisual(); }
    public void DisableMirrorGravity() { gravityDisabled = true; Clone?.SetGravityDisabled(true); }
    public void ClearTemporaryEffects() { gravityDisabled = false; Clone?.SetGravityDisabled(false); }
}
