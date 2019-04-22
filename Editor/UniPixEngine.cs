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
            Frames.Add(new Frame());
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
        }
    }

    [System.Serializable]
    public class Frame
    {
        [SerializeField]
        public List<Layer> Layers;
        public Frame()
        {
            Layers = new List<Layer>();
        }
    }
}