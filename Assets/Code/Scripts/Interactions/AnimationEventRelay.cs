using UnityEngine;

namespace Interactions
{
    /// <summary>
    /// Placed on a child GameObject whose Animator needs to forward Animation Events
    /// to a component living on a parent or sibling GameObject.
    /// </summary>
    public class AnimationEventRelay : MonoBehaviour
    {
        [SerializeField] private ExplodingMushroomBehaviour _mushroomBehaviour;

        /// <summary>Called by an Animation Event on the Explode clip.</summary>
        public void TriggerExplosionDamage()
        {
            if (_mushroomBehaviour != null)
                _mushroomBehaviour.TriggerExplosionDamage();
        }
    }
}
