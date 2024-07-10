/*
 * =============================================================================
 *  Feel free to use this tool for quick tweaks without the hassle of 
 *  opening image software. Initially, it was simple, but now it includes 
 *  features like outline editing. Expect some bugs, and I'm open to adding 
 *  more features upon request.
 *
 *  Enjoy, and consider starring the project on GitHub!
 *  https://github.com/SirHolmstrom
 * =============================================================================
 */
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using PlasticGui.WebApi.Responses;
using UnityEditor.Toolbars;
using Codice.CM.Common.Tree.Partial;

public partial class TextureAtlasEditor /*Editor*/      : EditorWindow
{
    [MenuItem("Tools/Texture Atlas Creator #&a", priority = -1000)]
    public static void ShowWindow()
    {
        GetWindow<TextureAtlasEditor>("Texture Atlas Creator");
    }

    private void OnEnable()
    {
        // Creates the state textures.
        CreateActiveTextures();

        // Setup the lists and callbacks.
        SetupTextureList();

        // Get the path of the current script.
        GetEditorPath();

        // Caches Editor Icons. (WIP).
        CacheIcons();
    }

    void OnGUI()
    {
        // Init styles.
        HandleStyles();

        // Top Toolbar.
        DrawToolbar();

        // Start Editor Window.
        GUILayout.BeginVertical(GUILayout.Height(position.height - BOTTOM_BAR_HEIGHT));

        // Start Left Left, Middle and Right Areas.
        GUILayout.BeginHorizontal();

        // Left side: Menu Buttons & Zoom bar.
        DrawLeftSidebar();

        // Center: Image Preview.
        Rect centerArea = new Rect(SIDE_BAR_WIDTH + 7 /*extra padding*/, EditorStyles.toolbar.fixedHeight, PREVIEW_AREA_WIDTH + SIDE_BAR_WIDTH, position.height - BOTTOM_BAR_HEIGHT);
        GUILayout.BeginArea(centerArea);

        DrawPreviewArea(centerArea);

        GUILayout.EndArea();

        // Right side: Configuration area.
        Rect rightSidebarArea = new Rect(position.width - ONE_THIRD_SIZE, EditorStyles.toolbar.fixedHeight, ONE_THIRD_SIZE, position.height - BOTTOM_BAR_HEIGHT);

        GUILayout.BeginArea(rightSidebarArea);

        DrawRightSidebar(rightSidebarArea);

        GUILayout.EndArea();

        // End of Left, Middle and Right Areas.
        GUILayout.EndHorizontal();

        // End Editor Window.
        GUILayout.EndVertical();

        // Bottom bar.
        DrawBottomBar();

        // Updates various events. (WIP).
        HandleEvents();

        // Handle dialogs
        HandleDialogs();

    }
}

public partial class TextureAtlasEditor // Fields       
{
    #region Editor Fields

    // Lists ---
    private List<Texture2D> textures = new List<Texture2D>();
    private ReorderableList textureList;

    // Scroll ---
    private Vector2 listScrollPos;

    // Zoom ---
    private float zoomScale = 0.5f;
    private const float zoomMin = 0.1f;
    private const float zoomMax = 1f;

    // Debug ---
    private List<Rect> textureRects = new List<Rect>();
    private int calculatedWidth = 0;
    private int calculatedHeight = 0;
    protected string LAST_DEBUG_MESSAGE = "";

    // Atlas Textures ---
    private Texture2D atlas; // original.
    private Texture2D resizedAtlas; // downsized.
    private Texture2D gridTexture; // grid.

    // Preview Section
    private Vector2 previewScrollPos;
    private Vector2 previewOffset;
    private bool isPanning;
    private Vector2 lastMousePos;

    // Measurement Modes ---
    private enum MeasurementMode { Columns, Rows, /*Custom */ }
    private MeasurementMode measurementMode = MeasurementMode.Columns;
    private MeasurementMode lastMeasurementMode = MeasurementMode.Columns;

    // Outline ---
    private Color outlineColor = Color.black;
    private int outlineThickness = 0;
    private enum OutlineMode { Pixel, Gaussian }
    private OutlineMode outlineMode = OutlineMode.Gaussian;

    // Good to have ---
    protected string scriptPath = "";
    protected string directoryPath = "";
    protected string combinedPath = "";

    // Right Side Menu ---
    private int SelectionRightMenu = 0;
    private enum RightMenu { AtlasSetting, ScaleSetting, OutlineSetting }
    #endregion

    #region Modification & Alignment Fields

    // Modification & Alignment Settings ---
    private int resizePercentage = 100;
    private int columns = 3;
    private int rows = 3;

    // Padding & Cropping (WILL FIX LATER) ---
    private int padding = 0; // collected value of crop and padding.
    private int cropValue = 0;
    private int extraPadding = 0;
    // Max ---
    private int maxPadding = 0;
    private int maxCropping = 0;

    // tracking
    private int oldResizePercentage = 0; // will be used.
    private int oldPadding = 0;
    private int oldColumns = 0;
    private int oldRows = 0;

    #endregion

    #region Textures & Icons && Constants && States && Styles
    // Styles ---
    private GUIStyle FOLDOUT_STYLE;
    private GUIStyle LEFT_HELPBOX_STYLE;
    private GUIStyle GENERIC_BUTTON_STYLE;
    private GUIStyle GENERIC_BUTTON_STYLE_SMALL;

    private GUIStyle RIGHT_TOOLBAR_STYLE;
    private GUIStyle CENTERED_MINI_LABEL_LARGE;

    // Button State Textures ---
    private Texture2D checkerboardTexture;
    private Texture2D activeBackground;
    private Texture2D normalBackground;
    private Texture2D clearTexture;
    private Texture2D greyTexture;
    private Texture2D lightGreyTexture;
    private Texture2D darkGreyTexture;

    // Icons ---
    private GUIContent SCALE_ICON = null;
    private GUIContent SAVE_ICON = null;
    private GUIContent PLUS_ICON = null;
    private GUIContent ZOOM_IN_ICON = null;

    private GUIContent MODE_ICON = null;
    private GUIContent OUTLINE_TYPE_ICON = null;

    private GUIContent PADDING_ICON = null;
    private GUIContent CROP_ICON = null;

    private GUIContent GIZMO_ICON = null;
    private GUIContent CHECKERS_ICON = null;


    // Editor/Foldout States ---
    private bool FOLDOUT_SCALE_CROP = true;
    private bool FOLDOUT_TEXTURE_ALIGNMENT = true;
    private bool FOLDOUT_OUTLINE = false;
    private bool FOLDOUT_SIZE = false;
    private bool FOLDOUT_GIZMO = false;

    private bool SHOW_HELP = false;
    private bool SHOW_GIZMOS = false;
    private bool SHOW_CHECKER = true;

    // Dialogue States (WIP) ---
    private bool showResizeDialog = false;
    private bool showSaveDialog = false;
    private bool showResetConfirmationDialog = false;

    // Constants ---
    private const int SIDE_BAR_WIDTH = 32;
    private const int MAX_SIZE_TEXTURE = 8192;
    private const int BOTTOM_BAR_HEIGHT = 70;
    private const float DEFAULT_TOOLBARWIDTH = 112.5F;

    // Constant Properties
    private float ONE_THIRD_SIZE => (position.width / 3);
    private float PREVIEW_AREA_WIDTH => (ONE_THIRD_SIZE * 2) - SIDE_BAR_WIDTH;

    // Static
    private static readonly string normalizedDataPath = Application.dataPath.Replace("\\", "/");

    #endregion
}

public partial class TextureAtlasEditor // Atlas        
{
    /// <summary>
    /// Calculate the final Atlas/Preview Size.
    /// </summary>
    private void CalculateAtlasTextureSize()
    {
        calculatedWidth = 0;
        calculatedHeight = 0;

        if (textures == null || textures.Count == 0)
        {
            DebugEditor("No textures selected!");
            return;
        }

        int currentX = 0;
        int currentY = 0;
        int rowHeight = 0;
        int columnCounter = 0;

        foreach (Texture2D texture in textures)
        {
            if (texture == null) continue;

            int paddedWidth = texture.width + padding * 2;
            int paddedHeight = texture.height + padding * 2;

            if (measurementMode == MeasurementMode.Columns && columnCounter >= columns)
            {
                columnCounter = 0;
                currentX = 0;
                currentY += rowHeight;
                rowHeight = 0;
            }
            else if (measurementMode == MeasurementMode.Rows && columnCounter >= Mathf.CeilToInt((float)textures.Count / rows))
            {
                columnCounter = 0;
                currentX = 0;
                currentY += rowHeight;
                rowHeight = 0;
            }

            currentX += paddedWidth;
            rowHeight = Mathf.Max(rowHeight, paddedHeight);
            columnCounter++;

            calculatedWidth = Mathf.Max(calculatedWidth, currentX);
            calculatedHeight = currentY + rowHeight;
        }

        calculatedWidth = Mathf.Min(calculatedWidth, MAX_SIZE_TEXTURE);
        calculatedHeight = Mathf.Min(calculatedHeight, MAX_SIZE_TEXTURE);
    }

    //(TODO IMPLEMENTS THE NEW PROCESSING BUT NEEDS SOME ADJUSTMENTS FIRST)

