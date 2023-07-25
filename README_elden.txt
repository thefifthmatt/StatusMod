Rot Mod is a specialized version of the Sekiro Floor is Status mod which inflicts scarlet rot with any
buildup, duration, and tick damage. It could be extended to other status effects in the future. Some
actions like lighting graces and backstabs grant temporary invincibility.

This mod is a program which edits the regulation.bin file in this directory. The directory can be used
as a Mod Engine 2 directory.

Thanks to TKGP, Pav, and Meowmaritus for creating the parts of SoulsFormats used by this mod.

Source code link: https://github.com/thefifthmatt/StatusMod

- thefifthmatt

## Installation

First download the mod zip into Mod Engine 2 (https://github.com/soulsmods/ModEngine2/releases)
and use Extract All so that the "rot" directory created as a mod subdirectory.

Running RotMod.exe may require some files to be set up first, using your Elden Ring install
directory (by default, C:\Program Files (x86)\Steam\steamapps\common\ELDEN RING\Game):

- The vanilla regulation.bin included with the mod will only work if you're running Elden Ring
  1.09.1. If you're using a different version of the game, you must copy regulation.bin from
  the game directory before running RotMod.exe.
- If you're not running Elden Ring 1.09.1, you must also copy common.emevd.dcx from the event
  directory in the Elden Ring game directory. To get the event directory, you can use
  UXM Selective Unpack to unpack only the event directory.
- The mod will automatically copy oo2core_6_win64.dll from the default C:\ Elden Ring install
  location. You may need to manually copy oo2core_6_win64.dll if you use a different install
  location.

Finally, edit rotconfig.ini and run RotMod.exe. By default, rotconfig.ini inflicts perma-rot.
You can adjust the build-up rate and build-up amount so it doesn't immediately apply.
The rot strength can also be adjusted to match any rot in the game. By default, it's Swamp
of Aeonia rot. Other rot tick amounts are listed in the config file for reference.

If there is an error, it will show an error message followed by a stack trace. Most likely
this will be because the file setup is incorrect.

The edited regulation.bin and common.emevd.dcx can be a modded one, in which case the status 
ffect will be merged into it. This mod only edits SpEffectParam in regulation.bin.

The final step is to configure Mod Engine 2 to load the files. You can do this by editing
config_eldenring.toml so that the first line references the mod directory, as follows
(do not edit the commented-out lines starting with #):

    { enabled = true, name = "default", path = "rot" },
