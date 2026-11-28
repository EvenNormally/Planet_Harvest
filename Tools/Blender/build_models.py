"""Planet Harvest · procedural low-poly models.

Builds every game model from primitives with flat vertex colours (palette from the art direction:
green/turquoise/mint landscape and devices, orange-yellow industry, violet accents) and exports one FBX per
model to Assets/_Game/Art/Models. Reproducible; run headless:

    blender --background --factory-startup --python Tools/Blender/build_models.py

Coordinates in this file are given in Unity space (x right, y up, z forward, metres) and converted to
Blender's Z-up space. Named child objects are the animated parts the game looks up by name
(Spindel, Bohrkopf, Bein0..3, Statusleuchte, Strahlkern, Ring, Pumpenkopf, Schauglas).
"""
import math
import os
import sys

import bpy

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Game", "Art", "Models")

PALETTE = {
    "mint": (0.38, 0.84, 0.68), "turquoise": (0.18, 0.66, 0.66), "green": (0.47, 0.88, 0.62),
    "leaf": (0.42, 0.74, 0.30), "orange": (0.98, 0.60, 0.16), "yellow": (1.00, 0.80, 0.25),
    "violet": (0.62, 0.38, 0.95), "graphite": (0.17, 0.24, 0.27), "steel": (0.55, 0.60, 0.64),
    "dark": (0.11, 0.14, 0.20), "ochre": (0.78, 0.50, 0.22), "soil": (0.46, 0.28, 0.13),
    "copper": (0.88, 0.52, 0.25), "iron": (0.67, 0.28, 0.18), "quartz": (0.90, 0.88, 1.00),
    "glass": (0.70, 0.88, 1.00), "water": (0.33, 0.62, 0.95), "white": (0.95, 0.95, 0.92),
    "frame": (0.22, 0.32, 0.36), "ivory": (0.93, 0.89, 0.74),
}


def B(v):
    """Unity (x, y, z) to Blender (x, -z, y)."""
    return (v[0], -v[2], v[1])


def S(v):
    return (v[0], v[2], v[1])


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def paint(obj, color):
    mesh = obj.data
    attr = mesh.color_attributes.new(name="Col", type='BYTE_COLOR', domain='CORNER')
    rgb = PALETTE[color] if isinstance(color, str) else color
    rgba = (rgb[0], rgb[1], rgb[2], 1.0)
    for item in attr.data:
        item.color_srgb = rgba
    mesh.color_attributes.active_color = attr


def part(name, shape, position, size, color, rotation=(0, 0, 0), vertices=8, top=0.0):
    """Adds a primitive; size is the full extent along Unity x, y, z."""
    loc = B(position)
    rot = tuple(math.radians(r) for r in rotation)
    if shape == "cube":
        bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    elif shape == "cylinder":
        bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=0.5, depth=1, location=loc, rotation=rot)
    elif shape == "cone":
        bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=0.5, radius2=0.5 * top, depth=1, location=loc, rotation=rot)
    elif shape == "sphere":
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.5, location=loc, rotation=rot)
    elif shape == "facet":
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.5, location=loc, rotation=rot)
    else:
        raise ValueError(shape)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = S(size)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    paint(obj, color)
    return obj


def group(name, objects, pivot=(0, 0, 0), parent=None):
    """Joins objects into one mesh named `name` with its origin at `pivot` (Unity space)."""
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    if len(objects) > 1:
        bpy.ops.object.join()
    obj = bpy.context.active_object
    obj.name = name
    obj.data.name = name
    bpy.context.scene.cursor.location = B(pivot)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    if parent is not None:
        obj.parent = parent
        obj.location = tuple(a - b for a, b in zip(obj.location, parent.location))
    return obj


def root(name):
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=(0, 0, 0))
    obj = bpy.context.active_object
    obj.name = name
    return obj


def export(name):
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.object.select_all(action='SELECT')
    path = os.path.join(OUT, name + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={'EMPTY', 'MESH'},
        apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y', bake_space_transform=True,
        mesh_smooth_type='FACE', colors_type='SRGB', add_leaf_bones=False, use_mesh_modifiers=True,
        use_custom_props=False, path_mode='STRIP', embed_textures=False)
    print("exported", path)


