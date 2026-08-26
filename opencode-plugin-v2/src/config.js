import { existsSync, readFileSync } from "node:fs"
import { homedir } from "node:os"
import { join } from "node:path"
import { normalizeConfig, stripJsonComments, SUPPORTED_EVENTS } from "@opencode-logi-companion/core"

const CONFIG_FILENAME = "opencode-logi-companion.jsonc"
const LEGACY_CONFIG_FILENAME = "opencode-companion.jsonc"

export const DEFAULT_V2_EVENT_TYPES = Object.freeze({
  "session.execution.started": "session_started",
  "session.execution.succeeded": "complete",
  "session.execution.interrupted": "error",
  "permission.v2.asked": "permission",
  "form.created:question": "question",
})

function isRecord(value) {
  return value !== null && typeof value === "object" && !Array.isArray(value)
}

function candidatePaths({ env, cwd, home }) {
  const explicit = env.OPENCODE_LOGI_COMPANION_CONFIG
  if (explicit) return [explicit]
  return [join(cwd, CONFIG_FILENAME), join(home, ".config", "opencode", CONFIG_FILENAME)]
}

function legacyCandidatePaths({ cwd, home }) {
  return [join(cwd, LEGACY_CONFIG_FILENAME), join(home, ".config", "opencode", LEGACY_CONFIG_FILENAME)]
}

function readConfigFile(paths, exists, readFile) {
  const path = paths.find((candidate) => exists(candidate))
  if (!path) return { path: undefined, value: {}, diagnostics: [] }
  try {
    return { path, value: JSON.parse(stripJsonComments(readFile(path, "utf8"))), diagnostics: [] }
  } catch (error) {
    return { path, value: {}, diagnostics: [`Could not read configuration at ${path}: ${error.message}`] }
  }
}

function normalizeEventTypes(eventTypes, diagnostics) {
  if (eventTypes === undefined) return {}
  if (!isRecord(eventTypes)) {
    diagnostics.push("eventTypes must be an object; using no stream-event mappings.")
    return {}
  }
  return Object.fromEntries(Object.entries(eventTypes).filter(([type, event]) => {
    if (typeof event !== "string" || !SUPPORTED_EVENTS.includes(event)) {
      diagnostics.push(`eventTypes.${type} must map to a supported haptic event; it was ignored.`)
      return false
    }
    return true
  }))
}

export function loadV2Config({ env = process.env, cwd = process.cwd(), home = homedir(), exists = existsSync, readFile = readFileSync } = {}) {
  const file = readConfigFile(candidatePaths({ env, cwd, home }), exists, readFile)
  const diagnostics = [...file.diagnostics]
  if (!file.path && !env.OPENCODE_LOGI_COMPANION_CONFIG) {
    const legacyPath = legacyCandidatePaths({ cwd, home }).find((candidate) => exists(candidate))
    if (legacyPath) diagnostics.push(`Found legacy configuration at ${legacyPath}; rename it to ${CONFIG_FILENAME} in the same location (its values are ignored).`)
  }
  const source = isRecord(file.value) ? file.value : {}
  if (!isRecord(file.value)) diagnostics.push("Configuration must be a JSON object; using defaults.")
  const normalized = normalizeConfig(source)
  diagnostics.push(...normalized.diagnostics)
  return {
    config: normalized.config,
    eventTypes: normalizeEventTypes(source.advanced?.eventTypes, diagnostics),
    logEventTypes: source.logEventTypes === true,
    path: file.path,
    diagnostics,
  }
}

export function resolveV2Options(options = {}, dependencies = {}) {
  const file = loadV2Config(dependencies)
  const inline = isRecord(options) ? options : {}
  const normalized = normalizeConfig({ ...file.config, ...inline, events: { ...file.config.events, ...(isRecord(inline.events) ? inline.events : {}) } })
  return {
    ...normalized.config,
    eventTypes: { ...DEFAULT_V2_EVENT_TYPES, ...file.eventTypes, ...normalizeEventTypes(inline.advanced?.eventTypes, []) },
    logEventTypes: inline.logEventTypes === true || (inline.logEventTypes === undefined && file.logEventTypes),
  }
}