    /// <summary>
    /// Generate the Final Atlas/Preview.
    /// </summary>
    private void UpdateAtlas(bool drawOutline = false)
    {
        if (textures == null || textures.Count == 0)
        {
            gridTexture = null;
            resizedAtlas = null;
            atlas = null;
            DebugEditor("No textures selected!");
            return;
        }

        CalculateAtlasTextureSize();

        if (calculatedWidth > MAX_SIZE_TEXTURE || calculatedHeight > MAX_SIZE_TEXTURE)
        {
            DebugEditor($"Cannot generate texture atlas. Calculated size {calculatedWidth}x{calculatedHeight} exceeds maximum allowed size {MAX_SIZE_TEXTURE}x{MAX_SIZE_TEXTURE}.");
            return;
        }

        // Create the atlas texture
        atlas = new Texture2D(calculatedWidth, calculatedHeight, TextureFormat.RGBA32, false);
        atlas.filterMode = FilterMode.Point;
        atlas.wrapMode = TextureWrapMode.Clamp;

        // Create the grid texture
        gridTexture = new Texture2D(calculatedWidth, calculatedHeight, TextureFormat.RGBA32, false);
        gridTexture.filterMode = FilterMode.Point;
        gridTexture.wrapMode = TextureWrapMode.Clamp;


        // Initialize the atlas with fully transparent pixels
        Color[] transparentPixels = new Color[calculatedWidth * calculatedHeight];
        for (int i = 0; i < transparentPixels.Length; i++)
        {
            transparentPixels[i] = new Color(0, 0, 0, 0); // Fully transparent color
        }

        // invisible pixels.
        atlas.SetPixels(transparentPixels);
        gridTexture.SetPixels(transparentPixels);

        // update.
        atlas.Apply();
        gridTexture.Apply();

        // collect all rects of the textures.
        textureRects.Clear();

        int currentX = 0;
        int currentY = 0;
        int rowHeight = 0;
        int columnCounter = 0;

        foreach (Texture2D texture in textures)
        {
            if (texture == null) continue;

            Texture2D processedTexture = texture;
            if (outlineThickness > 0 && drawOutline)
            {
                processedTexture = ApplyOutline(texture, outlineThickness, outlineColor);
            }

            int paddedWidth = processedTexture.width + padding * 2;
            int paddedHeight = processedTexture.height + padding * 2;

            if (measurementMode == MeasurementMode.Columns && columnCounter >= columns)
            {
                columnCounter = 0;
                currentX = 0;
                currentY += rowHeight;
                rowHeight = 0;
            }
            else if (measurementMode == MeasurementMode.Rows && columnCounter >= Mathf.CeilToInt((float)textures.Count / rows))
            {
                columnCounter = 0;
                currentX = 0;
                currentY += rowHeight;
                rowHeight = 0;
            }

            // Create a new texture with padding or cropping
            Texture2D paddedTexture = new Texture2D(paddedWidth, paddedHeight);
            Color[] clearPixels = new Color[paddedWidth * paddedHeight];

            for (int i = 0; i < clearPixels.Length; i++)
            {
                clearPixels[i] = new Color(0, 0, 0, 0); // Transparent color
            }

            paddedTexture.SetPixels(clearPixels);

            if (padding >= 0)
            {
                paddedTexture.SetPixels(padding, padding, processedTexture.width, processedTexture.height, processedTexture.GetPixels());
            }
            else
            {
                int cropX = Mathf.Abs(padding);
                int cropY = Mathf.Abs(padding);
                int cropWidth = Mathf.Max(processedTexture.width - 2 * cropX, 0);
                int cropHeight = Mathf.Max(processedTexture.height - 2 * cropY, 0);

                if (cropWidth > 0 && cropHeight > 0)
                {
                    paddedTexture.SetPixels(0, 0, cropWidth, cropHeight, processedTexture.GetPixels(cropX, cropY, cropWidth, cropHeight));
                }
            }

            paddedTexture.Apply();

            // Set pixels to atlas
            Color[] texturePixels = paddedTexture.GetPixels();
            atlas.SetPixels(currentX, currentY, paddedWidth, paddedHeight, texturePixels);

            textureRects.Add(new Rect(currentX, currentY, paddedWidth, paddedHeight));

            currentX += paddedWidth;
            rowHeight = Mathf.Max(rowHeight, paddedHeight);
            columnCounter++;
        }

        if (packingOption == Packing.TightExperimental)
        {
            atlas = TightPacking(textures, calculatedWidth, calculatedHeight);
        }

        atlas.Apply();

        // Draw outline for debugging
        DrawDebugGrid(atlas, textureRects);

        DebugEditor("Texture atlas generated successfully.");

        // after the atlas is ready let's build the resize preview.
        CheckResize();
    }

    /// <summary>
    /// Updates the Preview with all current settings.
    /// </summary>
    private void UpdatePreview(bool drawOutline = false)
    {
        UpdateAtlas(drawOutline);
    }

    // TODO IMPLEMENT TO UPDATE ATLAS (BUT BROKE FOR SOME RESON)
    #region Processing
    /// <summary>
    /// Processes a texture by applying an outline (if needed), padding or cropping, and resizing. Finally, ensures the texture is readable.
    /// </summary>
    /// <param name="texture">The original texture to process.</param>
    /// <returns>A new, processed texture that is readable.</returns>
    private Texture2D ProcessTexture(Texture2D texture)
    {
        // Apply an outline to the texture (if the outline thickness is greater than 0).
        Texture2D processedTexture = ApplyOutlineIfNeeded(texture);

        // Apply padding and cropping based on the specified padding value. (TODO CHANGE PADDING TO EXTENDING)
        processedTexture = ApplyPaddingOrCropping(processedTexture);

        // Resize the texture if requested.
        processedTexture = ResizeIfNeeded(processedTexture);

        // Making sure that the processed texture is readable.
        return GetReadableTexture(processedTexture);
    }

    /// <summary>
    /// Applies an outline to the texture if the specified outline thickness is greater than 0.
    /// </summary>
    /// <param name="texture">The texture to potentially apply an outline to.</param>
    /// <returns>The original texture with an outline applied, or the original texture if no outline is needed.</returns>
    private Texture2D ApplyOutlineIfNeeded(Texture2D texture)
    {
        if (outlineThickness > 0)
        {
            // Apply and return the texture with an outline.
            return ApplyOutline(texture, outlineThickness, outlineColor);
        }

        // Return the original texture if no outline is applied.
        return texture;
    }

    /// <summary>
    /// Applies padding or cropping to the texture based on the specified padding value.
    /// </summary>
    /// <param name="texture">The texture to apply padding or cropping to.</param>
    /// <returns>A new texture with padding or cropping applied.</returns>
    private Texture2D ApplyPaddingOrCropping(Texture2D texture)
    {
        // Calculate the new texture dimensions after extendng/cropping (padding).
        int paddedWidth = texture.width + padding * 2;
        int paddedHeight = texture.height + padding * 2;
        Texture2D finalTexture = new Texture2D(paddedWidth, paddedHeight);

        // Initialize the new texture with transparent pixels.
        Color[] clearPixels = new Color[paddedWidth * paddedHeight];
        for (int j = 0; j < clearPixels.Length; j++)
        {
            clearPixels[j] = new Color(0, 0, 0, 0); // Transparent color
        }
        finalTexture.SetPixels(clearPixels);

        // Apply extending or cropping based on the padding value.
        if (padding >= 0)
        {
            finalTexture.SetPixels(padding, padding, texture.width, texture.height, texture.GetPixels());
        }
        else
        {
            // Apply cropping by calculating the crop dimensions and setting the cropped pixels.
            int cropX = Mathf.Abs(padding);
            int cropY = Mathf.Abs(padding);
            int cropWidth = Mathf.Max(texture.width - 2 * cropX, 0);
            int cropHeight = Mathf.Max(texture.height - 2 * cropY, 0);

            if (cropWidth > 0 && cropHeight > 0)
            {
                finalTexture.SetPixels(0, 0, cropWidth, cropHeight, texture.GetPixels(cropX, cropY, cropWidth, cropHeight));
            }
        }

        finalTexture.Apply();

        return finalTexture;
    }

    /// <summary>
    /// Resizes the texture based on the specified resize percentage, if it is not set to 100% (default).
    /// </summary>
    /// <param name="texture">The texture to resize.</param>
    /// <returns>The resized texture, or the original texture if no resizing is needed.</returns>
    private Texture2D ResizeIfNeeded(Texture2D texture)
    {
        if (resizePercentage != 100)
        {
            // Resize and return the texture based on the specified percentage.
            return GetTexture(texture, resizePercentage);
        }
        // Return the original texture.
        return texture;
    }

    #endregion

}

public partial class TextureAtlasEditor // Save         
{
    // Methods ---

    /// <summary>
    /// Saves the whole Atlas with modifications.
    /// </summary>
    private void SaveAtlas()
    {
        if (atlas != null)
        {
            string path = EditorUtility.SaveFilePanel("Save Texture Atlas", "", "TextureAtlas.png", "png");
            if (!string.IsNullOrEmpty(path))
            {
                Texture2D textureToSave = resizedAtlas != null ? resizedAtlas : atlas;
                SaveTextureAsPNG(textureToSave, path, false);
            }
        }
        else
        {
            DebugEditor("No texture atlas to save!");
        }
    }

    /// <summary>
    /// Saves every image with modification seperately as a batch.
    /// </summary>
    private void SaveTexturesIndividually()
    {
        // Check if there are any textures selected for processing.
        if (textures == null || textures.Count == 0)
        {
            DebugEditor("No textures selected!");
            return;
        }

        // Select where to save the textures.
        string folderPath = EditorUtility.SaveFolderPanel("Save Textures Individually", "", "");
        if (string.IsNullOrEmpty(folderPath))
        {
            DebugEditor("No folder selected for saving textures!");
            return;
        }

        // Go through each texture in the list.
        for (int i = 0; i < textures.Count; i++)
        {
            Texture2D texture = textures[i];
            // Skip null textures.
            if (texture == null) continue;

            // Process the texture (apply outline, padding/cropping, resize, etc.).
            Texture2D finalTexture = ProcessTexture(texture);
            // Construct the path where the texture will be saved.
            string texturePath = Path.Combine(folderPath, $"Texture_{i}.png");
            // Save the processed texture as a PNG file.
            SaveTextureAsPNG(finalTexture, texturePath, true);

            // Clean up: Destroy the processed texture if it's a new instance.
            if (finalTexture != texture)
            {
                DestroyImmediate(finalTexture);
            }
        }

        // Notify that all textures have been saved.
        DebugEditor($"All textures saved to: {folderPath}.");
    }

    /// <summary>
    /// Encoding to PNG to path.
    /// </summary>
    private void SaveTextureAsPNG(Texture2D texture, string path, bool isBulkSave)
    {
        // If it's a bulk save operation, ensure we don't overwrite existing files.
        if (isBulkSave && File.Exists(path))
        {
            path = GetIncrementalPath(path);
        }

        // Encode the texture to PNG format and save it to the specified path.
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(path, bytes);

        // Notify the user where the texture was saved.
        DebugEditor($"Texture saved to: {path}");

        // Normalize the path for asset importing.
        string normalizedPath = NormalizePath(path);

        // If the saved texture is within the Assets folder, apply import settings.
        if (normalizedPath.StartsWith(normalizedDataPath))
        {
            string relativePath = $"Assets{normalizedPath.Substring(normalizedDataPath.Length)}";
            SetTextureImporterSettings(relativePath);

            // TODO: CREATE A FOLDER INSTAED WHEN DOING BULK?
        }
    }

