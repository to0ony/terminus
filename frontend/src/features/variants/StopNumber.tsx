import { Box } from '@mantine/core'

/** Redni broj stanice u varijanti, isti kružić kao na karti. */
export function StopNumber({ value }: { value: number }) {
  return (
    <Box
      component="span"
      display="inline-flex"
      style={{
        minWidth: 20,
        height: 20,
        padding: '0 5px',
        borderRadius: 10,
        alignItems: 'center',
        justifyContent: 'center',
        background: '#e8590c',
        color: '#ffffff',
        fontSize: 11,
        fontWeight: 700,
        lineHeight: 1,
        flexShrink: 0,
        boxShadow: '0 0 0 2px #ffffff',
      }}
    >
      {value}
    </Box>
  )
}
