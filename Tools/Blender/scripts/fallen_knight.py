"""Build the concept-led Fallen Knight as a painted, skinned Unity character."""

import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Euler, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from environment import material, reset, validate


PALETTE = {
    "KnightPlate": (0.28, 0.34, 0.39),
    "KnightEdge": (0.43, 0.49, 0.49),
    "KnightLeather": (0.16, 0.105, 0.082),
    "KnightCloth": (0.17, 0.35, 0.29),
    "KnightHem": (0.26, 0.42, 0.34),
    "KnightBronze": (0.60, 0.42, 0.22),
    "KnightVisor": (0.24, 0.71, 0.61),
}
SURFACES = {}
PIECES = []


def hold(obj, surface, attachment, smooth=True):
    obj.data.materials.append(SURFACES[surface])
    if smooth:
        for face in obj.data.polygons:
            face.use_smooth = True
    PIECES.append((obj, attachment))
    return obj


def mesh(name, points, faces, surface, attachment):
    data = bpy.data.meshes.new(name)
    data.from_pydata(points, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    return hold(obj, surface, attachment)


def solidify(obj, thickness):
    bpy.context.view_layer.objects.active = obj
    modifier = obj.modifiers.new("Fabric thickness", "SOLIDIFY")
    modifier.thickness = thickness
    bpy.ops.object.modifier_apply(modifier=modifier.name)


def rings(name, profile, surface, attachment, sides=24, form=1.0):
    """Loft measured cross-sections along a curved axis."""
    points = []
    centers = [Vector(center) for center, _, _ in profile]
    for index, (center, (_, wide, deep)) in enumerate(zip(centers, profile)):
        tangent = (centers[min(len(centers) - 1, index + 1)] -
                   centers[max(0, index - 1)]).normalized()
        reference = Vector((1, 0, 0))
        if abs(reference.dot(tangent)) > 0.94:
            reference = Vector((0, 1, 0))
        right = (reference - reference.dot(tangent) * tangent).normalized()
        up = tangent.cross(right)
        for side in range(sides):
            angle = math.tau * side / sides
            cosine, sine = math.cos(angle), math.sin(angle)
            across = math.copysign(abs(cosine) ** form, cosine)
            forward = math.copysign(abs(sine) ** form, sine)
            points.append(center + across * wide * right + forward * deep * up)

    count = len(points)
    faces = [tuple(range(sides - 1, -1, -1))]
    for start in range(0, count - sides, sides):
        for side in range(sides):
            next_side = (side + 1) % sides
            faces.append((start + side, start + next_side, start + next_side + sides, start + side + sides))
    faces.append(tuple(range(count - sides, count)))
    obj = mesh(name, points, faces, surface, attachment)
    obj.data.polygons[0].use_smooth = False
    obj.data.polygons[-1].use_smooth = False
    return obj


def panel(name, outline, front_y, thickness, surface, bone, bevel=0.012):
    count = len(outline)
    area = sum(x * outline[(i + 1) % count][1] - z * outline[(i + 1) % count][0]
               for i, (x, z) in enumerate(outline))
    if area < 0:
        outline = list(reversed(outline))
    points = [(x, front_y, z) for x, z in outline] + [(x, front_y + thickness, z) for x, z in outline]
    faces = [tuple(range(count)), tuple(reversed(range(count, count * 2)))]
    for i in range(count):
        j = (i + 1) % count
        faces.append((i, i + count, j + count, j))
    obj = mesh(name, points, faces, surface, bone)
    bpy.context.view_layer.objects.active = obj
    modifier = obj.modifiers.new("Rounded armor edge", "BEVEL")
    modifier.width = min(bevel, thickness * 0.37)
    modifier.segments = 2
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    normals = obj.modifiers.new("Plate normals", "WEIGHTED_NORMAL")
    bpy.ops.object.modifier_apply(modifier=normals.name)
    return obj


def rivet(name, position, radius, attachment, surface="KnightBronze"):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=6, radius=radius, location=position)
    obj = bpy.context.object
    obj.name = name
    hold(obj, surface, attachment)


