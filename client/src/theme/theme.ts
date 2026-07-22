import { createTheme } from '@mui/material/styles'

declare module '@mui/material/styles' {
  interface Palette {
    surface: {
      input: string
    }
    border: {
      default: string
      hover: string
    }
    nav: {
      activeText: string
    }
    avatarGradient: string
  }
  interface PaletteOptions {
    surface?: {
      input: string
    }
    border?: {
      default: string
      hover: string
    }
    nav?: {
      activeText: string
    }
    avatarGradient?: string
  }
}

const fontFamily = ['DM Sans', 'system-ui', 'sans-serif'].join(', ')

// Responsive breakpoints: MUI defaults (xs:0, sm:600, md:900, lg:1200, xl:1536),
// no custom values needed. Mobile/desktop branch in screens goes through
// useIsMobile() (client/src/hooks/useIsMobile.ts), not a raw useMediaQuery call.
//
//   xs (<600px, phone)     sidebar -> bottom nav bar; tables -> stacked cards;
//                          dialogs -> fullScreen; metadata panels -> inline sections
//   sm (600-900px, tablet) same treatment as xs (reflows still needed, fixed
//                          table columns don't fit); two-column screens stack
//   md+ (>=900px)          current desktop layout, unchanged
export const theme = createTheme({
  palette: {
    mode: 'dark',
    background: {
      default: '#09090c',
      paper: '#111117',
    },
    surface: {
      input: '#18181f',
    },
    border: {
      default: 'rgba(255, 255, 255, 0.08)',
      hover: 'rgba(255, 255, 255, 0.12)',
    },
    divider: 'rgba(255, 255, 255, 0.08)',
    primary: {
      main: '#6366f1',
      light: '#818cf8',
    },
    nav: {
      activeText: '#a5b4fc',
    },
    avatarGradient: 'linear-gradient(135deg, #6366f1, #8b5cf6)',
    text: {
      primary: '#f0f0f6',
      secondary: '#c8c8dc',
      disabled: '#52526a',
    },
    success: {
      main: '#22c55e',
    },
    warning: {
      main: '#f59e0b',
    },
    error: {
      main: '#ef4444',
    },
  },
  shape: {
    borderRadius: 8,
  },
  typography: {
    fontFamily,
    h1: {
      fontSize: '1.375rem',
      fontWeight: 600,
      letterSpacing: '-0.45px',
    },
    h2: {
      fontSize: '1.3125rem',
      fontWeight: 600,
      letterSpacing: '-0.4px',
    },
    subtitle1: {
      fontSize: '0.875rem',
      fontWeight: 600,
    },
    body1: {
      fontSize: '0.875rem',
      fontWeight: 400,
    },
    body2: {
      fontSize: '0.84375rem',
      fontWeight: 400,
    },
    caption: {
      fontSize: '0.75rem',
      color: '#9898b0',
    },
    overline: {
      fontSize: '0.6875rem',
      fontWeight: 600,
      letterSpacing: '0.65px',
      textTransform: 'uppercase',
      color: '#71717a',
    },
  },
  components: {
    MuiCssBaseline: {
      styleOverrides: {
        body: {
          backgroundColor: '#09090c',
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
          border: '1px solid rgba(255, 255, 255, 0.08)',
        },
      },
      defaultProps: {
        elevation: 0,
      },
    },
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: 8,
          textTransform: 'none',
        },
      },
    },
    MuiOutlinedInput: {
      styleOverrides: {
        root: ({ theme }) => ({
          backgroundColor: theme.palette.surface.input,
          borderRadius: 7,
          '& .MuiOutlinedInput-notchedOutline': {
            borderColor: theme.palette.border.default,
          },
          '&:hover .MuiOutlinedInput-notchedOutline': {
            borderColor: theme.palette.border.hover,
          },
        }),
      },
    },
  },
})
