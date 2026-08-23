using UnityEngine;

[DisallowMultipleComponent]
public sealed class RoomEntrance2D : MonoBehaviour
{
    [SerializeField] private string entranceId = SaveIds.DefaultEntrance;
    [SerializeField] private bool defaultEntrance;
    [SerializeField] private bool facingRight = true;

    public string EntranceId => entranceId;
    public bool IsDefault => defaultEntrance;
    public bool FacingRight => facingRight;

    public void Configure(string id, bool isDefault, bool initialFacingRight = true)
    {
        entranceId = string.IsNullOrWhiteSpace(id) ? SaveIds.DefaultEntrance : id.Trim().ToUpperInvariant();
        defaultEntrance = isDefault;
        facingRight = initialFacingRight;
    }

    private void OnValidate()
    {
        entranceId = string.IsNullOrWhiteSpace(entranceId)
            ? SaveIds.DefaultEntrance
            : entranceId.Trim().ToUpperInvariant();
    }
}
