export type ToastSeverity = 'success' | 'error'

type ToastListener = (message: string, severity: ToastSeverity) => void

let listener: ToastListener | null = null

export function subscribeToast(fn: ToastListener): () => void {
  listener = fn
  return () => {
    if (listener === fn) {
      listener = null
    }
  }
}

export function showToast(message: string, severity: ToastSeverity = 'success'): void {
  listener?.(message, severity)
}
