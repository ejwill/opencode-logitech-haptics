import { copyFileSync, mkdirSync } from "node:fs"
import { dirname, join } from "node:path"
import { fileURLToPath } from "node:url"

const packageRoot = dirname(dirname(fileURLToPath(import.meta.url)))
const coreRoot = join(packageRoot, "..", "packages", "haptics-core")
const targetRoot = join(packageRoot, "node_modules", "@opencode-logi-companion", "core")

mkdirSync(join(targetRoot, "src"), { recursive: true })
for (const relativePath of ["package.json", "README.md", "src/index.js"]) {
  copyFileSync(join(coreRoot, relativePath), join(targetRoot, relativePath))
}
