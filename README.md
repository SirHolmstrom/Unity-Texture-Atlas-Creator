# Texture Atlas Creator

The Texture Atlas Creator is designed to help you with doing simple tasks directly from the Unity Editor it simplify the process of creating texture atlases or collections. It allows for easy texture packing, resizing, padding, cropping, outlining and rescaling.
*(Expect some bugs when it's coming to edge cases and I'm open to adding more features upon request)*.
*The Editor was just intended to collect and stack 2D Textures into a Atlas with ease, Then it became something else and I do plan on updating it but for now it is what it is!*.
*I wanted it to be a one file only editor*.

## Features

### Texture Packing
- **Grid Packing**: Arrange textures in a grid layout with customizable rows and columns.
- **Tight Packing**: Efficiently pack textures to minimize unused space.

### Texture Resizing
- **Custom Resize**: Adjust the size of the texture atlas by a specified percentage.
- **Original Quality Preservation**: Always resizes based on the original textures to prevent quality degradation.

### Padding and Cropping
- **Padding**: Add space around each texture to avoid bleeding artifacts.
- **Cropping**: Remove unwanted space around textures.

### Outlining
- **Customizable Outline**: Add outlines to textures with adjustable thickness and color.
- **Gaussian Blur**: Apply a Gaussian blur to the outlines for a smoother appearance.

### Resizing
- **Resizing**: Simple Resizing Scale by (%).

### Export Options
- **Save Atlas**: Export the entire texture atlas as a single PNG file.
- **Save Individually**: Export each texture individually with all applied modifications.

### Preview and UI
- **Real-time Preview**: View the texture atlas as you make adjustments.
- 
## Getting Started

### Installation

1. Clone or download the repository.
2. Place the `TextureAtlasEditor.cs` script into your Unity project, preferably within an `Editor` folder.

### Usage

1. Open the Texture Atlas Creator from the Unity menu: `Tools -> Texture Atlas Creator`.
2. Add textures to the list by dragging and dropping them into the designated area or using the `+` button.
3. Adjust the settings on the right sidebar:
   - **Packing**: Choose between Grid Packing and Tight Packing.
   - **Resize**: Set the percentage to resize the atlas.
   - **Padding and Cropping**: Adjust the padding and cropping values.
   - **Outline**: Set the thickness and color of the outline and apply Gaussian blur if needed.
4. Click the `Generate` button to create the texture atlas.
5. Preview the atlas in the middle area.
6. Save the atlas using the `Save Atlas` button or save individual textures using the `Save Individually` button.
