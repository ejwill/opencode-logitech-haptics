import assert from "node:assert/strict"
import { execFileSync } from "node:child_process"
import { mkdtemp, readdir, rm } from "node:fs/promises"
import { tmpdir } from "node:os"
import { join } from "node:path"
import { pathToFileURL } from "node:url"

const artifactsDirectory = process.argv[2]
if (!artifactsDirectory) throw new Error("Usage: node scripts/verify-packed-adapters.mjs <artifact-directory>")

const artifacts = await readdir(artifactsDirectory)
const legacyTarball = artifacts.find((name) => name.startsWith("opencode-companion-") && !name.startsWith("opencode-companion-v2-"))
const v2Tarball = artifacts.find((name) => name.startsWith("opencode-companion-v2-"))
if (!legacyTarball || !v2Tarball) throw new Error("Both legacy and v2 package tarballs are required.")

const prefix = await mkdtemp(join(tmpdir(), "opencode-companion-artifact-"))
try {
  execFileSync("npm", ["install", "--prefix", prefix, "--ignore-scripts", join(artifactsDirectory, legacyTarball), join(artifactsDirectory, v2Tarball)], { stdio: "inherit" })
  const installed = join(prefix, "node_modules")
  const legacy = await import(pathToFileURL(join(installed, "opencode-companion", "src", "index.js")))
  const v2 = await import(pathToFileURL(join(installed, "opencode-companion-v2", "src", "index.js")))

  assert.equal(typeof legacy.createLogitechHapticsPlugin, "function")
  assert.equal(v2.default.id, "opencode.companion")
  assert.equal(typeof v2.default.setup, "function")

  const delivered = []
  const plugin = legacy.createLogitechHapticsPlugin({ suppressDuplicatesMs: 0 }, {
    fetchImpl: async (_url, request) => {
      delivered.push(JSON.parse(request.body))
      return { ok: true, status: 202 }
    },
  })
  const hooks = await plugin({ directory: "/artifact-check", worktree: "/artifact-check" })
  await hooks.event({ event: { type: "session.error" } })
  assert.deepEqual(delivered.map((payload) => payload.event), ["error"])
  console.log("packed-adapters=ok")
} finally {
  await rm(prefix, { recursive: true, force: true })
}
