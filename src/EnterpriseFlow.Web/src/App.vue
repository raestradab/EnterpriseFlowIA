<script setup lang="ts">
import ToastHost from './components/ToastHost.vue'
</script>

<template>
  <v-app>
    <router-view />
    <ToastHost />
  </v-app>
</template>
