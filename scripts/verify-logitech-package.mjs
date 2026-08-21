#!/usr/bin/env node

import { spawnSync } from "node:child_process"

const packagePath = process.argv[2]

if (!packagePath) {
  console.error("Usage: node scripts/verify-logitech-package.mjs <package.lplug4>")
  process.exit(2)
}

const tool = ["tool", "run", "logiplugintool"]

function run(args) {
  const result = spawnSync("dotnet", [...tool, ...args], {
    encoding: "utf8",
    env: { ...process.env, DOTNET_ROLL_FORWARD: "Major" },
  })

  if (result.error) throw result.error
  if (result.status !== 0) {
    process.stderr.write(result.stderr || result.stdout || "")
    process.exit(result.status ?? 1)
  }

  return result.stdout
}

function listArchive() {
  const result = spawnSync("unzip", ["-Z1", packagePath], { encoding: "utf8" })
  if (result.error || result.status !== 0) {
    console.error("Unable to inspect .lplug4 archive contents")
    process.exit(1)
  }
  return result.stdout.split(/\r?\n/).filter(Boolean)
}

run(["verify", packagePath])
const archiveEntries = listArchive()
if (archiveEntries.includes("bin/PluginApi.dll")) {
  console.error("Package must not include host-provided bin/PluginApi.dll")
  process.exit(1)
}
const metadataOutput = run(["metadata", packagePath])
const metadataStart = metadataOutput.indexOf("{")
const metadataEnd = metadataOutput.lastIndexOf("}") + 1
if (metadataStart < 0) {
  console.error("Logi Plugin Tool did not return JSON metadata")
  process.exit(1)
}
const metadata = JSON.parse(metadataOutput.slice(metadataStart, metadataEnd))

const required = [
  ["name", metadata.name],
  ["displayName", metadata.displayName],
  ["version", metadata.version],
]

for (const [field, value] of required) {
  if (typeof value !== "string" || value.length === 0) {
    console.error(`Package metadata is missing ${field}`)
    process.exit(1)
  }
}

for (const [field, expected] of [
  ["isWindowsSupported", true],
  ["isMacSupported", true],
  ["isUniversalPlugin", true],
]) {
  if (metadata[field] !== expected) {
    console.error(`Package metadata ${field}=${JSON.stringify(metadata[field])}; expected ${JSON.stringify(expected)}`)
    process.exit(1)
  }
}

console.log(`logitech-package=ok name=${metadata.name} version=${metadata.version}`)
