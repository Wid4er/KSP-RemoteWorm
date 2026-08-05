# RemoteTech Wormhole Bridge (RTWB)

**Beta mod for Kerbal Space Program 1, version 1.12.5.**

RemoteTech Wormhole Bridge allows **RemoteTech signals to travel through
Kopernicus Expansion (KEX) wormholes**. This lets you maintain communication
networks across star systems without antennas that reach for light-years or
impractically long interstellar signal delays.

Current version: **0.5.0-beta.1**.

## What it does

Without RTWB, RemoteTech tries to communicate across the real distance between
star systems. Even after a vessel travels through a wormhole, its signal cannot
use the wormhole.

RTWB lets you:

- build RemoteTech relays around each wormhole mouth;
- send communications through the wormhole;
- let RemoteTech automatically choose the best route for each vessel;
- retain RemoteTech remote control, routing, and signal delay;
- see active links and potential coverage areas in the map view;
- retarget antennas both in flight and from the Tracking Station.

You do not need to pair two specific relays manually. RTWB discovers every
compatible exit, and RemoteTech chooses the most suitable route for the origin
of each signal.

## Dependencies

You need:

- **Kerbal Space Program 1.12.5**;
- **RemoteTech 1.9.12**;
- **Kopernicus Expansion Continued-er**, including `KEX-Wormholes`;
- KEX dependencies, including **Kopernicus**;
- **HarmonyKSP**;
- **ModuleManager**.

**WormholeSignalBridge is not a dependency.** It can coexist with RTWB, but it
targets CommNet/RealAntennas and does not participate in the RemoteTech network.

## Download and installation

1. Download the ZIP from the
   [Releases page](https://github.com/Wid4er/KSP-RemoteWorm/releases).
2. Extract it directly into the KSP root directory.
3. Verify that the plugin is installed at:

```text
Kerbal Space Program/
└── GameData/
    └── RemoteTechWormholeBridge/
        └── Plugins/
            └── RemoteTechWormholeBridge.dll
```

The ZIP contains RTWB only. It does not include KSP or any dependency DLLs.

## Setting up a link

1. Place a relay with a RemoteTech directional antenna around each mouth of the
   same wormhole.
2. Target each antenna at its local wormhole body.
3. Keep both antennas active and powered.
4. Place each relay between **100 and 300 km from the KEX transition surface**.
5. Position the relays in compatible regions around both mouths so that each
   falls within the cone projected by the other.

For the `WH3141A` and `WH3141B` wormholes in Kcalbeloh System, the valid band is
approximately **135 to 335 km altitude** above the body.

Once the geometry is valid, the link appears automatically and RemoteTech can
use it for routing. If several relays are compatible, RemoteTech chooses the
lowest-cost complete route for each vessel.

## Visual indicators

In map view or the Tracking Station, enable RemoteTech's dish and cone filters:

- **magenta lines** represent active wormhole links;
- **magenta cones** show where a compatible relay could be placed;
- each cone ends 300 km from the transition surface;
- RTWB never draws a line across the interstellar distance between the systems.

## Beta status

This version has been tested with KSP 1.12.5, RemoteTech 1.9.12, and
KEX-Wormholes 1.0. Verified support includes:

- loaded and unloaded relays;
- flight and the Tracking Station;
- power loss and recovery;
- antenna retargeting;
- docking, undocking, and vessel destruction;
- multiple relays around the same pair of wormhole mouths.

As this is a beta, back up your save before installing it.

## Technical documentation

Architecture, inspected APIs, build instructions, and development tests are
documented under [docs/](docs/).

## License

RTWB is licensed under [GNU GPL v3.0 only](LICENSE). KSP, RemoteTech, KEX, and
Harmony DLLs are external dependencies and are not redistributed with this
project.
