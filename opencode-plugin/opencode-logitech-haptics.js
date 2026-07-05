export const LogitechHapticsPlugin = async ({ $, directory, worktree }) => {
  const endpoint = process.env.LOGITECH_HAPTICS_URL || "http://127.0.0.1:17844/haptic"
  const lastSent = new Map()
  const suppressDuplicatesMs = Number(process.env.LOGITECH_HAPTICS_DEDUPE_MS || 750)

  async function trigger(eventName, message) {
    const now = Date.now()
    const last = lastSent.get(eventName) || 0
    if (now - last < suppressDuplicatesMs) return
    lastSent.set(eventName, now)

    try {
      const payload = JSON.stringify({
        source: "opencode",
        event: eventName,
        message,
        directory,
        worktree,
        time: new Date().toISOString(),
      })

      await $`curl -fsS -X POST ${endpoint} -H "Content-Type: application/json" --data ${payload}`.quiet()
    } catch {
      // Haptics should never break OpenCode.
    }
  }

  return {
    event: async ({ event }) => {
      if (event.type === "session.idle") await trigger("complete", "Session completed")
      if (event.type === "session.error") await trigger("error", "Session error")
      if (event.type === "permission.asked") await trigger("permission", "Permission requested")
    },

    "permission.ask": async (input) => {
      await trigger("permission", `Permission requested: ${input?.type || "unknown"}`)
    },

    "tool.execute.before": async (input) => {
      if (input.tool === "question") await trigger("question", "OpenCode has a question")
      if (input.tool === "plan_exit") await trigger("plan_exit", "Plan ready for review")
    },
  }
}

export default LogitechHapticsPlugin
