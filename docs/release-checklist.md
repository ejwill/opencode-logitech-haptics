# Release Checklist

Use this checklist to cut a GitHub Release after CI and hardware validation are ready.

## 1. Confirm release readiness

- [ ] Hardware validation completed or intentionally deferred.
- [ ] `README.md` still accurately states hardware validation status.
- [ ] Version in `opencode-plugin/package.json` matches the intended tag.
- [ ] Version in `opencode-plugin-v2/package.json` matches the intended tag.
- [ ] `main` contains the commits you want to release.
- [ ] No tracked local changes:

```bash
git status --short
```

## 2. Run local verification

```bash
npm test
npm --workspace @opencode-logitech-haptics/core test
npm --workspace opencode-logitech-haptics-v2 test
```

```bash
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet build tests/PluginApiStubs/PluginApiStubs.csproj -c Release
dotnet build logitech-plugin/OpenCodeHapticsPlugin/OpenCodeHapticsPlugin.sln \
  -c Release \
  /p:SkipLogiDeploy=true \
  /p:PluginApiDir="$PWD/tests/PluginApiStubs/bin/Release/net10.0/"
PLUGIN_DLL_PATH="$PWD/logitech-plugin/OpenCodeHapticsPlugin/bin/Release/bin/OpenCodeCompanionPlugin.dll" \
  dotnet run --project tests/ServerSmokeTest/ServerSmokeTest.csproj -c Release
node scripts/package-logitech.mjs \
  logitech-plugin/OpenCodeHapticsPlugin/bin/Release/ \
  artifacts/logitech \
  OpenCodeCompanion_0_1_0
node scripts/verify-logitech-package.mjs artifacts/logitech/OpenCodeCompanion_0_1_0_marketplace.lplug4
node scripts/verify-logitech-package.mjs artifacts/logitech/OpenCodeCompanion_0_1_0.lplug4
```

Expected smoke output:

```text
complete=202:accepted
missing=400:missing event
invalid=400:invalid json
unknown=204:
get=404:not found
raised=opencodeComplete,opencodeTest,...
```

## 3. Push main

```bash
git push origin main
```

Wait for the normal CI workflow to pass on `main`.

## 4. Create and push the release tag

For version `0.1.0`:

```bash
git tag v0.1.0
git push origin v0.1.0
```

CI converts tag dots to underscores for the Logitech package name:

```text
v0.1.0 -> OpenCodeCompanion_0_1_0.lplug4
```

## 5. Verify GitHub Actions

In GitHub Actions, confirm the tag run completed:

- [ ] `OpenCode plugin packages` job passed.
- [ ] `Logitech plugin` job passed.
- [ ] `GitHub release` job passed.

## 6. Verify GitHub Release assets

The release for `v0.1.0` should contain:

- [ ] generated release notes
- [ ] OpenCode npm package: `opencode-logitech-haptics-0.1.0.tgz`
- [ ] OpenCode v2 npm package: `opencode-logitech-haptics-v2-0.1.0.tgz`
- [ ] Logitech direct-install package: `OpenCodeCompanion_0_1_0.lplug4`
- [ ] Logitech marketplace ZIP: `OpenCodeCompanion_0_1_0_marketplace.lplug4`

Download the `.lplug4` asset and verify it locally if possible:

```bash
node scripts/verify-logitech-package.mjs OpenCodeCompanion_0_1_0_marketplace.lplug4
```

## 7. If the release workflow fails

1. Read the failed GitHub Actions log.
2. Fix the workflow or project files on `main`.
3. Delete the failed local/remote tag if needed:

```bash
git tag -d v0.1.0
git push origin :refs/tags/v0.1.0
```

4. Recreate and push the tag after CI is green.

Do not manually upload replacement release artifacts unless you also record why the automated path failed.
