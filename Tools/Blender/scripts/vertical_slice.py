"""Build the original low-poly characters, weapon, and ruin kit for CombatSandbox."""

import argparse
import math
import random
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from environment import export, finish, import_asset, material, meshes, normalize, reset, validate


def plate(name, center, size, surface, bevel=0.035, tilt=0, segments=1):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = finish(f"{surface}.{name}", size, PALETTE[surface])
    obj.rotation_euler.y = tilt
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    modifier = obj.modifiers.new("Edge", "BEVEL")
    modifier.width = min(bevel, min(size) * 0.38)
    modifier.segments = segments
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    if segments > 1:
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
        normals = obj.modifiers.new("Keep plate faces flat", "WEIGHTED_NORMAL")
        bpy.ops.object.modifier_apply(modifier=normals.name)
    return obj


def facet(name, center, size, surface, detail=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=detail, radius=1, location=center)
    return finish(f"{surface}.{name}", size, PALETTE[surface])


def sculpt(name, center, size, surface, pitch=0, roll=0):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=1, location=center)
    obj = finish(f"{surface}.{name}", size, PALETTE[surface])
    obj.rotation_euler = (pitch, roll, 0)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    return obj


def taper(name, start, end, radius, tip, surface, sides=5, smooth=False):
    vector = Vector(end) - Vector(start)
    bpy.ops.mesh.primitive_cone_add(vertices=sides, radius1=radius, radius2=tip,
                                   depth=vector.length, location=Vector(start) + vector * 0.5)
    obj = finish(f"{surface}.{name}", (1, 1, 1), PALETTE[surface])
    obj.rotation_euler = vector.to_track_quat("Z", "Y").to_euler()
    if smooth:
        for polygon in obj.data.polygons:
            polygon.use_smooth = len(polygon.vertices) == 4
    return obj


