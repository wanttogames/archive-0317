using UnityEngine;

namespace Archive0317
{
    public abstract class Inspectable : MonoBehaviour
    {
        public virtual string Prompt => "E 조사";

        /// <summary>
        /// Lets stateful interactables suppress input while a transition is already running.
        /// The player still keeps the target, but does not dispatch another interaction.
        /// </summary>
        public virtual bool IsInteractionAvailable => isActiveAndEnabled;

        /// <summary>
        /// Small per-interaction debounce applied by FirstPersonPlayer.
        /// Stateful objects can request a longer window without owning player input.
        /// </summary>
        public virtual float InteractionCooldown => .18f;

        /// <summary>
        /// Doors and other objects with dedicated feedback can opt out of the generic inspect tap.
        /// </summary>
        public virtual bool PlayInspectSound => true;

        public abstract void Inspect(FirstPersonPlayer player);
    }
}
