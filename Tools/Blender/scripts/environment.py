"""Generate or normalize static environment assets in Blender 4.5 LTS."""

import argparse
import math
import random
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


def arguments():
    parser = argparse.ArgumentParser(description=__doc__)
    modes = parser.add_subparsers(dest="mode", required=True)
    generate = modes.add_parser("generate")
    generate.add_argument("--asset", choices=["Rock", "DeadTree", "Pillar", "Ruin", "Altar", "all"], default="all")
    generate.add_argument("--seed", type=int, default=71)
    external = modes.add_parser("import")
    external.add_argument("source", type=Path)
    external.add_argument("--scale", type=float, default=1)
    external.add_argument("--rotation-z", type=float, default=0, help="Blender Z rotation in degrees")
    for mode in (generate, external):
        mode.add_argument("--output-dir", type=Path, required=True)
        mode.add_argument("--format", choices=["fbx", "glb"], default="fbx")
        mode.add_argument("--max-triangles", type=int, default=10000)
        mode.add_argument("--verify", action="store_true", help="Reimport and validate the exported mesh")
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = "METRIC"


def material(name, color):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = color
    shader.inputs["Roughness"].default_value = 0.91
    return mat


def finish(name, dimensions, mat):
    obj = bpy.context.object
    obj.name = name
    obj.scale = dimensions
    obj.data.materials.append(mat)
    return obj


def block(name, center, dimensions, mat, rng):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = finish(name, dimensions, mat)
    obj.rotation_euler.z = rng.uniform(-0.09, 0.09)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel = obj.modifiers.new("Chipped Edges", "BEVEL")
    bevel.width = 0.065
    bevel.segments = 1
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    return obj


def limb(name, start, end, radius, mat):
    direction = Vector(end) - Vector(start)
    bpy.ops.mesh.primitive_cone_add(
        vertices=5, radius1=radius, radius2=radius * 0.3,
        depth=direction.length, location=Vector(start) + direction * 0.5,
    )
    obj = finish(name, (1, 1, 1), mat)
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()


def generate_asset(name, rng):
    stone = material("Ash Stone", (0.24, 0.22, 0.19, 1))
    basalt = material("Basalt", (0.12, 0.14, 0.14, 1))
    bark = material("Withered Bark", (0.15, 0.10, 0.07, 1))
    if name == "Rock":
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1)
        obj = finish("Rock", (1.05, 0.72, 0.57), stone)
        obj.data.materials.append(basalt)
        for vertex in obj.data.vertices:
            vertex.co *= rng.uniform(0.78, 1.18)
            vertex.co.z = max(-0.53, vertex.co.z)
        for polygon in obj.data.polygons:
            polygon.material_index = int(rng.random() < 0.27)
    elif name == "DeadTree":
        limb("Trunk", (0, 0, 0), (0.22, 0, 2.65), 0.25, bark)
        for index in range(4):
            angle = index * math.tau / 4 + rng.uniform(-0.3, 0.3)
            height = 1.15 + index * 0.32
            elbow = (math.cos(angle) * 0.68, math.sin(angle) * 0.68, height + 0.42)
            tip = (math.cos(angle) * 0.93, math.sin(angle) * 0.93, height + 0.92)
            limb("Branch", (0.13, 0, height), elbow, 0.10, bark)
            limb("Twig", elbow, tip, 0.045, bark)
    elif name in ("Pillar", "Ruin"):
        count = 5 if name == "Pillar" else 3
        block("Plinth", (0, 0, 0.16), (1.2, 1.05, 0.32), basalt, rng)
        for index in range(count):
            block("Course", (rng.uniform(-0.07, 0.07), 0, 0.53 + 0.42 * index),
                  (0.86, 0.78, 0.42), stone, rng)
        if name == "Pillar":
            block("Capital", (0, 0, 2.55), (1.15, 1, 0.26), basalt, rng)
        else:
            for index in range(3):
                fragment = block("Rubble", (0.78 + 0.45 * index, -0.35 * index, 0.18),
                                 (0.48, 0.36, 0.36), stone, rng)
                fragment.rotation_euler.z += rng.uniform(-0.7, 0.7)
    else:
        block("Base", (0, 0, 0.16), (1.5, 0.95, 0.32), basalt, rng)
        block("Pedestal", (0, 0, 0.55), (1.15, 0.62, 0.46), stone, rng)
        block("Slab", (0, 0, 0.93), (1.65, 1.05, 0.30), basalt, rng)