    // Helpers ---

    /// <summary>
    /// Finds the next available path and names the bulked export to it instead of overriding.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private string GetIncrementalPath(string path)
    {
        string directory = Path.GetDirectoryName(path);
        string filename = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        int counter = 1;

        string newPath = path;
        while (File.Exists(newPath))
        {
            newPath = Path.Combine(directory, $"{filename}_{counter}{extension}");
            counter++;
        }

        return NormalizePath(newPath);
    }

    /// <summary>
    /// Sets the textures to Sprite and make then read/write in bulk after export.
    /// </summary>
    /// <param name="assetPath"></param>
    private void SetTextureImporterSettings(string assetPath)
    {
        // Delay the execution of the importer settings application
        EditorApplication.delayCall += () =>
        {
            // Ensure the asset exists
            if (!File.Exists(assetPath))
            {
                Debug.LogError("Asset does not exist: " + assetPath);
                return;
            }

            // Import asset to ensure it's up-to-date
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            // Retrieve the TextureImporter
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("Failed to get TextureImporter for path: " + assetPath);
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.isReadable = true;
            importer.SaveAndReimport();

            // Save and reimport to apply changes
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        };

    }
}

public partial class TextureAtlasEditor // Menu Methods 
{
    // Reset ---

    /// <summary>
    /// Resets Atlas Creator.
    /// </summary>
    private void PerformResetAtlas()
    {
        // Clear the textures list
        ClearTextureList();

        // clear up textures.
        DestroyTextures();

        // Reset some values.
        ResetValues();

        // Clear the preview window
        Repaint();

        // Notify.
        DebugEditor("Atlas has been reset.");
    }

    /// <summary>
    /// Saves the whole Atlas with modifications.
    /// </summary>
    private void ResetAtlas()
    {
        if (atlas != null)
        {
            // Making sure to Save prompt.
            showResetConfirmationDialog = true;
        }
        else
        {
            // Just Reset.
            PerformResetAtlas();
        }
    }

    /// <summary>
    /// Destroys all atlases and debug grid.
    /// </summary>
    private void DestroyTextures()
    {
        // Temp Atlas/Preview.
        if (atlas != null)
        {
            DestroyImmediate(atlas);
            atlas = null;
        }

        // Temp Resized.
        if (resizedAtlas != null)
        {
            DestroyImmediate(resizedAtlas);
            resizedAtlas = null;
        }

        // Grid.
        if (gridTexture != null)
        {
            DestroyImmediate(gridTexture);
            gridTexture = null;
        }
    }

    /// <summary>
    /// Clears the texture list.
    /// </summary>
    private void ClearTextureList()
    {
        textures.Clear();
        textureList.list = textures;
    }

    /// <summary>
    /// Resets most standard values.
    /// </summary>
    private void ResetValues()
    {
        // Reset calculated dimensions
        calculatedWidth = 0;
        calculatedHeight = 0;

        // Reset resize percentage
        resizePercentage = 100;
        oldResizePercentage = 0;

        // Reset scroll positions and zoom
        previewScrollPos = Vector2.zero;
        zoomScale = 1.0f;

        // Reset other related fields if needed
        padding = 0;
        outlineThickness = 0;
    }

    // Popup Dialogues (WIP) ---

    /// <summary>
    /// Handles the dialogs when Reseting.
    /// </summary>
    private void HandleDialogs()
    {
        if (showResetConfirmationDialog)
        {
            if (EditorUtility.DisplayDialog("Warning", "Are you sure you want to reset the current Atlas?", "Yes", "No"))
            {
                showSaveDialog = true;
            }
            showResetConfirmationDialog = false;
        }

        if (showSaveDialog)
        {
            if (EditorUtility.DisplayDialog("Save", "Do you want to save the current Atlas?", "Yes", "No"))
            {
                SaveAtlas();
            }
            else
            {
                PerformResetAtlas();
            }

            showResetConfirmationDialog = false;
            showSaveDialog = false;
        }
    }

    // Drag and Drop ---
    /// <summary>
    /// Logic for Drag and Drop (Asset or Explorer).
    /// </summary>
    private bool HandleDroppedAssets()
    {
        // we need to whole loop.
        bool added = false;

        DragAndDrop.AcceptDrag();

        foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
        {
            if (draggedObject is Texture2D texture)
            {
                if (!texture.isReadable)
                {
                    // If the texture is not read/write we will create a temporary one in its place.
                    Texture2D readableTexture = GetReadableTexture(texture);

                    // keep the name.
                    readableTexture.name = texture.name;

                    // add the temp.
                    textures.Add(readableTexture);

                    DebugEditor("Created a temporary image because read/write not set.");
                }

                else
                {
                    // was readable.
                    textures.Add(texture);
                }
            }

            added = true;

        }

        if (!added)
        {
            // Handle dragging from the Windows Explorer
            foreach (string path in DragAndDrop.paths)
            {
                if (path.EndsWith(".png") || path.EndsWith(".jpg") || path.EndsWith(".jpeg") || path.EndsWith(".tga"))
                {
                    byte[] fileData = File.ReadAllBytes(path);
                    // Create a new texture
                    Texture2D externalTexture = new Texture2D(2, 2);
                    externalTexture.LoadImage(fileData); // Load the texture from bytes

                    // get the name and use it.
                    string fileName = Path.GetFileNameWithoutExtension(path); // Get the file name without extension
                    externalTexture.name = fileName;

                    textures.Add(externalTexture);
                    added = true;
                }
            }
        }


        return added;
    }

    // Import ---

    /// <summary>
    /// Sets Read/Write for the texture.
    /// </summary>
    /// <param name="assetPath"></param>
    private void SetTextureReadable(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
    }

    /// <summary>
    /// Loads all selected textures from folder.
    /// </summary>
    private void ImportImagesFromFolder()
    {
        string[] filters = new string[] { "Image files", "png,jpg,jpeg", "All files", "*" };
        string path = EditorUtility.OpenFilePanelWithFilters("Select One Image To Import Folder", "", filters);

        if (!string.IsNullOrEmpty(path))
        {
            string directoryPath = Path.GetDirectoryName(path);
            string[] files = Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly);

            List<string> imageFiles = new List<string>();
            foreach (string file in files)
            {
                if (file.EndsWith(".png") || file.EndsWith(".jpg") || file.EndsWith(".jpeg"))
                {
                    imageFiles.Add(file);
                }
            }

            if (imageFiles.Count > 0)
            {
                LoadSelectedImages(imageFiles);
            }
            else
            {
                DebugEditor("No image files found in the selected folder.");
            }
        }
    }

    /// <summary>
    /// Loads all the Images from Import.
    /// </summary>
    /// <param name="imageFiles"></param>
    private void LoadSelectedImages(List<string> imageFiles)
    {
        foreach (string imagePath in imageFiles)
        {
            byte[] fileData = File.ReadAllBytes(imagePath);
            Texture2D texture = new Texture2D(2, 2);

            string fileName = Path.GetFileNameWithoutExtension(imagePath); // Get the file name without extension
            texture.name = fileName;

            texture.LoadImage(fileData);
            textures.Add(texture);
        }

        maxPadding = CalculateMaxPadding();
        maxCropping = CalculateMaxCropping();
        rows = Mathf.Max(rows, CalculateMinimumRows());
        columns = Mathf.Max(columns, CalculateMinimumColumns());

        UpdatePreview();
        DebugEditor($"{imageFiles.Count} images loaded.");
    }
}

public partial class TextureAtlasEditor // Packing      
{
    // Fields ---
    public enum Packing { Grid, TightExperimental }
    public Packing packingOption = Packing.Grid;

    // Draw Editor ---

    /// <summary>
    /// Draw packing dropdown.
    /// </summary>
    private void DrawPackingOptions()
    {
        GUILayout.BeginHorizontal();

        IconButton(MODE_ICON, "Packing:", "Grid: Respects order and Aligment fully.\nTight: Packs the Textures as tightly as possible.");
        GUILayout.FlexibleSpace();
        packingOption = (Packing)EditorGUILayout.EnumPopup(packingOption);

        GUILayout.EndHorizontal();
    }

    // Logic ---

    /// <summary>
    /// Simple Grid packing packs all images one by one and will not try to fit everything.<br></br>
    /// (Don't need after all as it was my default behaviour but I will improve upon the packing methods later).
    /// </summary>
    private Texture2D GridPacking(List<Texture2D> textures, int atlasWidth, int atlasHeight)
    {
        Texture2D atlas = new Texture2D(atlasWidth, atlasHeight);
        int x = 0;
        int y = 0;
        int rowHeight = 0;

        foreach (Texture2D texture in textures)
        {
            if (x + texture.width > atlasWidth)
            {
                x = 0;
                y += rowHeight;
                rowHeight = 0;
            }

            atlas.SetPixels(x, y, texture.width, texture.height, texture.GetPixels());
            x += texture.width;

            if (texture.height > rowHeight)
            {
                rowHeight = texture.height;
            }
        }

        atlas.Apply();
        return atlas;
    }

    /// <summary>
    /// Packs and takes into account the sizes of the textures making sure to fill up the spaces.
    /// </summary>
    private Texture2D TightPacking(List<Texture2D> textures, int atlasWidth, int atlasHeight)
    {
        Texture2D atlas = new Texture2D(atlasWidth, atlasHeight);
        Color[] clearPixels = new Color[atlasWidth * atlasHeight];
        for (int i = 0; i < clearPixels.Length; i++)
        {
            clearPixels[i] = Color.clear; // Sets the pixels to transparent
        }
        atlas.SetPixels(clearPixels);
        atlas.Apply();

        bool[,] occupied = new bool[atlasWidth, atlasHeight];

        // Sort textures by size (largest to smallest)
        textures.Sort((a, b) => (b.width * b.height).CompareTo(a.width * a.height));

        foreach (Texture2D texture in textures)
        {
            if (texture == null) continue;

            bool packed = false;
            Texture2D processedTexture = texture;
            if (outlineThickness > 0)
            {
                processedTexture = ApplyOutline(texture, outlineThickness, outlineColor);
            }

            int paddedWidth = processedTexture.width + padding * 2;
            int paddedHeight = processedTexture.height + padding * 2;

            for (int y = 0; y <= atlasHeight - paddedHeight; y++)
            {
                for (int x = 0; x <= atlasWidth - paddedWidth; x++)
                {
                    if (CanPlaceTexture(x, y, paddedWidth, paddedHeight, occupied))
                    {
                        PlaceTexture(x, y, paddedWidth, paddedHeight, processedTexture, atlas, occupied);
                        packed = true;
                        break;
                    }
                }

                if (packed)
                {
                    break;
                }
            }
            // Check if texture is not packed and exceeds the max texture size
            if (!packed)
            {
                DebugEditor($"Texture {texture.name} could not be packed within the atlas size {atlasWidth}x{atlasHeight}.");
                break;
            }
        }

        atlas.Apply();
        return atlas;
    }

