# TODO

Pixel Art Editor for Unity

- Way to create a layout with docking system and no toolbar
- Way to save current layout (in the mode at the right path)
1- Create layout with docking and uni pix
2- create layout with docking and uni pix + project browser
3- create layout with docking and uni pix + project browser + console?
4- Function to create uni pix and no docking
5- Button to pop the project browser next to pix? Pop it modal? ShowAtPosition?
6- Validate what happens (which data) in drag and drop across multi process : surely miss dropping of sprites

In mode, do not draw the toolbar?

Need to Expose commands and shortcut:
- New Image
- Open/Load
- Save
- Sync
- Undo/Redo
- Export Current frame
- Export Multi frame
- Export SpriteSheet

Edit
- Undo
- Redo

Frame
- Next Frame
- Previous frame
- Delete current frame
- Clone current frame

Layer
- Next Layer
- Previous Layer
- Delete current layer
- Clone current layer
- Merge current layer with bottom
- Move layer up
- Move layer down

View
- Pan (no command?)
- Zoom (mouse?)
- Start Preview
- Stop Preview
- Increse preview speed
- Slow previw speed

Tools
- Select Brush
- Select Erase
- Increase brush size
- Lower brush size
- Swap color

TODO
* Mode:
    * Its own menu
    * its own layout? or single window?
        * layout with a projectBrowser
	* Shortcuts: ensure all actions are mapped on shortcut
* UMPE:
    * with a mode
    * Open in isolation
	* validate drag and drop from another project
	* Add Right click on asset: open in UniPix
* export as gif: https://github.com/simonwittber/uGIF/tree/master/Assets/uGIF/Scripts
* Undo/Redo
    * Add to createAsset menu (See Damian prez)
* Tools:
    * Bucket filler
    * Mirror?
    * Shape:
        * circle
        * rectangle
        * Line
    * Selection?
        * Move/Cut
        * copy + paste
    * Palette switcher:
        * switch palette and apply palette to image: all colors are replaced with colors from the other palette?

If time:
* spriesheet syncing if needed
* UIElements:
	* create ui with uielements and benchmark
	* benchmark current ui code
* Rename layers
* Ensure creation of image allows speciying the size of the image + allow resize of image


Not for hackweek:
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

- quick enumeration of all features while using the pixel art editor
    - layers
    - multi frames
    - import from texture2D, sprite, jpg, png 
    - export as separate frames or spritesheet
    - synchronize with source asset
    - grid
    - tools:
    - palette management
    - animation preview
    - undo/redo
    - everything has a shorcuts
    - but everty unity pixel art editor would have that...
- In its own process:
    - separate undo-queue
    - not impacted by global selection
    - sync with master instance
    - its own menubar and shortcuts!
- Can even be started standalone:
    - create shortcut with proper command line switch on the project