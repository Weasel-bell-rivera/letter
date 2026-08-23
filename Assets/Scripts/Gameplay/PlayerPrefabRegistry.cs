using UnityEngine;

[CreateAssetMenu(menuName = "Mirror Puzzle/Player Prefab Registry")]
public sealed class PlayerPrefabRegistry : ScriptableObject
{
    public const string ResourcesPath = "PlayerPrefabRegistry";

    [SerializeField] private GameObject playerPrefab;

    public GameObject PlayerPrefab => playerPrefab;

    public void Configure(GameObject prefab) => playerPrefab = prefab;

    public bool IsValid(out string error)
    {
        if (playerPrefab == null)
        {
            error = "Player Prefab Registry has no Player prefab.";
            return false;
        }

        if (playerPrefab.GetComponent<PlayerController2D>() == null ||
            playerPrefab.GetComponent<MirrorPlayer2D>() == null ||
            playerPrefab.GetComponent<UnityEngine.InputSystem.PlayerInput>() == null)
        {
            error = "Registered Player prefab is missing required runtime components.";
            return false;
        }

        error = null;
        return true;
    }
}