    // Helpers ---

    /// <summary>
    /// Checks for space to pack the texture.
    /// </summary>
    private bool CanPlaceTexture(int x, int y, int width, int height, bool[,] occupied)
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (occupied[x + i, y + j])
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Places the Texture where it fit if used Tight Packing.
    /// </summary>
    private void PlaceTexture(int x, int y, int width, int height, Texture2D texture, Texture2D atlas, bool[,] occupied)
    {
        Color[] clearPixels = new Color[width * height];
        for (int i = 0; i < clearPixels.Length; i++)
        {
            clearPixels[i] = new Color(0, 0, 0, 0); // Transparent color
        }

        Texture2D paddedTexture = new Texture2D(width, height);
        paddedTexture.SetPixels(clearPixels);

        if (padding >= 0)
        {
            paddedTexture.SetPixels(padding, padding, texture.width, texture.height, texture.GetPixels());
        }
        else
        {
            int cropX = Mathf.Abs(padding);
            int cropY = Mathf.Abs(padding);
            int cropWidth = Mathf.Max(texture.width - 2 * cropX, 0);
            int cropHeight = Mathf.Max(texture.height - 2 * cropY, 0);

            if (cropWidth > 0 && cropHeight > 0)
            {
                paddedTexture.SetPixels(0, 0, cropWidth, cropHeight, texture.GetPixels(cropX, cropY, cropWidth, cropHeight));
            }
        }

        paddedTexture.Apply();

        atlas.SetPixels(x, y, width, height, paddedTexture.GetPixels());

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                occupied[x + i, y + j] = true;
            }
        }
    }

}

public partial class TextureAtlasEditor // Outline      
{

    /// <summary>
    /// Applies the Outline.
    /// </summary>
    private Texture2D ApplyOutline(Texture2D original, int thickness, Color outlineColor)
    {
        int width = original.width;
        int height = original.height;
        Texture2D outlinedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Color[] originalPixels = original.GetPixels();
        Color[] newPixels = new Color[width * height];
        Color[] outlinePixels = new Color[width * height];

        // Copy the original pixels to newPixels
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                newPixels[y * width + x] = originalPixels[y * width + x];
            }
        }

        // Apply the outline
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (originalPixels[y * width + x].a > 0)
                {
                    for (int oy = -thickness; oy <= thickness; oy++)
                    {
                        for (int ox = -thickness; ox <= thickness; ox++)
                        {
                            int nx = x + ox;
                            int ny = y + oy;
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                if (ox != 0 || oy != 0) // Skip the center pixel
                                {
                                    if (outlinePixels[ny * width + nx].a == 0)
                                    {
                                        outlinePixels[ny * width + nx] = outlineColor;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        if (outlineMode == OutlineMode.Gaussian)
        {
            // Apply Gaussian blur to the outline
            outlinePixels = ApplyGaussianBlur(outlinePixels, width, height, 3);
        }

        // Combine the original and blurred outline
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (newPixels[y * width + x].a == 0 && outlinePixels[y * width + x].a > 0)
                {
                    newPixels[y * width + x] = outlinePixels[y * width + x];
                }
            }
        }

        outlinedTexture.SetPixels(newPixels);
        outlinedTexture.Apply();

        return outlinedTexture;
    }

    /// <summary>
    /// Blurs the Outline.
    /// </summary>
    private Color[] ApplyGaussianBlur(Color[] colors, int width, int height, int iterations)
    {
        Color[] result = new Color[colors.Length];
        System.Array.Copy(colors, result, colors.Length);

        for (int i = 0; i < iterations; i++)
        {
            result = Blur(result, width, height);
        }

        return result;
    }

    /// <summary>
    /// Creates a Blur Effect.
    /// </summary>
    private Color[] Blur(Color[] colors, int width, int height)
    {
        Color[] blurred = new Color[colors.Length];
        float[,] kernel = {
        { 1 / 16f, 2 / 16f, 1 / 16f },
        { 2 / 16f, 4 / 16f, 2 / 16f },
        { 1 / 16f, 2 / 16f, 1 / 16f }
    };

        int kernelSize = 3;
        int kernelRadius = kernelSize / 2;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color sum = Color.clear;
                for (int ky = -kernelRadius; ky <= kernelRadius; ky++)
                {
                    for (int kx = -kernelRadius; kx <= kernelRadius; kx++)
                    {
                        int pixelX = Mathf.Clamp(x + kx, 0, width - 1);
                        int pixelY = Mathf.Clamp(y + ky, 0, height - 1);
                        Color pixelColor = colors[pixelY * width + pixelX];
                        sum += pixelColor * kernel[ky + kernelRadius, kx + kernelRadius];
                    }
                }
                blurred[y * width + x] = sum;
            }
        }

        return blurred;
    }


}

public partial class TextureAtlasEditor // Events       
{
    /// <summary>
    /// Handles Mouse Events such as Panning.
    /// </summary>
    private void HandleEvents()
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && (e.button == 0 || e.button == 2))
        {
            isPanning = true;
            lastMousePos = e.mousePosition;
            e.Use();
        }
        else if (e.type == EventType.MouseUp && (e.button == 0 || e.button == 2))
        {
            isPanning = false;
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && (e.button == 0 || e.button == 2) && isPanning)
        {
            Vector2 delta = e.mousePosition - lastMousePos;
            previewScrollPos -= delta;
            lastMousePos = e.mousePosition;
            e.Use();
        }

        // Zoom was here originally but didn't feel as good as I'd hope TBD.
    }

    /// <summary>
    /// Handles Deselection in the Reordable list.
    /// </summary>
    private void HandleDeselection(Rect listArea)
    {
        Event e = Event.current;

        // Handle Deselection.
        if (e.type == EventType.MouseDown)
        {
            if (!listArea.Contains(e.mousePosition))
            {
                // Clear the selection
                textureList.index = -1;

                // Use the current event (to prevent other GUI elements from using it)
                Event.current.Use();

                // Optionally, repaint the editor window to immediately reflect the deselection
                Repaint();
            }
        }
    }

    /// <summary>
    /// Handles Drag and Drop into the Preview Area.
    /// </summary>
    /// <param name="dropArea"></param>
    private void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;

        if (textures.Count == 0 || textureList.count == 0)
        {
            GUI.Box(dropArea, "Drag and Drop Textures Here", CENTERED_MINI_LABEL_LARGE);
        }

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    if (HandleDroppedAssets())
                    {
                        UpdatePreview();
                    }
                }

                Event.current.Use();
                break;
        }
    }
}

public partial class TextureAtlasEditor // Resize       
{
    // Resize ---
    /// <summary>
    /// Applies and Updates the Preview with the resizes atlas. <br></br>
    /// (Automated in UpdatePreview anyway but will be optimized in future).
    /// </summary>
    private void ApplyResize()
    {
        if (resizePercentage == 100)
        {
            // Reset resized atlas so we don't preview it anymore.
            resizedAtlas = null;

            // Update the Resolution.
            calculatedHeight = atlas.height;
            calculatedWidth = atlas.width;
        }

        else
        {
            int newWidth = calculatedWidth * resizePercentage / 100;
            int newHeight = calculatedHeight * resizePercentage / 100;

            // Create resized version.
            resizedAtlas = ResizeTexture(atlas, newWidth, newHeight);

            // Update the Resolution.
            calculatedHeight = resizedAtlas.height;
            calculatedWidth = resizedAtlas.width;
        }
    }

    /// <summary>
    /// Seamless way to check for scaling.
    /// </summary>
    private Texture2D GetTexture(Texture2D texture, int percentage)
    {
        if (percentage == 100)
        {
            return texture;
        }

        int newWidth = texture.width * percentage / 100;
        int newHeight = texture.height * percentage / 100;

        return ResizeTexture(texture, newWidth, newHeight);
    }

    /// <summary>
    /// Resizes the texture if scale is anything other than 100%.
    /// </summary>
    private Texture2D ResizeTexture(Texture2D texture, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        Graphics.Blit(texture, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D resizedTexture = new Texture2D(newWidth, newHeight);
        resizedTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        resizedTexture.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return resizedTexture;
    }

    /// <summary>
    /// Updates resize more frequently if we forgot to apply it.
    /// </summary>
    private void CheckResize()
    {
        // only if changed:
        if (resizePercentage == 100)
        {
            resizedAtlas = null;

            calculatedHeight = atlas.height;
            calculatedWidth = atlas.width;
        }
        else
        {
            ApplyResize();
        }
    }
}

public partial class TextureAtlasEditor // Helpers      
{
    // Textures ---
    /// <summary>
    /// Creates the textures used to flatten buttons style.
    /// </summary>
    private void CreateActiveTextures()
    {
        // Ensuring that the 'textures' list is initialized.
        if (textures == null)
        {
            textures = new List<Texture2D>();
        }

        if (!activeBackground)
        {
            activeBackground = MakeColoredTexture(1, 1, new Color(0.24f, 0.49f, 0.91f)); // Blue color
        }
        if (!normalBackground)
        {
            normalBackground = MakeColoredTexture(1, 1, new Color(0.75f, 0.75f, 0.75f, 0.2f)); // Grey color
        }

        if (clearTexture == null)
        {
            clearTexture = MakeColoredTexture(1, 1, new Color(0.75f, 0.75f, 0.75f, .1f));
        }

        if (greyTexture == null)
        {
            greyTexture = MakeColoredTexture(1, 1, new Color(0.75f, 0.75f, 0.75f, 1f));
        }
        if (lightGreyTexture == null)
        {
            lightGreyTexture = MakeColoredTexture(1, 1, new Color(0.85f, 0.85f, 0.85f, 1f));
        }
        if (darkGreyTexture == null)
        {
            darkGreyTexture = MakeColoredTexture(1, 1, new Color(0.65f, 0.65f, 0.65f, 1f));
        }
        if (checkerboardTexture == null)
        {
            checkerboardTexture = (Texture2D)EditorGUIUtility.IconContent("textureCheckerDark").image;
        }
    }

