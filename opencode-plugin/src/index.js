import { existsSync, readFileSync } from "node:fs"

const DEFAULT_ENDPOINT = "http://127.0.0.1:17844/haptic"

export const DEFAULT_CONFIG = Object.freeze({
  enabled: true,
  endpoint: DEFAULT_ENDPOINT,
  events: Object.freeze({
    complete: true,
    permission: true,
    error: true,
    question: true,
    plan_exit: true,
    session_started: false,
    user_message: false,
    subagent_complete: false,
  }),
  minDurationSeconds: 0,
  suppressDuplicatesMs: 750,
})

function deepMergeConfig(base, override = {}) {
  return { ...base, ...override, events: { ...base.events, ...(override.events ?? {}) } }
}

export function loadConfig(env = process.env) {
  let config = DEFAULT_CONFIG
  const configPath = env.OPENCODE_LOGITECH_HAPTICS_CONFIG
  if (configPath && existsSync(configPath)) {
    config = deepMergeConfig(config, JSON.parse(readFileSync(configPath, "utf8")))
  }
  if (env.LOGITECH_HAPTICS_URL) config = { ...config, endpoint: env.LOGITECH_HAPTICS_URL }
  return config
}

function sessionDurationSeconds(event) {
  const session = event?.properties?.session ?? event?.session ?? {}
  const startedAt = session.time?.created ?? session.createdAt ?? session.created_at
  if (!startedAt) return undefined
  const started = Date.parse(startedAt)
  if (!Number.isFinite(started)) return undefined
  return Math.max(0, (Date.now() - started) / 1000)
}

export function createLogitechHapticsPlugin(config = loadConfig()) {
  const lastSentAt = new Map()
  return async ({ $, directory, worktree }) => {
    const shell = $

    async function trigger(eventName, message, extra = {}) {
      if (!config.enabled || !config.events?.[eventName]) return false
      const now = Date.now()
      const suppressMs = Number(config.suppressDuplicatesMs ?? 0)
      const previous = lastSentAt.get(eventName) ?? 0
      if (suppressMs > 0 && now - previous < suppressMs) return false
      lastSentAt.set(eventName, now)
      const payload = JSON.stringify({ source: "opencode", event: eventName, message, directory, worktree, time: new Date(now).toISOString(), ...extra })
      try {
        await shell`curl -fsS -X POST ${config.endpoint} -H "Content-Type: application/json" --data ${payload}`.quiet()
        return true
      } catch {
        return false
      }
    }

    return {
      event: async ({ event }) => {
        if (event?.type === "session.idle") {
          const duration = sessionDurationSeconds(event)
          if (duration !== undefined && duration < Number(config.minDurationSeconds ?? 0)) return
          await trigger("complete", "OpenCode session completed", { durationSeconds: duration })
        }
        if (event?.type === "session.error") await trigger("error", "OpenCode session error")
        if (event?.type === "permission.asked") await trigger("permission", "OpenCode permission requested")
      },
      "permission.ask": async (input = {}) => {
        await trigger("permission", `OpenCode permission requested: ${input.type ?? "unknown"}`, { permissionType: input.type })
      },
      "tool.execute.before": async (input = {}) => {
        if (input.tool === "question") await trigger("question", "OpenCode has a question")
        if (input.tool === "plan_exit") await trigger("plan_exit", "OpenCode plan is ready for review")
      },
    }
  }
}

export const LogitechHapticsPlugin = createLogitechHapticsPlugin()
export default LogitechHapticsPlugin