def import_asset(path):
    if not path.is_file():
        raise ValueError(f"Input file does not exist: {path}")
    if path.suffix.lower() == ".fbx":
        bpy.ops.import_scene.fbx(filepath=str(path))
    elif path.suffix.lower() in (".glb", ".gltf"):
        bpy.ops.import_scene.gltf(filepath=str(path))
    else:
        raise ValueError("Supported imports are FBX, GLB and glTF")


def meshes():
    objects = list(bpy.context.scene.objects)
    if any(obj.type == "ARMATURE" or obj.animation_data for obj in objects):
        raise ValueError("Animated or rigged assets require a separate character pipeline")
    result = [obj for obj in objects if obj.type == "MESH"]
    if not result:
        raise ValueError("No mesh objects found")
    if any(obj.data.shape_keys for obj in result):
        raise ValueError("Shape keys require a separate character pipeline")
    return result


def normalize(objects, scale, angle):
    if not math.isfinite(scale) or scale <= 0 or not math.isfinite(angle):
        raise ValueError("Scale must be positive and scale and rotation must be finite")
    bpy.context.view_layer.update()
    adjustment = Matrix.Rotation(math.radians(angle), 4, "Z") @ Matrix.Scale(scale, 4)
    matrices = [adjustment @ obj.matrix_world for obj in objects]
    for obj, matrix in zip(objects, matrices):
        obj.parent = None
        obj.data = obj.data.copy()
        obj.data.transform(matrix)
        if matrix.to_3x3().determinant() < 0:
            obj.data.flip_normals()
        obj.matrix_world = Matrix.Identity(4)
    base = min((vertex.co.z for obj in objects for vertex in obj.data.vertices), default=0)
    for obj in objects:
        obj.data.transform(Matrix.Translation((0, 0, -base)))
        obj.data.update()
        if not obj.data.materials:
            obj.data.materials.append(material("Neutral Stone", (0.22, 0.21, 0.19, 1)))


def validate(objects, maximum):
    total = 0
    for obj in objects:
        mesh = obj.data
        if mesh.validate() or not mesh.polygons:
            raise ValueError(f"Invalid or empty geometry in {obj.name}")
        mesh.calc_loop_triangles()
        if any(not math.isfinite(axis) for vertex in mesh.vertices for axis in vertex.co):
            raise ValueError(f"Non-finite mesh coordinates in {obj.name}")
        if any(triangle.area <= 1e-12 for triangle in mesh.loop_triangles):
            raise ValueError(f"Degenerate triangle in {obj.name}")
        total += len(mesh.loop_triangles)
    if total > maximum:
        raise ValueError(f"Triangle budget exceeded ({total} > {maximum})")
    return total


def export(objects, path):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    if path.suffix == ".fbx":
        result = bpy.ops.export_scene.fbx(
            filepath=str(path), use_selection=True, object_types={"MESH"},
            axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_UNITS",
            bake_anim=False, path_mode="COPY", embed_textures=True,
        )
    else:
        result = bpy.ops.export_scene.gltf(filepath=str(path), export_format="GLB", use_selection=True)
    if "FINISHED" not in result or not path.is_file() or path.stat().st_size == 0:
        raise RuntimeError(f"Export failed: {path}")


def main():
    args = arguments()
    folder = args.output_dir.resolve()
    folder.mkdir(parents=True, exist_ok=True)
    if args.mode == "generate":
        names = ["Rock", "DeadTree", "Pillar", "Ruin", "Altar"] if args.asset == "all" else [args.asset]
    else:
        if args.source.resolve() == folder / f"{args.source.stem}.{args.format}":
            raise ValueError("Choose an output path different from the source")
        names = [args.source.stem]
    for name in names:
        reset()
        if args.mode == "generate":
            generate_asset(name, random.Random(f"{args.seed}:{name}"))
            objects = meshes()
            normalize(objects, 1, 0)
        else:
            import_asset(args.source.resolve())
            objects = meshes()
            normalize(objects, args.scale, args.rotation_z)
        triangle_count = validate(objects, args.max_triangles)
        path = folder / f"{name}.{args.format}"
        export(objects, path)
        print(f"[Ashen Realm] {path} ({triangle_count} triangles)", flush=True)
        if args.verify:
            reset()
            import_asset(path)
            actual = validate(meshes(), args.max_triangles)
            if actual != triangle_count:
                raise ValueError(f"Round trip changed triangle count: {triangle_count} -> {actual}")
            print(f"[Ashen Realm] Round trip OK: {path.name} ({actual} triangles)", flush=True)


if __name__ == "__main__":
    try:
        main()
    except Exception as error:
        print(f"[Ashen Realm] ERROR: {error}", file=sys.stderr, flush=True)
        raise