# ---------------------------------------------------------------- models

def hand_drill():
    r = root("HandDrill")
    body = [
        part("Griff", "cube", (0, -0.08, 0), (0.063, 0.20, 0.085), "graphite"),
        part("Motor", "cube", (0, 0.07, 0.02), (0.15, 0.145, 0.24), "mint"),
        part("Motorschutz", "cube", (0, 0.13, -0.035), (0.165, 0.05, 0.19), "orange"),
        part("Bügel", "cube", (0, 0.19, -0.025), (0.035, 0.03, 0.125), "graphite"),
        part("Anzeige", "cube", (0.078, 0.072, -0.025), (0.009, 0.044, 0.067), "turquoise"),
        part("Bohrfutter", "cylinder", (0, 0.07, 0.14), (0.127, 0.127, 0.033), "orange", rotation=(90, 0, 0)),
    ]
    group("Gehäuse", body, parent=r)
    bits = [part("Schneide", "cone", (0, 0.07, 0.24), (0.07, 0.07, 0.18), "steel", rotation=(90, 0, 0), vertices=6)]
    for i in range(3):
        bits.append(part("Rille", "cube", (0, 0.07, 0.18 + i * 0.04), (0.08 - i * 0.015, 0.012, 0.012), "dark",
                         rotation=(0, 0, i * 40)))
    group("Spindel", bits, pivot=(0, 0.07, 0.15), parent=r)


def workbench():
    r = root("Workbench")
    parts = [
        part("Sockel", "cube", (0, 0.12, 0), (2.6, 0.34, 1.7), "graphite"),
        part("Werkbank", "cube", (0, 0.67, 0), (2.05, 1.12, 1.12), "ochre"),
        part("Arbeitsplatte", "cube", (0, 1.28, 0), (2.45, 0.16, 1.4), "graphite"),
        part("Rückwand", "cube", (0, 1.78, 0.48), (2.26, 1.02, 0.13), "frame"),
        part("Lichtleiste", "cube", (0, 2.32, 0.46), (2.3, 0.06, 0.19), "mint"),
        part("Schraubstock", "cube", (-0.8, 1.45, -0.3), (0.35, 0.2, 0.25), "orange"),
        part("Werkzeugkiste", "cube", (0.75, 1.45, -0.1), (0.5, 0.18, 0.35), "violet"),
    ]
    for side in (-1, 1):
        parts.append(part("Bein", "cube", (side * 0.95, 0.55, -0.5), (0.12, 0.9, 0.12), "graphite"))
    group("Werkbank", parts, parent=r)
    group("Statusleuchte", [part("Statusleuchte", "facet", (1.05, 2.45, 0.46), (0.14, 0.14, 0.14), "mint")],
          pivot=(1.05, 2.45, 0.46), parent=r)


def tractor_mast():
    r = root("TractorMast")
    group("Mast", [
        part("Fuß", "cylinder", (0, 0.1, 0), (0.7, 0.2, 0.7), "graphite", vertices=6),
        part("Mast", "cylinder", (0, 1.5, 0), (0.16, 3.0, 0.16), "frame", vertices=6),
        part("Strebe", "cube", (0.18, 0.55, 0), (0.06, 0.9, 0.06), "orange", rotation=(0, 0, 18)),
        part("Strebe", "cube", (-0.18, 0.55, 0), (0.06, 0.9, 0.06), "orange", rotation=(0, 0, -18)),
    ], parent=r)
    group("Ring", [part("Ring", "cylinder", (0, 2.9, 0), (0.62, 0.05, 0.62), "mint", vertices=10)],
          pivot=(0, 2.9, 0), parent=r)
    group("Strahlkern", [part("Strahlkern", "facet", (0, 3.2, 0), (0.38, 0.38, 0.38), "violet")],
          pivot=(0, 3.2, 0), parent=r)


