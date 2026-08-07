using Unity.AI.Navigation;
using UnityEngine;

namespace HiddenValley.Unity
{
    /// <summary>
    /// Builds the NavMesh on device, once, at scene start.
    ///
    /// This exists because the editor-time bake corrupts the packed scene: a scene saved
    /// referencing baked NavMeshData reliably produced a level0 the iOS player refused to
    /// load ("corrupted", "Position out of bounds"), found by bisection on 2026-08-07 —
    /// grey-box scene ran, identical scene plus baked NavMesh crashed on boot. The world
    /// is one village and a forest shelf; a runtime bake is a one-time cost of well under
    /// a second on the target device, paid behind the initial load.
    ///
    /// Runs before the NPC binders (execution order) so agents wake up on a mesh that
    /// already exists.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [RequireComponent(typeof(NavMeshSurface))]
    public sealed class RuntimeNavMesh : MonoBehaviour
    {
        private void Awake()
        {
            var surface = GetComponent<NavMeshSurface>();
            float start = Time.realtimeSinceStartup;
            surface.BuildNavMesh();
            Debug.Log($"[HiddenValley] Runtime NavMesh bake: {(Time.realtimeSinceStartup - start) * 1000f:0} ms.");
        }
    }
}
