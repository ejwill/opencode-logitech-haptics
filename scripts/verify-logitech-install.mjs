#!/usr/bin/env node

import os from "node:os"
import path from "node:path"
import { existsSync } from "node:fs"

const pluginName = process.argv[2] ?? "OpenCodeCompanion"
const pluginDirectory = path.join(
  os.homedir(),
  "Library",
  "Application Support",
  "Logi",
  "LogiPluginService",
  "Plugins",
  pluginName,
)

const requiredFiles = [
  "metadata/LoupedeckPackage.yaml",
  "bin/OpenCodeCompanionPlugin.dll",
  "events/DefaultEventSource.yaml",
  "events/extra/eventMapping.yaml",
]

const missing = requiredFiles.filter((file) => !existsSync(path.join(pluginDirectory, file)))

if (missing.length > 0) {
  console.error(`logitech-install=missing path=${pluginDirectory}`)
  for (const file of missing) console.error(`missing=${file}`)
  process.exit(1)
}

console.log(`logitech-install=ok path=${pluginDirectory}`)