def beam_hopper():
    r = root("BeamHopper")
    group("Trichter", [
        part("Sockel", "cylinder", (0, 0.3, 0), (1.4, 0.6, 1.4), "graphite", vertices=8),
        part("Trichter", "cone", (0, 0.92, 0), (1.8, 0.4, 1.8), "orange", vertices=8, top=1.6),
        part("Kante", "cylinder", (0, 1.13, 0), (1.85, 0.05, 1.85), "yellow", vertices=8),
    ], parent=r)
    group("Strahlkern", [part("Strahlkern", "facet", (0, 1.25, 0), (0.45, 0.45, 0.45), "violet")],
          pivot=(0, 1.25, 0), parent=r)
    group("Statusleuchte", [part("Statusleuchte", "facet", (0.75, 0.7, 0), (0.22, 0.22, 0.22), "mint")],
          pivot=(0.75, 0.7, 0), parent=r)


def drill_robot():
    r = root("DrillRobot")
    group("Rumpf", [
        part("Rumpf", "cube", (0, 0.9, 0), (1.1, 0.6, 1.3), "orange"),
        part("Panzer", "cube", (0, 1.22, 0.1), (0.9, 0.08, 1.0), "yellow"),
        part("Kuppel", "sphere", (0, 1.25, -0.15), (0.7, 0.45, 0.7), "mint"),
        part("Auge", "facet", (-0.18, 1.3, 0.16), (0.14, 0.14, 0.08), "dark"),
        part("Auge", "facet", (0.18, 1.3, 0.16), (0.14, 0.14, 0.08), "dark"),
        part("Antenne", "cylinder", (0.3, 1.6, -0.3), (0.04, 0.5, 0.04), "graphite", vertices=6),
        part("Trichter", "cone", (0, 1.05, -0.62), (0.7, 0.4, 0.3), "violet", rotation=(-90, 0, 0), vertices=6, top=1.4),
    ], parent=r)
    group("Statusleuchte", [part("Statusleuchte", "facet", (0.3, 1.88, -0.3), (0.16, 0.16, 0.16), "mint")],
          pivot=(0.3, 1.88, -0.3), parent=r)
    for i in range(4):
        x = -0.55 if i % 2 == 0 else 0.55
        z = 0.45 if i < 2 else -0.45
        group(f"Bein{i}", [
            part("Bein", "cube", (x, 0.35, z), (0.14, 0.7, 0.14), "graphite"),
            part("Fuß", "cube", (x, 0.04, z + 0.05), (0.22, 0.08, 0.3), "frame"),
        ], pivot=(x, 0.7, z), parent=r)
    head = [part("Bohrscheibe", "cylinder", (0, 0.45, 0.5), (0.8, 0.05, 0.8), "violet", vertices=10)]
    for i in range(6):
        a = math.radians(i * 60)
        head.append(part("Zahn", "cube", (math.sin(a) * 0.38, 0.42, 0.5 + math.cos(a) * 0.38), (0.12, 0.12, 0.12), "steel",
                         rotation=(0, i * 60, 0)))
    group("Bohrkopf", head, pivot=(0, 0.45, 0.5), parent=r)


def pump():
    r = root("Pump")
    group("Pumpe", [
        part("Platte", "cube", (0, 0.08, 0), (2.2, 0.16, 2.2), "graphite"),
        part("Gehäuse", "cylinder", (0, 0.7, 0), (0.9, 1.1, 0.9), "orange", vertices=8),
        part("Band", "cylinder", (0, 0.95, 0), (0.95, 0.1, 0.95), "yellow", vertices=8),
        part("Saugrüssel", "cylinder", (0, 0.5, 1.4), (0.18, 1.6, 0.18), "violet", rotation=(70, 0, 0), vertices=6),
        part("Tankfuß", "cylinder", (-0.95, 0.4, 0), (0.6, 0.5, 0.6), "frame", vertices=8),
    ], parent=r)
    group("Pumpenkopf", [part("Pumpenkopf", "cube", (0, 1.4, 0), (0.5, 0.3, 0.5), "mint")],
          pivot=(0, 1.4, 0), parent=r)
    group("Statusleuchte", [part("Statusleuchte", "facet", (0.45, 1.7, 0), (0.18, 0.18, 0.18), "mint")],
          pivot=(0.45, 1.7, 0), parent=r)


