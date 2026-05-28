# Agent Instructions — Ultimate Glove Ball

Ultimate Glove Ball is a Unity multiplayer VR esports showcase for Meta Quest. It demonstrates Oculus Social / Platform APIs, Meta Avatars, Photon Realtime + Photon Voice (with Oculus Spatializer), in-app purchases, and asymmetric player/spectator experiences built on Unity Netcode for GameObjects. A published version is on the [Horizon Store](https://www.meta.com/en-gb/experiences/ultimate-glove-ball/5704438046269164/).

## Source-of-truth files (read these first, do not duplicate their contents in this file)

For setup, build steps, SDK versions, and project layout, read:

- `README.md` — official setup, dependencies, and editor-play instructions
- `Documentation/Configuration.md` — Meta Quest + Photon configuration (App IDs, IAPs)
- `Documentation/CodeStructure.md`, `Documentation/Multiplayer.md`, `Documentation/BallPhysicsAndNetworking.md`, `Documentation/IAP.md`, `Documentation/LightBaking.md`, `Documentation/Avatars.md` — system-level docs
- `ProjectSettings/ProjectVersion.txt` — pinned Unity editor version
- `Packages/manifest.json` — Unity package versions
- `.gitattributes` — Git LFS filters; run `git lfs install` before cloning
- `LICENSE`, `Assets/Oculus/LICENSE.txt`, `Packages/Photon/Photon/license.txt` — license terms (MIT for most; embedded Photon, TMP, Oculus Integration have their own)

## Quest / Horizon-specific notes

- The Photon Voice 2 package and the `com.community.netcode.transport.photon-realtime@<sha>` package are embedded under `Packages/` because they have been **modified** for this project. Do not bump versions in place — re-import from the Asset Store and re-apply the patches.
- Files under `Assets/Oculus/` follow the Oculus SDK License Agreement, separate from the project's MIT license. Leave license headers intact when refactoring.
- Some shared utility packages (Meta Utilities, Meta Input Utilities) are mirrored into [`meta-quest/Unity-UtilityPackages`](https://github.com/meta-quest/Unity-UtilityPackages); changes to those may want to land there first.
- Don't commit your Meta or Photon **App IDs / API keys**; configuration is per-developer per `Documentation/Configuration.md`.
- The bundled XR FPS Simulator captures the mouse during play mode; hold **Left Alt** to release it (see `Packages/com.meta.utilities.input/README.md#mouse-capture`).

## Meta Quest tooling

This repository is part of the Meta Quest / Horizon OS ecosystem (a sample, library, template, or related project — the bespoke intro above describes which). Use that intro and the source-of-truth files it references for project-specific decisions; don't restate or invent facts from memory.

When the user asks anything about Quest device behavior, build / deploy / debug / capture flows, on-device performance, or Horizon OS APIs, reach for these tools instead of generic Unity answers:

- **`hzdb`** — Quest-aware ADB wrapper (device list, install / launch / stop, logs, screenshots, Perfetto traces, on-device docs search). Already wired up as an MCP server via `.mcp.json`, `.vscode/mcp.json`, and `.cursor/mcp.json`. Also runnable directly: `npx -y @meta-quest/hzdb <subcommand>`.
- **Meta Quest Agentic Tools** — the full skill set, including Unity-specific skills: [github.com/meta-quest/agentic-tools](https://github.com/meta-quest/agentic-tools). Install per your client (Claude Code: `/plugin install meta-vr@meta-quest`; Gemini CLI: `gemini extensions install https://github.com/meta-quest/agentic-tools`; Cursor / VS Code: install the **Meta Horizon** extension from the Marketplace).

A few behavior expectations:

- **Read this repo's files first.** Before answering anything project-specific, read `README.md` and whichever source-of-truth files the intro above points at. Don't restate their contents in chat — quote or link instead.
- **Use `hzdb` for device-side work.** Anything that touches an attached Quest (install, launch, logs, screenshot, capture, manifest inspection) goes through `hzdb`, not raw `adb`.
- **Check live Horizon OS docs before answering API questions.** `hzdb docs search "..."` queries the live docs; training data on Horizon OS APIs goes stale fast.
- **Don't fabricate SDK / engine versions.** If a version isn't visible in this repo's files, say so rather than guessing.