def armature():
    skeleton = bpy.data.armatures.new("Fallen Knight Skeleton")
    rig = bpy.data.objects.new("FallenKnightRig", skeleton)
    bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")

    def add(name, head, tail, parent=None, deform=True):
        bone = skeleton.edit_bones.new(name)
        bone.head = head
        bone.tail = tail
        bone.use_deform = deform
        if parent:
            bone.parent = skeleton.edit_bones[parent]

    add("Root", (0, 0, 0), (0, 0, 0.17), deform=False)
    add("Pelvis", (0, 0.025, 0.94), (0, 0.015, 1.04), "Root")
    add("Chest", (0, 0.015, 1.04), (0, 0.015, 1.47), "Pelvis")
    add("Head", (0, 0, 1.47), (0, -0.005, 1.91), "Chest")
    add("CloakUpper", (-0.18, 0.164, 1.42), (-0.09, 0.275, 1.02), "Chest")
    add("CloakLower", (-0.09, 0.275, 1.02), (-0.15, 0.43, 0.38), "CloakUpper")
    add("Tabard", (0, -0.19, 0.99), (0.04, -0.20, 0.57), "Pelvis")
    for side, suffix in ((-1, "L"), (1, "R")):
        hip = (0.13 * side, 0.021, 0.97)
        knee = (0.161 * side, -0.036, 0.56)
        ankle = (0.168 * side, 0.042, 0.19)
        shoulder = (0.25 * side, 0.014, 1.42)
        elbow = (0.37 * side, -0.015, 1.20)
        wrist = (0.423 * side, -0.165, 1.055)
        hand = (0.431 * side, -0.246, 1.044)
        add(f"Thigh.{suffix}", hip, knee, "Pelvis")
        add(f"Shin.{suffix}", knee, ankle, f"Thigh.{suffix}")
        add(f"Foot.{suffix}", ankle, (0.169 * side, -0.157, 0.075), f"Shin.{suffix}")
        add(f"Arm.{suffix}", shoulder, elbow, "Chest")
        add(f"Forearm.{suffix}", elbow, wrist, f"Arm.{suffix}")
        add(f"Hand.{suffix}", wrist, hand, f"Forearm.{suffix}")

    add("SwordGrip", (0.431, -0.250, 1.045), (0.431, -0.357, 1.045), "Hand.R", deform=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    rig.show_in_front = True
    return rig


def torso():
    rings("Leather foundation", [
        ((0, 0.022, 0.91), 0.176, 0.13), ((0, 0.022, 0.99), 0.19, 0.14),
        ((0, 0.027, 1.12), 0.16, 0.149), ((0, 0.015, 1.35), 0.237, 0.161),
        ((0, 0.025, 1.45), 0.20, 0.116)], "KnightLeather", "Chest")
    rings("Cuirass", [
        ((0, 0.008, 1.025), 0.158, 0.133), ((0, 0.008, 1.064), 0.167, 0.151),
        ((0, -0.012, 1.18), 0.192, 0.159), ((0, -0.015, 1.31), 0.235, 0.171),
        ((0, 0.002, 1.42), 0.224, 0.139), ((0, 0.012, 1.455), 0.14, 0.102)],
        "KnightPlate", "Chest", form=0.76)
    for index in range(3):
        at = 1.057 - index * 0.041
        rings("Waist lame", [
            ((0, 0.004, at - 0.037), 0.164 + index * 0.014, 0.145),
            ((0, 0.004, at - 0.012), 0.17 + index * 0.014, 0.153),
            ((0, 0.004, at + 0.016), 0.158 + index * 0.014, 0.140)],
            "KnightPlate", "Pelvis", form=0.74)
    rings("Belt", [((0, 0.007, 0.958), 0.192, 0.153), ((0, 0.007, 0.994), 0.192, 0.153)],
        "KnightLeather", "Pelvis", form=0.72)
    panel("Belt buckle", [(-0.052, 0.954), (0.045, 0.954), (0.045, 1.003), (-0.052, 1.003)],
        -0.159, 0.019, "KnightBronze", "Pelvis", 0.007)

    for side in (-1, 1):
        for index in range(3):
            rivet("Cuirass rivet", (side * (0.159 + 0.012 * index), -0.144, 1.144 + index * 0.077), 0.011, "Chest")
    panel("Vigil token", [(-0.127, 1.284), (-0.084, 1.307), (-0.061, 1.339), (-0.081, 1.372), (-0.124, 1.358)],
        -0.177, 0.022, "KnightBronze", "Chest", 0.008)
    panel("Forged breastplate ridge", [(-0.143, 1.213), (0, 1.176), (0.143, 1.213),
        (0.105, 1.221), (0, 1.196), (-0.105, 1.221)], -0.191, 0.029, "KnightEdge", "Chest", 0.006)
    panel("Gorget", [(-0.120, 1.38), (0.120, 1.38), (0.100, 1.492), (0.061, 1.534),
        (-0.061, 1.534), (-0.100, 1.492)], -0.125, 0.095, "KnightEdge", "Chest", 0.026)


def helmet():
    rings("Sallet", [
        ((0, 0.069, 1.53), 0.114, 0.171), ((0, 0.042, 1.568), 0.135, 0.163),
        ((0, 0.011, 1.63), 0.155, 0.166), ((0, -0.006, 1.71), 0.153, 0.169),
        ((0, 0.007, 1.778), 0.132, 0.145), ((0, 0.021, 1.827), 0.109, 0.105),
        ((0, 0.047, 1.855), 0.047, 0.062)], "KnightPlate", "Head", form=0.66)
    panel("Bevor", [(-0.109, 1.531), (-0.054, 1.481), (0.050, 1.481), (0.113, 1.534),
        (0.092, 1.581), (-0.092, 1.581)], -0.165, 0.066, "KnightEdge", "Head", 0.015)
    panel("Visor recess", [(-0.110, 1.606), (0, 1.589), (0.110, 1.606), (0.107, 1.659),
        (0, 1.653), (-0.107, 1.659)], -0.174, 0.023, "KnightLeather", "Head", 0.008)
    panel("Narrow slit", [(-0.088, 1.635), (0, 1.622), (0.088, 1.635), (0.088, 1.642),
        (0, 1.632), (-0.088, 1.642)], -0.188, 0.009, "KnightVisor", "Head", 0.0025)
    panel("Visor brow", [(-0.132, 1.661), (0, 1.647), (0.132, 1.661), (0.112, 1.687),
        (0, 1.669), (-0.112, 1.687)], -0.183, 0.037, "KnightEdge", "Head", 0.008)
    for side in (-1, 1):
        panel("Sallet cheek", [(side * x, z) for x, z in
            [(0.072, 1.55), (0.117, 1.562), (0.151, 1.654), (0.114, 1.647)]],
            -0.152, 0.045, "KnightPlate", "Head", 0.011)
        rivet("Sallet pin", (side * 0.129, -0.158, 1.614), 0.011, "Head")


def legs_and_arms():
    for side, label in ((-1, "L"), (1, "R")):
        x = 0.16 * side
        thigh, shin, foot = f"Thigh.{label}", f"Shin.{label}", f"Foot.{label}"
        rings("Chausses", [((x, -0.027, 0.55), 0.071, 0.071), ((x, -0.013, 0.66), 0.075, 0.079),
            ((x * 0.84, 0.019, 0.92), 0.094, 0.096), ((x * 0.84, 0.018, 0.99), 0.094, 0.095)],
            "KnightLeather", thigh)
        rings("Thigh plate", [((x, -0.024, 0.59), 0.082, 0.098), ((x, -0.005, 0.65), 0.094, 0.102),
            ((x * 0.87, 0.008, 0.82), 0.104, 0.109), ((x * 0.83, 0.011, 0.90), 0.097, 0.108)],
            "KnightPlate", thigh, form=0.76)
        panel("Tasset", [(side * u, z) for u, z in
            [(0.105, 0.946), (0.209, 0.921), (0.230, 0.795), (0.164, 0.816), (0.108, 0.861)]],
            -0.125, 0.031, "KnightEdge", thigh, 0.007)
        rings("Greave", [((x, 0.025, 0.167), 0.063, 0.084), ((x, 0.025, 0.25), 0.068, 0.088),
            ((x, 0.031, 0.41), 0.091, 0.103), ((x, -0.013, 0.513), 0.083, 0.088),
            ((x, -0.028, 0.565), 0.063, 0.067)], "KnightPlate", shin, form=0.75)
        panel("Poleyn", [(x - 0.075, 0.51), (x, 0.466), (x + 0.075, 0.51),
            (x + 0.061, 0.575), (x, 0.608), (x - 0.061, 0.575)], -0.118, 0.073, "KnightEdge", shin, 0.019)
        rings("Sabatons", [((x, 0.053, 0.087), 0.066, 0.062), ((x, -0.039, 0.087), 0.077, 0.073),
            ((x, -0.134, 0.076), 0.076, 0.056), ((x, -0.199, 0.064), 0.035, 0.037)],
            "KnightPlate", foot, form=0.63)

        arm, forearm, hand = f"Arm.{label}", f"Forearm.{label}", f"Hand.{label}"
        shoulder = Vector((side * 0.254, 0.013, 1.41))
        elbow = Vector((side * 0.369, -0.015, 1.20))
        wrist = Vector((side * 0.423, -0.165, 1.055))
        rings("Upper sleeve", [(shoulder, 0.092, 0.090), (shoulder.lerp(elbow, 0.33), 0.100, 0.098),
            (shoulder.lerp(elbow, 0.73), 0.085, 0.083), (elbow, 0.068, 0.071)], "KnightLeather", arm)
        for layer in range(3 if side < 0 else 2):
            anchor = shoulder.lerp(elbow, 0.18 + 0.18 * layer)
            crest = anchor + Vector((-0.057 * side, 0.013, 0.059))
            edge = anchor + Vector((0.10 * side, -0.002, -0.045))
            width = 0.128 - 0.018 * layer if side < 0 else 0.095 - 0.018 * layer
            rings("Overlapping pauldron", [(crest, width * 0.61, width),
                (crest.lerp(edge, 0.43), width * 0.98, width * 1.08), (edge, width * 0.52, width * 0.88)],
                "KnightPlate", arm, sides=16, form=0.64)
        rings("Vambrace", [(elbow, 0.080, 0.079), (elbow.lerp(wrist, 0.20), 0.087, 0.086),
            (elbow.lerp(wrist, 0.74), 0.064, 0.073), (wrist, 0.057, 0.061)],
            "KnightPlate", forearm, sides=20, form=0.67)
        rings("Gauntlet", [(wrist, 0.049, 0.058), ((0.428 * side, -0.205, 1.049), 0.067, 0.062),
            ((0.431 * side, -0.254, 1.045), 0.059, 0.046), ((0.431 * side, -0.274, 1.045), 0.049, 0.032)],
            "KnightLeather", hand, sides=16, form=0.65)

    panel("Vigil buckler", [(-0.633, 1.235), (-0.501, 1.313), (-0.370, 1.243),
        (-0.366, 1.061), (-0.475, 0.949), (-0.611, 1.033)],
        -0.259, 0.042, "KnightPlate", "Forearm.L", 0.023)
    panel("Buckler ridge", [(-0.550, 1.175), (-0.513, 1.204), (-0.475, 1.175),
        (-0.509, 1.071)], -0.278, 0.023, "KnightBronze", "Forearm.L")
    for position in ((-0.60, -0.283, 1.21), (-0.404, -0.283, 1.21), (-0.48, -0.282, 1.011)):
        rivet("Buckler rivet", position, 0.013, "Forearm.L")


def cloak_weights(point):
    lower = max(0.0, min(1.0, (1.20 - point.z) / 0.44))
    return {"CloakUpper": 1.0 - lower, "CloakLower": lower}


def cloth():
    columns, rows = 15, 14
    points = []
    for row in range(rows):
        t = row / (rows - 1)
        for index in range(columns):
            u = index / (columns - 1)
            center = -0.094 - 0.066 * t
            width = 0.51 + t * 0.24
            x = center + (u - 0.5) * width
            waved = math.sin(u * math.tau * 2.0 + t * 2.8)
            y = 0.135 + t * 0.305 + waved * (0.022 + t * 0.035)
            hem = 0.112 * math.sin(u * math.tau * 1.47) + 0.098 * u
            z = 1.47 - 1.13 * t - 0.09 * u + hem * t ** 4
            points.append((x, y, z))
    faces = []
    for row in range(rows - 1):
        for col in range(columns - 1):
            start = row * columns + col
            faces.append((start, start + 1, start + columns + 1, start + columns))
    cape = mesh("Asymmetric mantle", points, faces, "KnightCloth", cloak_weights)
    cape.data.materials.append(SURFACES["KnightHem"])
    for face in cape.data.polygons:
        row, col = divmod(face.index, columns - 1)
        if row == rows - 2 or col == 0 or col == columns - 2:
            face.material_index = 1
    solidify(cape, 0.017)
    rivet("Mantle clasp", (-0.185, -0.153, 1.419), 0.040, "Chest")

    wrap_points = []
    sections = 13
    for index in range(sections):
        t = index / (sections - 1)
        x = -0.236 + 0.446 * t
        y = -0.058 - 0.131 * math.sin(math.pi * t)
        z = 1.49 - 0.044 * t - 0.018 * math.sin(math.pi * t)
        for band in range(4):
            f = band / 3
            wrap_points.append((x, y - math.sin(math.pi * f) * 0.025, z - f * 0.073))
    wrap_faces = []
    for section in range(sections - 1):
        for band in range(3):
            start = section * 4 + band
            wrap_faces.append((start, start + 1, start + 5, start + 4))
    wrap = mesh("Wrapped mantle", wrap_points, wrap_faces, "KnightCloth", "Chest")
    solidify(wrap, 0.013)

    outline = [(-0.10, -0.166, 0.974), (0.077, -0.168, 0.973), (0.117, -0.199, 0.587),
               (0.010, -0.203, 0.656), (-0.085, -0.205, 0.559), (-0.119, -0.190, 0.77)]
    tabard = mesh("Split tabard", outline, [(0, 5, 3, 2, 1), (5, 4, 3)], "KnightCloth", "Tabard")
    solidify(tabard, 0.010)


def bind(rig):
    bpy.context.view_layer.update()
    points, faces, surfaces, normals, assignments = [], [], [], [], []
    for obj, attachment in PIECES:
        offset = len(points)
        data = obj.data
        normal_matrix = obj.matrix_world.to_3x3().inverted().transposed()
        for vertex in data.vertices:
            point = obj.matrix_world @ vertex.co
            points.append(point)
            assignments.append((attachment, data.materials[0].name))
        for face in data.polygons:
            faces.append(tuple(offset + index for index in face.vertices))
            surfaces.append((data.materials[face.material_index].name, face.use_smooth))
        normals.extend((normal_matrix @ corner.vector).normalized() for corner in data.corner_normals)

    data = bpy.data.meshes.new("FallenKnight Geometry")
    data.from_pydata(points, [], faces)
    data.update()
    body = bpy.data.objects.new("FallenKnight Painted Body", data)
    bpy.context.collection.objects.link(body)
    slots = {}
    for face, (surface, smooth) in zip(data.polygons, surfaces):
        if surface not in slots:
            slots[surface] = len(data.materials)
            data.materials.append(SURFACES[surface])
        face.material_index = slots[surface]
        face.use_smooth = smooth
    data.normals_split_custom_set(normals)

    weights_by_bone = {}
    paint = []
    for index, (point, (attachment, swatch)) in enumerate(zip(points, assignments)):
        weights = attachment(point) if callable(attachment) else {attachment: 1.0}
        for bone_name, weight in weights.items():
            if weight >= 0.0001:
                weights_by_bone.setdefault(bone_name, {}).setdefault(weight, []).append(index)

        broad = 0.80 + 0.13 * point.z / 1.9
        broad += 0.046 * math.sin(point.z * 8.9 + point.x * 11.7 - point.y * 4.5)
        broad += 0.030 * math.sin(point.z * 21.1 - point.x * 4.7 + point.y * 17.3)
        if swatch in ("KnightPlate", "KnightEdge"):
            broad += 0.04 * max(0.0, min(1.0, -point.y / 0.18))
        paint.extend((broad, broad, broad, 1.0))

    for bone_name, weighted_vertices in weights_by_bone.items():
        group = body.vertex_groups.new(name=bone_name)
        for weight, indices in weighted_vertices.items():
            group.add(indices, weight, "REPLACE")
    data.color_attributes.new(name="Paint", type="BYTE_COLOR", domain="POINT").data.foreach_set("color", paint)

    for piece, _ in PIECES:
        bpy.data.objects.remove(piece, do_unlink=True)
    bpy.context.view_layer.objects.active = body
    body.select_set(True)
    body.parent = rig
    skin = body.modifiers.new("Fallen Knight skin", "ARMATURE")
    skin.object = rig
    skin.use_deform_preserve_volume = True
    return body


def check_skin(body, rig):
    bones = {bone.name for bone in rig.data.bones if bone.use_deform}
    groups = {group.index: group.name for group in body.vertex_groups}
    for vertex in body.data.vertices:
        if not math.isclose(sum(group.weight for group in vertex.groups), 1.0, abs_tol=0.0003):
            raise ValueError(f"Invalid skin weights on vertex {vertex.index}")
        if any(groups[group.group] not in bones for group in vertex.groups):
            raise ValueError(f"Unknown deform bone at vertex {vertex.index}")
    if len(body.data.color_attributes) == 0:
        raise ValueError("The vertex paint did not survive export")
    return validate([body], 20000)


def frame_model():
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type != "VIEW_3D":
                continue
            viewport = area.spaces.active
            viewport.shading.color_type = "MATERIAL"
            viewport.overlay.show_overlays = False
            viewport.region_3d.view_location = (0, 0, 0.96)
            viewport.region_3d.view_distance = 3.5
            viewport.region_3d.view_rotation = Euler((math.radians(75), 0, math.radians(22))).to_quaternion()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--fbx", type=Path, required=True)
    parser.add_argument("--blend", type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:])
    reset()
    SURFACES.update({name: material(name, (*rgb, 1)) for name, rgb in PALETTE.items()})
    rig = armature()
    torso()
    helmet()
    legs_and_arms()
    cloth()
    body = bind(rig)
    triangles = check_skin(body, rig)
    args.fbx.parent.mkdir(parents=True, exist_ok=True)
    args.blend.parent.mkdir(parents=True, exist_ok=True)
    frame_model()
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(args.blend.resolve()))
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    body.select_set(True)
    bpy.context.view_layer.objects.active = rig
    result = bpy.ops.export_scene.fbx(filepath=str(args.fbx.resolve()), use_selection=True,
        object_types={"MESH", "ARMATURE"}, axis_forward="-Z", axis_up="Y",
        apply_scale_options="FBX_SCALE_UNITS", add_leaf_bones=False,
        use_armature_deform_only=False, bake_anim=False, colors_type="LINEAR")
    if "FINISHED" not in result:
        raise RuntimeError("Could not write the Fallen Knight FBX")

    reset()
    bpy.ops.import_scene.fbx(filepath=str(args.fbx.resolve()), colors_type="LINEAR")
    imported_rig = next(obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE")
    imported_body = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
    if check_skin(imported_body, imported_rig) != triangles:
        raise ValueError("FBX round trip changed triangle count")
    print(f"[Ashen Realm] {triangles} triangles, vertex paint and skin verified, FBX round trip OK.", flush=True)


if __name__ == "__main__":
    main()
