using UnityEngine.UI;

namespace LMCore.UI
{
    public class LMButton : Button
    {
        public delegate void InteractableEvent(bool interactable);
        public event InteractableEvent OnInteractableChange;

        protected bool firstTime = true;
        protected bool prevInteractable;

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            if (firstTime || interactable != prevInteractable)
            {
                firstTime = false;
                prevInteractable = interactable;
                OnInteractableChange?.Invoke(interactable);
            }
        }
    }
}
