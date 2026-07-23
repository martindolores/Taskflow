export function formatRelativeTime(isoDate: string): string {
  const diffMs = Date.now() - new Date(isoDate).getTime()
  const diffMinutes = Math.round(diffMs / 60_000)
  if (diffMinutes < 1) {
    return 'just now'
  }
  if (diffMinutes < 60) {
    return `${diffMinutes}m ago`
  }
  const diffHours = Math.round(diffMinutes / 60)
  if (diffHours < 24) {
    return `${diffHours}h ago`
  }
  const diffDays = Math.round(diffHours / 24)
  return `${diffDays}d ago`
}
