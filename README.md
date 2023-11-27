# LC Sound Tool
Simple Lethal Company sound tool and debugger. Patches all native Unity AudioSource components allowing you to dynamically replace (almost) any sound in the game at the final stages of playback by just specifying a new audio clip and the original clips name to this mod. Let's you easily load your own custom .wav sound files for your own mods and optionally allows logging all AudioSources playback, including PlayOneShot, PlayClipAtPoint etc. and the names of each clip playing into the BepInEx console when you press the F5 key. More in depth logging can be toggled with LeftAlt + F5.

## ATTENTION
This mod won't work without a mod of your own that does the replacing. This mod is just purely a tool/API for replacing and loading sound files from the game folder. For replacing without a mod of your own you can try the following seperate mod utilizing this tool: https://thunderstore.io/c/lethal-company/p/Clementinise/CustomSounds/

## Features

- Press F5 to log all audio playback to BepInEx console.
- Load your own custom .wav audio files for your mod
- Replace any audio clip with another one.


## Installation

Install like any other BepInEx mod. Install to the following directory:

```
  \GAME_LOCATION\Lethal Company\BepInEx\plugins
```
    
## Usage/Examples

Loading a custom sound .wav file:
```csharp
using LCSoundTool;

AudioClip newSound;

// 'test.wav' is the name of the .wav sound file you're going to load 
// 'YourModDirectory' is the name of your mods installation folder which mod managers create
// This is usually the format of 'Author-ModName', if you're unsure check how your mod manager installs mods
// As an example, this mod is installed as 'no00ob-LCSoundTool'
// This is because the author on this website is listed as me 'no00ob' and my mod is called 'LCSoundTool' here
// 'SubFolder' is the OPTIONAL folder inside the 'YourModDirectory' folder where the mod will try to load the sound files from
// You can also leave this blank and it will just load them from the root where your mod's DLL file is also located.

newSound = SoundTool.GetAudioClip("YourModDirectory", "SubFolder", "test.wav");
```
Adding a replacement sound:
```csharp
using LCSoundTool;

// Your own logic for the new sound
// This could be what is shown above or you could load it from an AssetBundle

AudioClip newSound;

// GhostDevicePing is the name of the original sound
// In this case it is the radar ping sound

SoundTool.ReplaceAudioClip("GhostDevicePing", newSound); 
```
Removing a replacement sound:
```csharp
using LCSoundTool;

// GhostDevicePing is the name of the sound we replaced 
// In this case it is the radar ping sound
// Which we now restore back to the default vanilla sound

SoundTool.RestoreAudioClip("GhostDevicePing");
```
For more in-depth example see the following github repo: https://github.com/no00ob/CustomPingSound
## FAQ

#### Why are none of the logs showing up?

Make sure you have at least the following BepInEx.cfg settings:
- [Chainloader] HideManagerGameObject = true
- [Logging.Console] Enabled = true
- [Logging.Console] LogLevels = Fatal, Error, Info, Debug, Warning
- Possibly [Logging.Disk] WriteUnityLog = false

If they're still not showing up just shoot me a msg in Discord (@no00ob) and we can try to figure it out.

#### Why does this one sound not show up in log and I cannot replace it?

Few of the AudioSources with playOnAwake do not work with this tool. They for some reason refuse to work. Might try to look at this later but for now check the bottom of the page for all the culprits.

#### Can you use this tool to record sounds or interact with the voice chat?

No, not at the moment. I might look into this later.

#### What is the performance impact of this tool compared to just manual replacement?

Idk, haven't done any major testing. In theory this should add slight delay and overhead to any audio playback, this game is so messily programmed however that finding certain audio clips was painful enough where I just decided that a tool like this might come in handy.

#### Can you add this or that?

Depends totally on what you're asking. If you want a new feature that has something to do with this mods original scope feel free to shoot me a message and I'll take a look. Otherwise no.

#### Does this mod work with other Unity games?

Any version prior to 1.2.0 should and versions past that if the game uses Unity's Netcode for GameObjects. I tried it with my Unity 2022.3.x game and it did work for it too. The mods replacing the audio do not however, as different games use different sounds obviously.

#### Can I contribute somehow?

Yes. If you find any bugs or errors let me know and feel free to send pull requests my way if you feel like you can provide better programming or more features.

## Known Issues

- Sometimes some AudioSources escape the logging. It seems to be mostly once that are either already playing before logging with loop set to true (intended behaviour) or ones that have playOnAwake set to true
- The following AudioSources cannot be edited with this mod currently: all 4 of the ship's ThrusterCloseAudio, the ship's ThrusterAmbientAudio and the ship's Ship3dSFX. These AudioSource's break and refuse to play anything if handled through my mod's custom playOnAwake handling.