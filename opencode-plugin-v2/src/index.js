import { Plugin } from "@opencode-ai/plugin"
import { createNotifier, SUPPORTED_EVENTS } from "@opencode-logitech-haptics/core"

export const PLUGIN_ID = "opencode.logitech-haptics"

export function notificationFromV2Event(event, eventTypes = {}) {
  const notification = eventTypes?.[event?.type]
  if (typeof notification !== "string" || !SUPPORTED_EVENTS.includes(notification)) return undefined
  return { event: notification, message: `OpenCode v2 event: ${event.type}` }
}

export function createV2Plugin(pluginApi = Plugin, dependencies = {}) {
  return pluginApi.define({
    id: PLUGIN_ID,
    setup: async (ctx) => {
      const notifier = createNotifier({ config: ctx.options, fetchImpl: dependencies.fetchImpl })
      const controller = new AbortController()
      const eventTypes = ctx.options?.eventTypes ?? {}
      const streamTask = (async () => {
        try {
          for await (const event of ctx.event.subscribe({ signal: controller.signal })) {
            if (controller.signal.aborted) return
            const notification = notificationFromV2Event(event, eventTypes)
            if (notification) await notifier.notify(notification.event, notification)
          }
        } catch (error) {
          if (!controller.signal.aborted) console.error("OpenCode Logitech Haptics v2 event subscription stopped", error)
        }
      })()

      await ctx.tool.hook("execute.before", async (event) => {
        if (event.tool === "question") await notifier.notify("question", { message: "OpenCode v2 has a question" })
        if (event.tool === "plan_exit") await notifier.notify("plan_exit", { message: "OpenCode v2 plan is ready for review" })
      })

      return async () => {
        controller.abort()
        await streamTask
      }
    },
  })
}

export default createV2Plugin()
