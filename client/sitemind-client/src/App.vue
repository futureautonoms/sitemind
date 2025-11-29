<template>
  <div id="app">
    <NavBar v-if="showNavbar" />
    <router-view />
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from './stores/authStore'
import NavBar from './components/NavBar.vue'

const route = useRoute()
const authStore = useAuthStore()

// Show navbar only on dashboard, hide on login/register pages
const showNavbar = computed(() => {
  const hideNavbarRoutes = ['Login', 'Register']
  return !hideNavbarRoutes.includes(route.name)
})
</script>

<style>
#app {
  min-height: 100vh;
}
</style>
