import { Link as RouterLink } from 'react-router-dom'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Typography from '@mui/material/Typography'

export function NotFoundScreen() {
  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 2,
        p: 2,
        textAlign: 'center',
      }}
    >
      <Typography
        sx={{ fontSize: 56, fontWeight: 600, letterSpacing: '-1.5px', color: 'text.disabled' }}
      >
        404
      </Typography>
      <Typography variant="h1">Page not found</Typography>
      <Typography variant="body2" sx={{ color: 'text.disabled', maxWidth: 360 }}>
        The page you're looking for doesn't exist or may have been moved.
      </Typography>
      <Button component={RouterLink} to="/" variant="contained">
        Back to Taskflow
      </Button>
    </Box>
  )
}