    /// <summary>
    /// Helper to create a texture from color.
    /// </summary>
    private Texture2D MakeColoredTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    /// <summary>
    /// Method to get a readable texture.
    /// </summary>
    private Texture2D GetReadableTexture(Texture2D texture)
    {
        // Check if the texture format is supported
        if (texture.format == TextureFormat.ARGB32 || texture.format == TextureFormat.RGBA32 ||
            texture.format == TextureFormat.RGB24 || texture.format == TextureFormat.Alpha8)
        {
            return texture;
        }

        // If not, create a new texture with the same dimensions and copy the pixels
        Texture2D readableTexture = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);
        RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height);
        Graphics.Blit(texture, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        readableTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        readableTexture.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return readableTexture;
    }

    // Misc ---

    /// <summary>
    /// Normalizes path for the Texture Importer.
    /// </summary>
    private string NormalizePath(string path)
    {
        return path.Replace("\\", "/");
    }

    /// <summary>
    /// Gets the path this script it located in case we want to store something in the future relative to this path.
    /// </summary>
    private void GetEditorPath()
    {
        scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
        directoryPath = Path.GetDirectoryName(scriptPath);
        combinedPath = Path.Combine(directoryPath);
    }

    /// <summary>
    /// Creates some useful textures for buttons styles.
    /// </summary>
    private void SetupTextureList()
    {
        textureList = new ReorderableList(textures, typeof(Texture2D), true, false, true, true)
        {
            displayAdd = true,
            displayRemove = true,
            multiSelect = true,

            drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Textures");
            },

            drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (index >= textures.Count) return;

                // Adjust the rect for the ObjectField and allowing selection background to be visible around edges.
                float fieldHeight = EditorGUIUtility.singleLineHeight;
                float verticalPadding = (rect.height - fieldHeight) / 2;
                Rect fieldRect = new Rect(rect.x + 15, rect.y + verticalPadding, rect.width - 45, fieldHeight); // Adjusted x to account for drag handler

                // Draw the ObjectField
                textures[index] = (Texture2D)EditorGUI.ObjectField(fieldRect, textures[index], typeof(Texture2D), false);

                // Additional remove button.
                if (GUI.Button(new Rect(rect.x + rect.width - 25, rect.y + verticalPadding, 23, fieldHeight), new GUIContent(EditorGUIUtility.IconContent("winbtn_win_close_h"))))
                {
                    textures.RemoveAt(index);
                    UpdatePreview();
                    GUIUtility.ExitGUI();
                }
            },

            onAddCallback = (ReorderableList list) =>
            {
                textures.Add(null);
            },

            onRemoveCallback = (ReorderableList list) =>
            {
                if (EditorUtility.DisplayDialog("Warning", "Are you sure you want to delete the texture?", "Yes", "No"))
                {
                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                    UpdatePreview();
                    GUIUtility.ExitGUI();
                }
            },

            drawElementBackgroundCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                Event e = Event.current;

                if (e.type == EventType.Repaint)
                {
                    if (isActive || isFocused)
                    {
                        EditorGUI.DrawRect(rect, new Color(0.24f, 0.49f, 0.91f, 1f)); // A bright blue, for example
                    }
                }

                // Handle drag-and-drop
                if (rect.Contains(e.mousePosition))
                {
                    if (e.type == EventType.DragUpdated)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        e.Use();
                    }

