public static class MirrorAbilityState
{
    public static bool UnlockedThisRun => SaveService.IsReady && SaveService.Instance.HasAbility(SaveIds.MirrorAbility);

    public static bool Unlock() => SaveService.Instance.TryCollectPermanent(
        SaveIds.MirrorPickup, PermanentPickupType.Ability, SaveIds.MirrorAbility);

#if UNITY_INCLUDE_TESTS
    public static void ResetForTests() => SaveService.Instance.ReplaceStateForTests(SaveData.CreateNew());
#endif
}
