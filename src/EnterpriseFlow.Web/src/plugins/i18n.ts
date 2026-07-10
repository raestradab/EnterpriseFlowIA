import { createI18n } from 'vue-i18n'
import es from '../locales/es'
import en from '../locales/en'

const storedLocale = localStorage.getItem('locale')
const browserLocale = navigator.language.startsWith('en') ? 'en' : 'es'

export const i18n = createI18n({
  legacy: false,
  locale: storedLocale ?? browserLocale,
  fallbackLocale: 'es',
  messages: { es, en },
})