                    if (e.type == EventType.DragPerform)
                    {
                        if (HandleDroppedAssets())
                        {
                            UpdatePreview();
                            GUIUtility.ExitGUI();
                        }

                        e.Use();
                    }
                }
            },

        };
    }

    // Calculations ---

    /// <summary>
    /// Calculates the minimum COLUMNS possible to not go over 8192x8192.
    /// </summary>
    private int CalculateMinimumColumns()
    {
        int minColumns = 1;
        int currentWidth = 0;

        foreach (Texture2D texture in textures)
        {
            if (texture == null) continue;

            int paddedWidth = texture.width + padding * 2;

            if (currentWidth + paddedWidth > MAX_SIZE_TEXTURE)
            {
                minColumns++;
                currentWidth = 0;
            }
            currentWidth += paddedWidth;
        }

        return minColumns;
    }

    /// <summary>
    /// Calculates the minimum ROWS possible to not go over 8192x8192.
    /// </summary>
    private int CalculateMinimumRows()
    {
        int minRows = 1;
        int currentHeight = 0;

        foreach (Texture2D texture in textures)
        {
            if (texture == null) continue;

            int paddedHeight = texture.height + padding * 2;

            if (currentHeight + paddedHeight > MAX_SIZE_TEXTURE)
            {
                minRows++;
                currentHeight = 0;
            }
            currentHeight += paddedHeight;
        }

        return minRows;
    }

    /// <summary>
    /// Calculates the maximum Extend (Padding) we can have on the images before running out or pixels.
    /// </summary>
    private int CalculateMaxPadding()
    {
        if (textureList.count <= 0) return 0;

        int totalWidth = 0;
        int totalHeight = 0;
        int currentX = 0;
        int currentY = 0;
        int rowHeight = 0;
        int columnCounter = 0;

        // Calculate current dimensions without padding
        foreach (Texture2D texture in textures)
        {
            if (texture == null) continue;

            int textureWidth = texture.width;
            int textureHeight = texture.height;

            if (measurementMode == MeasurementMode.Columns && columnCounter >= columns)
            {
                columnCounter = 0;
                currentX = 0;
                currentY += rowHeight;
                rowHeight = 0;
            }
            else if (measurementMode == MeasurementMode.Rows && columnCounter >= Mathf.CeilToInt((float)textures.Count / rows))
            {
                columnCounter = 0;
                currentX = 0;
                currentY += rowHeight;
                rowHeight = 0;
            }

            currentX += textureWidth;
            rowHeight = Mathf.Max(rowHeight, textureHeight);
            columnCounter++;

            totalWidth = Mathf.Max(totalWidth, currentX);
            totalHeight = currentY + rowHeight;
        }

        // Calculate remaining space
        int remainingWidth = MAX_SIZE_TEXTURE - totalWidth;
        int remainingHeight = MAX_SIZE_TEXTURE - totalHeight;

        // Calculate the maximum padding that can be added without exceeding the max texture size
        int maxPaddingX = remainingWidth / (2 * columns);
        int maxPaddingY = remainingHeight / (2 * Mathf.CeilToInt((float)textures.Count / columns));

        // The maximum padding is the smallest of the two
        int maxPadding = Mathf.Min(maxPaddingX, maxPaddingY);

        // Ensure maxPadding is non-negative
        maxPadding = Mathf.Max(0, maxPadding);

        return maxPadding;
    }

    /// <summary>
    /// Calculates the maximum Crop value we can have on the images before having to few pixels.
    /// </summary>
    private int CalculateMaxCropping()
    {
        if (textures == null || textures.Count == 0) return 0;

        // Find the smallest dimension to determine the max cropping
        int maxCropping = int.MaxValue;

        foreach (Texture2D texture in textures)
        {
            if (texture == null) continue;
            if (textureList.count <= 0) continue;

            maxCropping = Mathf.Min(maxCropping, texture.width / 2 - 4, texture.height / 2 - 4);
        }

        return maxCropping - 1;
    }

    // Menu Toggles ---

    /// <summary>
    /// Toggles help boxes ON or OFF.
    /// </summary>
    private void ToggleHelpOption()
    {
        SHOW_HELP = !SHOW_HELP;
    }

    // Editor Layout Helpers ---

    /// <summary>
    /// Helper to keep centering sane (Beginning).
    /// </summary>
    private void BeginVerticalCenter()
    {
        // Horizontal to center.
        EditorGUILayout.BeginHorizontal();
        // into center with flex space.
        GUILayout.FlexibleSpace();
    }
    /// <summary>
    /// Helper to keep centering sane (End).
    /// </summary>
    private void EndVerticalCenter()
    {
        GUILayout.FlexibleSpace();

        EditorGUILayout.EndHorizontal();
    }

    // GUIContent Helpers ---

    /// <summary>
    /// Drawn Icon & Label and tooltip.
    /// </summary>
    private void IconButton(GUIContent content, string label)
    {
        var style = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleRight,
        };

        GUILayout.BeginHorizontal(style, GUILayout.ExpandWidth(true));
        GUILayout.Label(content);
        GUILayout.Label(label);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Drawn Icon & Label and tooltip.
    /// </summary>
    private bool ToolbarIconButton(GUIContent content, string label, string tooltip)
    {
        // Combine the icon and text into a single GUIContent
        GUIContent buttonContent = new GUIContent(label, content.image, tooltip);

        // Create a GUIStyle that aligns the text to the right of the icon
        GUIStyle buttonStyle = new GUIStyle(EditorStyles.toolbarButton)
        {
            alignment = TextAnchor.MiddleLeft,
            // Adjust padding and other style properties as needed
        };

        // Create the button with the combined content and custom style
        if (GUILayout.Button(buttonContent, buttonStyle))
        {
            // Button action goes here
            return true;
        }

        return false;
    }

    /// <summary>
    /// Drawn Icon & Label and tooltip.
    /// </summary>
    private void IconButton(GUIContent content, string label, string tooltip)
    {
        var style = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleRight,
        };

        content.tooltip = tooltip;

        GUILayout.BeginHorizontal(style, GUILayout.ExpandWidth(true));
        GUILayout.Label(content);
        GUILayout.Label(label);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Draws Icon Label (from string) and tooltip (optional)
    /// </summary>
    private void IconButton(string iconName, string label, string tooltip = "")
    {
        var style = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleRight,
        };
        GUIContent content = new GUIContent(EditorGUIUtility.IconContent($"{iconName}").image, tooltip);

        GUILayout.BeginHorizontal(style, GUILayout.ExpandWidth(true));
        GUILayout.Label(content);
        GUILayout.Label(label);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Drawn Icon & Label and tooltip with a int field.
    /// </summary>
    private void IconButtonWithField(GUIContent content, string label, ref int value, int min, int max)
    {
        GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

        IconButton(content, label);
        GUILayout.FlexibleSpace();
        value = EditorGUILayout.IntField(Mathf.Clamp(value, min, max));

        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Drawn Icon & Label and an int slider.
    /// </summary>
    private void IconButtonWithSlider(GUIContent content, string label, ref int value, int min, int max)
    {
        GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

        IconButton(content, label);
        GUILayout.FlexibleSpace();
        value = EditorGUILayout.IntSlider(padding, min, max);

        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Drawn Icon & Label and an int slider.
    /// </summary>
    private void IconButtonWithSlider(string icon, string label, ref int value, int min, int max, string tooltip = "")
    {
        GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
        GUIContent content = new GUIContent(EditorGUIUtility.IconContent($"{icon}").image, tooltip);
        IconButton(content, label);
        GUILayout.FlexibleSpace();
        value = EditorGUILayout.IntSlider(value, min, max);

        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Needs to be called OnGUI because of the way Unity uses a “singleton” like approach to manage the different editor styles.
    /// <br></br> Otherwise we will get an Null Error everytime we call OnEnable.
    /// </summary>
    private void HandleStyles()
    {
        CreateFoldoutStyle();
        CreateLeftMenuStyle();
        CreateLargeMiniLabel();
        CreateRightToolbarStyle();
    }
}

public partial class TextureAtlasEditor // GUI Styles   
{
    /// <summary>
    /// Caches Icons for this Editor.
    /// </summary>
    private void CacheIcons()
    {
        // this is a mess right now, should probably be initiated OnEnable in the future.
        if (SCALE_ICON == null)
            SCALE_ICON = new GUIContent(EditorGUIUtility.IconContent("d_ScaleTool").image);

        if (SAVE_ICON == null)
            SAVE_ICON = new GUIContent(EditorGUIUtility.IconContent("SaveFromPlay").image);

        if (PLUS_ICON == null)
            PLUS_ICON = new GUIContent(EditorGUIUtility.IconContent("d_Toolbar Plus").image);

        if (ZOOM_IN_ICON == null)
            ZOOM_IN_ICON = new GUIContent(EditorGUIUtility.IconContent("d_ViewToolZoom").image);

        if (PADDING_ICON == null)
            PADDING_ICON = new GUIContent(EditorGUIUtility.IconContent("d_MoveTool@2x").image, $"Extend: Increases the textures size by adding space around the edges.");

        if (CROP_ICON == null)
            CROP_ICON = new GUIContent(EditorGUIUtility.IconContent("d_OrientationGizmo@2x").image, $"Crop: Reduce the image to a selected area, removing outer parts.");

        if (MODE_ICON == null)
            MODE_ICON = new GUIContent(EditorGUIUtility.IconContent("FreeformLayoutGroup Icon").image, $"Tight: Won't respect limits and will try to pack the textures tightly. Grid: Respects the Columns and Rows completly.");

        if (OUTLINE_TYPE_ICON == null)
            OUTLINE_TYPE_ICON = new GUIContent(EditorGUIUtility.IconContent("Grid.PaintTool").image, $"If the outline is to blurry try the Pixel setting.");

        if (GIZMO_ICON == null)
            GIZMO_ICON = new GUIContent(EditorGUIUtility.IconContent("d_GizmosToggle").image, $"Bounds Grid:\n- Draws a border around the individual textures bounds.");

        if (CHECKERS_ICON == null)
            CHECKERS_ICON = new GUIContent(EditorGUIUtility.IconContent("CheckerFloor").image, $"Checkers Background:\n- Draws a checkers pattern behind the textures.");

    }

    private void CreateLargeMiniLabel()
    {
        if (CENTERED_MINI_LABEL_LARGE == null)
            CENTERED_MINI_LABEL_LARGE = new GUIStyle(EditorStyles.whiteMiniLabel)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter,/**/
            };
    }

    /// <summary>
    /// Minimal foldout style.
    /// </summary>
    private void CreateFoldoutStyle()
    {
        if (FOLDOUT_STYLE == null)
        {
            FOLDOUT_STYLE = new GUIStyle(EditorStyles.foldout);
        }

        var temp = new GUIStyle(EditorStyles.miniLabel);

        // Has to set outside constructor.
        FOLDOUT_STYLE.fontStyle = temp.fontStyle;
        FOLDOUT_STYLE.fontSize = temp.fontSize;
        FOLDOUT_STYLE.normal.textColor = temp.normal.textColor;
        FOLDOUT_STYLE.onNormal.textColor = temp.onNormal.textColor;
        FOLDOUT_STYLE.active.textColor = temp.active.textColor;
        FOLDOUT_STYLE.onActive.textColor = temp.onActive.textColor;
        FOLDOUT_STYLE.focused.textColor = temp.focused.textColor;
        FOLDOUT_STYLE.onFocused.textColor = temp.onFocused.textColor;
    }

    /// <summary>
    /// Creates the Helpbox Style to elevate the left menu.
    /// </summary>
    private void CreateLeftMenuStyle()
    {
        if (LEFT_HELPBOX_STYLE == null)
        {
            // Adjustments for using helpBox style.
            RectOffset borders = EditorStyles.helpBox.border;
            borders.top = 0;
            borders.left = 0;
            borders.bottom = 0;
            // default right.

            RectOffset margins = EditorStyles.helpBox.margin;
            margins.left = 0;
            margins.top = 0;
            margins.bottom = 0;
            // default right.

            LEFT_HELPBOX_STYLE = new GUIStyle(EditorStyles.helpBox)
            {
                border = borders,
                margin = margins,
            };
        }

    }

    /// <summary>
    /// Creates the toolbar button style for Right-Toolbar.
    /// </summary>
    private void CreateRightToolbarStyle()
    {
        if (RIGHT_TOOLBAR_STYLE == null)
            // Create a custom style based on the toolbar style
            RIGHT_TOOLBAR_STYLE = new GUIStyle(EditorStyles.toolbar)
            {
                fixedHeight = 0, // Allows the height to stretch
                stretchHeight = true,
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(1, 1, 1, 1)
            };
    }

    /// <summary>
    /// Just a generic dynamic button style that is more flat (TEMP).
    /// </summary>
    private GUIStyle GetGenericButtonStyle()
    {
        if (GENERIC_BUTTON_STYLE == null)
        {
            GUIStyle customButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 32,
                fixedWidth = 32,
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(2, 2, 1, 1),
                margin = new RectOffset(2, 2, 0, 0),
                padding = new RectOffset(2, 2, 0, 0)
            };

            customButtonStyle.onNormal.background = normalBackground;
            customButtonStyle.onNormal.textColor = Color.white;
            customButtonStyle.onHover.background = normalBackground;
            customButtonStyle.onHover.textColor = Color.white;
            customButtonStyle.onActive.background = normalBackground;
            customButtonStyle.onActive.textColor = Color.white;
            customButtonStyle.onFocused.background = normalBackground;
            customButtonStyle.onFocused.textColor = Color.white;

            GENERIC_BUTTON_STYLE = customButtonStyle;

            GENERIC_BUTTON_STYLE.fixedHeight = 32;
            GENERIC_BUTTON_STYLE.fixedWidth = 32;
        }

        return GENERIC_BUTTON_STYLE;
    }

    private GUIStyle GetGenericButtonStyleSmall()
    {
        if (GENERIC_BUTTON_STYLE_SMALL == null)
        {
            GUIStyle customButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 22,
                fixedWidth = 22,
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(2, 2, 1, 1),
                margin = new RectOffset(2, 2, 0, 0),
                padding = new RectOffset(2, 2, 0, 0)
            };

            customButtonStyle.onNormal.background = normalBackground;
            customButtonStyle.onNormal.textColor = Color.white;
            customButtonStyle.onHover.background = normalBackground;
            customButtonStyle.onHover.textColor = Color.white;
            customButtonStyle.onActive.background = normalBackground;
            customButtonStyle.onActive.textColor = Color.white;
            customButtonStyle.onFocused.background = normalBackground;
            customButtonStyle.onFocused.textColor = Color.white;

            GENERIC_BUTTON_STYLE_SMALL = customButtonStyle;

            GENERIC_BUTTON_STYLE_SMALL.fixedHeight = 24;
            GENERIC_BUTTON_STYLE_SMALL.fixedWidth = 24;
        }

        return GENERIC_BUTTON_STYLE_SMALL;
    }

    /// <summary>
    /// Creates Menu Buttons, useful for Selection Grids (Dynamic).
    /// </summary>
    private GUIContent[] CreateSlectionGridMenuButtons(string[] icons, string[] tooltips = null)
    {
        GUIContent[] contents = new GUIContent[icons.Length];
        for (int i = 0; i < icons.Length; i++)
        {
            // Icon with tooltip.
            contents[i] = tooltips != null ? new GUIContent(EditorGUIUtility.IconContent(icons[i]).image, tooltips[i]) :
                                             new GUIContent(EditorGUIUtility.IconContent(icons[i]).image);

            // Check if valid icon.
            if (contents[i].image == null)
            {
                contents[i] = new GUIContent(icons[i], tooltips[i]);
            }
        }

        return contents;
    }
}

public partial class TextureAtlasEditor // Draw Methods 
{
    // Top Bar ---
    #region Top
    /// <summary>
    /// Draws the top toolbar (WIP).
    /// </summary>
    private void DrawToolbar()
    {
        // Start Toolbar
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        // Left Side ---


        if (ToolbarIconButton(EditorGUIUtility.IconContent("UnityEditor.ConsoleWindow"), "File", "File Menu."))
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("New"), false, ResetAtlas);

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Import (Folder)"), false, ImportImagesFromFolder);


            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Export (Atlas)"), false, SaveAtlas);
            menu.AddItem(new GUIContent("Export (Seperate)"), false, SaveTexturesIndividually);

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Close"), false, Close);

