import { useId, type ReactNode } from 'react'
import Box from '@mui/material/Box'
import TextField, { type TextFieldProps } from '@mui/material/TextField'
import Typography from '@mui/material/Typography'

interface LabeledFieldProps extends Omit<TextFieldProps, 'label'> {
  label: string
  labelExtra?: ReactNode
}

export function LabeledField({ label, labelExtra, id, ...textFieldProps }: LabeledFieldProps) {
  const generatedId = useId()
  const inputId = id ?? generatedId

  return (
    <Box>
      <Box
        sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 0.75 }}
      >
        <Typography component="label" htmlFor={inputId} variant="caption" sx={{ fontWeight: 500 }}>
          {label}
        </Typography>
        {labelExtra}
      </Box>
      <TextField id={inputId} fullWidth {...textFieldProps} />
    </Box>
  )
}
