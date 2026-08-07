using System.Collections.Generic;
using UnityEngine;

namespace HiddenValley.Unity
{
    /// <summary>
    /// Picks what the player would interact with, and does it.
    ///
    /// Candidates register themselves via trigger enter/exit rather than being found by a
    /// per-frame OverlapSphere. On a populated village scene that difference is the whole
    /// budget of a physics query every frame versus none, and the Phase 3 done-state is a
    /// locked frame budget with the village fully populated.
    /// </summary>
    public sealed class InteractionController : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private float maxAngleFromFacing = 120f;

        private readonly List<WorldObjectBinder> _candidates = new List<WorldObjectBinder>();

        public WorldObjectBinder Current { get; private set; }

        public void Register(WorldObjectBinder binder)
        {
            if (binder != null && !_candidates.Contains(binder)) _candidates.Add(binder);
        }

        public void Unregister(WorldObjectBinder binder)
        {
            _candidates.Remove(binder);
            if (Current == binder) Current = null;
        }

        private void Update()
        {
            Current = Best();
        }

        private WorldObjectBinder Best()
        {
            if (player == null) return null;

            WorldObjectBinder best = null;
            float bestScore = float.MaxValue;

            for (int i = _candidates.Count - 1; i >= 0; i--)
            {
                var candidate = _candidates[i];

                // Binders can be deactivated by their own visibility condition mid-frame.
                if (candidate == null || !candidate.isActiveAndEnabled)
                {
                    _candidates.RemoveAt(i);
                    continue;
                }

                if (!candidate.IsInteractable) continue;

                var offset = candidate.transform.position - player.position;
                float angle = Vector3.Angle(player.forward, offset);
                if (angle > maxAngleFromFacing) continue;

                // Distance first, facing as a tie-breaker: reaching for the thing you are
                // looking at beats the thing behind your elbow.
                float score = offset.sqrMagnitude + angle * 0.01f;
                if (score >= bestScore) continue;

                bestScore = score;
                best = candidate;
            }

            return best;
        }

        public bool InteractWithCurrent() => Current != null && Current.Interact();
    }

    /// <summary>Put this on the trigger volume around an interactable.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class InteractionZone : MonoBehaviour
    {
        [SerializeField] private WorldObjectBinder binder;
        [SerializeField] private string playerTag = "Player";

        private void Reset()
        {
            binder = GetComponentInParent<WorldObjectBinder>();
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            Controller(other)?.Register(binder);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            Controller(other)?.Unregister(binder);
        }

        private static InteractionController Controller(Collider other)
            => other.GetComponentInParent<InteractionController>();
    }
}
