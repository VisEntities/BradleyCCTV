# See everything, miss nothing with CCTV cameras!
No more blindly heading to the launch site completely clueless about who might be hiding there, just waiting to pounce on you.

![](https://i.imgur.com/ZutJPx4.png)

----

## Installation
Download the plugin and place it in your server's oxide/plugins directory. Once loaded, the plugin will automatically scan the map for existing Bradleys and attach CCTV cameras on them.

----

## How it works

### Placement
Each Bradley can have a maximum of two cameras, positioned at the front or back, with the ability to customize their view angles, positions, and rotations to your liking.

----

### Vulnerability
The cameras are just as vulnerable as the Bradleys themselves, so if a player destroys one, it will not respawn. Additionally, players can also pick up the cameras, although it's quite a feat to get that close to a Bradley.

----

### Unique identifiers
For each CCTV camera deployed, a unique random identifier will be generated. These identifiers will always be prefixed with **BRADLEY** followed by a random number, allowing you to deploy cameras on as many Bradleys as you desire.

![](https://i.imgur.com/EnDkfqK.png)

----

Players can also view the identifiers using a command, which will drop a note in the player's inventory containing a list of active camera identifiers.

![](https://i.imgur.com/qUBCssj.png)

----

## Permission
* `bradleycctv.use` - Allows players to use the chat command to view a list of active camera identifiers.

## Chat Command
* `bradley.cctv` - Use this command to receive a note in your inventory listing all active camera identifiers.


## Configuration
```json
{
  "Version": "2.0.0",
  "Front Camera": {
    "Enabled": true,
    "Static": true,
    "Up Down Rotation": 4.0,
    "Right Left Rotation": 0.0,
    "Position": {
      "x": -0.03,
      "y": 1.8,
      "z": 2.15
    },
    "Rotation": {
      "x": 0.0,
      "y": 0.0,
      "z": 0.0
    }
  },
  "Back Camera": {
    "Enabled": true,
    "Static": true,
    "Up Down Rotation": -3.0,
    "Right Left Rotation": 0.0,
    "Position": {
      "x": 0.0,
      "y": 1.7,
      "z": -2.93
    },
    "Rotation": {
      "x": 13.0,
      "y": 180.0,
      "z": -5.22274552E-14
    }
  }
}
```

* `Enabled` - Whether the specified camera will spawn or not.
* `Static` - Determines whether the camera direction should be static or changeable by players.
* `Up Down Rotation` - The angle at which the camera should be pointed up or down.
* `Right Left Rotation` - The angle at which the camera should be pointed right or left.
* `Position` - The position of the camera relative to the Bradley.
* `Rotation` - The rotation of the camera relative to the Bradley.

----

## Uninstallation
When the plugin is unloaded, any attached cameras will automatically be removed.

---


## Keep the mod alive
Creating plugins is my passion, and I love nothing more than exploring new ideas and bringing them to the community. But it takes hours of work every day to maintain and improve these plugins that you have come to love and rely on.

With your support on [Patreon](https://www.patreon.com/VisEntities), you're  giving me the freedom to devote more time and energy into what I love, which in turn allows me to continue providing new and exciting updates to the community.

![](https://i.imgur.com/EZiy53h.png)

A portion of the contributions will also be donated to the uMod team as a token of appreciation for their dedication to coding quality, inspirational ideas, and time spent for the community.

----

## Credits
* Originally created by **bearr**, up to version 1.0.0
* Thank you, **WhiteThunder**, for being an inspiration through your work.
* Completely rewritten from scratch and maintained to present by **Dana**.