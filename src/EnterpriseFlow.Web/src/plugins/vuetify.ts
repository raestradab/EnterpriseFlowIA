import 'vuetify/styles'
import '@mdi/font/css/materialdesignicons.css'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'

// Brand-neutral placeholder palette (dataviz skill convention) — swap for the product's real
// brand colors before shipping past portfolio stage.
const lightTheme = {
  dark: false,
  colors: {
    primary: '#3A5BC7',
    secondary: '#5A6B87',
    success: '#1E8E5A',
    warning: '#B7791F',
    error: '#C0392B',
    background: '#F7F8FA',
    surface: '#FFFFFF',
  },
}

const darkTheme = {
  dark: true,
  colors: {
    primary: '#7C93E8',
    secondary: '#9AA7BD',
    success: '#4CAF7D',
    warning: '#D9A441',
    error: '#E27060',
    background: '#12151C',
    surface: '#1B1F29',
  },
}

export const vuetify = createVuetify({
  components,
  directives,
  theme: {
    defaultTheme: 'light',
    themes: {
      light: lightTheme,
      dark: darkTheme,
    },
  },
  defaults: {
    VCard: { rounded: 'lg' },
    VBtn: { rounded: 'lg' },
    VTextField: { variant: 'outlined', density: 'comfortable' },
    VSelect: { variant: 'outlined', density: 'comfortable' },
  },
})
