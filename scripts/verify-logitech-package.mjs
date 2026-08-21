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

function archiveFormat() {
  const result = spawnSync("file", ["-b", packagePath], { encoding: "utf8" })
  if (result.error || result.status !== 0) {
    console.error("Unable to identify .lplug4 archive format")
    process.exit(1)
  }
  if (result.stdout.includes("Zip archive")) return "zip"
  if (result.stdout.includes("tar archive")) return "tar"
  console.error(`Unsupported .lplug4 format: ${result.stdout.trim()}`)
  process.exit(1)
}

function listArchive(format) {
  const command = format === "zip" ? "unzip" : "tar"
  const args = format === "zip" ? ["-Z1", packagePath] : ["-tf", packagePath]
  const result = spawnSync(command, args, { encoding: "utf8" })
  if (result.error || result.status !== 0) {
    console.error("Unable to inspect .lplug4 archive contents")
    process.exit(1)
  }
  return result.stdout.split(/\r?\n/).map((entry) => entry.replace(/^\.\//, "")).filter(Boolean)
}

const format = archiveFormat()
if (format === "zip") run(["verify", packagePath])
const archiveEntries = listArchive(format)
for (const requiredEntry of [
  "metadata/LoupedeckPackage.yaml",
  "metadata/Icon256x256.png",
  "bin/OpenCodeCompanionPlugin.dll",
]) {
  if (!archiveEntries.includes(requiredEntry)) {
    console.error(`Package is missing ${requiredEntry}`)
    process.exit(1)
  }
}
if (archiveEntries.includes("bin/PluginApi.dll")) {
  console.error("Package must not include host-provided bin/PluginApi.dll")
  process.exit(1)
}
let metadata
if (format === "zip") {
  const metadataOutput = run(["metadata", packagePath])
  const metadataStart = metadataOutput.indexOf("{")
  const metadataEnd = metadataOutput.lastIndexOf("}") + 1
  if (metadataStart < 0) {
    console.error("Logi Plugin Tool did not return JSON metadata")
    process.exit(1)
  }
  metadata = JSON.parse(metadataOutput.slice(metadataStart, metadataEnd))
} else {
  const metadataOutput = spawnSync("tar", ["-xOf", packagePath, "metadata/LoupedeckPackage.yaml"], { encoding: "utf8" })
  if (metadataOutput.error || metadataOutput.status !== 0) {
    console.error("Tar package is missing metadata/LoupedeckPackage.yaml")
    process.exit(1)
  }
  metadata = Object.fromEntries(
    metadataOutput.stdout
      .split(/\r?\n/)
      .map((line) => line.match(/^(name|displayName|version):\s*(.+)$/))
      .filter(Boolean)
      .map(([, key, value]) => [key, value.trim()]),
  )
  metadata.isWindowsSupported = /^pluginFolderWin:\s*\S+/m.test(metadataOutput.stdout)
  metadata.isMacSupported = /^pluginFolderMac:\s*\S+/m.test(metadataOutput.stdout)
  metadata.isUniversalPlugin = true
}

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

if (metadata.name !== "OpenCodeCompanion" || metadata.displayName !== "OpenCode Companion") {
  console.error(`Unexpected marketplace identity: ${metadata.name} / ${metadata.displayName}`)
  process.exit(1)
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

console.log(`logitech-package=ok format=${format} name=${metadata.name} version=${metadata.version}`)
