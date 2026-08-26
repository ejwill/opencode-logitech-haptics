# Logitech Marketplace and OpenCode Branding Research

Date: 2026-08-21

## Sources

- [Logi Actions SDK documentation index](https://logitech.github.io/actions-sdk-docs/llms.txt)
- [Marketplace Approval Guidelines](https://logitech.github.io/actions-sdk-docs/marketplace-approval-guidelines/)
- [C# plugin distribution](https://logitech.github.io/actions-sdk-docs/csharp/plugin-development/distributing-the-plugin/)
- [OpenCode brand assets](https://github.com/anomalyco/opencode/tree/dev/packages/console/app/src/asset/brand)
- [OpenCode repository license](https://github.com/anomalyco/opencode/blob/dev/LICENSE)

## Confirmed Logitech requirements

The Marketplace guidelines say that:

- Logitech manually reviews every new Digital Product and every update before publication.
- The plugin must be tested with the supported hardware and software.
- The plugin icon must be present in the package `metadata` folder.
- The package must be a valid `.lplug4` file and include `metadata/LoupedeckPackage.yaml`.
- External links in the Marketplace listing must be reachable and relate to the product or author.
- The plugin must be compatible with Logitech Plugin Service and install successfully.
- The developer must accept the Marketplace Developer Agreement.
- The developer must provide a Developer EULA.
- Developer-created open-source content must use Apache 2.0 or MIT and comply with that license; GPL2/GPL3 content is not accepted.

The SDK distribution page additionally confirms that `.lplug4` is the package format, that the icon belongs under `metadata/`, and that `logiplugintool verify` is the documented package validation command.

## Branding and icon implications

The Logitech documents do not provide an exception or special permission for third-party trademarks. They require the icon and package metadata, but they do not determine whether an OpenCode logo may be reused.

The OpenCode repository is MIT-licensed and publishes official square logo assets in its brand directory. That confirms the assets are publicly available, but the repository license alone should not be treated as conclusive proof that the artwork and trademarks have unrestricted third-party usage terms.

For OpenCode Companion, the release-safe approach is:

1. Use the official OpenCode square logo only if its asset terms are confirmed or the maintainers approve the use.
2. Keep the package name `OpenCode Companion`, not `OpenCode`, to communicate compatibility rather than ownership.
3. Keep an explicit disclaimer that the plugin is independent and not affiliated with or endorsed by OpenCode.
4. Attribute OpenCode if an official logo asset is included or adapted.
5. Add the required Developer EULA before Marketplace submission.
6. Rebuild, install-test, and run `logiplugintool verify` after changing the icon or metadata.

## Current decision

The generated icon variants did not communicate OpenCode clearly enough. The selected direction is the official OpenCode square asset as the primary plugin icon, used unchanged inside a transparent 256×256 canvas. The independent-companion name and disclaimer carry the integration context. This is a branding decision based on the published asset and compatibility use, not a conclusion that the MIT repository license automatically grants unrestricted trademark permission.

## Logitech icon requirements

The SDK's plugin structure documentation identifies the plugin icon as:

- `metadata/Icon256x256.png`
- A required metadata asset for the standard C# package layout
- Displayed in Options+, Loupedeck, and the Logitech Marketplace web UI

The package metadata can optionally override the default path with `icon256x256`, but the default `metadata/Icon256x256.png` path is the simplest choice and is already used by this repository.

The Marketplace icon guidance adds these image constraints:

- The PNG canvas must be 256×256 pixels.
- The actual graphic must fit within a centered 192×192 pixel area.
- The remaining 32-pixel border must be transparent.

This is distinct from action symbols. Action symbols are small SVG files in `actionsymbols/` and identify individual actions in the action picker. The Marketplace/plugin icon should therefore be optimized for square thumbnail recognition, while action symbols can remain action-specific.

The documentation also lists optional `backgroundColor`, `foregroundColor`, and `textColor` metadata fields. We should not add those unless we need them; the icon itself should carry its background and contrast consistently across Marketplace and Options+.
