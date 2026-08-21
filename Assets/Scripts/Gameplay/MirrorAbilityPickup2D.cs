using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class MirrorAbilityPickup2D : PermanentPickup2D
{
    [SerializeField] private MirrorPlayer2D mirror;

    protected override void Awake()
    {
        Configure(SaveIds.MirrorPickup, PermanentPickupType.Ability, SaveIds.MirrorAbility);
        base.Awake();
        if (mirror == null) mirror = FindFirstObjectByType<MirrorPlayer2D>();
    }

    protected override void Start()
    {
        if (MirrorAbilityState.UnlockedThisRun) mirror?.Unlock();
        base.Start();
    }

    public override bool TryCollect(PlayerController2D player)
    {
        return base.TryCollect(player);
    }

    protected override void ApplyReward(PlayerController2D player) => mirror?.Unlock();
}
