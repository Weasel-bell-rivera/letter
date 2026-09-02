using UnityEngine;

namespace W1.Accessibility.UI
{
    [DisallowMultipleComponent]
    public sealed class NonColorStateCue : MonoBehaviour
    {
        [SerializeField] private GameObject inactiveMarker;
        [SerializeField] private GameObject activeMarker;

        public bool IsActive { get; private set; }

        public void SetState(bool active)
        {
            IsActive = active;
            if (inactiveMarker != null)
                inactiveMarker.SetActive(!active);
            if (activeMarker != null)
                activeMarker.SetActive(active);
        }
    }
}