def alien_crew():
    r = root("AlienCrew")
    group("Körper", [
        part("Anzug", "sphere", (0, -0.18, 0), (0.62, 1.0, 0.58), "orange"),
        part("Gürtel", "cylinder", (0, -0.25, 0), (0.64, 0.08, 0.6), "yellow", vertices=10),
        part("Rucksack", "cube", (0, 0, -0.32), (0.5, 0.64, 0.27), "violet"),
        part("Tank", "cylinder", (0.15, 0.05, -0.47), (0.12, 0.5, 0.12), "mint", vertices=6),
        part("Kopf", "sphere", (0, 0.66, 0), (0.8, 0.74, 0.68), "green"),
        part("Auge", "facet", (-0.18, 0.73, 0.28), (0.16, 0.22, 0.12), "dark"),
        part("Auge", "facet", (0.18, 0.73, 0.28), (0.16, 0.22, 0.12), "dark"),
        part("Fühler", "cylinder", (-0.15, 1.08, 0), (0.04, 0.3, 0.04), "green", rotation=(0, 0, 15), vertices=5),
        part("Fühler", "cylinder", (0.15, 1.08, 0), (0.04, 0.3, 0.04), "green", rotation=(0, 0, -15), vertices=5),
        part("Knubbel", "facet", (-0.19, 1.23, 0), (0.09, 0.09, 0.09), "violet"),
        part("Knubbel", "facet", (0.19, 1.23, 0), (0.09, 0.09, 0.09), "violet"),
    ], parent=r)


def elevator_station():
    r = root("SpaceElevator")
    group("Basisstation", [
        part("Fundament", "cylinder", (-1.08, 0.2, 0.58), (3.2, 0.4, 3.2), "frame", vertices=8),
        part("Kuppel", "sphere", (-1.08, 0.6, 0.58), (2.4, 1.6, 2.4), "mint"),
        part("Ring", "cylinder", (-1.08, 0.5, 0.58), (2.8, 0.12, 2.8), "orange", vertices=8),
    ], parent=r)
    group("Orbitalstation", [
        part("Station", "cylinder", (-1.08, 82, 0.58), (7.0, 0.8, 7.0), "frame", vertices=10),
        part("Habitat", "sphere", (-1.08, 82.8, 0.58), (3.5, 2.0, 3.5), "mint"),
        part("Solarflügel", "cube", (4.2, 82, 0.58), (5.0, 0.1, 2.0), "turquoise"),
        part("Solarflügel", "cube", (-6.36, 82, 0.58), (5.0, 0.1, 2.0), "turquoise"),
        part("Signal", "facet", (-1.08, 84.2, 0.58), (0.6, 0.6, 0.6), "violet"),
    ], pivot=(-1.08, 82, 0.58), parent=r)


