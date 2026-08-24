import { existsSync, readFileSync } from "node:fs"
import { join } from "node:path"
import { createNotifier, DEFAULT_CONFIG, normalizeConfig, stripJsonComments } from "@opencode-logitech-haptics/core"

export { DEFAULT_CONFIG }

function sessionDurationSeconds(event) {
  const session = event?.properties?.session ?? event?.session ?? {}
  const startedAt = session.time?.created ?? session.createdAt ?? session.created_at
  if (!startedAt) return undefined
  const started = Date.parse(startedAt)
  if (!Number.isFinite(started)) return undefined
  return Math.max(0, (Date.now() - started) / 1000)
}

function defaultConfigPath(cwd, exists = existsSync) {
  for (const filename of ["opencode-companion.jsonc", "opencode-companion.json"]) {
    const candidate = join(cwd, filename)
    if (exists(candidate)) return candidate
  }
  return undefined
}

export function loadConfigResult({ env = process.env, cwd = process.cwd(), exists = existsSync, readFile = readFileSync } = {}) {
  const diagnostics = []
  const path = env.OPENCODE_LOGITECH_HAPTICS_CONFIG || defaultConfigPath(cwd, exists)
  let override = {}
  if (path) {
    try {
      override = JSON.parse(stripJsonComments(readFile(path, "utf8")))
    } catch (error) {
      diagnostics.push(`Could not read configuration at ${path}: ${error.message}`)
    }
  }
  if (env.LOGITECH_HAPTICS_URL) override = { ...override, endpoint: env.LOGITECH_HAPTICS_URL }
  const normalized = normalizeConfig(override)
  return { config: normalized.config, diagnostics: [...diagnostics, ...normalized.diagnostics], path }
}

export function loadConfig(options) {
  return loadConfigResult(options).config
}

export function createLogitechHapticsPlugin(config = loadConfig(), dependencies = {}) {
  const notifier = createNotifier({ config, fetchImpl: dependencies.fetchImpl, now: dependencies.now })
  return async ({ directory, worktree, serverUrl }) => {
    if (serverUrl) await notifier.announceServer(String(serverUrl), { directory, worktree })
    const trigger = (event, message, extra = {}) => notifier.notify(event, { message, directory, worktree, ...extra })
    return {
      event: async ({ event }) => {
        const sessionID = event?.properties?.sessionID ?? event?.properties?.sessionId ?? event?.sessionID ?? event?.sessionId
        const sessionContext = typeof sessionID === "string" ? { sessionID } : {}
        if (event?.type === "session.idle") {
          const duration = sessionDurationSeconds(event)
          if (duration !== undefined && duration < notifier.config.minDurationSeconds) return
          await trigger("complete", "OpenCode session completed", { ...sessionContext, durationSeconds: duration })
        }
        if (event?.type === "session.error") await trigger("error", "OpenCode session error", sessionContext)
        if (event?.type === "permission.asked") await trigger("permission", "OpenCode permission requested", sessionContext)
      },
      "permission.ask": async (input = {}) => {
        const sessionID = input.sessionID ?? input.sessionId
        await trigger("permission", `OpenCode permission requested: ${input.type ?? "unknown"}`, { ...(typeof sessionID === "string" ? { sessionID } : {}), permissionType: input.type })
      },
      "tool.execute.before": async (input = {}) => {
        const sessionID = input.sessionID ?? input.sessionId
        const sessionContext = typeof sessionID === "string" ? { sessionID } : {}
        if (input.tool === "question") await trigger("question", "OpenCode has a question", sessionContext)
        if (input.tool === "plan_exit") await trigger("plan_exit", "OpenCode plan is ready for review", sessionContext)
      },
    }
  }
}

export const LogitechHapticsPlugin = createLogitechHapticsPlugin()
export default LogitechHapticsPlugin
