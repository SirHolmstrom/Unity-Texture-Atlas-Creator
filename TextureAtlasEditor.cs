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

public partial class TextureAtlasEditor /*Editor*/      : EditorWindow
{
    [MenuItem("Tools/Texture Atlas Creator #&a", priority = -1000)]
    public static void ShowWindow()
    {
        GetWindow<TextureAtlasEditor>("Texture Atlas Creator");
    }

    private void OnEnable()
    {
        // Ensuring that the 'textures' list is initialized.
        if (textures == null)
        {
            textures = new List<Texture2D>();
        }

        // Creates the state textures.
        CreateActiveTextures();

        // Setup the lists and callbacks.
        SetupTextureList();

        // Get the path of the current script.
        GetEditorPath();

        // Init styles.
        #region Create Styles

        CreateFoldoutStyle();
        CreateLeftMenuStyle();
        CreateRightToolbarStyle();

        #endregion

        // Caches Editor Icons. (WIP).
        CacheIcons();
    }

    void OnGUI()
    {
        // Top Toolbar.
        DrawToolbar();

        // Start Editor Window.
        GUILayout.BeginVertical(GUILayout.Height(position.height - BOTTOM_BAR_HEIGHT));

        // Start Left Left, Middle and Right Areas.
        GUILayout.BeginHorizontal();

        // Left side: Menu Buttons & Zoom bar.
        DrawLeftSidebar();

        // Center: Image Preview.
        DrawPreviewArea();

        // Right side: Configuration area.
        DrawRightSidebar();

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
    private const float zoomMin = 0.01f;
    private const float zoomMax = 1f;

    // Debug ---
    private int calculatedWidth = 0;
    private int calculatedHeight = 0;
    protected string LAST_DEBUG_MESSAGE = "";

    // Atlas Textures ---
    private Texture2D atlas;
    private Texture2D resizedAtlas;

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
    private GUIStyle RIGHT_TOOLBAR_STYLE;


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

    // Editor/Foldout States ---
    private bool FOLDOUT_SCALE_CROP = true; // add this as a class member variable
    private bool FOLDOUT_TEXTURE_ALIGNMENT = true; // add this as a class member variable
    private bool FOLDOUT_OUTLINE = false; // add this as a class member variable
    private bool FOLDOUT_SIZE = false; // add this as a class member variable
    private bool SHOW_HELP = true; // add this as a class member variable

    // Dialogue States (WIP) ---
    private bool showResizeDialog = false;
    private bool showNewDialog = false;
    private bool showResetDialog = false;
    private bool showSaveDialog = false;
    private bool showResetConfirmationDialog = false;
    private bool showSaveAsDialog = false;

    // Constants ---
    private const int SIDE_BAR_WIDTH = 32;
    private const int MAX_SIZE_TEXTURE = 8192;
    private const int BOTTOM_BAR_HEIGHT = 70;
    private const float DEFAULT_TOOLBARWIDTH = 112.5F;

    // Constant Properties
    private float ONE_THIRD_SIZE => (position.width / 3);
    private float PREVIEW_AREA_WIDTH => (ONE_THIRD_SIZE * 2) - SIDE_BAR_WIDTH;
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

    /// <summary>
    /// Generate the Final Atlas/Preview.
    /// </summary>
    private void UpdateAtlas()
    {
        if (textures == null || textures.Count == 0)
        {
            DebugEditor("No textures selected!");
            return;
        }

        CalculateAtlasTextureSize();

        if (calculatedWidth > MAX_SIZE_TEXTURE || calculatedHeight > MAX_SIZE_TEXTURE)
        {
            DebugEditor($"Cannot generate texture atlas. Calculated size {calculatedWidth}x{calculatedHeight} exceeds maximum allowed size {MAX_SIZE_TEXTURE}x{MAX_SIZE_TEXTURE}.");
            return;
        }

        atlas = new Texture2D(calculatedWidth, calculatedHeight, TextureFormat.RGBA32, false);
        atlas.filterMode = FilterMode.Point;
        atlas.wrapMode = TextureWrapMode.Clamp;

        // Initialize the atlas with fully transparent pixels
        Color[] transparentPixels = new Color[calculatedWidth * calculatedHeight];
        for (int i = 0; i < transparentPixels.Length; i++)
        {
            transparentPixels[i] = new Color(0, 0, 0, 0); // Fully transparent color
        }
        atlas.SetPixels(transparentPixels);

        int currentX = 0;
        int currentY = 0;
        int rowHeight = 0;
        int columnCounter = 0;

        foreach (Texture2D texture in textures)
        {
            if (texture == null) continue;

            Texture2D processedTexture = texture;
            if (outlineThickness > 0)
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
            currentX += paddedWidth;
            rowHeight = Mathf.Max(rowHeight, paddedHeight);
            columnCounter++;
        }

        if (packingOption == Packing.Tight)
        {
            atlas = TightPacking(textures, calculatedWidth, calculatedHeight);
        }

        atlas.Apply();
        DebugEditor("Texture atlas generated successfully.");

        // after the atlas is ready let's build the resize preview.
        CheckResize();

    }

    /// <summary>
    /// Updates the Preview with all current settings. (TODO CHECK DIRTY)
    /// </summary>
    private void UpdatePreview()
    {
        UpdateAtlas();
    }
}

public partial class TextureAtlasEditor // Menu Methods 
{
    // Reset ---

