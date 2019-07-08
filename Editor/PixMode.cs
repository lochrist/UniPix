using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UniPix
{
    public static class PixMode
    {
        internal static PixSession s_Session;
        [CommandHandler("UniPix/NewImage")]
        private static void NewImage(CommandExecuteContext context)
        {
            // TODO: allow selection of image size
            PixCommands.CreatePix(s_Session, 32, 32);
        }

        [CommandHandler("UniPix/Open")]
        private static void Open(CommandExecuteContext context)
        {
            PixCommands.LoadPix(s_Session);
        }

        [CommandHandler("UniPix/Save")]
        private static void Save(CommandExecuteContext context)
        {
            PixCommands.SavePix(s_Session);
        }

        [CommandHandler("UniPix/Sync")]
        private static void Sync(CommandExecuteContext context)
        {
            // TODO with a validator
            PixCommands.SaveImageSources(s_Session);
        }

        [CommandHandler("UniPix/ExportCurrentFrame")]
        private static void ExportCurrentFrame(CommandExecuteContext context)
        {
            PixCommands.ExportFrames(s_Session, new[] { s_Session.CurrentFrame });
        }

        [CommandHandler("UniPix/ExportFrames")]
        private static void ExportFrames(CommandExecuteContext context)
        {
            PixCommands.ExportFrames(s_Session);
        }

        [CommandHandler("UniPix/ExportSpriteSheet")]
        private static void ExportSpriteSheet(CommandExecuteContext context)
        {
            PixCommands.ExportFramesToSpriteSheet(s_Session);
        }

        [CommandHandler("UniPix/GotoDefaultMode")]
        private static void GotoDefaultMode(CommandExecuteContext context)
        {
            ModeService.ChangeModeById("default");
        }

        [CommandHandler("UniPix/GotoNextFrame")]
        private static void GotoNextFrame(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/GotoPreviousFrame")]
        private static void GotoPreviousFrame(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/DeleteCurrentFrame")]
        private static void DeleteCurrentFrame(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/CloneCurrentFrame")]
        private static void CloneCurrentFrame(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/NewFrame")]
        private static void NewFrame(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/GotoNextLayer")]
        private static void GotoNextLayer(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/GotoPreviousLayer")]
        private static void GotoPreviousLAyer(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/DeleteCurrentLayer")]
        private static void DeleteCurrentLayer(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/CloneCurrentLayer")]
        private static void CloneCurrentLayer(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/NewLayer")]
        private static void NewLayer(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/MergeCurrentLayer")]
        private static void MergeCurrentLayer(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/MoveLayerUp")]
        private static void MoveLayerUp(CommandExecuteContext context)
        {

        }
        [CommandHandler("UniPix/MoveLayerDown")]
        private static void MoveLayerDown(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/Zoom")]
        private static void Zoom(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/ZoomBack")]
        private static void ZoomBack(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/CenterCanvas")]
        private static void CenterImage(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/ToggleGrid")]
        private static void ToggleGrid(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/ToggleAnimation")]
        private static void ToggleAnimation(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/IncreaseAnimationSpeed")]
        private static void IncreaseAnimationSpeed(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/DecreaseAnimationSpeed")]
        private static void DecreaseAnimationSpeed(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/ActivateBrush")]
        private static void ActivateBrush(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/ActivateEraser")]
        private static void ActivateEraser(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/IncreaseBrushSize")]
        private static void IncreaseBrushSize(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/DecreaseBrushSize")]
        private static void DecreaseBrushSize(CommandExecuteContext context)
        {

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

        [CommandHandler("UniPix/OpenProjectBrowse")]
        private static void OpenProjectBrowse(CommandExecuteContext context)
        {
            LayoutUtils.CreateProject();
        }

        private static void LoadLayout(string name)
        {
            if (ModeService.currentId == "unipix")
            {
                var layouts = ModeService.GetModeDataSectionList<string>(ModeService.currentIndex, "layouts");
                foreach (var layout in layouts)
                {
                    var layoutName = Path.GetFileNameWithoutExtension(layout);
                    if (name == layoutName)
                    {
                        WindowLayout.LoadWindowLayout(layout, false);
                    }
                }
            }
        }

        [CommandHandler("UniPix/Layout/Pix")]
        private static void LayoutPix(CommandExecuteContext context)
        {
            LoadLayout("Pix");
        }

        [CommandHandler("UniPix/Layout/PixBrowse")]
        private static void LayoutPixBrowse(CommandExecuteContext context)
        {
            LoadLayout("Browse");
        }

        [CommandHandler("UniPix/Layout/PixDebug")]
        private static void LayoutPixDebug(CommandExecuteContext context)
        {
            LoadLayout("Debug");
        }
    }
}

