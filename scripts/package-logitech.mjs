#!/usr/bin/env node

import { execFileSync } from "node:child_process"
import { copyFileSync, existsSync, mkdirSync, mkdtempSync, rmSync } from "node:fs"
import { tmpdir } from "node:os"
import { join, resolve } from "node:path"

const [, , buildDirectoryArg, outputDirectoryArg, packageBaseName] = process.argv

if (!buildDirectoryArg || !outputDirectoryArg || !packageBaseName) {
  console.error("Usage: node scripts/package-logitech.mjs <build-directory> <output-directory> <package-base-name>")
  process.exit(2)
}

const buildDirectory = resolve(buildDirectoryArg)
const outputDirectory = resolve(outputDirectoryArg)
if (!existsSync(buildDirectory)) {
  console.error(`Build directory does not exist: ${buildDirectory}`)
  process.exit(1)
}

mkdirSync(outputDirectory, { recursive: true })
const stagingRoot = mkdtempSync(join(tmpdir(), "opencode-companion-package-"))
const zipPackage = join(stagingRoot, `${packageBaseName}.lplug4`)
const extractedPackage = join(stagingRoot, "package")
const tarPackage = join(outputDirectory, `${packageBaseName}.lplug4`)
const marketplacePackage = join(outputDirectory, `${packageBaseName}_marketplace.lplug4`)

try {
  execFileSync("dotnet", [
    "tool", "run", "logiplugintool", "pack", buildDirectory, zipPackage,
  ], {
    stdio: "inherit",
    env: { ...process.env, DOTNET_ROLL_FORWARD: "Major" },
  })

  mkdirSync(extractedPackage)
  execFileSync("unzip", ["-q", zipPackage, "-d", extractedPackage], { stdio: "inherit" })

  for (const requiredDirectory of ["bin", "events", "metadata"]) {
    if (!existsSync(join(extractedPackage, requiredDirectory))) {
      throw new Error(`Packed package is missing ${requiredDirectory}/`)
    }
  }

  copyFileSync(zipPackage, marketplacePackage)
  execFileSync("python3", [
    "scripts/write-logitech-tar.py", extractedPackage, tarPackage,
  ], {
    stdio: "inherit",
  })

  console.log(`Marketplace ZIP: ${marketplacePackage}`)
  console.log(`Direct-install tar: ${tarPackage}`)
} finally {
  rmSync(stagingRoot, { recursive: true, force: true })
}
