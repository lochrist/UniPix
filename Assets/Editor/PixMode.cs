using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;

namespace UniPix
{
    public static class PixMode
    {
        internal static PixSession s_Session;
        internal static PixEditor s_Editor;
        [CommandHandler("UniPix/NewImage")]
        private static void NewImage(CommandExecuteContext context)
        {
            // TODO: allow selection of image size
            PixCommands.NewImage(s_Session, 32, 32);
        }

        [CommandHandler("UniPix/Open")]
        private static void Open(CommandExecuteContext context)
        {
            PixCommands.OpenImage(s_Session);
        }

        [CommandHandler("UniPix/Save")]
        private static void Save(CommandExecuteContext context)
        {
            PixCommands.SaveImage(s_Session);
        }

        // Project specific
        // [CommandHandler("UniPix/Sync")]
        private static void Sync(CommandExecuteContext context)
        {
            // TODO with a validator
            PixIO.UpdateImageSourceSprites(s_Session);
        }

        [CommandHandler("UniPix/ExportCurrentFrame")]
        private static void ExportCurrentFrame(CommandExecuteContext context)
        {
            PixIO.ExportFrames(s_Session, new[] { s_Session.CurrentFrame });
        }

        [CommandHandler("UniPix/ExportFrames")]
        private static void ExportFrames(CommandExecuteContext context)
        {
            PixIO.ExportFrames(s_Session);
        }

        [CommandHandler("UniPix/ExportSpriteSheet")]
        private static void ExportSpriteSheet(CommandExecuteContext context)
        {
            PixIO.ExportFramesToSpriteSheet(s_Session);
        }

        [CommandHandler("UniPix/GotoDefaultMode")]
        private static void GotoDefaultMode(CommandExecuteContext context)
        {
            ModeService.ChangeModeById("default");
        }

        [CommandHandler("UniPix/GotoNextFrame")]
        private static void GotoNextFrame(CommandExecuteContext context)
        {
            var nextFrameIndex = s_Session.CurrentFrameIndex + 1;
            if (nextFrameIndex >= s_Session.Image.Frames.Count)
            {
                nextFrameIndex = 0;
            }
            PixCommands.SetCurrentFrame(s_Session, nextFrameIndex);
        }

        [CommandHandler("UniPix/GotoPreviousFrame")]
        private static void GotoPreviousFrame(CommandExecuteContext context)
        {
            var nextFrameIndex = s_Session.CurrentFrameIndex - 1;
            if (nextFrameIndex < 0)
            {
                nextFrameIndex = s_Session.Image.Frames.Count - 1;
            }
            PixCommands.SetCurrentFrame(s_Session, nextFrameIndex);
        }

        [CommandHandler("UniPix/DeleteCurrentFrame")]
        private static void DeleteCurrentFrame(CommandExecuteContext context)
        {
            PixCommands.DeleteFrame(s_Session, s_Session.CurrentFrameIndex);
        }

        [CommandHandler("UniPix/CloneCurrentFrame")]
        private static void CloneCurrentFrame(CommandExecuteContext context)
        {
            PixCommands.CloneFrame(s_Session, s_Session.CurrentFrameIndex);
        }

        [CommandHandler("UniPix/NewFrame")]
        private static void NewFrame(CommandExecuteContext context)
        {
            PixCommands.NewFrame(s_Session);
        }

        [CommandHandler("UniPix/GotoNextLayer")]
        private static void GotoNextLayer(CommandExecuteContext context)
        {
            PixCommands.NextLayer(s_Session);
        }

        [CommandHandler("UniPix/GotoPreviousLayer")]
        private static void GotoPreviousLayer(CommandExecuteContext context)
        {
            PixCommands.PreviousLayer(s_Session);
        }

        [CommandHandler("UniPix/DeleteCurrentLayer")]
        private static void DeleteCurrentLayer(CommandExecuteContext context)
        {
            PixCommands.DeleteCurrentLayer(s_Session);
        }

        [CommandHandler("UniPix/CloneCurrentLayer")]
        private static void CloneCurrentLayer(CommandExecuteContext context)
        {
            PixCommands.CloneCurrentLayer(s_Session);
        }

        [CommandHandler("UniPix/NewLayer")]
        private static void NewLayer(CommandExecuteContext context)
        {
            PixCommands.CreateLayer(s_Session);
        }

        [CommandHandler("UniPix/MergeCurrentLayer")]
        private static void MergeCurrentLayer(CommandExecuteContext context)
        {
            PixCommands.MergeLayers(s_Session);
        }

        [CommandHandler("UniPix/MoveLayerUp")]
        private static void MoveLayerUp(CommandExecuteContext context)
        {
            PixCommands.MoveCurrentLayerUp(s_Session);
        }
        [CommandHandler("UniPix/MoveLayerDown")]
        private static void MoveLayerDown(CommandExecuteContext context)
        {
            PixCommands.MoveCurrentLayerDown(s_Session);
        }

        [CommandHandler("UniPix/Zoom")]
        private static void Zoom(CommandExecuteContext context)
        {
            PixCommands.IncreaseZoom(s_Session);
        }

        [CommandHandler("UniPix/ZoomBack")]
        private static void ZoomBack(CommandExecuteContext context)
        {
            PixCommands.DecreaseZoom(s_Session);
        }

