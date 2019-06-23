using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UniPix
{
    [System.Serializable]
    public class Image : ScriptableObject
    {
        [SerializeField]
        public List<Frame> Frames;

        [SerializeField]
        public int Width;
        [SerializeField]
        public int Height;
        public Image()
        {
            Width = 16;
            Height = 16;
            Frames = new List<Frame>();
        }

        public Frame AddFrame()
        {
            var frame = new Frame(Width, Height);
            Frames.Add(frame);
            return frame;
        }

        public static Image CreateImage(int width, int height)
        {
            var img = CreateInstance<Image>();
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

        public Frame(int width, int height)
        {
            Layers = new List<Layer>();
            Width = width;
            Height = height;
        }

        public Layer AddLayer()
        {
            var layer = new Layer();
            layer.Init(Width, Height);
            layer.Name = $"Layer {Layers.Count + 1}";
            Layers.Add(layer);
            if (BlendedLayer == null)
            {
                BlendedLayer = new Layer();
                BlendedLayer.Init(Width, Height);
            }
            return layer;
        }
    }
}