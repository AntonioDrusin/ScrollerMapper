
Processes the json file defining the game and outputs assembly and binaries with the assets.

The binary files include a header (FileStructureChip and FileStructureFast) with the offsets within the file to the "top level" asset structures.
Assembly code should have structure definitions for everything that is created by the mapper.

When loading a file you have to write code to adjust offsets to be pointers if that is what you need.