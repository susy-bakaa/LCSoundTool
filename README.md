# LC Sound Tool
Simple Lethal Company sound tool and debugger. Patches all native Unity AudioSource components allowing you to dynamically replace any sound in the game at the final stages of playback by just specifying a new audio clip and the original clips name to this mod. Optionally allows logging all AudioSources playback, including PlayOneShot, PlayClipAtPoint etc. and the names of each clip into the BepInEx console when you press the F5 key.

## Features

- Press F5 to log all audio playback to BepInEx console.
- Replace any audio clip with another one.


## Installation

Install like any other BepInEx mod. Install to the following directory:

```
  \GAME_LOCATION\Lethal Company\BepInEx\plugins
```
    
## Usage/Examples

Adding a replacement sound:
```csharp
using LCSoundTool;

AudioClip newSound; // your logic for the new sound

SoundTool.ReplaceAudioClip("GhostDevicePing", newSound); // GhostDevicePing is the name of the original sound in this case the radar ping sound
```
Removing a replacement sound:
```csharp
using LCSoundTool;

SoundTool.RestoreAudioClip("GhostDevicePing"); // GhostDevicePing is the name of the sound we replaced and in this case the radar ping sound which we now restore back to default
```
For more in-depth example see the following github repo: https://github.com/no00ob/CustomPingSound
## FAQ

#### Why are none of the logs showing up?

Make sure you have the following BepInEx.cfg settings:
- [Chainloader] HideManagerGameObject = true
- [Logging.Console] Enabled = true
- [Logging.Console] LogLevels = Fatal, Error, Info, Debug
- Possibly [Logging.Disk] WriteUnityLog = false

If they're still not showing up just shoot me a msg in Discord (@no00ob) and we can try to figure it out.

#### Can you use this tool to record sounds or interact with the voice chat?

No, not at the moment. I might look into this later.

#### What is the performance impact of this tool compared to just manual replacement?

Idk, haven't done any major testing. In theory this should add slight delay and overhead to any audio playback, this game is so messily programmed however that finding certain audio clips was painful enough where I just decided that a tool like this might come in handy.

#### Can you add this or that?

Depends totally on what you're asking. If you want a new feature that has something to do with this mods original scope feel free to shoot me a message and I'll take a look. Otherwise no.

#### Does this mod work with other Unity games?

Surprisingly it should work actually. I tried it with my Unity 2022.3.x game and it did work for it too. The mods replacing the audio do not however, as different games use different sounds obviously.

#### Can I contribute somehow?

Yes. If you find any bugs or errors let me know and feel free to send pull requests my way if you feel like you can provide better programming or more features.