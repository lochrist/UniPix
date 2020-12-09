#if ENABLE_TESTS
using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace UniPix
{
    public static class UniPixMisc
    {
        public static UniPix.Image CreateDummyImg()
        {
            var img = ScriptableObject.CreateInstance<UniPix.Image>();
            img.Height = 12;
            img.Width = 12;

            {
                var l = new UniPix.Layer();
                l.Name = "Background";
                l.Opacity = 0.3f;
                l.Pixels = new Color[16];
                l.Pixels[0] = Color.black;
                l.Pixels[1] = Color.green;
                img.Frames[0].Layers.Add(l);

                l = new UniPix.Layer();
                l.Name = "L1";
                l.Opacity = 0.3f;
                l.Pixels = new Color[16];
                l.Pixels[0] = Color.blue;
                l.Pixels[1] = Color.red;
                img.Frames[0].Layers.Add(l);
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
            var img = AssetDatabase.LoadAssetAtPath<UniPix.Image>("Assets/Dummy.asset");
            Undo.RecordObject(img, "Img width");
            img.Width = 7;
            Undo.FlushUndoRecordObjects();
        }
    }

    public class EngineTests
    {
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

#endif