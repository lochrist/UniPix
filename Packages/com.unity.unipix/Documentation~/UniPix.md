# TODO

Pixel Art Editor for Unity

TODO

Not for hackweek:
* Finish all shortcuts + keep same shortcut in default mode
  * Bug de mode:
    * shortcut pas dispo dans le shortcut manager
    * Seems to have double bindings for Up/Down
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

* Tools:
    * Selection
    * Brush / Spray tool

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

Comments
- Mode:
    - command are not seen by the shortcut manager... should we use [Shortcut]??
    - validate if we use shortcut_id in mode file if it conflicts with shortcut manager (does the shortcut manager overrides the mode file?)
    - Lots of trouble generating the Close left without docking
    - Window menu difficult to map
- Shortcut:
  - up arrow + down arrow doesn't map?

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
- PRofiling:
  - StyleCstalog.GetStyle: seems to be a bit slow because of the allocation for GetStyleState

175 videos
15 min breaks each hour
started at 13h15

Interesting video:
- High contrast mode: see if mary needs stuff to use as a package.
- Ryan buildbot: 
    - round of applause
    - plugs umpe
- Coroutine: all C# and uses entities
- DataProfiler: profiles jobs, and cache line efficiency
- unity terminal!
- Damian workspaces video:
    - see what roman did
- Timeline expressions
- Intel GPA SDK with Unity
- MArkdown editor in Unity
- scaning objects with iphone to generate Mesh data connecting to a running unity instance: could use UMPE 
    - How did they implement communication?
- Pixel Art tool (not mine) (Teck Lee tian)
    - John Dickinson
    - See history feature to see if you can affect history??
- One Doc website: will it contains internal doc as well?
- Unity performance reporting!!! 
- 2 projects to stream the game on browser (using webRTC)
- Sunstone: @niels ways to get data??? on what? analygtics?
- Tiny LEgend: @sean et @robertg (tiny game rpg using asset from oryx)
- Tiny Rogue: Roguelike in tiny 
- SpriteSkin + Text Mesh Pro
- UTail: should integrate umpe log (see on github)
- ZeroClient: grosse clappe!
- Check Dots Physics doc to know if we could adapt verlet physics
- Check procedurally generated music package hw19-procedural-music
- tub: fetch a build automaticaly with a commit version
    - not enable in mtl
- Mon video: trop difficile de lire le texte qui passe en bas de l'ecran vs le reste de l'action (et l'efet sur le texte etait mauvais). NEed to learn a better video editing tool.
- 