def poly(name, points, faces, surface):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(points, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(f"{surface}.{name}", mesh)
    bpy.context.collection.objects.link(obj)
    mesh.materials.append(PALETTE[surface])
    return obj


def curved_limb(name, profile, surface):
    sides = 14
    centers = [Vector(point) for point, _ in profile]
    points = []
    for index, (center, (_, radius)) in enumerate(zip(centers, profile)):
        tangent = (centers[min(index + 1, len(centers) - 1)] -
                   centers[max(index - 1, 0)]).normalized()
        right = tangent.cross(Vector((0, 1, 0))).normalized()
        up = tangent.cross(right)
        for side in range(sides):
            angle = side * math.tau / sides
            points.append(center + radius * (math.cos(angle) * right + math.sin(angle) * up))

    faces = [tuple(reversed(range(sides)))]
    for start in range(0, (len(profile) - 1) * sides, sides):
        for side in range(sides):
            neighbor = (side + 1) % sides
            faces.append((start + side, start + neighbor, start + neighbor + sides, start + side + sides))
    faces.append(tuple(range(len(points) - sides, len(points))))
    obj = poly(name, points, faces, surface)
    for polygon in obj.data.polygons:
        polygon.use_smooth = len(polygon.vertices) == 4


def knight():
    for side in (-1, 1):
        leg = "LeftLeg" if side < 0 else "RightLeg"
        x = side * 0.19
        plate(leg + "Boot", (x, -0.10, 0.14), (0.22, 0.37, 0.25), "Iron", segments=3)
        plate(leg + "Greave", (x, -0.035, 0.38), (0.19, 0.23, 0.40), "Steel", bevel=0.048, segments=3)
        plate(leg + "Thigh", (x, 0.02, 0.69), (0.23, 0.26, 0.33), "Iron")
        sculpt(leg + "Knee", (x, -0.136, 0.50), (0.12, 0.10, 0.115), "Steel")
        width = 0.37 if side < 0 else 0.29
        for layer in range(2):
            plate("Pauldron", (side * (0.34 + layer * 0.043), 0.005, 1.42 - layer * 0.064),
                  (width - layer * 0.042, 0.37 - layer * 0.035, 0.166), "Steel",
                  bevel=0.056, tilt=side * 0.43, segments=3)
        plate("Arm", (side * 0.46, -0.01, 1.17), (0.19, 0.21, 0.36), "Iron", tilt=side * -0.15, segments=3)
        plate("Gauntlet", (side * 0.46, -0.12, 1.02), (0.18, 0.31, 0.19), "Steel", bevel=0.045, segments=3)

    plate("Cuisses", (0, 0.018, 0.87), (0.54, 0.36, 0.22), "Iron")
    plate("Belt", (0, -0.012, 0.99), (0.58, 0.41, 0.12), "Leather")
    plate("Buckle", (0, -0.228, 0.99), (0.15, 0.05, 0.12), "Brass")
    sculpt("Chest", (0, -0.035, 1.265), (0.37, 0.255, 0.35), "Steel", pitch=0.08)
    for layer in range(3):
        plate("Lame", (0, -0.19 + layer * 0.01, 1.23 - layer * 0.074),
              (0.50 - 0.05 * layer, 0.13, 0.095), "Steel", tilt=-0.04, segments=2)
    plate("Neck", (0, 0.01, 1.51), (0.26, 0.29, 0.16), "Iron")
    sculpt("Helm", (0, 0.015, 1.73), (0.193, 0.29, 0.265), "Steel", pitch=-0.16)
    taper("SalletTail", (0, 0.15, 1.67), (0, 0.42, 1.51), 0.154, 0.069, "Steel", 12, smooth=True)
    plate("Faceplate", (0, -0.247, 1.66), (0.315, 0.18, 0.139), "Iron", bevel=0.041, segments=3)
    plate("Bevor", (0, -0.202, 1.565), (0.27, 0.20, 0.13), "Steel", bevel=0.046)
    plate("FacingVisor", (0, -0.344, 1.687), (0.148, 0.027, 0.018), "Spirit", bevel=0.006)
    plate("Crest", (0, -0.005, 1.979), (0.044, 0.24, 0.046), "Brass", bevel=0.014)
    plate("MantleClasp", (-0.22, -0.227, 1.39), (0.13, 0.042, 0.14), "Brass")
    plate("Shield", (-0.49, -0.22, 1.02), (0.35, 0.11, 0.55), "Iron", bevel=0.085, tilt=0.20)
    plate("ShieldRidge", (-0.49, -0.28, 1.04), (0.12, 0.035, 0.33), "Brass", tilt=0.20)
    cape = poly("Mantle", [(-0.39, 0.12, 1.47), (0.21, 0.18, 1.37), (0.15, 0.31, 0.94),
                    (0.19, 0.46, 0.70), (-0.09, 0.46, 0.44), (-0.38, 0.41, 0.27),
                    (-0.26, 0.29, 1.0), (0.01, 0.30, 0.98)],
         [(0, 7, 1), (0, 6, 7), (1, 7, 2), (2, 7, 3), (3, 7, 4), (4, 7, 5),
          (5, 7, 6)], "Mantle")
    bpy.context.view_layer.objects.active = cape
    for polygon in cape.data.polygons:
        polygon.use_smooth = True
    drape = cape.modifiers.new("Drape folds", "SUBSURF")
    drape.subdivision_type = "CATMULL_CLARK"
    drape.levels = 2
    bpy.ops.object.modifier_apply(modifier=drape.name)
    cloth = cape.modifiers.new("Cloth thickness", "SOLIDIFY")
    cloth.thickness = 0.018
    bpy.ops.object.modifier_apply(modifier=cloth.name)


def grunt():
    for side in (-1, 1):
        leg = "LeftLeg" if side < 0 else "RightLeg"
        x = side * 0.25
        sculpt(leg + "Paw", (x, -0.15, 0.16), (0.16, 0.27, 0.15), "Carapace")
        taper(leg + "Shin", (x, -0.02, 0.16), (x, 0.10, 0.56), 0.12, 0.18, "Hide", 12, smooth=True)
        taper(leg + "Thigh", (x, 0.10, 0.55), (x * 0.8, 0.08, 0.93), 0.18, 0.22, "Carapace", 12, smooth=True)
        prefix = "StrikeArm" if side > 0 else "SupportArm"
        size = 1.34 if side > 0 else 0.82
        shoulder = (side * 0.44, 0.08, 1.31)
        elbow = (side * 0.60, -0.08, 0.91)
        fist = (side * (0.68 if side > 0 else 0.53), -0.38, 0.49 if side > 0 else 0.68)
        sculpt(prefix + "Shoulder", shoulder, (0.235 * size, 0.27, 0.29), "Carapace", roll=side * 0.45)
        upper = Vector(shoulder).lerp(Vector(elbow), 0.48)
        forearm = Vector(elbow).lerp(Vector(fist), 0.57)
        curved_limb(prefix + "Muscle", ((shoulder, 0.16 * size), (upper, 0.193 * size),
                    (elbow, 0.141 * size), (forearm, 0.193 * size), (fist, 0.151 * size)), "Hide")
        sculpt(prefix + "Fist", fist, (0.174 * size, 0.20 * size, 0.15 * size), "Carapace")
        taper(prefix + "Ridge", (elbow[0], -0.16, 0.87),
              (side * 0.83, -0.19, 0.97), 0.068 * size, 0.009, "Bone")
        for claw in range(3):
            x_claw = fist[0] + (claw - 1) * 0.08 * size
            taper(prefix + "Claw", (x_claw, -0.52, fist[2]),
                  (x_claw, -0.66, fist[2] - 0.15), 0.032 * size, 0.007, "Bone")
        start = (side * 0.20, -0.20, 1.60)
        bend = (side * (0.35 if side < 0 else 0.45), -0.13, 1.69)
        taper("AntlerBase", start, bend, 0.105, 0.069, "Bone", 10, smooth=True)
        taper("AntlerTip", bend, (side * 0.49, 0.12, 1.86 if side < 0 else 1.76), 0.071, 0.009, "Bone", 10, smooth=True)
        for level in range(3):
            z = 0.99 + level * 0.105
            taper("Rib", (side * 0.085, -0.30, z), (side * 0.31, -0.20, z + 0.08), 0.049, 0.022, "Carapace")

    sculpt("Spine", (0, 0.18, 1.12), (0.47, 0.43, 0.49), "Carapace", pitch=0.33)
    sculpt("Chest", (0, -0.055, 1.16), (0.405, 0.325, 0.375), "Hide", pitch=0.30)
    facet("Core", (0, -0.336, 1.13), (0.105, 0.072, 0.135), "Ember", 2)
    sculpt("Skull", (0, -0.34, 1.43), (0.22, 0.31, 0.20), "Carapace", pitch=0.26)
    sculpt("Jaw", (0, -0.47, 1.32), (0.162, 0.22, 0.111), "Hide")
    for side in (-1, 1):
        plate("FacingEye" + ("A" if side < 0 else "B"), (side * 0.115, -0.588, 1.46),
              (0.10, 0.048, 0.047), "Ember", bevel=0.013, tilt=side * 0.24)
    for index in range(4):
        taper("DorsalSpur", (0, 0.42 - index * 0.028, 0.94 + index * 0.145),
              (0, 0.59 - index * 0.034, 1.04 + index * 0.15), 0.081, 0.006, "Bone")


def sword():
    points = []
    for z, width, thickness in ((0.22, 0.105, 0.036), (0.41, 0.13, 0.047),
                                  (0.89, 0.07, 0.032), (1.08, 0.018, 0.013)):
        points.extend([(-width, 0, z), (0, -thickness, z), (width, 0, z), (0, thickness, z)])
    faces = [(3, 2, 1, 0)]
    for start in range(0, 12, 4):
        for side in range(4):
            neighbor = (side + 1) % 4
            faces.append((start + side, start + neighbor, start + 4 + neighbor, start + 4 + side))
    faces.append((12, 13, 14, 15))
    poly("Blade", points, faces, "Steel")
    plate("Guard", (0, 0, 0.24), (0.44, 0.12, 0.08), "Brass", bevel=0.033, tilt=0.22)
    plate("Grip", (0, 0, 0.125), (0.085, 0.09, 0.22), "Leather", bevel=0.016)
    facet("Pommel", (0, 0, 0.05), (0.07, 0.072, 0.055), "Spirit")


def merge_surfaces():
    by_material = {}
    for obj in meshes():
        by_material.setdefault(obj.data.materials[0].name, []).append(obj)
    for label, group in sorted(by_material.items()):
        bpy.ops.object.select_all(action="DESELECT")
        for obj in group:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = group[0]
        if len(group) > 1:
            bpy.ops.object.join()
        group[0].name = label


def floor():
    rng = random.Random(132)
    for row in range(12):
        y = (row - 5.5) * 1.55
        on_path = abs(y) < 2.35
        for column in range(15):
            x = (column - 7) * 1.56
            if not on_path and abs(y) > 3.75 and rng.random() < 0.19:
                continue
            surface = rng.choice(("Path", "Path", "Stone")) if on_path else rng.choice(
                ("Stone", "Slate", "Slate", "Moss"))
            offset = 0 if on_path else rng.uniform(-0.10, 0.10)
            slab = plate("Flagstone", (x + offset, y, 0.025),
                         (rng.uniform(1.38, 1.47), rng.uniform(1.39, 1.48), 0.064),
                         surface, bevel=0.026)
            slab.rotation_euler.z = rng.uniform(-0.017, 0.017) if on_path else rng.uniform(-0.09, 0.09)
            if not on_path and abs(y) > 3.75 and rng.random() < 0.26:
                fragment = facet("LooseFragment", (x + 0.69, y + rng.uniform(-0.54, 0.54), 0.045),
                                 (rng.uniform(0.19, 0.34), 0.18, 0.055), "Slate")
                fragment.rotation_euler.z = rng.uniform(-0.65, 0.65)
    merge_surfaces()


def vigil():
    plate("Plinth", (0, 0, 0.105), (1.5, 1.02, 0.21), "Slate", bevel=0.068)
    plate("Stipes", (0.03, 0, 0.40), (1.10, 0.73, 0.46), "Stone", bevel=0.074)
    plate("Crown", (-0.04, 0, 0.72), (1.50, 0.98, 0.22), "Path", bevel=0.062)
    plate("FoldedVigilCloth", (0.22, -0.30, 0.838), (0.54, 0.48, 0.031), "Mantle", bevel=0.01)
    plate("HangingCloth", (0.22, -0.52, 0.65), (0.52, 0.041, 0.32), "Mantle", bevel=0.01)
    plate("ClothHem", (0.22, -0.551, 0.505), (0.50, 0.025, 0.038), "Brass", bevel=0.009)
    taper("Vessel", (-0.24, -0.04, 0.84), (-0.24, -0.04, 1.06), 0.103, 0.165, "Iron", 8)
    facet("Ash", (-0.24, -0.04, 1.08), (0.14, 0.14, 0.031), "Slate", 2)
    for x, y, height in ((0.42, 0.13, 0.17), (0.51, -0.12, 0.11), (-0.49, 0.18, 0.13)):
        taper("Candle", (x, y, 0.83), (x, y, 0.83 + height), 0.046, 0.040, "Bone", 6)
        facet("Flame", (x, y, 0.88 + height), (0.022, 0.022, 0.059), "Ember")
    merge_surfaces()


def gate():
    rng = random.Random(289)
    for side in (-1, 1):
        x = side * 1.61
        plate("Plinth", (x, 0, 0.15), (1.18, 1.12, 0.30), "Slate", bevel=0.08)
        for course in range(5):
            plate("Pier", (x + rng.uniform(-0.028, 0.028), 0, 0.49 + course * 0.47),
                  (0.91, 0.92, 0.45), rng.choice(("Stone", "Slate")), bevel=0.055)
        plate("Capital", (x, 0, 2.67), (1.16, 1.07, 0.22), "Path", bevel=0.06)
        plate("Torch", (side * 1.47, -0.61, 1.78), (0.36, 0.31, 0.18), "Iron", bevel=0.04)
    for index in range(-4, 5):
        angle = index * math.pi / 8
        plate("Arch", (math.sin(angle) * 1.62, 0, 2.71 + math.cos(angle) * 1.11),
              (0.60, 0.85, 0.48), "Path" if index == 0 else "Stone",
              bevel=0.065, tilt=angle)
    for index in range(-3, 4):
        x = index * 0.30
        plate("Bar", (x, 0.31, 1.42), (0.076, 0.10, 2.80), "Iron", bevel=0.018)
        taper("BarTip", (x, 0.31, 2.80), (x, 0.31, 3.13), 0.065, 0.008, "Iron", 4)
    for height in (0.55, 1.75, 2.70):
        plate("Brace", (0, 0.29, height), (2.12, 0.12, 0.11), "Iron", bevel=0.018)
    merge_surfaces()


COLORS = {
    "Iron": (0.11, 0.14, 0.17), "Steel": (0.29, 0.39, 0.46), "Brass": (0.57, 0.37, 0.17),
    "Leather": (0.20, 0.11, 0.07), "Spirit": (0.20, 0.85, 0.71), "Mantle": (0.12, 0.29, 0.28),
    "Carapace": (0.25, 0.12, 0.12), "Hide": (0.48, 0.20, 0.14), "Bone": (0.72, 0.54, 0.32),
    "Ember": (1.0, 0.44, 0.17), "Stone": (0.21, 0.23, 0.24), "Slate": (0.15, 0.18, 0.19),
    "Moss": (0.16, 0.22, 0.19), "Path": (0.26, 0.27, 0.24),
}
PALETTE = {}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir", required=True, type=Path)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    args.output_dir.mkdir(parents=True, exist_ok=True)
    for name, build in (("FallenKnight", knight), ("CorruptedGrunt", grunt), ("AshenSword", sword),
                        ("ArenaPaving", floor), ("AshGate", gate), ("VigilAltar", vigil)):
        reset()
        PALETTE.clear()
        PALETTE.update({label: material(label, (*rgb, 1)) for label, rgb in COLORS.items()})
        build()
        parts = meshes()
        normalize(parts, 1, 0)
        triangles = validate(parts, 14000)
        path = (args.output_dir / f"{name}.fbx").resolve()
        export(parts, path)
        reset()
        import_asset(path)
        assert validate(meshes(), 14000) == triangles, f"Triangle count changed for {name}"
        print(f"[Ashen Realm] {name}: {triangles} triangles, FBX round trip OK.", flush=True)


if __name__ == "__main__":
    main()
