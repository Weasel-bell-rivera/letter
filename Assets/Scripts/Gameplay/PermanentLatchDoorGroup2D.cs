using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class DoorGroupId
{
    private static readonly Regex Pattern = new("^[A-Z]+_[0-9]{3}:DOOR_GROUP:[0-9]{2}$", RegexOptions.CultureInvariant);

    public static bool IsValid(string value) => !string.IsNullOrWhiteSpace(value) && Pattern.IsMatch(value);

    public static bool HasDuplicates(IEnumerable<string> values)
    {
        HashSet<string> unique = new(StringComparer.Ordinal);
        foreach (string value in values)
            if (!unique.Add(value)) return true;
        return false;
    }
}

public sealed class PermanentLatchDoorGroup2D : MonoBehaviour, IRoomResettable, IOrderedRoomResettable
{
    public enum GroupState { Closed, TemporaryOpen, Latched }

    [SerializeField] private string doorGroupId;
    [SerializeField] private Door2D door;
    [SerializeField] private PressurePlate2D plateA;
    [SerializeField] private PressurePlate2D plateB;
    [SerializeField] private SpriteRenderer[] connectionRenderers = Array.Empty<SpriteRenderer>();
    [SerializeField] private AudioSource feedbackAudio;
    [SerializeField] private AudioClip temporaryOpenClip;
    [SerializeField] private AudioClip latchedClip;
    [SerializeField] private Color idleConnectionColor = new(.5f, .22f, .12f, .45f);
    [SerializeField] private Color temporaryConnectionColor = new(1f, .75f, .18f, .9f);
    [SerializeField] private Color latchedConnectionColor = new(.2f, .9f, 1f, 1f);

    private bool initialized;
    private bool ownsTemporaryClip;
    private bool ownsLatchedClip;
    private bool configurationErrorLogged;

    public string DoorGroupId => doorGroupId;
    public GroupState State { get; private set; }
    public bool IsLatched => State == GroupState.Latched;
    public int ResetOrder => 100;
    public event Action Latched;

    private void Awake()
    {
        EnsureFeedbackAudio();
        // The reusable prefab intentionally carries no production ID. Scene instances
        // must override it before play, so only configured instances initialize here.
        if (!string.IsNullOrWhiteSpace(doorGroupId)) InitializeFromSave();
    }

    private void Start()
    {
        // A prefab asset may keep an empty ID, but an instantiated scene object may not.
        if (gameObject.scene.IsValid() && !initialized) InitializeFromSave();
    }

    private void FixedUpdate() => SettlePhysicsState();

    private void OnDestroy()
    {
        if (ownsTemporaryClip && temporaryOpenClip != null) Destroy(temporaryOpenClip);
        if (ownsLatchedClip && latchedClip != null) Destroy(latchedClip);
    }

    public void Configure(string id, Door2D targetDoor, PressurePlate2D firstPlate, PressurePlate2D secondPlate,
        SpriteRenderer[] feedbackConnections = null, AudioSource audioSource = null)
    {
        doorGroupId = id;
        door = targetDoor;
        plateA = firstPlate;
        plateB = secondPlate;
        connectionRenderers = feedbackConnections ?? Array.Empty<SpriteRenderer>();
        feedbackAudio = audioSource;
        initialized = false;
        configurationErrorLogged = false;
        if (Application.isPlaying)
        {
            EnsureFeedbackAudio();
            InitializeFromSave();
        }
    }

    public void InitializeFromSave()
    {
        if (!ReferencesAreValid()) return;
        bool savedLatch = SaveService.Instance.HasLatchedDoorGroup(doorGroupId);
        ApplyState(savedLatch ? GroupState.Latched : GroupState.Closed, false);
        initialized = true;
    }

    public void SettlePhysicsState()
    {
        if (!initialized) InitializeFromSave();
        if (!ReferencesAreValid()) return;
        if (IsLatched)
        {
            ApplyState(GroupState.Latched, false);
            return;
        }

        bool firstActive = plateA.IsActive;
        bool secondActive = plateB.IsActive;
        if (firstActive && secondActive)
        {
            SaveService.Instance.TryLatchDoorGroup(doorGroupId);
            ApplyState(GroupState.Latched, true);
            Latched?.Invoke();
            return;
        }

        ApplyState(firstActive || secondActive ? GroupState.TemporaryOpen : GroupState.Closed, true);
    }

    private bool ReferencesAreValid()
    {
        if (global::DoorGroupId.IsValid(doorGroupId) && door != null && plateA != null && plateB != null && plateA != plateB)
            return true;
        if (!configurationErrorLogged)
        {
            Debug.LogError($"Invalid permanent door group configuration on {name}.", this);
            configurationErrorLogged = true;
        }
        return false;
    }

    private void ApplyState(GroupState next, bool playFeedback)
    {
        GroupState previous = State;
        State = next;
        bool latched = State == GroupState.Latched;
        plateA.SetLatchedVisual(latched);
        plateB.SetLatchedVisual(latched);
        door.SetState(State switch
        {
            GroupState.Latched => Door2D.VisualState.LatchedOpen,
            GroupState.TemporaryOpen => Door2D.VisualState.TemporaryOpen,
            _ => Door2D.VisualState.Closed
        });

        Color connectionColor = State switch
        {
            GroupState.Latched => latchedConnectionColor,
            GroupState.TemporaryOpen => temporaryConnectionColor,
            _ => idleConnectionColor
        };
        foreach (SpriteRenderer renderer in connectionRenderers)
            if (renderer != null) renderer.color = connectionColor;

        if (!playFeedback || previous == State) return;
        if (State == GroupState.Latched) PlayFeedback(latchedClip);
        else if (State == GroupState.TemporaryOpen) PlayFeedback(temporaryOpenClip);
    }

    private void EnsureFeedbackAudio()
    {
        if (feedbackAudio == null) feedbackAudio = GetComponent<AudioSource>();
        if (feedbackAudio == null) feedbackAudio = gameObject.AddComponent<AudioSource>();
        feedbackAudio.playOnAwake = false;
        feedbackAudio.spatialBlend = 0f;
        if (temporaryOpenClip == null)
        {
            temporaryOpenClip = CreateTone("Temporary Door Open", 440f, .08f, false);
            ownsTemporaryClip = true;
        }
        if (latchedClip == null)
        {
            latchedClip = CreateTone("Permanent Door Latch", 660f, .18f, true);
            ownsLatchedClip = true;
        }
    }

    private void PlayFeedback(AudioClip clip)
    {
        if (feedbackAudio != null && clip != null) feedbackAudio.PlayOneShot(clip);
    }

    private static AudioClip CreateTone(string clipName, float frequency, float duration, bool rising)
    {
        const int sampleRate = 22050;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Sin(Mathf.PI * i / Mathf.Max(1, sampleCount - 1));
            float currentFrequency = rising ? Mathf.Lerp(frequency, frequency * 1.5f, i / (float)sampleCount) : frequency;
            samples[i] = Mathf.Sin(2f * Mathf.PI * currentFrequency * t) * envelope * .08f;
        }
        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public void ResetRoomState()
    {
        bool savedLatch = SaveService.Instance.HasLatchedDoorGroup(doorGroupId);
        ApplyState(savedLatch || IsLatched ? GroupState.Latched : GroupState.Closed, false);
    }
}
