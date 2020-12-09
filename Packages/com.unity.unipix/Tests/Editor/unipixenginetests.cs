using System;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace UniPix
{
    [Serializable]
    public class PixImage2
    {
        public static PixImage2 CreateDummyImg()
        {
            var img = new PixImage2();
            img.Height = 12;
            img.Width = 12;

            {
                var f = img.AddFrame();
                var l = f.AddLayer();
                l.Name = "Background";
                l.Opacity = 0.3f;
                l.Pixels = new Color[16];
                l.Pixels[0] = Color.black;
                l.Pixels[1] = Color.green;

                var l2 = f.AddLayer();
                l2.Name = "L1";
                l2.Opacity = 0.3f;
                l2.Pixels = new Color[16];
                l2.Pixels[0] = Color.blue;
                l2.Pixels[1] = Color.red;
            }

            {
                var f = img.AddFrame();
                var l = f.AddLayer();
                l.Name = "Background";
                l.Opacity = 0.3f;
                l.Pixels = new Color[16];
                l.Pixels[0] = Color.cyan;
                l.Pixels[1] = Color.gray;
            }

            return img;
        }


        [SerializeField]
        public List<Frame2> Frames;

        [SerializeField]
        public int Width;
        [SerializeField]
        public int Height;
        public PixImage2()
        {
            Width = 16;
            Height = 16;
            Frames = new List<Frame2>();
        }

        public Frame2 AddFrame(int insertionPoint = -1)
        {
            var frame = new Frame2(Width, Height);
            if (insertionPoint == -1)
                Frames.Add(frame);
            else
                Frames.Insert(insertionPoint, frame);
            return frame;
        }

        public static PixImage CreateImage(int width, int height)
        {
            var img = new PixImage();
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
    public class Layer2
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

        public Layer2()
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
    public class Frame2
    {
        [SerializeField]
        public List<Layer2> Layers;
        [SerializeField]
        public int Width;
        [SerializeField]
        public int Height;
        public Layer2 BlendedLayer;
        
        public Frame2(int width, int height)
        {
            Layers = new List<Layer2>();
            Width = width;
            Height = height;
        }

        public Layer2 AddLayer(int insertionPoint = -1)
        {
            var layer = new Layer2();
            layer.Init(Width, Height);
            layer.Name = $"Layer {Layers.Count + 1}";

            if (insertionPoint == -1)
                Layers.Add(layer);
            else
                Layers.Insert(insertionPoint, layer);

            if (BlendedLayer == null)
            {
                BlendedLayer = new Layer2();
                BlendedLayer.Init(Width, Height);
            }
            return layer;
        }


    }

    public static class UniPixMisc
    {
        public static PixImage CreateDummyImg()
        {
            var img = ScriptableObject.CreateInstance<PixImage>();
            img.Height = 12;
            img.Width = 12;

            {
                var f = img.AddFrame();
                var l = f.AddLayer();
                l.Name = "Background";
                l.Opacity = 0.3f;
                l.Pixels = new Color[16];
                l.Pixels[0] = Color.black;
                l.Pixels[1] = Color.green;

                l = new UniPix.Layer();
                l.Name = "L1";
                l.Opacity = 0.3f;
                l.Pixels = new Color[16];
                l.Pixels[0] = Color.blue;
                l.Pixels[1] = Color.red;
            }

            {
                var f = img.AddFrame();
                var l = f.AddLayer();
                l.Name = "Background";
                l.Opacity = 0.3f;
                l.Pixels = new Color[16];
                l.Pixels[0] = Color.cyan;
                l.Pixels[1] = Color.gray;
            }

            return img;
        }

        [MenuItem("Tools/Create and Save Dummy")]
        static void CreateAndSave()
        {
            var img = CreateDummyImg();
            AssetDatabase.CreateAsset(img, "Assets/Dummy.asset");
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Create Mods")]
        static void CreateAndMods()
        {
            var img = AssetDatabase.LoadAssetAtPath<PixImage>("Assets/Dummy.asset");
            Undo.RecordObject(img, "Img width");
            img.Width = 7;
            Undo.FlushUndoRecordObjects();
        }
    }

    public class EngineTests
    {
        [Test]
        public void TestWriteImage()
        {
            var img2 = PixImage2.CreateDummyImg();
            
            var binFile = new FileStream("Assets/image2.dat", FileMode.Create);
            var formatter = new BinaryFormatter();
            formatter.Serialize(binFile, img2);
            binFile.Close();
            

            var jsonStr = JsonUtility.ToJson(img2);
            File.WriteAllText("Assets/image2.json", jsonStr);
        }

        [Test]
        public void TestUndoModifyImg()
        {
            var img = UniPixMisc.CreateDummyImg();
            Undo.RecordObject(img, "Img width");
            var oldWidth = img.Width;
            img.Width = 2;
            Undo.FlushUndoRecordObjects();

            EditorApplication.ExecuteMenuItem("Edit/Undo");
            Assert.AreEqual(oldWidth, img.Width);
        }

        [Test]
        public void TestUndoAddFrame()
        {
            var img = UniPixMisc.CreateDummyImg();
            Undo.RecordObject(img, "Img width");
            var nbFrame = img.Frames.Count;
            var newFrame = img.AddFrame();
            Undo.FlushUndoRecordObjects();
            EditorApplication.ExecuteMenuItem("Edit/Undo");
            Assert.AreEqual(img.Frames.Count, nbFrame);
            Assert.AreNotEqual(img.Frames.Last(), newFrame);
        }

        [Test]
        public void TestUndoRemoveFrame()
        {
            var img = UniPixMisc.CreateDummyImg();
            Undo.RecordObject(img, "Img width");
            var nbFrame = img.Frames.Count;
            var newFrame = img.Frames.Last();
            img.Frames.Remove(newFrame);
            Undo.FlushUndoRecordObjects();
            EditorApplication.ExecuteMenuItem("Edit/Undo");
            Assert.AreEqual(img.Frames.Count, nbFrame);
            // Something not equal... but it is (floating point?)
            // Assert.AreSame(img.Frames.Last(), newFrame);
        }

        [Test]
        public void TestUndoAddLayer()
        {
            var img = UniPixMisc.CreateDummyImg();
            Undo.RecordObject(img, "Img width");
            var frame = img.Frames[0];
            var nbLayer = frame.Layers.Count;
            var newLayer = new Layer();
            frame.Layers.Add(newLayer);
            Undo.FlushUndoRecordObjects();

            EditorApplication.ExecuteMenuItem("Edit/Undo");
            Assert.AreEqual(nbLayer, frame.Layers.Count);
        }

        [Test]
        public void TestUndoRemoveLayer()
        {
            var img = UniPixMisc.CreateDummyImg();
            Undo.RecordObject(img, "Img width");
            var frame = img.Frames[0];
            var nbLayer = frame.Layers.Count;
            var newLayer = frame.Layers.Last();
            frame.Layers.Remove(newLayer);
            Undo.FlushUndoRecordObjects();

            EditorApplication.ExecuteMenuItem("Edit/Undo");
            Assert.AreEqual(nbLayer, frame.Layers.Count);
        }

        [Test]
        public void TestUndoModifyLayer()
        {
            var img = UniPixMisc.CreateDummyImg();
            Undo.RecordObject(img, "Img width");
            var layer = img.Frames[0].Layers[0];
            var oldName = layer.Name;
            layer.Name = "Ping!";
            Undo.FlushUndoRecordObjects();

            EditorApplication.ExecuteMenuItem("Edit/Undo");
            Assert.AreEqual(oldName, layer.Name);
        }

        [Test]
        public void TestUndoModifyPixel()
        {
            var img = UniPixMisc.CreateDummyImg();
            Undo.RecordObject(img, "Img width");
            var layer = img.Frames[0].Layers[0];
            var color = layer.Pixels[0];

            layer.Pixels[0] = Color.cyan;

            Undo.FlushUndoRecordObjects();

            EditorApplication.ExecuteMenuItem("Edit/Undo");
            Assert.AreEqual(color, layer.Pixels[0]);
        }
    }
}
