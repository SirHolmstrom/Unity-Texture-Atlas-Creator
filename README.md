# Unity Texture Atlas Creator
- The `Texture Atlas Creator` was designed to help you doing simple tasks directly from the `Unity Editor` it simplify the process (and saves time) of creating texture `Atlases` or `collections` with extra tools.
- *`The Editor` was just intended to collect and stack `2DTextures` into a `Atlas` with ease, Then it became something else and I do plan on updating it but for now it is what it is!*.

- *I might make a more complex version with more structure, but I want this to be all in one file.*<br>
- *Expect some bugs when it's coming to edge cases and I'm open to adding more features upon request.*
- *For a quick look at the editor see*: [Screenshots](#screenshots).

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

### Import Options
- **Import Folder**: Imports the whole folder of selected image and creates assets from it without adding it to your project.

### Export Options
- **Save Atlas**: Export the entire texture atlas as a single PNG file.
- **Save Individually**: Export each texture individually with all applied modifications.

### Preview and UI
- **Real-time Preview**: View the texture atlas as you make adjustments.

## Getting Started

### Installation

1. Clone or download the repository.
2. Place the `TextureAtlasEditor.cs` script into your Unity project, preferably within an `Editor` folder.

### Usage

1. Open the Texture Atlas Creator from the Unity menu: `Tools -> Texture Atlas Creator` or `Alt + Shift + A`.
2. Add textures to the list by dragging and dropping them into the designated area or using the `+` button or use the `Top Left -> Import (Folder)` and select one item to import the folder.
3. Adjust the settings on the right sidebar:
   - **Packing**: Choose between Grid Packing and Tight Packing.
   - **Extend & Cropping**: Adjust the extend (padding) and cropping values.
   - **Outline**: Set the thickness and color of the outline and apply Gaussian blur if needed or use Pixel.
   - **Resize**: Set the percentage to resize the atlas.

4. Click the `Generate` button to create the texture atlas (Auto preview Generator).
5. Preview the atlas in the middle area.
6. Save the atlas using the `Save Atlas` button or save individual textures using the `Save Individually` button.

## Screenshots
![LatestEditor](https://github.com/SirHolmstrom/Unity-Texture-Atlas-Creator/assets/71155336/1b191d2e-d26a-4222-82d2-87e15b3791c8)
![image](https://github.com/SirHolmstrom/Unity-Texture-Atlas-Creator/assets/71155336/d414bb6f-78fb-4130-b7dc-a65981dc9227)
