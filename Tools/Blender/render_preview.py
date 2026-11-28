"""Renders a contact sheet of all exported models to Docs/Art/Modelluebersicht.png.

    blender --background --factory-startup --python Tools/Blender/render_preview.py
"""
import math
import os

import bpy

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
MODELS = os.path.join(ROOT, "Assets", "_Game", "Art", "Models")
OUT = os.path.join(ROOT, "Docs", "Art", "Modelluebersicht.png")

# Scale per model so every one reads at a similar size on the sheet.
LAYOUT = [
    ("HandDrill", 6.0), ("Workbench", 0.8), ("TractorMast", 0.55), ("BeamHopper", 1.0), ("DrillRobot", 1.0),
    ("Pump", 0.85), ("AlienCrew", 1.4), ("Container_PressTub", 1.0), ("Container_Barrel", 1.0),
    ("Container_GlassSphere", 1.0), ("Container_OreHopper", 1.0), ("Container_MagneticBin", 1.0),
    ("Container_PrismBox", 1.0), ("Container_OrganicTank", 1.0), ("Container_GlowPrism", 1.0),
]


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    columns = 8
    for index, (name, scale) in enumerate(LAYOUT):
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=os.path.join(MODELS, name + ".fbx"))
        added = [o for o in bpy.data.objects if o not in before]
        roots = [o for o in added if o.parent is None]
        for obj in roots:
            obj.scale = tuple(s * scale for s in obj.scale)
            obj.location = ((index % columns) * 3.2, -(index // columns) * 3.6, -(index // columns) * 3.4)
    bpy.ops.object.camera_add(location=(9.6, -16.5, 4.6), rotation=(math.radians(70), 0, 0))
    camera = bpy.context.active_object
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = 22
    scene.camera = camera
    scene.render.engine = 'BLENDER_WORKBENCH'
    shading = scene.display.shading
    shading.light = 'STUDIO'
    shading.color_type = 'VERTEX'
    shading.show_shadows = True
    shading.show_cavity = True
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 800
    scene.render.film_transparent = False
    world = bpy.data.worlds.new("Hintergrund")
    scene.world = world
    world.color = (0.86, 0.93, 0.9)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    scene.render.filepath = OUT
    bpy.ops.render.render(write_still=True)
    print("rendered", OUT)


if __name__ == "__main__":
    main()
