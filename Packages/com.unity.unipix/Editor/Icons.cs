using UnityEditor;
using UnityEngine;

namespace UniPix
{
    public static class Icons
    {
        public static string iconFolder = $"{PixEditor.packageFolderName}/Editor/Icons";

        public static Texture2D arrowDown = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/ArrowDown.png");
        public static Texture2D arrowUp = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/ArrowUp.png");
        public static Texture2D bucket = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/Bucket.png");
        public static Texture2D bucketFull = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/BucketFull.png");
        public static Texture2D cog = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/Cog.png");
        public static Texture2D colorSwapAndArrow = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/ColorSwapArrow.png");
        public static Texture2D newImage = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/New.png");
        public static Texture2D counterClockwiseRotation = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/CounterClockwiseRotation.png");
        public static Texture2D diskette = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/Diskette.png");
        public static Texture2D duplicateLayer = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/DuplicateLayer.png");
        public static Texture2D eraser = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/Eraser.png");
        public static Texture2D export = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/Export.png");
        public static Texture2D folder = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/Folder.png");
        public static Texture2D garbage = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/Garbage.png");
        public static Texture2D mergeLayer = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/MergeLayer.png");
        public static Texture2D pencil = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/Pencil.png");
        public static Texture2D stroke = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/Stroke.png");
        public static Texture2D rectangle = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/Rectangle.png");
        public static Texture2D rectSelect = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/RectSelect.png");
        public static Texture2D plus = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/Plus.png");
        public static Texture2D dithering = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/Dithering.png");
        public static Texture2D x = (Texture2D)EditorGUIUtility.Load($"{iconFolder}/X.png");

        static Icons()
        {
        }

        private static Texture2D LightenTexture(Texture2D texture)
        {
            Texture2D outTexture = new Texture2D(texture.width, texture.height);
            var outColorArray = outTexture.GetPixels();

            var colorArray = texture.GetPixels();
            for (var i = 0; i < colorArray.Length; ++i)
                outColorArray[i] = LightenColor(colorArray[i]);

            outTexture.hideFlags = HideFlags.HideAndDontSave;
            outTexture.SetPixels(outColorArray);
            outTexture.Apply();

            return outTexture;
        }

        public static Color LightenColor(Color color)
        {
            Color.RGBToHSV(color, out var h, out _, out _);
            var outColor = Color.HSVToRGB((h + 0.5f) % 1, 0f, 0.8f);
            outColor.a = color.a;
            return outColor;
        }
    }
}

