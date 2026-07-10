<script setup lang="ts">
import { useNotificationsStore } from '../stores/notifications'

const notifications = useNotificationsStore()
</script>

<template>
  <v-snackbar v-model="notifications.visible" :color="notifications.color" timeout="4000" location="bottom right">
    {{ notifications.message }}
  </v-snackbar>
</template>
