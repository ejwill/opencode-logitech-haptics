import { Plugin } from "@opencode-ai/plugin"
import { createNotifier, SUPPORTED_EVENTS } from "@opencode-logi-companion/core"
import { resolveV2Options } from "./config.js"

export const PLUGIN_ID = "opencode.logi.companion"

function eventPayload(event) {
  if (event?.properties && typeof event.properties === "object") return event.properties
  if (event?.data && typeof event.data === "object") return event.data
  return {}
}

export function notificationFromV2Event(event, eventTypes = {}) {
  const type = event?.type
  const payload = eventPayload(event)
  const formKind = payload.form?.metadata?.kind
  const propertyKey = type === "form.created" && typeof formKind === "string" ? `${type}:${formKind}` : undefined
  const notification = eventTypes?.[propertyKey] ?? eventTypes?.[type]
  if (typeof notification !== "string" || !SUPPORTED_EVENTS.includes(notification)) return undefined
  const sessionID = payload.sessionID ?? payload.sessionId
  return { event: notification, message: `OpenCode v2 event: ${type}`, ...(typeof sessionID === "string" ? { sessionID } : {}) }
}

export function observedV2EventType(event, logEventType) {
  const type = event?.type
  if (typeof type !== "string" || type.length === 0) return
  logEventType(type)
}

export function createV2Plugin(pluginApi = Plugin, dependencies = {}) {
  return pluginApi.define({
    id: PLUGIN_ID,
    setup: async (ctx) => {
      const options = resolveV2Options(ctx.options, dependencies)
      const notifier = createNotifier({ config: options, fetchImpl: dependencies.fetchImpl })
      const controller = new AbortController()
      const eventTypes = options.eventTypes
      const logEventType = dependencies.logEventType ?? ((type) => console.info(`[${PLUGIN_ID}] observed event type: ${type}`))
      const streamTask = (async () => {
        try {
          for await (const event of ctx.event.subscribe({ signal: controller.signal })) {
            if (controller.signal.aborted) return
              if (options.logEventTypes === true) observedV2EventType(event, logEventType)
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

      await ctx.command.transform((draft) => {
        draft.add({
          name: "haptic-test",
          description: "Send a diagnostic test event to the Logitech haptics bridge",
          execute: async () => {
            const result = await notifier.notify("test", { message: "OpenCode Logi Companion haptic test", force: true })
            const detail = result.sent ? "sent" : `not sent (${result.reason}${result.status ? ` ${result.status}` : ""})`
            console.info(`[${PLUGIN_ID}] haptic-test ${detail}`)
          },
        })
      })

      return async () => {
        controller.abort()
        await streamTask
      }
    },
  })
}

export default createV2Plugin()
