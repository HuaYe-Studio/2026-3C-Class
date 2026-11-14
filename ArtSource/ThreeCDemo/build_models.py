"""Build the editable Training Lab models and export Unity FBXs with Blender 5."""

from math import radians
from pathlib import Path

import bpy
from mathutils import Vector


SOURCE = Path(__file__).resolve().parent
MODELS = SOURCE.parents[1] / "Assets" / "ThreeCDemo" / "Models"
MODELS.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.unit_settings.system = "METRIC"


def material(name, rgb, metallic=0, glow=0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*rgb, 1)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*rgb, 1)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = .36
    shader.inputs["Emission Color"].default_value = (*rgb, 1)
    shader.inputs["Emission Strength"].default_value = glow
    return mat


graphite = material("Structure", (.025, .052, .082), .32)
alloy = material("Panels", (.095, .19, .25), .45)
ceramic = material("Ceramic", (.63, .80, .85), .16)
signal = material("Signal", (.018, .72, .58), .18, 1.4)
amber = material("Target", (1, .20, .035), .16, .32)


def finish(name, mat, bevel, pitch=0):
    obj = bpy.context.object
    obj.name = name
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.rotation_euler.x = radians(pitch)
    obj.data.materials.append(mat)
    if bevel:
        mod = obj.modifiers.new("Machined bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 1
        mod.harden_normals = True
    normals = obj.modifiers.new("Weighted corner normals", "WEIGHTED_NORMAL")
    normals.keep_sharp = True
    return obj


def box(name, pos, size, mat, bevel=.02, pitch=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    bpy.context.object.dimensions = size
    return finish(name, mat, bevel, pitch)


def round_part(name, pos, radii, depth, mat, sides=12, pitch=0, bevel=.01):
    bpy.ops.mesh.primitive_cone_add(
        vertices=sides, radius1=radii[0], radius2=radii[1], depth=depth, location=pos
    )
    return finish(name, mat, bevel, pitch)


def ring(name, pos, inner, outer, mat, sides=20):
    from math import cos, sin, tau

    vertices = [
        (radius * cos(tau * i / sides), radius * sin(tau * i / sides), 0)
        for i in range(sides) for radius in (inner, outer)
    ]
    faces = [(2*i, 2*i+1, (2*i+3) % (2*sides), (2*i+2) % (2*sides)) for i in range(sides)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = pos
    obj.rotation_euler.x = radians(90)
    obj.data.materials.append(mat)
    return obj


def robot():
    parts = [
        box("Hip chassis", (0, .015, .64), (.48, .32, .21), graphite),
        box("Chest chassis", (0, 0, .97), (.59, .40, .48), graphite, .063),
        box("Ceramic chest armor", (0, -.15, 1.03), (.57, .22, .35), ceramic, .059, -8),
        box("Power pack", (0, .23, 1.02), (.47, .25, .43), alloy, .054),
        box("Pack inset", (0, .365, 1.06), (.29, .035, .24), graphite, .018),
        box("Rear status bar", (0, .386, 1.10), (.24, .025, .04), signal, .009),
        box("Chest power strip", (0, -.277, 1.07), (.21, .023, .039), signal, .008),
        round_part("Neck joint", (0, 0, 1.26), (.12, .12), .16, alloy),
        box("Helmet shell", (0, -.018, 1.48), (.53, .49, .38), ceramic, .073),
        box("Visor socket", (0, -.241, 1.48), (.48, .13, .19), graphite, .045),
        box("Helmet rear vent", (0, .232, 1.50), (.26, .045, .09), alloy, .014),
    ]
    for side in (-1, 1):
        x = side * .18
        parts.extend([
            box("Thigh linkage", (x, .01, .48), (.20, .24, .30), alloy, .036),
            box("Shin armor", (x, -.031, .26), (.25, .26, .31), ceramic, .044),
            box("Knee inset", (x, -.164, .36), (.15, .05, .10), graphite, .017),
            box("Boot", (x, -.07, .086), (.27, .42, .17), graphite, .037),
            box("Toe armor", (x, -.21, .103), (.25, .18, .115), alloy, .022),
            box("Shoulder joint", (side * .33, 0, 1.08), (.22, .30, .19), graphite),
            box("Shoulder armor", (side * .39, 0, 1.15), (.25, .36, .22), ceramic, .050),
            box("Arm linkage", (side * .40, 0, .91), (.16, .23, .28), graphite, .026),
            box("Forearm gauntlet", (side * .42, -.035, .77), (.22, .25, .28), alloy, .041),
            box("Gauntlet stripe", (side * .42, -.163, .81), (.14, .025, .048), ceramic, .009),
            box("Hand", (side * .42, -.023, .59), (.16, .17, .15), graphite, .027),
        ])
    visor = box("Visor", (0, -.31, 1.49), (.39, .035, .102), signal, .025)
    return parts, visor


def target():
    parts = [
        round_part("Weighted plinth", (0, 0, .10), (.58, .53), .20, graphite),
        round_part("Plinth collar", (0, 0, .22), (.49, .43), .09, alloy),
        round_part("Piston cylinder", (0, .015, .46), (.17, .17), .46, graphite),
        round_part("Piston shaft", (0, .015, .68), (.107, .107), .47, ceramic),
        box("Back housing", (0, -.018, 1.06), (.56, .50, .67), graphite, .064),
        round_part("Impact housing", (0, -.248, 1.08), (.43, .43), .23, alloy, 12, 90, .027),
        round_part("Orange strike plate", (0, -.377, 1.08), (.369, .369), .055, amber, 12, 90),
        round_part("Recessed target face", (0, -.411, 1.08), (.291, .291), .035, graphite, 16, 90),
        ring("Target reticle", (0, -.431, 1.08), .204, .231, ceramic),
        box("Beacon housing", (0, -.01, 1.46), (.26, .25, .13), graphite, .028),
        box("Beacon", (0, -.008, 1.54), (.17, .15, .08), amber, .025),
    ]
    for side in (-1, 1):
        parts.extend([
            box("Plinth brace", (side * .26, .024, .34), (.15, .32, .23), alloy, .036),
            box("Flank bumper", (side * .36, -.117, 1.07), (.18, .34, .40), graphite, .045),
            box("Flank warning bar", (side * .372, -.299, 1.07), (.097, .035, .20), amber, .012),
        ])
    core = round_part("ImpactCore", (0, -.448, 1.08), (.116, .116), .066, amber, 12, 90)
    return parts, core


def select(objects):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]


def export(name, body, indicator):
    originals = [*body, indicator]
    duplicates = []
    for obj in originals:
        copy = obj.copy()
        copy.data = obj.data.copy()
        bpy.context.collection.objects.link(copy)
        select([copy])
        for modifier in list(copy.modifiers):
            bpy.ops.object.modifier_apply(modifier=modifier.name)
        duplicates.append(copy)

    select(duplicates[:-1])
    bpy.ops.object.join()
    chassis = bpy.context.object
    chassis.name = name + "Body"
    light = duplicates[-1]
    # The source assembly is editable by part. Each exported mesh uses the ground pivot.
    select([chassis, light])
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    light.name = indicator.name + "_FX"
    for obj in (chassis, light):
        obj.data.calc_loop_triangles()
        assert len(obj.data.loop_triangles) < 3000
        print(f"{name}/{obj.name}: {len(obj.data.loop_triangles)} triangles")
    bpy.ops.export_scene.fbx(
        filepath=str(MODELS / (name + ".fbx")), use_selection=True,
        object_types={"MESH"}, axis_forward="-Z", axis_up="Y", bake_anim=False,
        add_leaf_bones=False, mesh_smooth_type="OFF",
    )
    bpy.data.objects.remove(chassis, do_unlink=True)
    bpy.data.objects.remove(light, do_unlink=True)


def assemble(name, build, display_x):
    body, indicator = build()
    export(name, body, indicator)
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    for obj in [*body, indicator]:
        collection.objects.link(obj)
        for current in list(obj.users_collection):
            if current != collection:
                current.objects.unlink(obj)
        obj.location.x += display_x


assemble("TrainingRobot", robot, -.83)
assemble("GroundTarget", target, .83)
bpy.context.preferences.filepaths.save_version = 0
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == "VIEW_3D":
            area.spaces.active.shading.color_type = "MATERIAL"
            area.spaces.active.region_3d.view_rotation = Vector((3, -6, 2.6)).to_track_quat("Z", "Y")
            area.spaces.active.region_3d.view_location = (0, 0, .78)
            area.spaces.active.region_3d.view_distance = 4.8
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / "TrainingLabModels.blend"))
