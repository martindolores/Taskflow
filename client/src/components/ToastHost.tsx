import { useEffect, useState } from 'react'
import Alert from '@mui/material/Alert'
import Snackbar from '@mui/material/Snackbar'
import { subscribeToast, type ToastSeverity } from './toast'

interface ToastState {
  key: number
  message: string
  severity: ToastSeverity
}

export function ToastHost() {
  const [toast, setToast] = useState<ToastState | null>(null)

  useEffect(
    () => subscribeToast((message, severity) => setToast({ key: Date.now(), message, severity })),
    [],
  )

  return (
    <Snackbar
      key={toast?.key}
      open={!!toast}
      autoHideDuration={4000}
      onClose={() => setToast(null)}
      anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
    >
      <Alert severity={toast?.severity} variant="filled" onClose={() => setToast(null)}>
        {toast?.message}
      </Alert>
    </Snackbar>
  )
}