def containers():
    # Bin-local frame: body from y -0.85 to 0.75, footprint about 1.3 x 1.3, front towards -z.
    specs = {
        "Container_PressTub": [
            ("Wanne", "cube", (0, -0.15, 0), (1.45, 1.1, 1.3), "ochre"),
            ("Pressplatte", "cube", (0, 0.5, 0), (1.3, 0.1, 1.15), "graphite"),
            ("Kufe", "cube", (-0.55, -0.8, 0), (0.12, 0.1, 1.4), "frame"),
            ("Kufe", "cube", (0.55, -0.8, 0), (0.12, 0.1, 1.4), "frame"),
        ],
        "Container_Barrel": [
            ("Fass", "cylinder", (0, -0.1, 0), (1.3, 1.5, 1.3), "steel", 10),
            ("Reif", "cylinder", (0, 0.45, 0), (1.36, 0.08, 1.36), "frame", 10),
            ("Reif", "cylinder", (0, -0.65, 0), (1.36, 0.08, 1.36), "frame", 10),
            ("Steinfenster", "cube", (0, -0.1, -0.64), (0.6, 0.7, 0.04), "dark"),
        ],
        "Container_GlassSphere": [
            ("Sockel", "cylinder", (0, -0.7, 0), (1.3, 0.3, 1.3), "frame", 8),
            ("Glaskugel", "sphere", (0, 0.05, 0), (1.4, 1.4, 1.4), "glass"),
            ("Ventil", "cylinder", (0, 0.8, 0), (0.25, 0.2, 0.25), "orange", 6),
        ],
        "Container_OreHopper": [
            ("Trichter", "cone", (0, 0.05, 0), (1.5, 1.1, 1.5), "copper", 8, 0.35),
            ("Rand", "cylinder", (0, 0.62, 0), (1.55, 0.08, 1.55), "orange", 8),
            ("Bein", "cube", (-0.55, -0.6, -0.55), (0.1, 0.5, 0.1), "frame"),
            ("Bein", "cube", (0.55, -0.6, -0.55), (0.1, 0.5, 0.1), "frame"),
            ("Bein", "cube", (-0.55, -0.6, 0.55), (0.1, 0.5, 0.1), "frame"),
            ("Bein", "cube", (0.55, -0.6, 0.55), (0.1, 0.5, 0.1), "frame"),
        ],
        "Container_MagneticBin": [
            ("Behälter", "cube", (0, -0.15, 0), (1.3, 1.2, 1.3), "frame"),
            ("Magnet", "cube", (-0.7, 0.1, 0), (0.15, 0.9, 0.5), "iron"),
            ("Magnet", "cube", (0.7, 0.1, 0), (0.15, 0.9, 0.5), "iron"),
            ("Polkappe", "cube", (-0.7, 0.6, 0), (0.17, 0.12, 0.52), "steel"),
            ("Polkappe", "cube", (0.7, 0.6, 0), (0.17, 0.12, 0.52), "steel"),
        ],
        "Container_PrismBox": [
            ("Prisma", "cylinder", (0, -0.1, 0), (1.35, 1.4, 1.35), "quartz", 6),
            ("Kante", "cylinder", (0, 0.62, 0), (1.4, 0.06, 1.4), "violet", 6),
            ("Kristall", "cone", (0, 0.85, 0), (0.4, 0.5, 0.4), "quartz", 5, 0.0),
        ],
        "Container_OrganicTank": [
            ("Tank", "cylinder", (0, -0.1, 0), (1.2, 1.5, 1.2), "leaf", 10),
            ("Deckel", "cylinder", (0, 0.7, 0), (1.25, 0.12, 1.25), "graphite", 10),
            ("Blatt", "cube", (0.3, 0.85, 0), (0.4, 0.04, 0.2), "green", (0, 30, 20)),
        ],
    }
    for name, parts in specs.items():
        reset()
        r = root(name)
        objects = []
        for entry in parts:
            label, shape, position, size, color = entry[:5]
            extra = entry[5] if len(entry) > 5 else None
            kwargs = {}
            if isinstance(extra, int):
                kwargs["vertices"] = extra
            elif isinstance(extra, tuple):
                kwargs["rotation"] = extra
            if len(entry) > 6:
                kwargs["top"] = entry[6]
            objects.append(part(label, shape, position, size, color, **kwargs))
        group("Behälter", objects, parent=r)
        export(name)


MODELS = [
    ("HandDrill", hand_drill), ("Workbench", workbench), ("TractorMast", tractor_mast), ("BeamHopper", beam_hopper),
    ("DrillRobot", drill_robot), ("Pump", pump), ("AlienCrew", alien_crew), ("SpaceElevator", elevator_station),
]


def main():
    for name, build in MODELS:
        reset()
        build()
        export(name)
    containers()


if __name__ == "__main__":
    try:
        main()
    except Exception as error:  # surface failures as a non-zero exit in background mode
        print("FAILED:", error)
        import traceback
        traceback.print_exc()
        sys.exit(1)
