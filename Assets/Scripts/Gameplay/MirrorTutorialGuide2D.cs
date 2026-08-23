using UnityEngine;

public sealed class MirrorTutorialGuide2D : MonoBehaviour
{
    [SerializeField] private MirrorPlayer2D mirror;
    [SerializeField] private GameObject placeIndicator;
    [SerializeField] private GameObject recallIndicator;
    [SerializeField] private GameObject completeIndicator;
    private bool sawPlaced;
    public bool PracticeCompleted { get; private set; }

    public void Configure(MirrorPlayer2D mirrorSystem, GameObject place, GameObject recall, GameObject complete)
    { mirror = mirrorSystem; placeIndicator = place; recallIndicator = recall; completeIndicator = complete; }

    private void Update()
    {
        if (mirror == null) mirror = FindAnyObjectByType<MirrorPlayer2D>();
        if (mirror == null) return;
        if (mirror.State == MirrorPlayer2D.MirrorState.Placed) sawPlaced = true;
        if (sawPlaced && mirror.State == MirrorPlayer2D.MirrorState.Held) PracticeCompleted = true;
        placeIndicator?.SetActive(!PracticeCompleted && mirror.State == MirrorPlayer2D.MirrorState.Held);
        recallIndicator?.SetActive(!PracticeCompleted && mirror.State == MirrorPlayer2D.MirrorState.Placed);
        completeIndicator?.SetActive(PracticeCompleted);
    }
}
