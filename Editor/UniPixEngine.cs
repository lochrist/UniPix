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
            Frames = new List<Frame>();
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
    }
}