        [CommandHandler("UniPix/CenterCanvas")]
        private static void CenterImage(CommandExecuteContext context)
        {
            PixCommands.FrameImage(s_Session);
        }

        [CommandHandler("UniPix/ToggleGrid")]
        private static void ToggleGrid(CommandExecuteContext context)
        {
            PixCommands.ToggleGrid(s_Session, s_Editor);
        }

        [CommandHandler("UniPix/ToggleAnimation")]
        private static void ToggleAnimation(CommandExecuteContext context)
        {
            PixCommands.ToggleAnimation(s_Session);
        }

        [CommandHandler("UniPix/IncreaseAnimationSpeed")]
        private static void IncreaseAnimationSpeed(CommandExecuteContext context)
        {
            var newFps = s_Session.PreviewFps + 1;
            if (newFps <= PixSession.k_MaxPreviewFps)
            {
                PixCommands.SetPreviewFps(s_Session, newFps);
            }
        }

        [CommandHandler("UniPix/DecreaseAnimationSpeed")]
        private static void DecreaseAnimationSpeed(CommandExecuteContext context)
        {
            var newFps = s_Session.PreviewFps - 1;
            if (newFps >= PixSession.k_MinPreviewFps)
            {
                PixCommands.SetPreviewFps(s_Session, newFps);
            }
        }

        [CommandHandler("UniPix/ActivateBrush")]
        private static void ActivateBrush(CommandExecuteContext context)
        {
            PixCommands.SetTool(s_Session, s_Editor, BrushTool.kName);
        }

        [CommandHandler("UniPix/ActivateEraser")]
        private static void ActivateEraser(CommandExecuteContext context)
        {
            PixCommands.SetTool(s_Session, s_Editor, EraseTool.kName);
        }

        [CommandHandler("UniPix/IncreaseBrushSize")]
        private static void IncreaseBrushSize(CommandExecuteContext context)
        {
            PixCommands.SetBrushSize(s_Session, s_Session.BrushSize + 1);
        }

        [CommandHandler("UniPix/DecreaseBrushSize")]
        private static void DecreaseBrushSize(CommandExecuteContext context)
        {
            PixCommands.SetBrushSize(s_Session, s_Session.BrushSize - 1);
        }

        [CommandHandler("UniPix/IncreaseColor")]
        private static void IncreaseColor(CommandExecuteContext context)
        {
            Debug.Log("Color ->");
        }

        [CommandHandler("UniPix/DecreaseColor")]
        private static void DecreaseColor(CommandExecuteContext context)
        {
            Debug.Log("Color <-");
        }

        [CommandHandler("UniPix/SwapColor")]
        private static void SwapColor(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/Layout/Pix")]
        internal static void LayoutPix(CommandExecuteContext context)
        {
            PixUtils.LoadWindowLayout($"{PixEditor.packageFolderName}/Editor/Layouts/Pix.wlt");
        }

        [CommandHandler("UniPix/Layout/PixBrowse")]
        internal static void LayoutPixBrowse(CommandExecuteContext context)
        {
            PixUtils.LoadWindowLayout($"{PixEditor.packageFolderName}/Editor/Layouts/Browse.wlt");
        }

        [CommandHandler("UniPix/Layout/PixDebug")]
        internal static void LayoutPixDebug(CommandExecuteContext context)
        {
            PixUtils.LoadWindowLayout($"{PixEditor.packageFolderName}/Editor/Layouts/Debug.wlt");
        }

        [CommandHandler("UniPix/Layout/Dyn_Lyt_Pix")]
        internal static void LayoutDynPix(CommandExecuteContext context)
        {
            PixUtils.LoadDynamicLayout(true,
@"{
    top_view = null
    center_view = ""PixEditor""
    bottom_view = null
    restore_saved_layout = true
}");
        }

        [CommandHandler("UniPix/Layout/Dyn_Lyt_PixBrowse")]
        internal static void LayoutDynPixBrowse(CommandExecuteContext context)
        {
            PixUtils.LoadDynamicLayout(true,
@"{
    top_view = null
    center_view = {
        horizontal = true
        children = [
            { 
                size = 0.2
                tabs = true
                children = [
                    { class_name = ""ProjectBrowser"" }
                ] 
            }
            {
                size = 0.8
                tabs = true
                children = [
                    { class_name = ""PixEditor"" }
                ]
            }
        ]
    }
    bottom_view = null
    restore_saved_layout = true
}");
        }

        [CommandHandler("UniPix/Layout/Dyn_Lyt_PixDebug")]
        internal static void LayoutDynPixDebug(CommandExecuteContext context)
        {
            PixUtils.LoadDynamicLayout(true,
@"{
    top_view = null
    center_view = {
        horizontal = true
        children = [
            {
                size = 0.2
                tabs = true
                children = [
                    { class_name = ""ProjectBrowser"" }
                ]
            }
            { 
                vertical = true
                size = 0.6
                children = [
                    { 
                        size = 0.7
                        tabs = true
                        children = [
                            { class_name = ""PixEditor"" }
                        ] 
                    }
                    { 
                        size = 0.3
                        tabs = true
                        children = [
                            { class_name = ""ConsoleWindow"" }
                        ] 
                    }
                ]
            }
            {
                size = 0.2
                tabs = true
                children = [
                    { class_name = ""InspectorWindow"" }
                ]
            }
        ]
    }
    bottom_view = null
    restore_saved_layout = true
}");
        }
    }
}