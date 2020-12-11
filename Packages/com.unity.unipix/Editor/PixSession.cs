using UnityEngine;

namespace UniPix
{
    [System.Serializable]
    public class PixSession
    {
        public static PixSession Create()
        {
            var session = new PixSession();
            session.ImageSessionState = ScriptableObject.CreateInstance<PixImageSessionState>();
            return session;
        }

        public float ZoomLevel = 20f;

        public PixImage Image;

        Texture2D m_Overlay;

        public bool HasOverlay => m_Overlay != null;

        public void ClearOverlay(bool apply = true)
        {
            PixCore.SetTextureColor(Overlay, Color.clear, apply);
        }

        public void DestroyOverlay()
        {
            if (m_Overlay)
            {
                Object.DestroyImmediate(m_Overlay);
            }

            m_Overlay = null;
        }

        public Texture2D Overlay
        {
            get
            {
                if (m_Overlay == null)
                {
                    m_Overlay = PixCore.CreateTexture(Image.Width, Image.Height);
                    PixCore.SetTextureColor(m_Overlay, Color.clear);
                }

                return m_Overlay;
            }
        }

        public void SetOverlay(int imgCoordX, int imgCoordY, Color color, bool apply = true)
        {
            Overlay.SetPixel(imgCoordX, Overlay.height - imgCoordY - 1, color);
            if (apply)
                Overlay.Apply();
        }

        public string ImageTitle;
        public bool IsImageDirty;

        public int CurrentLayerIndex
        {
            get => ImageSessionState.CurrentLayerIndex;
            set => ImageSessionState.CurrentLayerIndex = value;
        }

        public int CurrentFrameIndex
        {
            get => ImageSessionState.CurrentFrameIndex;
            set => ImageSessionState.CurrentFrameIndex = value;
        }

        public Frame CurrentFrame => Image.Frames[CurrentFrameIndex];
        public Layer CurrentLayer => CurrentFrame.Layers[CurrentLayerIndex];

        public PixImageSessionState ImageSessionState;

        public Color CurrentColor = new Color(1, 0, 0);
        public int CurrentColorPaletteIndex = -1;
        public Color SecondaryColor = Color.black;
        public int SecondaryColorPaletteIndex = -1;

        public int CurrentToolIndex;

        public Vector2 CanvasSize;

        public float ImageOffsetX;
        public float ImageOffsetY;
        public Rect ScaledImgRect;
        public Vector2Int CursorImgCoord;
        public Vector2 CursorPos;
        public int CursorPixelIndex => ImgCoordToPixelIndex(CursorImgCoord.x, CursorImgCoord.y);

        public RectInt BrushRect => PixCore.GetBrushRect(Image, CursorImgCoord, BrushSize);

        public const int k_MinBrushSize = 1;
        public const int k_MaxBrushSize = 6;
        public int BrushSize = 1;
        public Palette Palette;

        public int ImgCoordToPixelIndex(int imgCoordX, int imgCoordY)
        {
            return PixCore.ImgCoordToPixelIndex(Image, imgCoordX, imgCoordY);
        }

        public const int k_MinPreviewFps = 1;
        public const int k_MaxPreviewFps = 24;

        public int PreviewFps = 4;
        public int PreviewFrameIndex = 0;
        public bool IsPreviewPlaying = true;
        public float PreviewTimer;
        public Vector2 FrameScroll = new Vector2(0, 0);
        public Vector2 RightPanelScroll = new Vector2(0, 0);
        public bool IsDebugDraw;

        public const int k_MinCheckPatternSize = 1;
        public const int k_MaxCheckPatternSize = 16;
        public bool ShowCheckerPattern = true;
        public int CheckPatternSize = 2;

        public bool ShowGrid = true;
        public const int k_MinGridSize = 1;
        public const int k_MaxGridSize = 6;
        public int GridSize = 1;
        public int GridPixelSize => (int)ZoomLevel * 3 * GridSize;
        public Color GridColor = Color.black;
    }
}