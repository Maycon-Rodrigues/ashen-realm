"""Generate the Ashen Ruins modular biome as Unity-ready FBX models."""

import argparse
import math
import random
import sys
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
import vertical_slice as shapes
from environment import material, meshes, reset, validate

PALETTE = {
    "RuinsPaint": (0.33, 0.23, 0.24),
    "RuinsPaving": (0.68, 0.56, 0.61),
    "RuinsMasonry": (0.33, 0.23, 0.24),
    "RuinsCliff": (0.20, 0.08, 0.13),
    "RuinsCoal": (0.053, 0.025, 0.037),
    "RuinsFire": (1.0, 0.29, 0.017),
    "RuinsRune": (0.84, 0.075, 0.045),
}
STONE = [(0.157, 0.088, 0.126), (0.236, 0.137, 0.147), (0.134, 0.083, 0.133),
         (0.174, 0.102, 0.144), (0.192, 0.105, 0.118)]
IRON = (0.115, 0.08, 0.10)
BRONZE = (0.54, 0.27, 0.115)
RED = (0.54, 0.043, 0.063)


def painted(obj, tint):
    obj["paint"] = tint
    return obj


def rock(rng, name, x, y, base, width, depth, height, colors=STONE, surface="RuinsMasonry"):
    """A masonry unit with irregular corners and a separate chipped shoulder."""
    half_x, half_y = width / 2, depth / 2
    cut = min(width, depth, height) * rng.uniform(0.15, 0.31)
    outline = [(-half_x + cut, -half_y), (half_x - cut, -half_y), (half_x, -half_y + cut),
               (half_x, half_y - cut), (half_x - cut, half_y), (-half_x + cut, half_y),
               (-half_x, half_y - cut), (-half_x, -half_y + cut)]
    points = []
    for factor, z in ((0.91, base), (1, base + height * 0.17),
                      (1, base + height * 0.77), (0.93, base + height)):
        points += [(x + u * factor + rng.uniform(-cut, cut) * 0.14,
                    y + v * factor + rng.uniform(-cut, cut) * 0.14,
                    z + rng.uniform(-cut, cut) * 0.16) for u, v in outline]
    faces = [tuple(reversed(range(8))), tuple(range(24, 32))]
    for start in (0, 8, 16):
        for i in range(8):
            j = (i + 1) % 8
            faces.append((start + i, start + j, start + j + 8, start + i + 8))
    return painted(shapes.poly(name, points, faces, surface), rng.choice(colors))


def tile(_, variant):
    # The artwork defines the stone joints continuously across the module grid.
    points = [(-1, -1, 0.012), (1, -1, 0.012), (1, 1, 0.012), (-1, 1, 0.012)]
    painted(shapes.poly("Paving patch", points, [(0, 1, 2, 3)], "RuinsPaving"),
            (0.72, 0.64, 0.68) if variant else (0.98, 0.95, 0.98))


def debris(rng):
    for index in range(16):
        x, y = rng.uniform(-0.95, 0.95), rng.uniform(-0.56, 0.56)
        size = rng.uniform(0.09, 0.51) if index < 10 else rng.uniform(0.045, 0.13)
        chip = rock(rng, "Fallen masonry", 0, 0, -0.02, size, size * rng.uniform(0.6, 1.2),
                    size * rng.uniform(0.4, 0.79))
        chip.location.x, chip.location.y = x, y
        chip.rotation_euler.z = rng.uniform(0, math.tau)


def wall(rng, broken):
    rock(rng, "Foundation", 0, 0, -0.13, 1.96, 0.68, 0.31, [IRON])
    for course in range(3 if broken else 5):
        cut = rng.uniform(-0.37, 0.37)
        for lo, hi in ((-0.985, cut), (cut, 0.985)):
            if broken and course >= 1 and (lo > -0.9 or course == 2 and rng.random() < 0.30):
                continue
            rock(rng, "Basalt course", (lo + hi) / 2, rng.uniform(-0.018, 0.018),
                 0.17 + 0.29 * course, hi - lo - 0.016, rng.uniform(0.50, 0.59), 0.30)
    if not broken:
        for x in (-0.50, 0.50):
            rock(rng, "Coping", x, 0, 1.60, 0.986, 0.65, 0.22)


def pier(rng, x=0, height=3):
    rock(rng, "Plinth", x, 0, -0.06, 0.93, 0.93, 0.24)
    for course in range(round(height / 0.43)):
        rock(rng, "Pier course", x + rng.uniform(-0.036, 0.036), 0, 0.15 + course * 0.43,
             rng.uniform(0.68, 0.75), 0.71, 0.427)
    rock(rng, "Capital", x, 0, 0.15 + round(height / 0.43) * 0.43, 0.88, 0.86, 0.20)


