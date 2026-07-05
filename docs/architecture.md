# Architecture

## Boundary

OpenCode decides **when** a notification-worthy event happened. Logitech decides **how** that event should feel on haptic hardware.

## HTTP contract

```http
POST /haptic HTTP/1.1
Host: 127.0.0.1:17844
Content-Type: application/json
```

```json
{
  "source": "opencode",
  "event": "complete",
  "message": "Session completed",
  "directory": "/path/to/project",
  "worktree": "/path/to/worktree",
  "time": "2026-07-04T12:00:00.000Z"
}
```

Event mapping:

```text
complete   -> opencodeComplete
permission -> opencodePermission
error      -> opencodeError
question   -> opencodeQuestion
plan_exit  -> opencodePlanExit
test       -> opencodeTest
```

Unknown events return `204 No Content`.
