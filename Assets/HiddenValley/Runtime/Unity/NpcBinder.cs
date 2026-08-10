using System.Collections.Generic;
using HiddenValley.Core;
using UnityEngine;

namespace HiddenValley.Unity
{
    /// <summary>
    /// Moves an NPC to wherever their authored schedule says they should be, and starts
    /// their conversation when interacted with.
    ///
    /// Schedules are coarse on purpose — four phases, one waypoint each. A minute-by-minute
    /// routine is a lot of pathfinding for something the player samples about twice, and the
    /// slice is 25 minutes long.
    ///
    /// NPCs walk straight to waypoints. There is no NavMesh in the shipped scene: any
    /// NavMeshSurface (baked or unbaked) packs into a level0 the iOS player rejects as
    /// corrupted (bisection, 2026-08-07). Waypoints are placed so paths stay in the open;
    /// grey-box fidelity does not need obstacle avoidance.
    /// </summary>
    public sealed class NpcBinder : MonoBehaviour
    {
        [SerializeField] private string npcId;
        [SerializeField] private List<Waypoint> waypoints = new List<Waypoint>();
        [SerializeField] private float walkSpeed = 2.2f;

        [System.Serializable]
        public struct Waypoint
        {
            public string id;
            public Transform point;
        }

        public string Id => npcId;
        public NpcDef Def { get; private set; }

        private GameBootstrap _boot;
        private string _currentWaypoint;
        private Transform _destination;

        /// <summary>Runtime wiring for <see cref="LayoutSpawner"/> (fields are otherwise private).</summary>
        public void Configure(string id, List<Waypoint> points = null, float speed = 2.2f)
        {
            npcId = id;
            if (points != null) waypoints = points;
            walkSpeed = speed;
        }

        private void Start()
        {
            _boot = GameBootstrap.Instance;
            if (_boot == null) { enabled = false; return; }

            Def = _boot.Game?.Content.Npc(npcId);
            if (Def == null)
            {
                Debug.LogError($"[HiddenValley] {name}: no NPC '{npcId}' in content.");
                enabled = false;
                return;
            }

            _boot.Changed += Refresh;
            Refresh();

            var visual = transform.Find("Visual");
            if (visual != null && GetComponent<CapsuleAnimator>() == null)
                CapsuleAnimator.Attach(visual);
        }

        private void OnDestroy()
        {
            if (_boot != null) _boot.Changed -= Refresh;
        }

        /// <summary>
        /// Only re-targets when the destination actually changes. Schedules are evaluated
        /// against state that moves constantly (the clock ticks every frame), so without
        /// this guard every NPC would re-resolve their waypoint 60 times a second.
        /// </summary>
        private void Refresh()
        {
            var target = _boot.Game.World.WaypointFor(npcId);
            if (target == _currentWaypoint) return;

            _currentWaypoint = target;
            if (string.IsNullOrEmpty(target)) return;

            // Pip's schedule names a virtual "follow the player" waypoint. PipCompanion
            // owns that motion; there is nothing to wire here.
            if (target == "wp.follow_player")
            {
                _destination = null;
                return;
            }

            foreach (var w in waypoints)
            {
                if (w.id != target || w.point == null) continue;
                _destination = w.point;
                return;
            }

            Debug.LogWarning($"[HiddenValley] {name}: schedule wants waypoint '{target}', which is not wired up.");
        }

        /// <summary>Set for the duration of a conversation: the NPC turns to face this
        /// and holds it. Talking to the back of a capsule reads as a bug.</summary>
        public Transform FaceTarget { get; set; }

        private Transform _player;

        private void Update()
        {
            // Conversation facing wins over everything, including the schedule.
            if (FaceTarget != null)
            {
                FaceToward(FaceTarget.position, 360f);
                return;
            }

            if (_destination != null)
            {
                Vector3 position = transform.position;
                Vector3 target = _destination.position;
                if ((target - position).sqrMagnitude >= 0.01f)
                {
                    transform.position = Vector3.MoveTowards(position, target, walkSpeed * Time.deltaTime);
                    FaceToward(target, 360f);
                    return;
                }
            }

            // Arrived idle: slow ambient head-turn toward a nearby player — the cheapest
            // "this person notices you" signal, rate-capped so nobody owl-necks.
            if (_player == null)
            {
                var playerGo = GameObject.FindWithTag("Player");
                if (playerGo != null) _player = playerGo.transform;
            }
            if (_player != null)
            {
                Vector3 offset = _player.position - transform.position;
                offset.y = 0;
                if (offset.sqrMagnitude < 16f && offset.sqrMagnitude > 0.04f)
                    FaceToward(_player.position, 90f);
            }
        }

        private void FaceToward(Vector3 worldPoint, float degreesPerSecond)
        {
            Vector3 flat = worldPoint - transform.position;
            flat.y = 0;
            if (flat.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(flat), degreesPerSecond * Time.deltaTime);
        }

        public DialogueSession BeginConversation() => _boot.Game.Dialogue.Begin(npcId);
    }
}
