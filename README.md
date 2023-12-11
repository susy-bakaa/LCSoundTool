# LC Sound Tool
This mod is by default **FULLY CLIENT SIDE**, but if you enable the networking feature in the config it requires everyone to have it installed.

Simplistic Lethal Company sound tool and debugger. Patches all native Unity AudioSource components allowing you to dynamically replace (almost) any sound in the game at the final stages of playback with one or multiple random options with specified chances by simply specifying new audio clip(s) and the original clip's name to this mod. Let's you easily load your own custom .wav sound files for your own mods. Experimental networking for sending and syncing audio clips across all of the connected players. Lastly, optionally allows logging all AudioSources playback, including PlayOneShot, PlayClipAtPoint etc. and the names of each clip playing into the BepInEx console when you press the F5 key. More in depth logging can be toggled with LeftAlt + F5.

## ATTENTION
This mod won't work without a mod of your own that does the replacing. This mod is just purely a tool/API for replacing and loading sound files from the game folder. For replacing without a mod of your own you can try the following seperate mod utilizing this tool: https://thunderstore.io/c/lethal-company/p/Clementinise/CustomSounds/

## Features

- Press F5 to log all audio playback to BepInEx console.
- Load your own custom .wav audio files for your mod.
- Replace any audio clip with another one or multiple random ones.
- (Optional) Send audio clips over the network and optionally sync the hosts clips with all clients.

## Installation

Install like any other BepInEx mod, either manually or with mod manager of your choice.

When installing manually, install the mod to the following directory:

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
Adding a replacement sound with multiple random options:
```csharp
using LCSoundTool;

// Your own logic for the new sounds
// This could be what is shown above or you could load it from an AssetBundle
// When doing multiple random sounds you define the chance for each sound at the end of the .wav files name.
// Eg. In this case if newRandomSound1 was called random-25.wav and newRandomSound2 was called random-75.wav
// newRandomSound1 would have a chance of 25% and newRandomSound2 would have a chance of 75%

AudioClip newRandomSound1;
AudioClip newRandomSound2;

// GhostDevicePing is the name of the original sound
// In this case it is the radar ping sound

SoundTool.ReplaceAudioClip("GhostDevicePing", newRandomSound1);
SoundTool.ReplaceAudioClip("GhostDevicePing", newRandomSound2); 
```
Removing a replacement sound:
```csharp
using LCSoundTool;

// GhostDevicePing is the name of the sound we replaced 
// In this case it is the radar ping sound
// Which we now restore back to the default vanilla sound

SoundTool.RestoreAudioClip("GhostDevicePing");
```
Remember that to be able to utilize the networking features you need to enable the networking in the mod config file! If your mod makes use of the networking you should mention this to the users on the mod page so they know it isn't only clientside and they need to turn this config feature on.

Networked audio clips:
```csharp
using LCSoundTool;

// This is the dictionary you can use to access all networked audio clips.

SoundTool.networkedClips;

// Your own logic for the new sound
// This could be what is shown above or you could load it from an AssetBundle

AudioClip networkedSound;

// You can use this method to send a networked audio clip to all clients that have LCSoundTool installed.
// It will be accessible through the variable specified above after the clients receive it.

SoundTool.SendNetworkedAudioClip(networkedSound);

// You can use this method to remove a networked audio clip from all clients that have LCSoundTool installed.
// It will be removed from the dictionary that is shown above after the clients receive the request for removal.

SoundTool.RemoveNetworkedAudioClip(networkedSound);

// Lastly with this method you can send all of the current clips stored in networkedClips dictionary to all clients.
// All of the hosts networked audio clips will be sent to all other clients that have LCSoundTool installed.

SoundTool.SyncNetworkedAudioClips();

// You can also use the following events to determine when the networkedClips dictionary has changed. 
// Check the example github repo for an idea on how this can be utilized.

SoundTool.ClientNetworkedAudioChanged;
SoundTool.HostNetworkedAudioChanged;
```

For more in-depth example see the following github repo: https://github.com/no00ob/LCSoundToolTest
## FAQ

#### Why are none of the logs showing up?

Make sure you have at least the following BepInEx.cfg settings:
- [Chainloader] HideManagerGameObject = true
- [Logging.Console] Enabled = true
- [Logging.Console] LogLevels = Fatal, Error, Info, Debug, Warning
- Possibly [Logging.Disk] WriteUnityLog = false

If nothing else works you can try my BepInEx.cfg from here: https://pastebin.com/LdsPhH5U

If they're still not showing up just shoot me a msg in Discord (@no00ob) and we can try to figure it out.

#### Why does this one sound not show up in log and I cannot replace it?

Few of the AudioSources with playOnAwake do not work with this tool. They for some reason refuse to work. Might try to look at this later but for now check the bottom of the page for all the culprits.

#### Why is the networking not working?

Make sure you turn the feature on in the mod config. When networking is turned on everyone needs LCSoundTool installed to join a lobby or else you will get an error.

#### Can you use this tool to record sounds or interact with the voice chat?

No, not at the moment. I might look into this later.

#### What is the performance impact of this tool compared to just manual replacement?

Idk, haven't done any major testing. In theory this should add slight delay and overhead to any audio playback, this game is so messily programmed however that finding certain audio clips was painful enough where I just decided that a tool like this might come in handy.

#### Can you add this or that?

Depends totally on what you're asking. If you want a new feature that has something to do with this mods original scope feel free to shoot me a message and I'll take a look. Otherwise no.

#### Does this mod work with other Unity games?

Any version prior to 1.2.0 should and versions past that will not work. I tried the pre 1.2.0 version with my Unity 2022.3.x game and it did work for it too. The mods replacing the audio do not however, as different games use different sounds obviously.

#### Can I contribute somehow?

Yes. If you find any bugs or errors let me know and feel free to send pull requests my way if you feel like you can provide better programming or more features.

## Config

The config can be found from: `\GAME_LOCATION\Lethal Company\BepInEx\config\LCSoundTool.cfg`

or if you're using mod managers you can find the config from here: [Example](https://i.imgur.com/OZAgeNL.png)

```
## Settings file was created by plugin LC Sound Tool v1.3.2
## Plugin GUID: LCSoundTool

[Experimental]

## Whether or not to use the networking built into this plugin. If set to true everyone in the lobby needs LCSoundTool to join.
# Setting type: Boolean
# Default value: false
EnableNetworking = false
```

## Known Issues

- Sometimes some AudioSources escape the logging. It seems to be mostly once that are either already playing before logging with loop set to true (intended behaviour) or ones that have playOnAwake set to true
- The following AudioSources cannot be edited with this mod currently: all 4 of the ship's ThrusterCloseAudio, the ship's ThrusterAmbientAudio and the ship's Ship3dSFX. These AudioSource's break and refuse to play anything if handled through my mod's custom playOnAwake handling.