            menu.ShowAsContext();
            menu.DropDown(new Rect(EditorStyles.toolbarButton.fixedWidth, 0, EditorStyles.toolbarButton.fixedWidth, EditorStyles.toolbarButton.fixedHeight));
        }

        GUILayout.FlexibleSpace();

        // Right Side ---        
        if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_Project").image, "Import Folder.\n- Select one image to import the folder."), EditorStyles.toolbarButton)) // Load textures from folder
        {
            ImportImagesFromFolder();
        }

        if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("SaveAs").image, "Save Atlas.\n- Saves the textures as an Atlas.\n- Use (file) menu for individual export."), EditorStyles.toolbarButton)) // Save.
        {
            SaveAtlas();
        }

        if (GUILayout.Button(new GUIContent (EditorGUIUtility.IconContent("d_Settings").image, "Settings.\n- Toggle Help On and Off.\n- (WIP) Presets settings."), EditorStyles.toolbarButton)) // Settings.
        {
            GenericMenu menu = new GenericMenu();
            menu.AddDisabledItem(new GUIContent("Hotkeys"));
            menu.AddDisabledItem(new GUIContent("Settings"));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Show Help"), SHOW_HELP, ToggleHelpOption);

            float size = EditorStyles.toolbarButton.fixedWidth;
            float menuXPosition = position.width - DEFAULT_TOOLBARWIDTH;

            menu.DropDown(new Rect(menuXPosition, 2.5f, size, EditorStyles.toolbarButton.fixedHeight));
        }

        GUILayout.EndHorizontal();

        // End Top toolbar
        GUILayout.EndVertical();
    }

    #endregion

    // Left Side ---
    #region Left Side

    /// <summary>
    /// Draws the left sidebar.
    /// </summary>
    private void DrawLeftSidebar()
    {
        GUILayout.BeginVertical(LEFT_HELPBOX_STYLE, GUILayout.Width(32 + 4), GUILayout.ExpandHeight(true));

        // TODO: Make this better.
        LeftMenu();

        // End Left Sidebar
        GUILayout.EndVertical();
    }

    /// <summary>
    /// Draws the Left side menu content (Zoom for now).
    /// </summary>
    private void LeftMenu()
    {
        // Horizontal to center.
        EditorGUILayout.BeginHorizontal();

        // into center with flex space.
        GUILayout.FlexibleSpace();

        // Contents in here.
        EditorGUILayout.BeginVertical();

        // TODO: Actually make this useful. (Disabled for now).
        #region Hide TBD Buttons

        SHOW_GIZMOS = GUILayout.Toggle(SHOW_GIZMOS, GIZMO_ICON, GetGenericButtonStyle());
        SHOW_CHECKER = GUILayout.Toggle(SHOW_CHECKER, CHECKERS_ICON, GetGenericButtonStyle());

        GUI.enabled = false;

        SHOW_GIZMOS = GUILayout.Toggle(SHOW_GIZMOS, PLUS_ICON, GetGenericButtonStyle());


        if (GUILayout.Toggle(showSaveDialog, SAVE_ICON, GetGenericButtonStyle()))
        {
            TBDOptions();
        }

        if (GUILayout.Toggle(showResizeDialog, SCALE_ICON, GetGenericButtonStyle()))
        {
            TBDOptions();
        }

        GUI.enabled = true;
        #endregion

        // Move to the bottom for zoom.
        GUILayout.FlexibleSpace();

        // Zoom Layout.
        #region Zoom

        // Thanks Unity for centering things is so easy..
        BeginVerticalCenter();

        // Draw zoom icon.
        GUILayout.Label(ZOOM_IN_ICON);
        // Thanks again Unity...
        EndVerticalCenter();
        GUILayout.Space(2);

        BeginVerticalCenter();
        // Because vertical sliders don't center due to pivot..
        bool isSmallPreview = resizePercentage <= 15;

        zoomScale = GUILayout.VerticalScrollbar(zoomScale, .1f, isSmallPreview ? zoomMax * 5 : zoomMax, isSmallPreview ? 1 : zoomMin, GUILayout.Height(150)); // vertical scrollbar for zoom..
        EndVerticalCenter();

        // Move it from the bottom.
        EditorGUILayout.Space(15);

        #endregion

        // Contents end here.
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

    }

    #endregion

    // Right Side ---
    #region Right Side

    private Vector2 settingsScrollPos;

    private void DrawRightSidebar(Rect area)
    {
        // Start right group
        GUILayout.BeginVertical(EditorStyles.inspectorFullWidthMargins); // Start right group

        // Scroll for all the content but 'Generate' button.
        settingsScrollPos = GUILayout.BeginScrollView(settingsScrollPos, false, false, GUILayout.Width(area.width), GUILayout.ExpandHeight(true));

        // Settings area
        DrawSettingsSection(area);

        GUILayout.EndScrollView();
        EditorGUILayout.Separator();

        GUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
        if (GUILayout.Button("Generate"))
        {
            UpdateAtlas();
            DebugEditor("Texture atlas generated successfully.");
        }
        GUILayout.EndVertical();

        // End right group
        GUILayout.EndVertical();
    }

    private void DrawSettingsSection(Rect area)
    {
        // InspectorFullWidthMargins & Helpbox styled vertical area with scroll view
        GUILayout.BeginVertical(EditorStyles.inspectorFullWidthMargins, GUILayout.ExpandHeight(true));

        GUILayout.BeginVertical(EditorStyles.helpBox);

        #region Label
        GUILayout.BeginHorizontal(EditorStyles.centeredGreyMiniLabel);
        GUILayout.Label("Texture(s) Settings.", EditorStyles.centeredGreyMiniLabel);
        GUILayout.Space(5);
        GUILayout.EndHorizontal();
        #endregion

        #region Fit & Alignment

        FOLDOUT_TEXTURE_ALIGNMENT = EditorGUILayout.Foldout(FOLDOUT_TEXTURE_ALIGNMENT, "Fit & Alignment", FOLDOUT_STYLE);

        if (FOLDOUT_TEXTURE_ALIGNMENT)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            DrawAlignmentSection();
            GUILayout.EndVertical();
        }
        #endregion

        #region Extend & Crop
        FOLDOUT_SCALE_CROP = EditorGUILayout.Foldout(FOLDOUT_SCALE_CROP, "Extend & Crop", FOLDOUT_STYLE);

        if (FOLDOUT_SCALE_CROP)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            DrawPaddingSection();
            GUILayout.EndVertical();
        }
        #endregion

        #region Outline
        FOLDOUT_OUTLINE = EditorGUILayout.Foldout(FOLDOUT_OUTLINE, "Outline", FOLDOUT_STYLE);

        if (FOLDOUT_OUTLINE)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            DrawOutlineSection();
            GUILayout.EndVertical();
        }
        #endregion

        #region Size
        FOLDOUT_SIZE = EditorGUILayout.Foldout(FOLDOUT_SIZE, "Adjust Size", FOLDOUT_STYLE);
        if (FOLDOUT_SIZE)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            DrawResizeSection();
            GUILayout.EndVertical();
        }
        #endregion

        GUILayout.EndVertical();

        // Push Texture and Atlas down.
        GUILayout.FlexibleSpace();

        #region Textures to Atlas & Generate

        GUILayout.BeginVertical(EditorStyles.helpBox);

        GUILayout.Space(5);

        #region Label
        GUILayout.BeginHorizontal(EditorStyles.centeredGreyMiniLabel);
        //GUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
        GUILayout.Label("Atlas Texture(s)", EditorStyles.centeredGreyMiniLabel);
        GUILayout.Space(5);
        //GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        #endregion

        listScrollPos = GUILayout.BeginScrollView(listScrollPos, EditorStyles.inspectorFullWidthMargins, GUILayout.MinHeight(area.height / 4));

        textureList.DoLayoutList(); // TODO: ADD FIXED BUTTON TO CLEAR SELECTION AND REMOVE '-' button from reordable list.
        GUILayout.EndScrollView();

        // Handle deselection.
        HandleDeselection(GUILayoutUtility.GetLastRect());

        GUILayout.EndVertical();
        #endregion

        GUILayout.EndVertical();
    }

    #region Right Section Specifics

    /// <summary>
    /// Draws the Resize (%) Section.
    /// </summary>
    private void DrawResizeSection()
    {
        var style = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleRight,
        };

        GUILayout.BeginVertical(style, GUILayout.ExpandWidth(true)); // Start horizontal for padding

        // Create a horizontal group for the resize slider
        GUILayout.BeginHorizontal();

        // Display the resize slider
        IconButtonWithSlider("ScaleTool On", "Resize(%):", ref resizePercentage, 1, 100, $"Resizes The Final Exported Asset by ({resizePercentage}%).\nOriginal Assets/Atlas are preserved.");

        if (GUILayout.Button("Apply"))
        {
            UpdatePreview();
        }

        GUILayout.EndHorizontal();
        if (SHOW_HELP)
        {
            GUILayout.Space(2);
            EditorGUILayout.HelpBox($"The new estimated size will be: {(calculatedWidth / 100) * resizePercentage}x{(calculatedHeight / 100) * resizePercentage}.", MessageType.None);
        }

        GUILayout.EndVertical();
    }

    /// <summary>
    /// Draws the Extend / Crop Section.
    /// </summary>
    private void DrawPaddingSection()
    {
        var style = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleRight,
        };

        #region padding
        maxPadding = CalculateMaxPadding();
        maxCropping = CalculateMaxCropping();

        GUILayout.BeginVertical(style, GUILayout.ExpandWidth(true)); // Start horizontal for padding

        EditorGUI.BeginChangeCheck();

        IconButtonWithField(PADDING_ICON, "Extend:", ref extraPadding, 0, maxPadding);
        IconButtonWithField(CROP_ICON, "Crop:", ref cropValue, 0, maxCropping);

        // Update combined padding value
        padding = Mathf.Clamp(extraPadding - cropValue, -maxCropping, maxPadding);

        if (EditorGUI.EndChangeCheck())
        {
            if (padding != oldPadding)
            {
                UpdatePreview();
                oldPadding = padding;
                DebugEditor($"Extending is now set to {padding}, new estimated output size is: {calculatedWidth}x{calculatedHeight}.");
            }
        }

        if (SHOW_HELP && textureList.count > 0)
        {
            GUILayout.Space(2);
            EditorGUILayout.HelpBox($"The Maximum EXTEND is: {maxPadding} & the Maximum CROPPING is: {maxCropping} to not exceed the maximum or minimum allowed size for this atlas.", MessageType.None);
        }


        GUILayout.EndVertical();
        #endregion

    }

    /// <summary>
    /// Draws the Outline Section.
    /// </summary>
    private void DrawOutlineSection()
    {
        var style = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleRight,
        };

        GUILayout.BeginVertical(style, GUILayout.ExpandWidth(true)); // Start horizontal for padding

        // Start Gaussian or Pixel
        GUILayout.BeginHorizontal();

        IconButton("d_Profiler.Rendering", "Type:", "Gaussian Blur is more suited for High-Res.\nPixel is more suited for Low-Res.");
        GUILayout.FlexibleSpace();
        outlineMode = (OutlineMode)EditorGUILayout.EnumPopup("", outlineMode);

        GUILayout.EndHorizontal();

        // Color Group.
        GUILayout.BeginHorizontal();

        // Color field.
        IconButton("d_Grid.FillTool", "Color:", "Outline(s) Color.");
        outlineColor = EditorGUILayout.ColorField(outlineColor);

        // End Color Group.
        GUILayout.EndHorizontal();

        // End Gaussian or Pixel

        // Thickness.
        IconButtonWithSlider("Mirror", "Thickness:", ref outlineThickness, 0, 20, "The thickness of the Outline.\n0 = disabled.");

        //if (SHOW_HELP)
        //    EditorGUILayout.HelpBox($"While Thickness > 0 updating the preview will take significant time as it will re-draw the outline everytime.", MessageType.None);

        if (SHOW_HELP && outlineThickness != 0)
        {
            GUILayout.Space(2);
            EditorGUILayout.HelpBox("When having Outline enabled the preview will take significant time to update.", MessageType.Warning);
        }

        // Spacing.
        GUILayout.Space(2);

        // Apply Outline Button.
        if (GUILayout.Button("Apply Outline"))
        {
            UpdatePreview(true);
        }

        GUILayout.EndVertical();

    }

    /// <summary>
    /// Draws the Alignment Section.
    /// </summary>
    private void DrawAlignmentSection()
    {
        #region Measurement
        var style = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleRight,
        };

        GUILayout.BeginVertical(style);

        #region TEST
        GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

        GUILayout.BeginHorizontal();

        DrawPackingOptions();

        GUILayout.EndHorizontal();


        GUILayout.BeginHorizontal();

        IconButton("d_FilterByType", "Alignment:", "Will re-arrange to either Columns or Rows.");
        GUILayout.FlexibleSpace();
        measurementMode = (MeasurementMode)EditorGUILayout.EnumPopup("", measurementMode);

        GUILayout.EndHorizontal();

        switch (measurementMode)
        {
            case MeasurementMode.Columns:
                int minColumns = CalculateMinimumColumns();

                EditorGUI.BeginChangeCheck();

                GUILayout.BeginHorizontal();

                IconButton("Grid.MoveTool", "Columns", "How many Columns the Atlas will have.");
                int newColumns = EditorGUILayout.IntField("", columns); // Find a creative solution for this.. I need to look up how to apply the value only after no longer focusing on this.

                GUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    columns = Mathf.Max(newColumns, minColumns);

                    // If the value has changed and editing is finished, apply resize and update preview
                    if (lastMeasurementMode != measurementMode || oldColumns != columns)
                    {
                        // I cannot do this enough..
                        maxPadding = CalculateMaxPadding();
                        maxCropping = CalculateMaxCropping();

                        UpdatePreview();

                        lastMeasurementMode = measurementMode;
                        oldColumns = columns;
                    }
                }

                if (SHOW_HELP)
                {
                    GUILayout.Space(2);
                    EditorGUILayout.HelpBox($"The minimum COLUMNS to not exceed the maximum allowed size for this atlas is ({minColumns}).", MessageType.None);
                }

                break;

            case MeasurementMode.Rows:
                int minRows = CalculateMinimumRows();

                GUILayout.BeginHorizontal();

                IconButton("Grid.MoveTool", "Rows:", "How many Rows the Atlas will have.");

                rows = EditorGUILayout.IntField("", rows);
                rows = Mathf.Max(rows, minRows);

                GUILayout.EndHorizontal();
                if (lastMeasurementMode != measurementMode || oldRows != rows)
                {
                    // I cannot do this enough..
                    maxPadding = CalculateMaxPadding();
                    maxCropping = CalculateMaxCropping();

                    UpdatePreview();

                    lastMeasurementMode = measurementMode;
                    oldRows = rows;
                }

                if (SHOW_HELP)
                {
                    GUILayout.Space(2);
                    EditorGUILayout.HelpBox($"The minimum ROWS to not exceed the maximum allowed size for this atlas is ({minRows}).", MessageType.None);
                }

                break;

                // (CUSTOM WIP)
        }


        GUILayout.EndVertical();
        #endregion

        GUILayout.EndVertical();
        #endregion
    }

    #endregion

    #endregion

    // Center Area ---
    #region Center Area

    /// <summary>
    /// Creates the centeral Preview container.
    /// </summary>
    private void DrawPreviewArea(Rect Area)
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(PREVIEW_AREA_WIDTH), GUILayout.ExpandHeight(true));

        // Preview Area
        Preview(Area);

        // End Middle Area
        GUILayout.EndVertical();
    }

    /// <summary>
    /// Preview Logic.
    /// </summary>
    private void Preview(Rect Area)
    {
        // Begin Preview Container.
        GUILayout.BeginVertical(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

        // Preview Container Rect.
        Rect viewRect = GUILayoutUtility.GetRect(PREVIEW_AREA_WIDTH, position.height - BOTTOM_BAR_HEIGHT);

        // Dynamic scaling based on zoom.
        float zoomedWidth = calculatedWidth * zoomScale;
        float zoomedHeight = calculatedHeight * zoomScale;

        // Final scaling for preview.
        Rect previewRect = new Rect(0, 0, zoomedWidth, zoomedHeight);

        // Begin Scroll Preview.
        previewScrollPos = GUI.BeginScrollView(viewRect, previewScrollPos, previewRect);

        // Draw the checkerboard background.
        if (checkerboardTexture != null && SHOW_CHECKER)
        {
            for (int y = 0; y < Area.height + zoomedHeight; y += checkerboardTexture.height)
            {
                for (int x = 0; x < Area.width + zoomedWidth; x += checkerboardTexture.width)
                {
                    GUI.DrawTexture(new Rect(x, y, checkerboardTexture.width, checkerboardTexture.height), checkerboardTexture);
                }
            }
        }

        // If we have a resize atlas we show this before the original.
        // TOOD: Switch between Final and Original Atlas mode to speed up generating.
        if (resizedAtlas != null && resizePercentage < 100)
        {
            GUI.DrawTexture(previewRect, resizedAtlas, ScaleMode.ScaleToFit);
        }

        // The original is always stored so we show this is resize is 100%.
        else if (atlas != null)
        {
            GUI.DrawTexture(previewRect, atlas, ScaleMode.ScaleToFit);
        }

        // Draw the grid texture if toggled on
        if (SHOW_GIZMOS && gridTexture != null)
        {
            GUI.DrawTexture(previewRect, gridTexture, ScaleMode.ScaleToFit);
        }

        // End Preview Scroll.
        GUI.EndScrollView();

        // End Preview Container.
        GUILayout.EndVertical();

        //Drag and drop iamges the preview field.
        HandleDragAndDrop(viewRect);
    }
    #endregion

    // Bottom Bar ---
    #region Bottom
    /// <summary>
    /// Draws the bottom bar with Debug and Est. Size.
    /// </summary>
    private void DrawBottomBar()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

        // DEBUG MESSAGE
        GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));

        // 2/3 for debug messages
        GUILayout.BeginVertical(EditorStyles.textArea, GUILayout.Width(position.width * 2 / 3));
        GUILayout.FlexibleSpace();


        GUI.enabled = false;
        GUILayout.Label($"{LAST_DEBUG_MESSAGE}");
        GUI.enabled = true;

        GUILayout.FlexibleSpace();

        GUILayout.EndVertical();

        // 1/3 for estimated size
        GUILayout.BeginVertical(GUILayout.Width(position.width / 3));

        GUILayout.FlexibleSpace();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        //GUILayout.Label("Estimated Size:", GUILayout.Width(90));
        GUI.enabled = false;

        EditorGUILayout.IntField(calculatedWidth, GUILayout.Width(100));
        EditorGUILayout.IntField(calculatedHeight, GUILayout.Width(100));
        GUI.enabled = true;

        GUILayout.Space(20);
        GUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }
    #endregion
}

