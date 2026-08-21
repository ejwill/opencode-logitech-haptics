export const DEFAULT_ENDPOINT = "http://127.0.0.1:17844/haptic"

export const NOTIFICATION_EVENTS = Object.freeze({
  turnStarted: "session_started",
  completion: "complete",
  permission: "permission",
  question: "question",
  error: "error",
  planReady: "plan_exit",
  userMessage: "user_message",
  subagentCompletion: "subagent_complete",
})

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
  notifications: Object.freeze({
    turnStarted: false,
    completion: true,
    permission: true,
    question: true,
    error: true,
    planReady: true,
    userMessage: false,
    subagentCompletion: false,
  }),
  minDurationSeconds: 0,
  suppressDuplicatesMs: 750,
  timeoutMs: 2_000,
})

export const SUPPORTED_EVENTS = Object.freeze(Object.keys(DEFAULT_CONFIG.events))

function isRecord(value) {
  return value !== null && typeof value === "object" && !Array.isArray(value)
}

function isLoopbackEndpoint(value) {
  try {
    const url = new URL(value)
    return url.protocol === "http:" && ["127.0.0.1", "::1", "localhost"].includes(url.hostname)
  } catch {
    return false
  }
}

function finiteNonNegative(value) {
  return typeof value === "number" && Number.isFinite(value) && value >= 0
}

export function normalizeConfig(override = {}, defaults = DEFAULT_CONFIG) {
  const diagnostics = []
  if (!isRecord(override)) {
    return { config: defaults, diagnostics: ["Configuration must be a JSON object; using defaults."] }
  }

  const config = { ...defaults, events: { ...defaults.events }, notifications: { ...defaults.notifications } }
  if (typeof override.enabled === "boolean") config.enabled = override.enabled
  else if (override.enabled !== undefined) diagnostics.push("enabled must be a boolean; using default.")

  if (override.endpoint !== undefined) {
    if (typeof override.endpoint === "string" && isLoopbackEndpoint(override.endpoint)) config.endpoint = override.endpoint
    else diagnostics.push("endpoint must be an http loopback URL; using default.")
  }

  if (override.notifications !== undefined) {
    if (!isRecord(override.notifications)) diagnostics.push("notifications must be an object; using defaults.")
    else for (const [notification, enabled] of Object.entries(override.notifications)) {
      if (notification in config.notifications && typeof enabled === "boolean") {
        config.notifications[notification] = enabled
        config.events[NOTIFICATION_EVENTS[notification]] = enabled
      } else if (notification in config.notifications) diagnostics.push(`notifications.${notification} must be a boolean; using default.`)
      else diagnostics.push(`notifications.${notification} is unsupported and was ignored.`)
    }
  }

  // The internal bridge-event form is applied last so normalized configs can
  // be passed through this function without re-enabling an intentional override.
  if (override.events !== undefined) {
    if (!isRecord(override.events)) diagnostics.push("events must be an object; using defaults.")
    else for (const [event, enabled] of Object.entries(override.events)) {
      if (event in config.events && typeof enabled === "boolean") config.events[event] = enabled
      else if (event in config.events) diagnostics.push(`events.${event} must be a boolean; using default.`)
      else diagnostics.push(`events.${event} is unsupported and was ignored.`)
    }
  }

  for (const key of ["minDurationSeconds", "suppressDuplicatesMs", "timeoutMs"]) {
    if (override[key] === undefined) continue
    if (finiteNonNegative(override[key])) config[key] = override[key]
    else diagnostics.push(`${key} must be a non-negative number; using default.`)
  }
  return { config: Object.freeze({ ...config, events: Object.freeze(config.events), notifications: Object.freeze(config.notifications) }), diagnostics }
}

export function stripJsonComments(source) {
  let result = ""
  let inString = false
  let escaped = false
  for (let index = 0; index < source.length; index += 1) {
    const character = source[index]
    const next = source[index + 1]
    if (inString) {
      result += character
      if (escaped) escaped = false
      else if (character === "\\") escaped = true
      else if (character === '"') inString = false
      continue
    }
    if (character === '"') { inString = true; result += character; continue }
    if (character === "/" && next === "/") {
      while (index < source.length && source[index] !== "\n") index += 1
      result += "\n"
      continue
    }
    if (character === "/" && next === "*") {
      index += 2
      while (index < source.length && !(source[index] === "*" && source[index + 1] === "/")) index += 1
      index += 1
      continue
    }
    result += character
  }
  return result
}

export function createNotifier({ config, fetchImpl = globalThis.fetch, now = () => Date.now() } = {}) {
  const lastSentAt = new Map()
  const normalized = normalizeConfig(config)
  const activeConfig = normalized.config

  async function notify(event, { message, directory, worktree, ...extra } = {}) {
    if (!activeConfig.enabled || !activeConfig.events[event]) return { sent: false, reason: "disabled" }
    const current = now()
    const previous = lastSentAt.get(event)
    if (previous !== undefined && activeConfig.suppressDuplicatesMs > 0 && current - previous < activeConfig.suppressDuplicatesMs) {
      return { sent: false, reason: "duplicate" }
    }
    if (typeof fetchImpl !== "function") return { sent: false, reason: "transport-unavailable" }
    const payload = { source: "opencode", event, message, directory, worktree, time: new Date(current).toISOString(), ...extra }
    try {
      const response = await fetchImpl(activeConfig.endpoint, {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify(payload),
        signal: AbortSignal.timeout(activeConfig.timeoutMs),
      })
      if (!response?.ok) return { sent: false, reason: "http-error", status: response?.status }
      lastSentAt.set(event, current)
      return { sent: true, payload }
    } catch (error) {
      return { sent: false, reason: error?.name === "TimeoutError" ? "timeout" : "transport-error" }
    }
  }
  return { config: activeConfig, diagnostics: normalized.diagnostics, notify }
}
