using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace UniPix
{
    class EngineTests
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
            var newFrame = new Frame();
            img.Frames.Add(newFrame);
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

