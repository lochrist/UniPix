using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UniPix
{
    [System.Serializable]
    public class PixImage : ScriptableObject
    {
        [SerializeField]
        public List<Frame> Frames;

        [SerializeField]
        public int Width;
        [SerializeField]
        public int Height;
        public PixImage()
        {
            Width = 16;
            Height = 16;
            Frames = new List<Frame>();
        }

        public Frame AddFrame(int insertionPoint = -1)
        {
            var frame = new Frame(Width, Height);
            if (insertionPoint == -1)
                Frames.Add(frame);
            else
                Frames.Insert(insertionPoint, frame);
            return frame;
        }

        public static PixImage CreateImage(int width, int height)
        {
            var img = CreateInstance<PixImage>();
            img.Width = width;
            img.Height = height;
            return img;
        }
    }

    [System.Serializable]
    public class Palette
    {
        public List<Color> Colors;
        public Palette()
        {
            Colors = new List<Color>();
        }
    }

    [System.Serializable]
    public class Layer
    {
        [SerializeField]
        public float Opacity;
        [SerializeField]
        public string Name;
        [SerializeField]
        public bool Visible;
        [SerializeField]
        public bool Locked;

        public Color[] Pixels;

        public Layer()
        {
            Opacity = 1.0f;
            Visible = true;
            Locked = false;
        }

        public void Init(int width, int height)
        {
            Pixels = new Color[width * height];
        }
    }

    [System.Serializable]
    public class Frame
    {
        [SerializeField]
        public List<Layer> Layers;
        [SerializeField]
        public int Width;
        [SerializeField]
        public int Height;
        public Layer BlendedLayer;
        [SerializeField]
        public Sprite SourceSprite;

        Texture2D m_Texture;
        public Texture2D Texture
        {
            get
            {
                if (m_Texture == null || !m_Texture)
                {
                    UpdateTextureFromFrame(this);
                }

                return m_Texture;
            }
        }

        public Frame(int width, int height)
        {
            Layers = new List<Layer>();
            Width = width;
            Height = height;
        }

        public Layer AddLayer(int insertionPoint = -1)
        {
            var layer = new Layer();
            layer.Init(Width, Height);
            layer.Name = $"Layer {Layers.Count + 1}";

            if (insertionPoint == -1)
                Layers.Add(layer);
            else
                Layers.Insert(insertionPoint, layer);
            
            if (BlendedLayer == null)
            {
                BlendedLayer = new Layer();
                BlendedLayer.Init(Width, Height);
            }
            return layer;
        }

        public void UpdateFrame()
        {
            UpdateTextureFromFrame(this);
        }

        private static void UpdateTextureFromFrame(Frame frame)
        {
            PixCore.SetLayerColor(frame.BlendedLayer, Color.clear);
            for (var layerIndex = 0; layerIndex < frame.Layers.Count; ++layerIndex)
            {
                if (frame.Layers[layerIndex].Visible)
                {
                    PixCore.Blend(frame.Layers[layerIndex], frame.BlendedLayer, frame.BlendedLayer);
                }
            }

            if (!frame.m_Texture || frame.m_Texture == null)
            {
                frame.m_Texture = PixCore.CreateTexture(frame.Width, frame.Height);
            }

            frame.m_Texture.SetPixels(frame.BlendedLayer.Pixels);
            frame.m_Texture.Apply();
        }
    }
}