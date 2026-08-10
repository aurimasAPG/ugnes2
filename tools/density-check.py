#!/usr/bin/env python3
"""Interest-density arithmetic over the layout — the pre-walk half of the Phase 3 gate.

The gate itself is a human with a stopwatch (a layout property can hide sightline
problems arithmetic can't see), but the 40-second rule is checkable geometrically:
walk every authored path polyline at 4.5 m/s and report the longest stretch that has
no interactable, landmark building, lamp, bridge or tree within noticing range.

    tools/density-check.py            # report
    tools/density-check.py --fail 30  # nonzero exit if any gap exceeds N meters
"""
import json
import math
import sys

LAYOUT = "Assets/StreamingAssets/Layout/heartwood.json"
WALK_SPEED = 4.5           # m/s — systems-README §10
NOTICE_RADIUS = 12.0       # something within 12 m counts as "there"
SAMPLE_STEP = 2.0          # meters between samples along a path

def main():
    layout = json.load(open(LAYOUT))

    points_of_interest = []
    for wo in layout.get("worldObjects", []):
        p = wo.get("pos")
        if p:
            points_of_interest.append((p[0], p[2], wo["id"]))
    for b in layout.get("blocks", []):
        name = b.get("name", "")
        interesting = (
            b.get("kit") in ("house", "kiln", "gearhouse", "bridge")
            or b.get("mat") == "lamp"
            or name.startswith("Tree")
            or b.get("fx")
        )
        if interesting and b.get("pos"):
            p = b["pos"]
            points_of_interest.append((p[0], p[2], name))
    for n in layout.get("npcs", []):
        p = n.get("pos")
        if p:
            points_of_interest.append((p[0], p[2], n["id"]))

    worst_gap, worst_where, worst_path = 0.0, None, None

    for pi, path in enumerate(layout.get("paths", [])):
        pts = path.get("points", [])
        gap_start = None
        distance = 0.0
        prev = None
        for a, b in zip(pts, pts[1:]):
            seg = math.dist(a, b)
            steps = max(1, int(seg / SAMPLE_STEP))
            for s in range(steps + 1):
                t = s / steps
                x = a[0] + (b[0] - a[0]) * t
                z = a[1] + (b[1] - a[1]) * t
                if prev is not None:
                    distance += math.dist(prev, (x, z))
                prev = (x, z)

                near = any(math.dist((x, z), (px, pz)) <= NOTICE_RADIUS
                           for px, pz, _ in points_of_interest)
                if near:
                    gap_start = distance
                else:
                    if gap_start is None:
                        gap_start = distance
                    gap = distance - gap_start
                    if gap > worst_gap:
                        worst_gap, worst_where, worst_path = gap, (x, z), pi

    budget = 40 * WALK_SPEED  # the literal rule: 40 s of walking
    print(f"points of interest: {len(points_of_interest)}")
    print(f"paths checked: {len(layout.get('paths', []))}")
    print(f"longest empty stretch: {worst_gap:.1f} m "
          f"(rule budget {budget:.0f} m; authoring target 30 m)")
    if worst_where:
        print(f"  at ({worst_where[0]:.0f}, {worst_where[1]:.0f}) on path {worst_path}")

    if "--fail" in sys.argv:
        limit = float(sys.argv[sys.argv.index("--fail") + 1])
        if worst_gap > limit:
            print(f"FAIL: exceeds {limit} m")
            sys.exit(1)
    print("OK")

if __name__ == "__main__":
    main()
