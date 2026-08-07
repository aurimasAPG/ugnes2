using HiddenValley.Core;
using UnityEngine;
using UnityEngine.AI;

namespace HiddenValley.Unity
{
    /// <summary>
    /// Binds one scene object to one authored world object id, and mirrors its state:
    /// present or not, blocking or not, interactable or not.
    ///
    /// This component is the entire reason "Orrel opens a shop" and "Coll opens a cliff"
    /// needed no code. Both are this script reading a condition it does not understand.
    ///
    /// Two binders may share an id-pair on one anchor — the dawn and dusk observation
    /// points do exactly that — so nothing here assumes a one-to-one mapping.
    /// </summary>
    public sealed class WorldObjectBinder : MonoBehaviour
    {
        [SerializeField] private string worldObjectId;

        [Tooltip("Toggled with the object's visibility. Leave empty to toggle this GameObject.")]
        [SerializeField] private GameObject visual;

        [Tooltip("Optional. Enabled only while the object blocks traversal.")]
        [SerializeField] private NavMeshObstacle obstacle;

        [Tooltip("Optional. Enabled only while the object blocks traversal.")]
        [SerializeField] private Collider blockingCollider;

        public string Id => worldObjectId;
        public WorldObjectDef Def { get; private set; }
        public bool IsInteractable { get; private set; }

        private GameBootstrap _boot;

        private void Start()
        {
            _boot = GameBootstrap.Instance;
            if (_boot == null)
            {
                Debug.LogError($"[HiddenValley] {name}: no GameBootstrap in the scene.");
                enabled = false;
                return;
            }

            _boot.Changed += Refresh;
            _boot.Ready += Rebind;

            Rebind();
        }

        private void OnDestroy()
        {
            if (_boot == null) return;
            _boot.Changed -= Refresh;
            _boot.Ready -= Rebind;
        }

        private void Rebind()
        {
            Def = _boot.Game?.Content.WorldObject(worldObjectId);

            if (Def == null)
            {
                // An id that does not resolve is a content typo. Loud, not silent — a
                // missing pickup is otherwise indistinguishable from one already taken.
                Debug.LogError($"[HiddenValley] {name}: no world object '{worldObjectId}' in content.");
                enabled = false;
                return;
            }

            Refresh();
        }

        private void Refresh()
        {
            if (Def == null || _boot.Game == null) return;

            var world = _boot.Game.World;

            bool visible = world.IsVisible(Def);
            var target = visual != null ? visual : gameObject;
            if (target.activeSelf != visible) target.SetActive(visible);

            bool blocks = world.Blocks(Def);
            if (obstacle != null && obstacle.enabled != blocks) obstacle.enabled = blocks;
            if (blockingCollider != null && blockingCollider.enabled != blocks) blockingCollider.enabled = blocks;

            IsInteractable = world.CanInteract(Def);
        }

        public string Label => Def?.InteractLabel;

        /// <summary>Body text for readables. Null for everything else.</summary>
        public string Text => Def?.Text;

        public bool Interact()
        {
            if (!IsInteractable) return false;
            return _boot.Game.World.Interact(worldObjectId);
        }
    }
}