public partial class TextureAtlasEditor // Debug        
{
    /// <summary>
    /// Debugs inside the Atlas Window.
    /// </summary>
    /// <param name="message"></param>
    private void DebugEditor(string message) => LAST_DEBUG_MESSAGE = message;

    /// <summary>
    /// Temporary Dummy Method.
    /// </summary>
    private void TBDOptions() => DebugEditor("TBD");

    private void DrawDebugGrid(Texture2D atlas, List<Rect> textureRects)
    {
        Color[] pixels = atlas.GetPixels();
        int atlasWidth = atlas.width;
        int atlasHeight = atlas.height;

        foreach (Rect rect in textureRects)
        {
            int startX = Mathf.FloorToInt(rect.x);
            int startY = Mathf.FloorToInt(rect.y);
            int width = Mathf.FloorToInt(rect.width);
            int height = Mathf.FloorToInt(rect.height);

            // Top and bottom borders
            for (int x = startX; x < startX + width; x++)
            {
                if (x >= 0 && x < atlasWidth)
                {
                    if (startY >= 0 && startY < atlasHeight)
                        pixels[startY * atlasWidth + x] = Color.black; // Top border
                    if (startY + height - 1 >= 0 && startY + height - 1 < atlasHeight)
                        pixels[(startY + height - 1) * atlasWidth + x] = Color.black; // Bottom border
                }
            }

            // Left and right borders
            for (int y = startY; y < startY + height; y++)
            {
                if (y >= 0 && y < atlasHeight)
                {
                    if (startX >= 0 && startX < atlasWidth)
                        pixels[y * atlasWidth + startX] = Color.black; // Left border
                    if (startX + width - 1 >= 0 && startX + width - 1 < atlasWidth)
                        pixels[y * atlasWidth + (startX + width - 1)] = Color.black; // Right border
                }
            }
        }

        gridTexture.SetPixels(pixels);
        gridTexture.Apply();
    }
}

public partial class TextureAtlasEditor // TODO         
{
    // TODO: Custom Column and Row with overflow.
    public enum OverflowHandling { Column, Row }
    private OverflowHandling overflow = OverflowHandling.Column;
}