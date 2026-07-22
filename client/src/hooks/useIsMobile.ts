import useMediaQuery from '@mui/material/useMediaQuery'
import { useTheme } from '@mui/material/styles'

/**
 * True below the `sm` breakpoint (600px) — phone width.
 * Single source of truth for the mobile/desktop branch across screens.
 * See docs/plan.md §2 for the xs/sm/md treatment table this backs.
 */
export function useIsMobile() {
  const theme = useTheme()
  return useMediaQuery(theme.breakpoints.down('sm'))
}
