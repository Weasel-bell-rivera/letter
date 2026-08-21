using System;
using System.Collections.Generic;
using UnityEngine;

public enum PermanentPickupType { Ability, Collectible, Progression }

public static class PermanentPickupId
{
    public static bool IsValid(string id, PermanentPickupType type)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        string[] parts = id.Split(':');
        if (parts.Length != 3 || parts[0].Length < 5 || parts[2].Length != 2 || !int.TryParse(parts[2], out int sequence) || sequence < 1)
            return false;
        string expected = type switch
        {
            PermanentPickupType.Ability => "ABILITY",
            PermanentPickupType.Collectible => "COLLECTIBLE",
            PermanentPickupType.Progression => "PROGRESSION",
            _ => string.Empty
        };
        return parts[1] == expected && parts[0] == parts[0].ToUpperInvariant();
    }

    public static bool HasDuplicates(IEnumerable<string> ids)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (string id in ids)
            if (!string.IsNullOrWhiteSpace(id) && !seen.Add(id)) return true;
        return false;
    }
}

[RequireComponent(typeof(Collider2D))]
public class PermanentPickup2D : MonoBehaviour
{
    [SerializeField] private string permanentId;
    [SerializeField] private PermanentPickupType pickupType;
    [SerializeField] private string rewardId;
    private bool settled;

    public string PermanentId => permanentId;
    public PermanentPickupType PickupType => pickupType;
    public bool Collected => SaveService.IsReady && SaveService.Instance.HasCollected(permanentId);
    public event Action CollectedOnce;

    protected virtual void Awake() => GetComponent<Collider2D>().isTrigger = true;

    protected virtual void Start()
    {
        if (Collected) HideCollected();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerController2D>(out PlayerController2D player)) TryCollect(player);
    }

    public virtual bool TryCollect(PlayerController2D player)
    {
        if (player == null || settled || !player.enabled || !player.ControlEnabled || Time.timeScale <= 0f || !SaveService.IsReady) return false;
        if (!SaveService.Instance.TryCollectPermanent(permanentId, pickupType, rewardId)) return false;
        settled = true;
        ApplyReward(player);
        CollectedOnce?.Invoke();
        HideCollected();
        return true;
    }

    public void Configure(string id, PermanentPickupType type, string reward)
    {
        permanentId = id;
        pickupType = type;
        rewardId = reward;
    }

    protected virtual void ApplyReward(PlayerController2D player) { }
    protected void HideCollected() => gameObject.SetActive(false);

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (!string.IsNullOrWhiteSpace(permanentId) && !PermanentPickupId.IsValid(permanentId, pickupType))
            Debug.LogError($"Invalid permanent pickup ID '{permanentId}' on {name}.", this);
    }
#endif
}