def arch(rng):
    for side in (-1, 1):
        pier(rng, side * 1.49, 2.58)
    for index in range(11):
        angle = math.pi * (index + 0.5) / 11
        x, z = 1.43 * math.cos(angle), 2.92 + 1.29 * math.sin(angle)
        entry = rock(rng, "Arch voussoir", 0, 0, -0.21, 0.49, 0.78, 0.57)
        entry.rotation_euler.y = math.pi * 0.5 - angle
        entry.location = (x, 0, z)
    shapes.plate("Gate recess", (0, 0.19, 1.57), (2.40, 0.20, 3.16), "RuinsCoal", bevel=0.11)
    for side in (-1, 1):
        shapes.taper("Sigil stroke", (side * 0.47, -0.008, 1.7), (0, -0.008, 2.35),
                     0.066, 0.038, "RuinsRune", 6)
        shapes.taper("Sigil stroke", (side * 0.47, -0.008, 1.7), (0, -0.008, 1.08),
                     0.058, 0.032, "RuinsRune", 6)
    shapes.taper("Sigil spine", (0, -0.013, 1.28), (0, -0.013, 2.12), 0.05, 0.03, "RuinsFire", 6)


def banner(_):
    painted(shapes.taper("Banner rod", (-0.72, 0, 2.37), (0.72, 0, 2.37),
                         0.051, 0.051, "RuinsPaint", 8), BRONZE)
    points = []
    for j in range(5):
        for i in range(9):
            t, u = j / 4, i / 8
            points.append(((u - 0.5) * (1.2 - 0.17 * t), -0.04 - 0.14 * t + 0.061 * math.sin(u * 16),
                           2.33 - t * 1.65 + (0.24 if i in (1, 5, 8) else 0) * t ** 5))
    faces = [(j * 9 + i, (j + 1) * 9 + i, (j + 1) * 9 + i + 1, j * 9 + i + 1)
             for j in range(4) for i in range(8)]
    back = len(points)
    points.extend([(x, y + 0.015, z) for x, y, z in points])
    faces.extend([tuple(back + i for i in reversed(face)) for face in faces])
    painted(shapes.poly("Torn vermilion standard", points, faces, "RuinsPaint"), RED)
    for side in (-1, 1):
        painted(shapes.taper("Stitched emblem", (side * 0.25, -0.21, 1.53), (0, -0.22, 1.85),
                0.025, 0.014, "RuinsPaint", 5), (0.80, 0.32, 0.20))
        painted(shapes.taper("Stitched emblem", (side * 0.25, -0.21, 1.53), (0, -0.22, 1.27),
                0.025, 0.014, "RuinsPaint", 5), (0.80, 0.32, 0.20))


def brazier(rng):
    rock(rng, "Brazier foot", 0, 0, 0, 0.80, 0.80, 0.24)
    painted(shapes.taper("Cast pedestal", (0, 0, 0.20), (0, 0, 0.92), 0.31, 0.18, "RuinsPaint", 8), IRON)
    painted(shapes.taper("Cast bowl", (0, 0, 0.88), (0, 0, 1.19), 0.22, 0.53, "RuinsPaint", 10), IRON)
    painted(shapes.taper("Copper lip", (0, 0, 1.17), (0, 0, 1.24), 0.53, 0.56, "RuinsPaint", 10), BRONZE)
    for i in range(8):
        a = i * math.tau / 8
        x, y = math.cos(a), math.sin(a)
        painted(shapes.taper("Brazier strap", (x * 0.22, y * 0.22, 0.91),
                             (x * 0.53, y * 0.53, 1.17), 0.049, 0.036, "RuinsPaint", 5), BRONZE)
    rock(rng, "Banked coals", 0, 0, 1.19, 0.70, 0.70, 0.062, [IRON], "RuinsPaint")


def briar(rng):
    for i in range(9):
        a = i * math.tau / 9
        x, y = math.cos(a), math.sin(a)
        end = (x * rng.uniform(0.38, 0.87), y * rng.uniform(0.29, 0.67), rng.uniform(0.30, 1.15))
        painted(shapes.taper("Briar branch", (0, 0, 0.02), end, 0.066, 0.014, "RuinsPaint", 5), IRON)


def ash_tree(rng):
    spine = [(0, 0, 0), (-0.20, 0.11, 1.0), (0.07, 0.30, 2.24), (-0.40, 0.41, 3.10),
             (-0.52, 0.51, 4.20)]
    bark = (0.105, 0.044, 0.047)
    for i, (start, end) in enumerate(zip(spine, spine[1:])):
        painted(shapes.taper("Twisted ash trunk", start, end, 0.31 - 0.063 * i,
                             0.25 - 0.063 * i, "RuinsPaint", 7), bark)
    for i in range(8):
        angle = i * math.tau / 8
        u, v = math.cos(angle), math.sin(angle)
        start = spine[1 + (i % 3)]
        end = (u * rng.uniform(0.84, 1.84), v * rng.uniform(0.74, 1.37), rng.uniform(2.85, 4.42))
        painted(shapes.taper("Charred limb", start, end, 0.119, 0.024, "RuinsPaint", 6), bark)
    for i in range(5):
        angle = i * math.tau / 5
        painted(shapes.taper("Ash root", (0, 0, 0.35), (math.cos(angle), math.sin(angle), 0.03),
                             0.19, 0.028, "RuinsPaint", 6), bark)


