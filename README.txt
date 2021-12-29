"The Floor is Status" is a highly configurable mod for Sekiro 1.06 which inflicts status effects on
Wolf constantly. Some versions of the mod only apply effects while he's standing on what the game
considers a "floor", which is basically any permanent standing place, which also includes the surface
and bottom of bodies of water. It excludes jumping, grappling, and hanging from a ledge. Some actions
like opening doors and deathblows also grant Wolf temporary invincibility.

There are three different ways to use the mod. They all require placing files in specific locations
using Sekiro Mod Engine. This assumes you've unzipped the mod and that you've placed the "floor"
folder into the same directory as "Sekiro.exe", so that this file is "Sekiro\floor\README.txt".

Thanks to TKGP, Pav, and Meowmaritus for creating the parts of SoulsFormats used by this mod.

Source code link: https://github.com/thefifthmatt/FloorMod

- thefifthmatt

1. Use a preset
These are the directories with status effect names. In rough increasing order of difficulty:

poison < fire < terror < enfeeble < permafire < permapoison

poison and fire are fairly similar. poison is just the poison swamp effect without slow-walk.
fire is based on standing in fire, but with the actual standing-in-fire damage removed and the tick
damage buffed instead. terror and enfeeble both offer their own interesting challenges. The effects
take effect either at the very beginning of the game or once you reach the Moon-View Tower.

Difficulty tuning suggestions are appreciated. I've gotten from the tutorial up to Gyoubu on all of
these (with many side excursions in some cases). permapoison seems like the most difficult at the
start, since I haven't yet beat Gyoubu with it. My testing assumed NG+0 and charm/no bell.

To install any of these, either edit modengine.ini to refer to the status folder, relative to where
Sekiro.exe is located (set e.g. modOverrideDirectory="\floor\poison"), or otherwise, copy the "event"
and "param" folders into your current mod directory. If there are conflicts, see option 3.

Each preset provides its own floorconfig.ini which was used to generate it. Those files are only for
reference and modifying the configs doesn't do anything.

2. Make a custom variant
Inside the "custom" folder, there is a program called FloorMod.exe. If you run this, it will edit
its "event" and "param" folders to install the modifications specified in floorconfig.ini. The
program can be run multiple times and the game should be restarted every time for it to take effect.

Like with step 1, using the modified files requires either setting
modOverrideDirectory="\floor\custom" or copying the "event" and "param" folders into a different
mod directory, if that doesn't cause conflicts.

3. Merge with other mods
If there's a conflict from editing the same file in a different mod, you'll need to run FloorMod.exe
in that other mod folder. This requires the Defs\ folder to exist, a floorconfig.ini, and
both param\gameparam\gameparam.parambnd.dcx and event\common.emevd.dcx.

Note that FloorMod.exe creates backups. If a .bak file doesn't already exist, it will create one.
