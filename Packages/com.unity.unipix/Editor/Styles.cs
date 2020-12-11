using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniPix
{
    public static class Styles
    {
        public const float scrollbarWidth = 13f;
        public const float kToolPaletteWidth = 100;
        public const float kFramePreviewWidth = 100;
        public const float kLeftPanelWidth = kToolPaletteWidth + kFramePreviewWidth;
        public const float kRightPanelWidth = 200;
        public const float kToolbarHeight = 25;
        public const float kStatusbarHeight = 35;
        public const float kColorSwatchSize = 40;
        public const float kPaletteItemSize = 25;
        public const float kLayerHeight = 25;
        public const float kLayerRectHeight = 6 * kLayerHeight;
        public const float kMargin = 2;
        public const float kToolSize = 45;
        public const float kFramePreviewBtn = 25;
        public const int kNbToolsPerRow = (int)kToolPaletteWidth / (int)kToolSize;
        public const int kFramePreviewSize = (int)(kFramePreviewWidth - 2 * kMargin - scrollbarWidth);

        public static Color canvasColor = new Color(0.4f, 0.4f, 0.4f);

        public static GUIContent newLayer = new GUIContent(Icons.plus, "Create new layer");
        public static GUIContent cloneLayer = new GUIContent(Icons.duplicateLayer, "Duplicate layer");
        public static GUIContent moveLayerUp = new GUIContent(Icons.arrowUp, "Move layer up");
        public static GUIContent moveLayerDown = new GUIContent(Icons.arrowDown, "Move layer down");
        public static GUIContent mergeLayer = new GUIContent(Icons.mergeLayer, "Merge layer");
        public static GUIContent deleteLayer = new GUIContent(Icons.x, "Delete layer");

        public static GUIContent cloneFrame = new GUIContent(Icons.duplicateLayer, "Clone frame");
        public static GUIContent deleteFrame = new GUIContent(Icons.x, "Delete frame");

        public static GUIContent newContent = new GUIContent(Icons.newImage, "New Image");
        public static GUIContent loadContent = new GUIContent(Icons.folder, "Load Image");
        public static GUIContent saveContent = new GUIContent(Icons.diskette, "Save Image");
        public static GUIContent gridSettingsContent = new GUIContent(Icons.cog, "Settings");
        public static GUIContent exportContent = new GUIContent(Icons.export, "Export Current Image");
        public static GUIContent syncContent = new GUIContent(Icons.counterClockwiseRotation, "Save and sync Sources");
        public static GUIContent colorSwitcherContent = new GUIContent(Icons.colorSwapAndArrow, "Swap Primary and Secondary");

        public static GUIStyle layerHeader = new GUIStyle(EditorStyles.boldLabel);
        public static GUIStyle layerName = new GUIStyle(EditorStyles.largeLabel);
        public static GUIStyle currentLayerName = new GUIStyle(EditorStyles.largeLabel);
        public static GUIStyle layerOpacitySlider = new GUIStyle(GUI.skin.horizontalSlider)
        {
            margin = new RectOffset(0, 15, 0, 0)
        };
        public static GUIStyle brushSlider = new GUIStyle(GUI.skin.horizontalSlider)
        {
            margin = new RectOffset(0, 17, 0, 0)
        };
        public static GUIStyle layerVisible = new GUIStyle(EditorStyles.toggle)
        {
            margin = new RectOffset(0, 0, 4, 0),
            padding = new RectOffset(0, 0, 4, 0)
        };
        public static GUIStyle layerLocked = new GUIStyle(EditorStyles.toggle);
        public static GUIStyle layerToolbarBtn = new GUIStyle(EditorStyles.miniButton)
        {
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(0, 0, 0, 0),
            fixedWidth = 30,
            fixedHeight = 30
        };

        public static GUIStyle frameBtn = new GUIStyle(EditorStyles.miniButton)
        {
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(2, 2, 2, 2)
        };

        public static GUIStyle statusLabel = new GUIStyle(EditorStyles.label)
        {
            richText = true
        };
        public static GUIStyle colorSwap = new GUIStyle()
        {
            name = "colorSwap"
        };
        public static GUIStyle pixMain = new GUIStyle()
        {
            name = "pixmain",
            padding = new RectOffset(2, 0, 0, 0)
        };

        public static GUIStyle pixBox = new GUIStyle()
        {
            name = "pixbox",
            margin = new RectOffset(2, 2, 2, 2),
            padding = new RectOffset(2, 2, 2, 2)
        };

        public static GUIStyle selectedPixBox = new GUIStyle(pixBox)
        {
            name = "selected-pixbox"
        };

        public static GUIStyle primaryColorBox = new GUIStyle(pixBox)
        {
            name = "primary-color-box"
        };

        public static GUIStyle secondaryColorBox = new GUIStyle(pixBox)
        {
            name = "secondary-color-box"
        };

        public static readonly GUIStyle itemBackground1 = new GUIStyle
        {
            name = "pix-item-background1",
        };

        public static readonly GUIStyle itemBackground2 = new GUIStyle(itemBackground1)
        {
            name = "pix-item-background2",
        };

        static Styles()
        {
            currentLayerName.normal.textColor = Color.yellow;
        }
    }
}