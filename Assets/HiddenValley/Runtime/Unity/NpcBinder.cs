using System.Collections.Generic;
using HiddenValley.Core;
using UnityEngine;
using UnityEngine.AI;

namespace HiddenValley.Unity
{
    /// <summary>
    /// Moves an NPC to wherever their authored schedule says they should be, and starts
    /// their conversation when interacted with.
    ///
    /// Schedules are coarse on purpose — four phases, one waypoint each. A minute-by-minute
    /// routine is a lot of pathfinding for something the player samples about twice, and the
    /// slice is 25 minutes long.
    /// </summary>
    public sealed class NpcBinder : MonoBehaviour
    {
        [SerializeField] private string npcId;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private List<Waypoint> waypoints = new List<Waypoint>();

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
        }

        private void OnDestroy()
        {
            if (_boot != null) _boot.Changed -= Refresh;
        }

        /// <summary>
        /// Only re-paths when the destination actually changes. Schedules are evaluated
        /// against state that moves constantly (the clock ticks every frame), so without
        /// this guard every NPC would call SetDestination 60 times a second.
        /// </summary>
        private void Refresh()
        {
            var target = _boot.Game.World.WaypointFor(npcId);
            if (target == _currentWaypoint) return;

            _currentWaypoint = target;
            if (string.IsNullOrEmpty(target)) return;

            foreach (var w in waypoints)
            {
                if (w.id != target || w.point == null) continue;
                if (agent != null && agent.isOnNavMesh) agent.SetDestination(w.point.position);
                return;
            }

            Debug.LogWarning($"[HiddenValley] {name}: schedule wants waypoint '{target}', which is not wired up.");
        }

        public DialogueSession BeginConversation() => _boot.Game.Dialogue.Begin(npcId);
    }
}