    /// <summary>
    /// Saves the whole Atlas with modifications.
    /// </summary>
    private void ResetAtlas()
    {
        if (atlas != null)
        {
            showResetConfirmationDialog = true;
        }
        else
        {
            PerformResetAtlas();
        }
    }

    private void PerformResetAtlas()
    {
        // Clear the textures list
        textures.Clear();
        textureList.list = textures;

        // Reset the atlas and resizedAtlas textures
        if (atlas != null)
        {
            DestroyImmediate(atlas);
            atlas = null;
        }

        if (resizedAtlas != null)
        {
            DestroyImmediate(resizedAtlas);
            resizedAtlas = null;
        }

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

        // Clear the preview window
        Repaint();

        DebugEditor("Atlas has been reset.");
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
                showSaveAsDialog = true;
            }
            else
            {
                PerformResetAtlas();
            }
            showSaveDialog = false;
        }

        if (showSaveAsDialog)
        {
            if (EditorUtility.DisplayDialog("Save As", "How do you want to save the Atlas?", "As Atlas", "As Individual"))
            {
                SaveAtlas();
            }
            else
            {
                SaveTexturesIndividually();
            }
            PerformResetAtlas();
            showSaveAsDialog = false;
        }
    }

    // Save ---

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
                SaveTextureAsPNG(textureToSave, path);
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
        if (textures == null || textures.Count == 0)
        {
            DebugEditor("No textures selected!");
            return;
        }

        string folderPath = EditorUtility.SaveFolderPanel("Save Textures Individually", "", "");
        if (string.IsNullOrEmpty(folderPath))
        {
            DebugEditor("No folder selected for saving textures!");
            return;
        }

        for (int i = 0; i < textures.Count; i++)
        {
            Texture2D texture = textures[i];
            if (texture != null)
            {
                // Apply outline if thickness > 0
                Texture2D processedTexture = texture;
                if (outlineThickness > 0)
                {
                    processedTexture = ApplyOutline(texture, outlineThickness, outlineColor);
                }

                // Apply padding or cropping
                int paddedWidth = processedTexture.width + padding * 2;
                int paddedHeight = processedTexture.height + padding * 2;
                Texture2D finalTexture = new Texture2D(paddedWidth, paddedHeight);

                Color[] clearPixels = new Color[paddedWidth * paddedHeight];
                for (int j = 0; j < clearPixels.Length; j++)
                {
                    clearPixels[j] = new Color(0, 0, 0, 0); // Transparent color
                }
                finalTexture.SetPixels(clearPixels);

                if (padding >= 0)
                {
                    finalTexture.SetPixels(padding, padding, processedTexture.width, processedTexture.height, processedTexture.GetPixels());
                }
                else
                {
                    int cropX = Mathf.Abs(padding);
                    int cropY = Mathf.Abs(padding);
                    int cropWidth = Mathf.Max(processedTexture.width - 2 * cropX, 0);
                    int cropHeight = Mathf.Max(processedTexture.height - 2 * cropY, 0);

                    if (cropWidth > 0 && cropHeight > 0)
                    {
                        finalTexture.SetPixels(0, 0, cropWidth, cropHeight, processedTexture.GetPixels(cropX, cropY, cropWidth, cropHeight));
                    }
                }

                finalTexture.Apply();

                // Resize the texture if necessary
                Texture2D resizedTexture = GetTexture(finalTexture, resizePercentage);

                // Ensure the texture is in a format that can be encoded
                Texture2D readableTexture = GetReadableTexture(resizedTexture);

                string texturePath = Path.Combine(folderPath, $"Texture_{i}.png");
                SaveTextureAsPNG(readableTexture, texturePath);

                // Clean up if we created a new texture
                if (readableTexture != finalTexture)
                {
                    DestroyImmediate(readableTexture);
                }

                if (processedTexture != texture)
                {
                    DestroyImmediate(processedTexture);
                }
            }
        }

        DebugEditor("All textures saved individually.");
    }

    /// <summary>
    /// Encoding to PNG to path.
    /// </summary>
    private void SaveTextureAsPNG(Texture2D texture, string path)
    {
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        DebugEditor("Texture saved to: " + path);
    }

    // Import ---

    /// <summary>
    /// Loads all selected textures from folder.
    /// </summary>
    private void ImportImagesFromFolder()
    {
        string[] filters = new string[] { "Image files", "png,jpg,jpeg", "All files", "*" };
        string path = EditorUtility.OpenFilePanelWithFilters("Select Images", "", filters);

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
    public enum Packing { Tight, Grid }
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
        textureList = new ReorderableList(textures, typeof(Texture2D), true, true, true, true);

        textureList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Textures");
        };

        textureList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            if (index >= textures.Count) return;

            textures[index] = (Texture2D)EditorGUI.ObjectField(new Rect(rect.x, rect.y, rect.width - 30, EditorGUIUtility.singleLineHeight),
                textures[index], typeof(Texture2D), false);

            if (GUI.Button(new Rect(rect.x + rect.width - 30, rect.y, 30, EditorGUIUtility.singleLineHeight), "X"))
            {
                textures.RemoveAt(index);
                UpdatePreview();
                GUIUtility.ExitGUI();
            }
        };

        textureList.onAddCallback = (ReorderableList list) =>
        {
            textures.Add(null);
        };

        textureList.onRemoveCallback = (ReorderableList list) =>
        {
            if (EditorUtility.DisplayDialog("Warning", "Are you sure you want to delete the texture?", "Yes", "No"))
            {
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
                UpdatePreview();
                GUIUtility.ExitGUI();
            }
        };

        // Handle drag-and-drop
        textureList.drawElementBackgroundCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            Event evt = Event.current;
            if (rect.Contains(evt.mousePosition) && evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }

            if (rect.Contains(evt.mousePosition) && evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                bool added = false;
                foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                {
                    if (draggedObject is Texture2D texture)
                    {
                        textures.Add(texture);
                        added = true;
                    }
                }
                if (added)
                {
                    UpdatePreview();
                    GUIUtility.ExitGUI();
                }
                evt.Use();
            }
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
            PADDING_ICON = new GUIContent(EditorGUIUtility.IconContent("d_MoveTool@2x").image, $"Extend: Increases the textures size by adding space around the edges, effectively extending the Atlas.");

        if (CROP_ICON == null)
            CROP_ICON = new GUIContent(EditorGUIUtility.IconContent("d_OrientationGizmo@2x").image, $"Crop: Reduce the image to a selected area, removing outer parts.");

        if (MODE_ICON == null)
            MODE_ICON = new GUIContent(EditorGUIUtility.IconContent("FreeformLayoutGroup Icon").image, $"Tight: Won't respect limits and will try to pack the textures tightly. Grid: Respects the Columns and Rows completly.");

        if (OUTLINE_TYPE_ICON == null)
            OUTLINE_TYPE_ICON = new GUIContent(EditorGUIUtility.IconContent("Grid.PaintTool").image, $"If the outline is to blurry try the Pixel setting.");
    }

    /// <summary>
    /// Minimal foldout style.
    /// </summary>
    private void CreateFoldoutStyle()
    {
        if(FOLDOUT_STYLE == null)
        {
            FOLDOUT_STYLE = new GUIStyle(EditorStyles.foldout);
        }

        // Has to set outside constructor.
        FOLDOUT_STYLE.fontStyle = EditorStyles.miniLabel.fontStyle;
        FOLDOUT_STYLE.fontSize = EditorStyles.miniLabel.fontSize;
        FOLDOUT_STYLE.normal.textColor = EditorStyles.miniLabel.normal.textColor;
        FOLDOUT_STYLE.onNormal.textColor = EditorStyles.miniLabel.onNormal.textColor;
        FOLDOUT_STYLE.active.textColor = EditorStyles.miniLabel.active.textColor;
        FOLDOUT_STYLE.onActive.textColor = EditorStyles.miniLabel.onActive.textColor;
        FOLDOUT_STYLE.focused.textColor = EditorStyles.miniLabel.focused.textColor;
        FOLDOUT_STYLE.onFocused.textColor = EditorStyles.miniLabel.onFocused.textColor;
    }

    /// <summary>
    /// Creates the Helpbox Style to elevate the left menu.
    /// </summary>
    private void CreateLeftMenuStyle()
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

    /// <summary>
    /// Creates the toolbar button style for Right-Toolbar.
    /// </summary>
    private void CreateRightToolbarStyle()
    {
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
    private GUIStyle GetGenericButtonStyle(float size = 32)
    {
        if (GENERIC_BUTTON_STYLE == null)
        {
            GUIStyle customButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = size,
                fixedWidth = size,
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
        }

        if (size != 32 /*default*/ )
        {
            GENERIC_BUTTON_STYLE.fixedHeight = size;
            GENERIC_BUTTON_STYLE.fixedWidth = size;

            return new GUIStyle(GENERIC_BUTTON_STYLE);
        }


        return GENERIC_BUTTON_STYLE;
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
        if (GUILayout.Button(EditorGUIUtility.IconContent("UnityEditor.ConsoleWindow"), EditorStyles.toolbarButton))
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

        if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("SceneLoadIn").image, "Import Folder."), EditorStyles.toolbarButton)) // Load textures from folder
        {
            ImportImagesFromFolder();
        }

        if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("SaveAs").image, "Save Atlas."), EditorStyles.toolbarButton)) // Save.
        {
            SaveAtlas();
        }

        if (GUILayout.Button(EditorGUIUtility.IconContent("pane options"), EditorStyles.toolbarButton)) // Settings.
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
        GUILayout.BeginVertical(LEFT_HELPBOX_STYLE, GUILayout.Width(32), GUILayout.ExpandHeight(true));

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
        GUI.enabled = false;

        if (GUILayout.Toggle(showNewDialog, PLUS_ICON, GetGenericButtonStyle()))
        {
            TBDOptions();
        }

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

        BeginVerticalCenter();
        // Because vertical sliders don't center due to pivot..
        zoomScale = GUILayout.VerticalScrollbar(zoomScale, .1f, zoomMax, zoomMin, GUILayout.Height(150)); // vertical scrollbar for zoom..
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
    private void DrawRightSidebar()
    {
        // Start right group
        GUILayout.BeginVertical(LEFT_HELPBOX_STYLE, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)); // Start right group

        // WIP
        #region Toolbar WIP

        GUILayout.BeginVertical(GUILayout.ExpandHeight(true)); // Start top

        GUILayout.Label("TBD Toolbar", EditorStyles.centeredGreyMiniLabel);

        GUILayout.EndVertical();

        // Top
        GUILayout.BeginVertical(GUILayout.ExpandHeight(true)); // Start top

        #region Toolbar

        // Temp Workspace Toolbar (Disabled WIP)
        string[] viewIcons = { "d_RectTool On", "d_ScaleTool On", "d_PositionAsUV1 Icon", "d_Image Icon", "d_OrientationGizmo@2x", "d_SceneViewFX@2x" };

        // Create the toolbar layout
        GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUILayout.BeginHorizontal(EditorStyles.inspectorFullWidthMargins);

        BeginVerticalCenter();
        var btns = new GUIStyle(EditorStyles.iconButton)
        {
            fixedHeight = 22,
            fixedWidth = 22,
            alignment = TextAnchor.MiddleCenter,
        };
        GUI.enabled = false;
        SelectionRightMenu = GUILayout.SelectionGrid(SelectionRightMenu, CreateSlectionGridMenuButtons(viewIcons), viewIcons.Length, btns);
        GUI.enabled = true;
        EndVerticalCenter();

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        #endregion

        EditorGUILayout.Separator();

        #endregion

        // HelpBox styled vertical area with scroll view
        GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        #region Fit & Alignment

        #region Foldout Style
        // Making sure to avoid Errors.
        if(FOLDOUT_STYLE == null)
        {
            CreateFoldoutStyle();
        }
        #endregion

        FOLDOUT_TEXTURE_ALIGNMENT = EditorGUILayout.Foldout(FOLDOUT_TEXTURE_ALIGNMENT, "Fit & Alignment", FOLDOUT_STYLE);

        GUILayout.Space(2);
        if (FOLDOUT_TEXTURE_ALIGNMENT)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox); // using helpBox style for a better look

            GUILayout.Space(2);

            DrawAlignmentSection();

            GUILayout.EndVertical();

        }
        #endregion

        #region Extend & Crop
        FOLDOUT_SCALE_CROP = EditorGUILayout.Foldout(FOLDOUT_SCALE_CROP, "Extend & Crop", FOLDOUT_STYLE);

        GUILayout.Space(2);
        if (FOLDOUT_SCALE_CROP)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox); // using helpBox style for a better look

            GUILayout.Space(2);
            DrawPaddingSection();

            GUILayout.EndVertical();
        }

        #endregion

        #region Outline
        FOLDOUT_OUTLINE = EditorGUILayout.Foldout(FOLDOUT_OUTLINE, "Outline", FOLDOUT_STYLE);
        GUILayout.Space(2);
        if (FOLDOUT_OUTLINE)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox); // using helpBox style for a better look

            GUILayout.Space(2);
            DrawOutlineSection();

            GUILayout.EndVertical();
        }
        #endregion

        #region Size

        FOLDOUT_SIZE = EditorGUILayout.Foldout(FOLDOUT_SIZE, "Adjust Size", FOLDOUT_STYLE);
        GUILayout.Space(2);
        if (FOLDOUT_SIZE)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox); // using helpBox style for a better look

            GUILayout.Space(2);
            DrawResizeSection();
            GUILayout.Space(2);

            GUILayout.EndVertical();
        }

        #endregion

        GUILayout.EndVertical();


        GUILayout.EndVertical(); // End top

        // Middle
        GUILayout.BeginVertical(GUILayout.ExpandHeight(true)); // Start middle

        #region Textures to Atlas & Generate

        EditorGUILayout.Separator();

        // Tile Palette Preview
        
        GUILayout.Label("Atlas Items", EditorStyles.centeredGreyMiniLabel);
        GUILayout.Space(5);

        listScrollPos = EditorGUILayout.BeginScrollView(listScrollPos, GUILayout.ExpandHeight(true));
        textureList.DoLayoutList();
        EditorGUILayout.EndScrollView();

        GUILayout.Space(8);
        GUILayout.FlexibleSpace();

        GUILayout.BeginVertical(LEFT_HELPBOX_STYLE, GUILayout.ExpandHeight(true)); // Start inner vertical for generate button

        if (GUILayout.Button("Generate"))
        {
            UpdateAtlas();
            DebugEditor("Texture atlas generated successfully.");
        }

        #endregion

        GUILayout.EndVertical(); // End inner vertical for generate button

        GUILayout.EndVertical(); // End middle

        GUILayout.Space(2);
        GUILayout.EndVertical(); // End right group
    }

    #region Right Section Specifics

    /// <summary>
    /// Draws the Resize (%) Section.
    /// </summary>
    private void DrawResizeSection()
    {
        // Create a horizontal group for the resize slider
        GUILayout.BeginHorizontal();

        // Display the resize slider
        IconButtonWithSlider("ScaleTool On", "Resize(%):", ref resizePercentage, 1, 100, $"Resizes The Final Exported Asset by ({resizePercentage}%).\nOriginal Assets/Atlas are preserved.");

        GUILayout.Space(2);
        if (GUILayout.Button("Apply"))
        {
            UpdatePreview();
        }

        GUILayout.EndHorizontal();
        if (SHOW_HELP)
            EditorGUILayout.HelpBox($"The new estimated size will be: {(calculatedWidth / 100) * resizePercentage}x{(calculatedHeight / 100) * resizePercentage}.", MessageType.None);
    }

    /// <summary>
    /// Draws the Extend / Crop Section.
    /// </summary>
    private void DrawPaddingSection()
    {
        #region padding
        maxPadding = CalculateMaxPadding();
        maxCropping = CalculateMaxCropping();

        GUILayout.BeginVertical(GUILayout.ExpandWidth(true)); // Start horizontal for padding

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
            EditorGUILayout.HelpBox($"The Maximum EXTEND is: {maxPadding} & the Maximum CROPPING is: {maxCropping} to not exceed the maximum or minimum allowed size for this atlas.", MessageType.None);


        GUILayout.EndVertical();
        #endregion

    }

    /// <summary>
    /// Draws the Outline Section.
    /// </summary>
    private void DrawOutlineSection()
    {
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
            EditorGUILayout.HelpBox("When having Outline enabled the preview will take significant time to update.", MessageType.Warning);
        }

        // Spacing.
        GUILayout.Space(2);

        // Apply Outline Button.
        if (GUILayout.Button("Apply Outline"))
        {
            UpdatePreview();
        }
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
                    EditorGUILayout.HelpBox($"The minimum COLUMNS to not exceed the maximum allowed size for this atlas is ({minColumns}).", MessageType.None);

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
                    EditorGUILayout.HelpBox($"The minimum ROWS to not exceed the maximum allowed size for this atlas is ({minRows}).", MessageType.None);

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
    private void DrawPreviewArea()
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(PREVIEW_AREA_WIDTH));

        // Preview Area
        Preview();

        // End Middle Area
        GUILayout.EndVertical();
    }

    /// <summary>
    /// Preview Logic.
    /// </summary>
    private void Preview()
    {
        // Begin Preview Container.
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

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
        if (checkerboardTexture != null)
        {
            for (int y = 0; y < zoomedHeight; y += checkerboardTexture.height)
            {
                for (int x = 0; x < zoomedWidth; x += checkerboardTexture.width)
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

        // End Preview Scroll.
        GUI.EndScrollView();

        // End Preview Container.
        GUILayout.EndVertical();
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
}

public partial class TextureAtlasEditor // TODO         
{
    // TODO: Custom Column and Row with overflow.
    public enum OverflowHandling { Column, Row }
    private OverflowHandling overflow = OverflowHandling.Column;
}