# TODO

Pixel Art Editor for Unity

TODO
* Undo/Redo
    * Add to createAsset menu (See Damian prez)
* UI beautify:
    * Proper background/border for BeginArea
    * USS for styling
    * Use icons
    * Color swapper needs border
    * tooltips
	* Use style with actual margin (is it possible to remove kMargin?)
* Export
    * export as gif
    * export as spritesheet
* Import
* Workflow with sprites:
    * How to "bind" to a sprite?
    * Generate the spritesheet on import of the asset?
* Palette:
	* Do we want to have palette as a separate asset?
	* See https://worms2d.info/Palette_file
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
* UIElements:
	* create ui with uielements and benchmark
	* benchmark current ui code
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