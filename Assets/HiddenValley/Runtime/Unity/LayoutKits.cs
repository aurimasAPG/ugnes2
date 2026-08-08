using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace HiddenValley.Unity
{
    /// <summary>
    /// Expands one layout entry carrying a <c>"kit"</c> field into the primitive specs
    /// that make it read as a built thing — a house gains a stone base, walls, a pitched
    /// slate roof and a door; a kiln gains a tapered chimney; the gear house gains its
    /// half-buried gear. Pure spec-to-spec (JObject in, JObjects out) so the editor scene
    /// generator and the runtime spawner share one vocabulary and cannot drift.
    ///
    /// Single cubes were the loudest remaining grey-box tell; kits are what make
    /// "twenty-odd buildings" read as a village. IP note: wet-stone valley vernacular —
    /// steep dark slate over low stone bases, worked-in rather than lived-in-cute.
    /// </summary>
    public static class LayoutKits
    {
        public static IEnumerable<JObject> Expand(JObject block)
        {
            string kit = block["kit"]?.Value<string>();
            if (string.IsNullOrEmpty(kit)) { yield return block; yield break; }

            float[] pos = Vec3(block["pos"]);
            float[] size = Vec3(block["size"], 4, 3, 4);
            string name = block["name"]?.Value<string>() ?? kit;
            string wallMat = block["mat"]?.Value<string>() ?? "dark";

            switch (kit)
            {
                case "house":
                    foreach (var p in House(name, pos, size, wallMat,
                                 block["floors"]?.Value<int>() ?? 1)) yield return p;
                    break;

                case "kiln":
                    foreach (var p in Kiln(name, pos, size)) yield return p;
                    break;

                case "gearhouse":
                    foreach (var p in House(name, pos, size, wallMat, 1)) yield return p;
                    foreach (var p in Gear(name, pos, size)) yield return p;
                    break;

                case "bridge":
                    foreach (var p in Bridge(name, pos, size)) yield return p;
                    break;

                default:
                    yield return block; // unknown kit: at least render the footprint
                    break;
            }
        }

        private static IEnumerable<JObject> House(
            string name, float[] pos, float[] size, string wallMat, int floors)
        {
            float w = size[0], d = size[2];
            float wallH = 2.8f * floors;
            float baseH = 0.35f;

            yield return Spec($"{name}.Base", "stone", pos[0], pos[1] + baseH / 2, pos[2],
                w + 0.4f, baseH, d + 0.4f);
            yield return Spec($"{name}.Walls", wallMat, pos[0], pos[1] + baseH + wallH / 2, pos[2],
                w, wallH, d);

            // Door: an inset dark slab on the south face.
            yield return Spec($"{name}.Door", "bark", pos[0] + w * 0.18f, pos[1] + baseH + 1.05f,
                pos[2] - d / 2 - 0.06f, 1.0f, 2.1f, 0.12f);

            // Pitched roof: two planes meeting over a ridge that runs along x.
            float roofY = pos[1] + baseH + wallH;
            float pitch = 40f;
            float halfD = d / 2f + 0.35f;
            float planeD = halfD / (float)Math.Cos(pitch * Math.PI / 180.0) + 0.15f;
            float rise = halfD * (float)Math.Tan(pitch * Math.PI / 180.0);

            yield return Spec($"{name}.Roof.N", "slate",
                pos[0], roofY + rise / 2, pos[2] + halfD / 2,
                w + 0.7f, 0.16f, planeD, rot: new[] { pitch, 0, 0 });
            yield return Spec($"{name}.Roof.S", "slate",
                pos[0], roofY + rise / 2, pos[2] - halfD / 2,
                w + 0.7f, 0.16f, planeD, rot: new[] { -pitch, 0, 0 });
            yield return Spec($"{name}.Ridge", "slate",
                pos[0], roofY + rise, pos[2], w + 0.8f, 0.18f, 0.3f);
        }

        private static IEnumerable<JObject> Kiln(string name, float[] pos, float[] size)
        {
            float r = Math.Max(size[0], size[2]);
            yield return Spec($"{name}.Body", "stone", pos[0], pos[1] + 0.9f, pos[2],
                r, 1.8f, r, shape: "cylinder");
            yield return Spec($"{name}.Mouth", "dark", pos[0], pos[1] + 0.6f, pos[2] - r * 0.52f,
                0.9f, 1.0f, 0.4f);

            // Tapered chimney: three shrinking drums. The smoke fx anchors at its top.
            float y = pos[1] + 1.8f;
            float cr = r * 0.55f;
            for (int i = 0; i < 3; i++)
            {
                float h = 1.1f;
                yield return Spec($"{name}.Chimney{i}", "stone",
                    pos[0], y + h / 2, pos[2], cr, h, cr, shape: "cylinder");
                y += h;
                cr *= 0.78f;
            }
        }

        private static IEnumerable<JObject> Gear(string name, float[] pos, float[] size)
        {
            // A big upright gear, half-buried beside the building — Coll's landmark.
            float gx = pos[0] + size[0] / 2 + 1.6f, gz = pos[2];
            yield return Spec($"{name}.Gear", "dark", gx, pos[1] + 1.1f, gz,
                3.4f, 0.5f, 3.4f, shape: "cylinder", rot: new[] { 90f, 0, 12f });

            for (int i = 0; i < 5; i++)
            {
                double a = i * Math.PI * 2 / 5 + 0.4;
                float tx = gx + (float)Math.Cos(a) * 1.85f;
                float ty = pos[1] + 1.1f + (float)Math.Sin(a) * 1.85f;
                if (ty < pos[1] + 0.2f) continue; // buried teeth stay buried
                yield return Spec($"{name}.Tooth{i}", "dark", tx, ty, gz,
                    0.5f, 0.5f, 0.45f, rot: new[] { 0, 0, (float)(a * 180 / Math.PI) });
            }
        }

        private static IEnumerable<JObject> Bridge(string name, float[] pos, float[] size)
        {
            float w = size[0], d = size[2];
            yield return Spec($"{name}.Deck", "stone", pos[0], pos[1] + 0.12f, pos[2],
                w, 0.24f, d);
            yield return Spec($"{name}.Kerb.E", "stone", pos[0] + w / 2 - 0.15f,
                pos[1] + 0.42f, pos[2], 0.3f, 0.36f, d);
            yield return Spec($"{name}.Kerb.W", "stone", pos[0] - w / 2 + 0.15f,
                pos[1] + 0.42f, pos[2], 0.3f, 0.36f, d);
        }

        /// <summary>Path polylines become thin quads laid 3 cm proud of the ground.</summary>
        public static IEnumerable<JObject> ExpandPath(JObject path, int index)
        {
            string mat = path["mat"]?.Value<string>() ?? "earth";
            float width = path["width"]?.Value<float>() ?? 1.6f;

            if (path["points"] is not JArray points || points.Count < 2) yield break;

            for (int i = 0; i < points.Count - 1; i++)
            {
                var a = (JArray)points[i];
                var b = (JArray)points[i + 1];
                float ax = a[0].Value<float>(), az = a[1].Value<float>();
                float bx = b[0].Value<float>(), bz = b[1].Value<float>();

                float mx = (ax + bx) / 2, mz = (az + bz) / 2;
                float dx = bx - ax, dz = bz - az;
                float length = (float)Math.Sqrt(dx * dx + dz * dz) + width * 0.4f;
                float yaw = (float)(Math.Atan2(dx, dz) * 180 / Math.PI);

                yield return Spec($"Path{index}.{i}", mat, mx, 0.015f, mz,
                    width, 0.03f, length, rot: new[] { 0, yaw, 0 });
            }
        }

        private static JObject Spec(string name, string mat,
            float x, float y, float z, float sx, float sy, float sz,
            string shape = null, float[] rot = null)
        {
            var o = new JObject
            {
                ["name"] = name,
                ["mat"] = mat,
                ["pos"] = new JArray(x, y, z),
                ["size"] = new JArray(sx, sy, sz),
            };
            if (shape != null) o["shape"] = shape;
            if (rot != null) o["rot"] = new JArray(rot[0], rot[1], rot[2]);
            return o;
        }

        private static float[] Vec3(JToken t, float dx = 0, float dy = 0, float dz = 0)
        {
            if (t is JArray a && a.Count >= 3)
                return new[] { a[0].Value<float>(), a[1].Value<float>(), a[2].Value<float>() };
            return new[] { dx, dy, dz };
        }
    }
}
