import Box from '@mui/material/Box'
import Typography from '@mui/material/Typography'

export function PlaceholderPage({ title }: { title: string }) {
  return (
    <Box sx={{ p: 5 }}>
      <Typography variant="h1">{title}</Typography>
    </Box>
  )
}
