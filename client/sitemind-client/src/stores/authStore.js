import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authService } from '../api/authService'
import router from '../router'

export const useAuthStore = defineStore('auth', () => {
  // State
  const token = ref(localStorage.getItem('token') || null)
  const organizationId = ref(localStorage.getItem('organizationId') || null)
  const user = ref(JSON.parse(localStorage.getItem('user') || 'null'))

  // Getters
  const isAuthenticated = computed(() => !!token.value)

  // Actions
  function setAuth(authData) {
    token.value = authData.token
    organizationId.value = authData.organization_id
    user.value = authData.user

    localStorage.setItem('token', authData.token)
    localStorage.setItem('organizationId', authData.organization_id)
    localStorage.setItem('user', JSON.stringify(authData.user))
  }

  function clearAuth() {
    token.value = null
    organizationId.value = null
    user.value = null

    localStorage.removeItem('token')
    localStorage.removeItem('organizationId')
    localStorage.removeItem('user')
  }

  async function login(username, password) {
    try {
      const response = await authService.login(username, password)
      setAuth(response)
      router.push('/')
      return { success: true }
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.error || 'Login failed. Please check your credentials.'
      }
    }
  }

  async function register(email, username, password, organizationName) {
    try {
      const response = await authService.register(email, username, password, organizationName)
      setAuth(response)
      router.push('/')
      return { success: true }
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.error || 'Registration failed. Please try again.'
      }
    }
  }

  function logout() {
    clearAuth()
    router.push('/login')
  }

  function updateUser(userData) {
    user.value = { ...user.value, ...userData }
    localStorage.setItem('user', JSON.stringify(user.value))
  }

  function updateToken(newToken) {
    token.value = newToken
    localStorage.setItem('token', newToken)
  }

  function updateOrganizationId(newOrganizationId) {
    organizationId.value = newOrganizationId
    localStorage.setItem('organizationId', newOrganizationId)
  }

  return {
    token,
    organizationId,
    user,
    isAuthenticated,
    login,
    register,
    logout,
    updateUser,
    updateToken,
    updateOrganizationId
  }
})

