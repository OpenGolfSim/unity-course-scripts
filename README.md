# OpenGolfSim Unity SDK

This repository contains our Unity editor scripts for course building.

## How to use

Drag these scripts to your project by adding them to your `./Assets/` folder. 

> [!NOTE]  
> You may need to create the `Editor` and `Scripts` folders if they doesn't exist in your Unity Project.

### Mesh Import

You can import your cut and conformed meshes into your course project using our import tool.

1. Select the parent folder of your mesh files and import
2. Batch assign materials to surfaces (i.e. rough, fairway, green, etc.)
3. Click import to add your meshes to the scene with the assigned materials

### Course POIs

1. Create a new Course Details game object (New > OpenGolfSim > Course Details)
2. Create holes and position tee, hole, and optional aim point.

### Course Export

To test or play your course in OpenGolfSim, you'll first need to export your course as an asset bundle using our course building tool.
