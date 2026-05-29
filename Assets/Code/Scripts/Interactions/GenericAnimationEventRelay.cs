using UnityEngine;
using UnityEngine.Events;

namespace Interactions
{
    /// <summary>
    /// A reusable relay placed on a child GameObject whose Animator needs to forward 
    /// Animation Events. Hook these UnityEvents up in the inspector to call methods
    /// on other GameObjects or components.
    /// </summary>
    public class GenericAnimationEventRelay : MonoBehaviour
    {
        [Tooltip("Triggered by an Animation Event without parameters. To use this, add an Animation Event in the Animation window and set the function name to 'TriggerEvent'.")]
        public UnityEvent OnAnimationEvent;

        [Tooltip("Triggered by an Animation Event with a string parameter. To use this, add an Animation Event in the Animation window, set the function name to 'TriggerStringEvent', and fill in the String parameter.")]
        public UnityEvent<string> OnAnimationStringEvent;

        /// <summary>
        /// Called by an Animation Event on your animation clip.
        /// </summary>
        public void TriggerEvent()
        {
            OnAnimationEvent?.Invoke();
        }

        /// <summary>
        /// Called by an Animation Event with a string parameter on your animation clip.
        /// </summary>
        /// <param name="eventName">The string passed from the Animation Event.</param>
        public void TriggerStringEvent(string eventName)
        {
            OnAnimationStringEvent?.Invoke(eventName);
        }
    }
}
