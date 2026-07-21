import Box from '@mui/material/Box'

function initials(firstName: string, lastName: string): string {
  return `${firstName[0] ?? ''}${lastName[0] ?? ''}`.toUpperCase()
}

export function UserAvatar({
  firstName,
  lastName,
  size = 31,
}: {
  firstName: string
  lastName: string
  size?: number
}) {
  return (
    <Box
      sx={{
        width: size,
        height: size,
        borderRadius: '50%',
        backgroundImage: (theme) => theme.palette.avatarGradient,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        fontSize: Math.round(size * 0.35),
        fontWeight: 600,
        flexShrink: 0,
      }}
    >
      {initials(firstName, lastName)}
    </Box>
  )
}
