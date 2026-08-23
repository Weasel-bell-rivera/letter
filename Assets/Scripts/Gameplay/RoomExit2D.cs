using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public sealed class RoomExit2D : MonoBehaviour
{
    [SerializeField] private string targetScene;
    [SerializeField] private string targetEntranceId = "DEFAULT";
    public bool Completed { get; private set; }
    public string TargetScene => targetScene;
    public string TargetEntranceId => targetEntranceId;
    public void Configure(string sceneName, string entranceId = "DEFAULT") { targetScene = sceneName; targetEntranceId = entranceId; }
    private void Awake() => GetComponent<Collider2D>().isTrigger = true;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<PlayerController2D>(out PlayerController2D player)) return;
        Completed = true;
        if (!string.IsNullOrWhiteSpace(targetScene) && Application.CanStreamedLevelBeLoaded(targetScene))
        {
            player.SetControlEnabled(false);
            player.GetComponent<MirrorPlayer2D>()?.RecallImmediate();
            RoomTransitionState.Request(targetScene, targetEntranceId);
            SceneManager.sceneLoaded += OnTargetLoaded;
            SceneManager.LoadScene(targetScene);
        }
    }

    private void OnTargetLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnTargetLoaded;
        string roomId = scene.name.ToUpperInvariant();
        SaveService.Instance.RecordRoomEntered(roomId, targetEntranceId);
    }
}
