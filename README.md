# Rhea - Geometry Shader Grass

**Rhea** is a geometry-shader based grass for Unity's [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@11.0/manual/) (URP).

![screenshot](https://github.com/Ryan-Gee/GeometryGrass/blob/main/Media/hero-image.gif)

## Features

*   Procedural and atlas-based texturing
    *   Atlas biasing for non-uniform texture distribution
    *   Vertex color tinting

![screenshot](https://github.com/Ryan-Gee/GeometryGrass/blob/main/Media/vertex-color.png)

*   Customizable blade shape and warping
*   View-based tessellation for increasing grass density efficiently
*   Texture-based trimming and height mapping
*   Vector wind displacement
*   Object-based displacement
*   Normal-based spawning and jittering
    *   Works well on non-planar meshes such as spheres, sharp terrain, etc

![screenshot](https://github.com/Ryan-Gee/GeometryGrass/blob/main/Media/grass-ball.gif)

## Usage

*   Several example materials are included, with examples of texture atlasing and procedural setups
*   For object displacement, attach the [SetDisplacementLocation.cs](https://github.com/Ryan-Gee/GeometryGrass/blob/main/Rhea/Assets/Scripts/SetDisplacementLocation.cs) script to your player, or the object that grass should be displaced from
*   The grass height map and wind vector map can be set to RenderTextures instead, allowing for a trail of grass following an object, dynamic wind from real-time flowmaps, or pre-baked height maps
*   This repository is intended as a technical demonstration of geometry shaders for terrain grass. However, it should be noted that platform support and performance of this implementation, and geometry shaders in general, may not be ideal for general use in games!
*   Use of this asset is under the [CC0 Creative Commons License](https://github.com/Ryan-Gee/RheaGrass/blob/main/LICENSE)

## Shader Property Reference

### Texture

*   **Blade Texture** - A transparent pixel texture or atlas used for texturing each blade of grass. This can be left unassigned if procedural texturing is desired.
    *   If the texture is to be used as an atlas, the individual textures should be aligned in a horizontal row
        *   The textures should be arranged in order of occurrence if biasing is used (most commonly occurring texture on the left, least common on the right)
    *   Textures can be provided at any aspect ration, but should be scaled as necessary to fill the full UDIM
*   **Blade Coloring** - Two colors used for gradient tinting, linearly interpolated between the base and tip of the blade
*   **Luma Jitter** - Randomly darkens some blades
*   **Height Brightness** - Darkens blades based on blade height: lower blades are darker, taller blades are brighter
*   **Hue Jitter** - Randomly varies the hue of each blade (only affects R and G channels to preserve more natural coloration)

### Atlasing

*   **Atlas Length** - The number of textures arranged horizontally in the blade texture
*   **Bias** - Creates a non-uniform distribution of textures from the atlas: textures on the left side will occur more frequently than textures on the right side
*   **Height Bias** - Applies texture biasing based on height instead of random selection: lower blades will have textures selected from the left hand side, higher blades from the right.

### Blade Size

*   **Width** - The upper and lower bounds of the width of the blades
*   **Height** - The upper and lower bounds of the height of the blades
*   **Tapered Blades** - Changes geometry from a mesh ribbon to a segmented triangle, useful for procedurally textured blades

### Blade Warping

*   **Bending** - The overall amount of flex in each blade (can be positive or negative)
*   **Curvature** - How curved the blade is across it's flex. Higher values will give straighter bases and more arched ends
*   **Variation** - Controls how much the blades flow together, higher values will give a denser, bushier appearance
*   **Position Jitter** - Randomly varies the position of each blade, useful to break up tessellation patterns

### Tessellation

*   **Density** - The overall maximum amount of tessellation applied
*   **View Distance** - Controls how much the density fades off with distance from the camera

### Trimming

*   **Trim Map** - A grayscale texture used to determine the height and culling of grass blades (darker values are shorter, brighter values are taller)
*   **Threshold** - The darkness threshold at which grass blades will be culled entirely
*   **Falloff** - Controls the contrast of the texture when used as a heightmap for blades

### Wind

*   **Wind Vector Map** - A vector displacement map used for bending blades as wind
*   **Velocity vector** - The direction of the wind displacement
*   **Speed** - The panning speed of the wind map

### Object Displacement

_To be used with SetDisplacementLocation.cs._

*   **Radius** - The radius from the objects location at which grass will be displaced
*   **Strength** - The amount of displacement on the grass from the object
*   **Falloff** - How sharply the displacement around the object ends
