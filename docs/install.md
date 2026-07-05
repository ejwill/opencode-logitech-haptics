# Install Guide

This guide is for installing the packaged OpenCode + Logitech haptics bridge.

## Install artifacts

Use either local artifacts or GitHub Release assets:

- OpenCode plugin npm package: `opencode-logitech-haptics-0.1.0.tgz`
- Logitech plugin package: `OpenCodeHaptics_0_1_0.lplug4`

Local artifacts can be created with:

```bash
mkdir -p artifacts/npm artifacts/logitech
npm pack --workspace opencode-logitech-haptics --pack-destination artifacts/npm
cp OpenCodeHaptics_0_1_0.lplug4 artifacts/logitech/
```

## 1. Install the Logitech plugin

Prerequisites:

- Logi Options+ installed.
- Logi Plugin Service running.
- MX Master 4 / MX 4 paired with haptics enabled.

Install `OpenCodeHaptics_0_1_0.lplug4` using the normal Logi Options+ / Logi Plugin Service local plugin install flow.

After install, confirm the HTTP bridge is listening:

```bash
curl -i http://127.0.0.1:17844/haptic
```

Expected response:

```text
HTTP/1.1 404 Not Found
not found
```

Then trigger a manual haptic:

```bash
curl -i -X POST http://127.0.0.1:17844/haptic \
  -H "Content-Type: application/json" \
  --data '{"source":"manual","event":"test","message":"Manual install test"}'
```

Expected response:

```text
HTTP/1.1 202 Accepted
accepted
```

Expected Logitech event: `opencodeTest`.

## 2. Install the OpenCode plugin package

Install the local package tarball with npm:

```bash
npm install ./artifacts/npm/opencode-logitech-haptics-0.1.0.tgz
```

If your OpenCode setup loads plugins from local files instead of installed npm packages, point it at the repository/plugin entrypoint according to your OpenCode config conventions:

```text
opencode-plugin/src/index.js
```

## 3. Configure OpenCode for live testing

Use the committed live-test config:

```bash
OPENCODE_LOGITECH_HAPTICS_CONFIG="$PWD/opencode-plugin/examples/live-test-config.json" opencode
```

Default endpoint:

```text
http://127.0.0.1:17844/haptic
```

Override endpoint if needed:

```bash
LOGITECH_HAPTICS_URL="http://127.0.0.1:17844/haptic" opencode
```

## 4. Validate end-to-end

1. Start / reload Logi Plugin Service.
2. Confirm `curl -i http://127.0.0.1:17844/haptic` returns `404 not found`.
3. Confirm direct POST returns `202 accepted` and fires a haptic.
4. Start OpenCode with `OPENCODE_LOGITECH_HAPTICS_CONFIG` set.
5. Trigger an OpenCode session completion.
6. Confirm the MX Master 4 / MX 4 haptic fires.

For the full hardware checklist and troubleshooting, see [`hardware-validation.md`](hardware-validation.md).

## Uninstall / reset

- Remove the Logitech plugin through Logi Options+ / Logi Plugin Service.
- Remove the npm package from the OpenCode environment:

```bash
npm uninstall opencode-logitech-haptics
```

- Restart OpenCode and Logi Plugin Service.
