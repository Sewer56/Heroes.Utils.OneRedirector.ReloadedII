<div align="center">
	<h1>Sonic Heroes ONE Archive Redirector</h1>
	<img src="https://i.imgur.com/BjPn7rU.png" width="150" align="center" />
	<br/> <br/>
	<strong>Game can load ONE archives from memory?<br/>
	No problem, here's another one at the FileSystem level.<br/></strong>
<b>Id: sonicheroes.utils.oneredirector</b>
</div>

# ONE Redirector

Archived. Superseded by [FileEmulationFramework](https://github.com/Sewer56/FileEmulationFramework).

This is an experimental project and completely functional proof of concept. This mod creates .ONE archives, "Just in Time", as the game requests to read them from disk. Using some hooks, we then make the game load the file from memory instead of from disk.

Essentially, this allows for loading of files from outside .ONE archives, allowing mods to mix and match files rather than having one mod's files replace another mod's outright.

All done with *ZERO* knowledge of game code, for fun.

# Table of Contents
- [Supported Applications](#supported-applications)
- [How to Use](#how-to-use)
		- [How to Use: Adding/Replacing Files](#how-to-use-addingreplacing-files)
		- [How to Use: Removing Files](#how-to-use-removing-files)
		- [Developing Mods: A Tip](#developing-mods-a-tip)
- [How it Works](#how-it-works)

# Supported Applications

- Sonic Heroes (PC)

Probably works with some standalone applications (e.g. `HeroesONE-R`) and the console versions of the game, if emulated and ran from filesystem.

# How to Use

A. Add a dependency on this mod in your mod configuration.

```json
"ModDependencies": ["sonicheroes.utils.oneredirector"]
```

B. Add a folder called `OneRedirector` in your mod folder.

C. Make folders corresponding to ONE Archive names, e.g. `s01_h.one`.

Then, simply place files in the directory.

### How to Use: Adding/Replacing Files
In order to add (or replace an existing) file, simply place the file in your ONE directory.
Both PRS compressed and uncompressed files are supported.

Uncompressed files should have the full name and extension of the file `SHADOW_LOCATOR.DFF`.
Compressed files should end with the additional extension, `.prs` e.g. `SHADOW_LOCATOR.DFF.PRS`.

### How to Use: Removing Files
In order to delete a file, create an empty file with the name of the file and an additional extension `.del`.

e.g. Adding `SHADOW_LOCATOR.DFF.DEL` would prevent `SHADOW_LOCATOR.DFF` from being added to the ONE archive.

Note: The archive builder works in the order `Delete` then `Add`, so if a file is first deleted, it can be re-added by either the same or another mod.

### Developing Mods: A Tip
Disabled by default to improve performance (caching), the configuration has a setting allows for the replacement of files as the game is running exiting the game  (i.e. New ONE archive is created every time it is requested from disk.)

This means that, when enabled, if you e.g. replace the files for a character model, exit the stage and start the stage, the new model would load.

Good luck 😛

# How it Works

This project is a spinoff of my [AFS FileSystem Redirector](https://github.com/Sewer56/AfsFsRedir.ReloadedII).

It is somewhat simplified, gimped here as the file is read in all at once and as such I don't have to worry about the file being read in chunks.

Many of the basic principles from before still apply. Only large conceptual difference is we actually build the ONE archive and keep it in memory to feed to the game, as generally ONE archives are very small < 3MB.
