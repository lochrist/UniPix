# TODO

Pixel Art Editor for Unity

TODO

If time:
* Bug with unity logo editing
* Finish all shortcuts + keep same shortcut in default mode

Not for hackweek:
* export as gif: https://github.com/simonwittber/uGIF/tree/master/Assets/uGIF/Scripts
* Some spritesheet syncing is not done
* UIElements:
	* create ui with uielements and benchmark
	* benchmark current ui code
* Rename layers
* Ensure creation of image allows speciying the size of the image + allow resize of image
* Palette:
	* Do we want to have palette as a separate asset?
	* See https://worms2d.info/Palette_file


Features:
* Advance deployment:
    * In its own process
    * with its own undo stack
    * with its own mode
        * Menu items
        * shortcut
        * Layouts
* Animation
* Layers
* Generate (bound to?) Unity Sprite
* Export png, as spritesheet, as gif
* Palette
* Undo/Redo
* Tools:
    * Selection
    * Brush / Spray tool
    * Eye dropper
    * Zoom
    * Show grid
    * Erase
* Primary Color + Secondary color


Things to might do or share for Copenhagen:
* Create ui in uielement (if already done in imgui)
* add more tools.
* tool api so it is possible to register tools?
* Add a shortcut to open the quicksearch tool with only texture and image visible!
* 

Good inspirations


* Pixel art tool (simlar to https://jackschaedler.github.io/goya/)
* https://github.com/jennschiffer/make8bitart
* https://github.com/gmattie/Data-Pixels
* https://assetstore.unity.com/packages/tools/sprite-management/ragepixel-2961
* https://assetstore.unity.com/packages/tools/painting/upa-toolkit-pixel-art-editor-27680
* https://github.com/collections/pixel-art-tools
* https://assetstore.unity.com/packages/tools/painting/pia-pixel-art-editor-113082
* https://felgo.com/game-resources/make-pixel-art-online
* Piskel:  https://www.piskelapp.com/p/agxzfnBpc2tlbC1hcHByEwsSBlBpc2tlbBiAgKCm3tHECww/edit


ou Pixel Art drawing app, created as a unity game that runs on ipad and that uses the Eventservice to communicate with the different instances?


Video ideas:

- Introducing
    - Open Pix
- In Pix layout
    - Edit unity:
        - layer
        - Bucket
        - rectangle
        - opacity
        - clone unity layer
        - change color
        - set opacity
- Open sprite sheet (scavenger)
    - Delete + brush
    - sync
- Open multi srite
    - show animation
- But wait there is more (scratch sound)
- In its own process (new faster tune)
- Pix in it own process:
    - Own menu
    - Own undo
    - Drag and drop across process
    - Edit and sync
- The end


- quick enumeration of all features while using the pixel art editor
    - layers
        - opacity
        - merging
    - multi frames
    - animation preview
    - import from texture2D, sprite, jpg, png 
    - export as separate frames or spritesheet
    - synchronize with source asset
        - Show how the scene with idle animation updates?
    - grid + grid settings
    - tools:
        - pencil
        - eraser
        - bucket
        - dithering
    - Color picker
    - Primary / Secondary color + switch
    - palette
    - undo/redo
    - everything has a shorcuts
    - but every unity pixel art editor should have that...

- In its own process with its own EditorMode:
    - separate undo-queue
    - not impacted by global selection
    - sync with master instance
    - its own menubar and shortcuts!
    - drag and drop across instances
- Can even be started standalone:
    - create shortcut with proper command line switch on the project


Comments
- Mode:
    - command are not seen by the shortcut manager... should we use [Shortcut]??
    - validate if we use shortcut_id in mode file if it conflicts with shortcut manager (does the shortcut manager overrides the mode file?)
    - Lots of trouble generating the Close left without docking
    - Window menu difficult to map

- In Window -> fails
{name = "Project" menu_item_id = "Window/General/UniPix"}
{name = "Console" menu_item_id = "Window/General/UniPix"}
- In Window this works:
{
    name = "General"
    children = [
        {name = "Project"}
        {name = "Console"}
    ]
}
- Bug: mode is unipix. Layout system is not properly loaded (default layout wtf???)
- Cannot save asset in slave mode
- Sync doesn't work in slave mode
- Image not square do not work
