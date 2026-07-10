import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr'
import * as tokenStorage from '../api/tokenStorage'
import { useNotificationCenterStore } from '../stores/notificationCenter'
import { useNotificationsStore } from '../stores/notifications'
import { i18n } from '../plugins/i18n'

// F6.1 (ADR-0011). SignalRNotifier (Infrastructure) sends via
// hubContext.Clients.User(id).SendAsync(eventName, payload) — eventName is whatever the domain
// event handler chose ("document.transitioned" is the only one that exists today, F6/HU-081).
// The JS client has to register a handler per event name; there's no wildcard listener in
// SignalR, so a future domain event handler adding a new eventName needs a matching .on() call
// here too.
interface DocumentTransitionedPayload {
  documentId: string
  toStateName: string
}

let connection: HubConnection | null = null

export function startNotificationHubConnection(): HubConnection {
  if (connection) {
    return connection
  }

  connection = new HubConnectionBuilder()
    .withUrl('/hubs/notifications', { accessTokenFactory: () => tokenStorage.getAccessToken() ?? '' })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()

  connection.on('document.transitioned', (payload: DocumentTransitionedPayload) => {
    const notificationCenter = useNotificationCenterStore()
    const toast = useNotificationsStore()
    notificationCenter.load()
    toast.show(i18n.global.t('notifications.documentTransitioned', { state: payload.toStateName }), 'success')
  })

  // Best-effort: a failed real-time connection must not break the app — the notification
  // center still works via on-demand fetches (bell click, page load), just without the
  // instant push.
  connection.start().catch(() => {
    // Intentionally ignored — see above.
  })

  return connection
}
