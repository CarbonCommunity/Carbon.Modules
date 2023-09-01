<p align="center">
  <img src="https://codefling.com/uploads/monthly_2023_03/image.thumb.png.276343ad1b15a658368a7ae6e252172f.png" />
  <hr />
</p>

Carbon is a self-updating, lightweight, intelligent mod loader for Rust utilizing the latest C# and Harmony for the best performance and stability possible. Its robust framework and backward compatibility with Oxide plugins make it the ultimate replacement for those wanting better functionality and performance from their plugins!

Carbon has all the creature comforts you need to run your server, such as a permission system, user system, and so much more. Carbon is developed by experienced developers and server owners working to take the tedium out of hosting servers and make configuration and setup seamless with an integrated GUI in-game to manage everything!

## :electric_plug: Modules

This repository contains the following:
- [**Gather Manager**](https://github.com/CarbonCommunity/Carbon.Modules/tree/main/Carbon.Modules/src/GatherManagerModule) module
  - The module allows you to modify the processing modifiers of Quarries, Excavators, Pickup and Gather amounts, globally set or item-specific. This module comes with a variety of useful tools, including custom recycler speed.
  - Check out documentation for [more info](https://docs.carbonmod.gg/docs/optional-modules/gather-manager-module)!
- [**Stack Manager**](https://github.com/CarbonCommunity/Carbon.Modules/tree/main/Carbon.Modules/src/StackManagerModule) module
  - High performance will allow to set custom item stacks based on item name, category and blacklisted items (useful when using categories).
  - Check out documentation for [more info](https://docs.carbonmod.gg/docs/optional-modules/stack-manager-module)!
- [**Vanish**](https://github.com/CarbonCommunity/Carbon.Modules/tree/main/Carbon.Modules/src/VanishModule) module
  - A very lightweight auth-level based system allowing you to become invisible, with various settings in the config file.
  - Check out documentation for [more info](https://docs.carbonmod.gg/docs/optional-modules/vanish-module)!
- [**Whitelist**](https://github.com/CarbonCommunity/Carbon.Modules/tree/main/Carbon.Modules/src/WhitelistModule) module
  - A very basic system that only grants players access to a server based on the 'whitelist.bypass' permission or 'whitelisted' group.
  - Check out documentation for [more info](https://docs.carbonmod.gg/docs/optional-modules/whitelist-module)!
- [**DRM**](https://github.com/CarbonCommunity/Carbon.Modules/tree/main/Carbon.Modules/src/DRMModule) module
  - A system that allows server hosts to bind endpoints that deliver plugin information with respect to the public and private keys.
  - Check out documentation for [more info](https://docs.carbonmod.gg/docs/optional-modules/drm-module)!
- [**Moderation Tools**](https://github.com/CarbonCommunity/Carbon.Modules/tree/main/Carbon.Modules/src/ModerationToolsModule) module
  - This module is a bundle of very helpful and often usable moderation tools that can grant the ability to players with regular authority level to use noclip and god-mode and nothing else (use the 'carbon.admin' permission to allow the use of the '/cadmin' command).
  - Check out documentation for [more info](https://docs.carbonmod.gg/docs/optional-modules/moderation-tools-module)!
- [**Map Protection**](https://github.com/CarbonCommunity/Carbon.Modules/tree/main/Carbon.Modules/src/MapProtectionModule) module (**WIP!**)
- [**Auto-Wipe**](https://github.com/CarbonCommunity/Carbon.Modules/tree/main/Carbon.Modules/src/AutoWipeModule) module (**WIP!**)
- [**Optimisations**](https://github.com/CarbonCommunity/Carbon.Modules/tree/main/Carbon.Modules/src/OptimisationsModule) module (**WIP!**)

## :package: Download

Start using Carbon today, download the latest version from our [releases page][production].
We also provide a [quick start script][quick-start] to get your server running in minutes, available for Windows and Linux.

## :blue_book: Documentation

For more in-depth Carbon documentation, from builds and deployment, check [here][documentation].
Find all currently available hooks [here][hooks].
If you are a developer take a look at our [Wiki page][wiki].

## :question: Support

Join our official [Discord server][discord] for support, more frequent development info, discussions and future plans.

## :heart: Sponsor

If you would like to [sponsor][patreon] the project the best way is to use [Patreon].

We would like to thank everyone who sponsors us.


[hooks]: https://carboncommunity.gitbook.io/docs/core/hooks/carbon-hooks
[wiki]: https://github.com/CarbonCommunity/Carbon.Core/wiki
[discord]: https://discord.gg/eXPcNKK4yd
[documentation]: https://carboncommunity.gitbook.io/docs
[patreon]: https://patreon.com/CarbonCommunity
[production]: https://github.com/CarbonCommunity/Carbon.Core/releases/tag/production_build
[quick-start]: https://github.com/CarbonCommunity/Carbon.QuickStart
