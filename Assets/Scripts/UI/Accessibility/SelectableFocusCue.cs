using UnityEngine;
using UnityEngine.EventSystems;

namespace W1.Accessibility.UI
{
    [DisallowMultipleComponent]
    public sealed class SelectableFocusCue : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private GameObject marker;

        public void Configure(GameObject focusMarker)
        {
            marker = focusMarker;
            SetVisible(false);
        }

        private void OnDisable() => SetVisible(false);
        public void OnSelect(BaseEventData eventData) => SetVisible(true);
        public void OnDeselect(BaseEventData eventData) => SetVisible(false);

        private void SetVisible(bool visible)
        {
            if (marker != null)
                marker.SetActive(visible);
        }
    }
}
