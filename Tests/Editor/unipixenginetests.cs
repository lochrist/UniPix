using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections.Generic;

namespace UniPix
{
    class EngineTests
    {
        [Test]
        public void TestUndo1()
        {
            var img = UniPixMisc.CreateDummyImg();
            Undo.RecordObject(img, "Img width");
            var oldWidth = img.Width;
            img.Width = 2;
            Undo.FlushUndoRecordObjects();

            EditorApplication.ExecuteMenuItem("Edit/Undo");
            Assert.AreEqual(oldWidth, img.Width);
        }
    }
}

