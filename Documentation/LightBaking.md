# Light Baking

To enhance performance in this project, we baked lighting into lightmaps, light probes, and reflection probes. Using a baked directional light improves GPU performance by eliminating shadow generation costs.

The Arena is an outdoor space with a partially roofed field, posing rendering and lighting challenges. Mixed Lighting was tested, but shadow costs were high due to the arena's size, and quality was poor. Adding Cascade 4 improved visuals, but rendering costs remained excessive. We recommend experimenting with different configurations.

To bake light effectively, we adjusted scene elements. These settings aren't useful at runtime, so we created a script to prepare the scene before generating arena lighting.

## How to Bake the Arena

Load the Arena Scene: [Assets/UltimateGloveBall/Scenes/Arena.unity](../Assets/UltimateGloveBall/Scenes/Arena.unity).

In the scene hierarchy, find the disabled game object named LightingSetup.
![LightingSetup GameObject](./Media/editor/baking_gameobject_location.png)

Click it, then use the context menu in the inspector to set up the scene.
![LightingSetup Context Menu](./Media/editor/baking_lightingsetup.png)

The last two options configure the scene. Selecting `Setup for Lighting` enables game objects needed for light baking. We activate objects that emit light, enable backfaces not visible at runtime but necessary for proper lightmaps, set crowd members to contribute to GI for baked blob shadows, and configure trees to contribute to GI for baked shadows in lightmaps and light probes. Once set up, generate light from the Lighting menu. Afterward, reload the Arena scene or choose the `Revert after lighting` option.
