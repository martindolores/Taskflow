import { ThemeProvider } from '@mui/material/styles'
import CssBaseline from '@mui/material/CssBaseline'
import Box from '@mui/material/Box'
import Typography from '@mui/material/Typography'
import { QueryClientProvider } from '@tanstack/react-query'
import { theme } from '@/theme'
import { queryClient } from '@/api/queryClient'

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            height: '100%',
          }}
        >
          <Typography variant="h1">Taskflow</Typography>
        </Box>
      </ThemeProvider>
    </QueryClientProvider>
  )
}

export default App