def cliff(rng):
    for i in range(7):
        x = (i - 3) * 0.57
        depth = rng.uniform(0.5, 1.62)
        height = rng.uniform(3.0, 7.2)
        rock(rng, "Broken basalt organ", x, rng.uniform(-0.2, 0.2), -height, 0.70, depth, height,
             [(0.147, 0.070, 0.10), (0.198, 0.091, 0.12), (0.101, 0.052, 0.081)], "RuinsCliff")


def rune(_):
    for ring, segments in ((1.07, 40), (0.90, 32)):
        for i in range(segments):
            if (i % 11 == 3):
                continue
            a, b = i * math.tau / segments, (i + 0.90) * math.tau / segments
            points = [(r * math.cos(t), r * math.sin(t), 0.039) for r, t in
                      ((ring, a), (ring, b), (ring - 0.030, b), (ring - 0.030, a))]
            shapes.poly("Chipped seal", points, [(0, 1, 2, 3)], "RuinsRune")
    points = [(0, 0.62), (-0.41, 0.16), (0, -0.63), (0.41, -0.16), (0, 0.62), (0.04, -0.29), (-0.41, 0.16)]
    for (x, y), (u, v) in zip(points, points[1:]):
        shapes.taper("Seal glyph", (x, y, 0.042), (u, v, 0.042), 0.031, 0.018, "RuinsRune", 5)


def combine(rng):
    bpy.context.view_layer.update()
    grouped = {}
    for obj in meshes():
        surface = obj.data.materials[0].name
        points, faces, paint = grouped.setdefault(surface, ([], [], []))
        offset = len(points)
        tint = obj.get("paint", PALETTE[surface])
        local_z = [vertex.co.z for vertex in obj.data.vertices]
        bottom = min(local_z)
        span = max(0.01, max(local_z) - bottom)
        for vertex in obj.data.vertices:
            points.append(obj.matrix_world @ vertex.co)
            edge = 0.69 + 0.36 * (vertex.co.z - bottom) / span + rng.uniform(-0.075, 0.075)
            paint.extend((*[min(1, value * edge) for value in tint], 1))
        for face in obj.data.polygons:
            faces.append(tuple(offset + i for i in face.vertices))
        bpy.data.objects.remove(obj, do_unlink=True)
    parts = []
    for surface, (points, faces, paint) in sorted(grouped.items()):
        result = shapes.poly(surface, points, faces, surface)
        colors = result.data.color_attributes.new(name="Pigment", type="BYTE_COLOR", domain="POINT")
        colors.data.foreach_set("color", paint)
        parts.append(result)
    return parts


MODULES = {
    "Flagstone": lambda rng: tile(rng, 0),
    "ScorchedFlagstone": lambda rng: tile(rng, 1),
    "Wall": lambda rng: wall(rng, False),
    "BrokenWall": lambda rng: wall(rng, True),
    "Pillar": pier,
    "SealedArch": arch,
    "Banner": banner,
    "Brazier": brazier,
    "CrimsonBriar": briar,
    "AshTree": ash_tree,
    "Cliff": cliff,
    "AshSeal": rune,
    "FallenMasonry": debris,
}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--asset", choices=MODULES)
    parser.add_argument("--seed", type=int, default=191)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:])
    args.output_dir.mkdir(parents=True, exist_ok=True)
    for name, build in MODULES.items():
        if args.asset and args.asset != name:
            continue
        reset()
        rng = random.Random(f"{args.seed}:{name}")
        shapes.PALETTE.clear()
        shapes.PALETTE.update({key: material(key, (*color, 1)) for key, color in PALETTE.items()})
        build(rng)
        parts = combine(rng)
        count = validate(parts, 8000)
        file = (args.output_dir / f"{name}.fbx").resolve()
        bpy.ops.object.select_all(action="DESELECT")
        for part in parts:
            part.select_set(True)
        bpy.context.view_layer.objects.active = parts[0]
        bpy.ops.export_scene.fbx(filepath=str(file), use_selection=True, object_types={"MESH"},
                                 axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_UNITS",
                                 bake_anim=False, colors_type="LINEAR")
        reset()
        bpy.ops.import_scene.fbx(filepath=str(file), colors_type="LINEAR")
        imported = meshes()
        if validate(imported, 8000) != count or any(not obj.data.color_attributes for obj in imported):
            raise ValueError(f"FBX round trip changed {name}")
        print(f"[Ashen Ruins] {name}: {count} triangles, pigment and geometry verified.", flush=True)


if __name__ == "__main__":
    main()
