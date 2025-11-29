<template>
  <nav class="navbar">
    <div class="navbar-container">
      <div class="navbar-content">
        <!-- Logo and Brand -->
        <router-link to="/" class="navbar-brand">
          <div class="brand-logo">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
            </svg>
          </div>
          <span class="brand-text">SiteMind</span>
        </router-link>

        <!-- Navigation Links (if authenticated) -->
        <div v-if="isAuthenticated" class="navbar-links">
          <router-link
            to="/"
            class="nav-link"
            active-class="nav-link-active"
          >
            <svg class="nav-link-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
            </svg>
            <span>Dashboard</span>
          </router-link>
          <router-link
            to="/profile"
            class="nav-link"
            active-class="nav-link-active"
          >
            <svg class="nav-link-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2" />
              <circle cx="12" cy="7" r="4" />
            </svg>
            <span>Profilim</span>
          </router-link>
        </div>

        <!-- User Menu -->
        <div v-if="isAuthenticated" class="navbar-user">
          <!-- User Info -->
          <router-link to="/profile" class="user-info">
            <div class="user-details">
              <p class="user-name">{{ user?.username || 'Kullanıcı' }}</p>
              <p class="user-email">{{ user?.email || '' }}</p>
            </div>
            <div class="user-avatar">
              {{ user?.username?.charAt(0).toUpperCase() || 'U' }}
            </div>
          </router-link>

          <!-- Logout Button -->
          <button
            @click="handleLogout"
            class="logout-button"
          >
            <svg class="logout-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
            </svg>
            <span class="logout-text">Çıkış Yap</span>
          </button>
        </div>

        <!-- Login/Register Links (if not authenticated) -->
        <div v-else class="navbar-auth">
          <router-link
            to="/login"
            class="auth-link"
          >
            Giriş Yap
          </router-link>
          <router-link
            to="/register"
            class="auth-button"
          >
            Kayıt Ol
          </router-link>
        </div>
      </div>
    </div>
  </nav>
</template>

<script setup>
import { computed } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { useRouter } from 'vue-router'

const authStore = useAuthStore()
const router = useRouter()

const isAuthenticated = computed(() => authStore.isAuthenticated)
const user = computed(() => authStore.user)

const handleLogout = () => {
  authStore.logout()
  router.push('/login')
}
</script>

<style scoped>
.navbar {
  background: white;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1), 0 1px 2px rgba(0, 0, 0, 0.06);
  border-bottom: 1px solid #e5e7eb;
  position: sticky;
  top: 0;
  z-index: 100;
  backdrop-filter: blur(10px);
  background: rgba(255, 255, 255, 0.95);
}

.navbar-container {
  max-width: 1280px;
  margin: 0 auto;
  padding: 0 1rem;
}

.navbar-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  height: 72px;
}

.navbar-brand {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  text-decoration: none;
  transition: transform 0.2s ease;
}

.navbar-brand:hover {
  transform: scale(1.02);
}

.brand-logo {
  width: 44px;
  height: 44px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
  transition: all 0.3s ease;
}

.navbar-brand:hover .brand-logo {
  box-shadow: 0 6px 16px rgba(102, 126, 234, 0.4);
  transform: rotate(5deg);
}

.brand-logo svg {
  width: 24px;
  height: 24px;
  color: white;
}

.brand-text {
  font-size: 1.5rem;
  font-weight: 800;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
  letter-spacing: -0.02em;
}

.navbar-links {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.nav-link {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.625rem 1rem;
  color: #6b7280;
  font-size: 0.9375rem;
  font-weight: 500;
  text-decoration: none;
  border-radius: 10px;
  transition: all 0.2s ease;
}

.nav-link:hover {
  color: #667eea;
  background: #f3f4f6;
}

.nav-link-active {
  color: #667eea;
  background: #eef2ff;
  font-weight: 600;
}

.nav-link-icon {
  width: 20px;
  height: 20px;
}

.navbar-user {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.5rem 1rem;
  border-radius: 12px;
  transition: background 0.2s ease;
  text-decoration: none;
  cursor: pointer;
}

.user-info:hover {
  background: #f9fafb;
}

.user-details {
  text-align: right;
  display: none;
}

.user-name {
  font-size: 0.875rem;
  font-weight: 600;
  color: #1f2937;
  margin: 0;
  line-height: 1.2;
}

.user-email {
  font-size: 0.75rem;
  color: #6b7280;
  margin: 0;
  line-height: 1.2;
}

.user-avatar {
  width: 40px;
  height: 40px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-weight: 700;
  font-size: 1rem;
  box-shadow: 0 2px 8px rgba(102, 126, 234, 0.3);
  transition: all 0.3s ease;
}

.user-info:hover .user-avatar {
  transform: scale(1.05);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.logout-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.625rem 1rem;
  background: #fef2f2;
  color: #dc2626;
  border: 1px solid #fecaca;
  border-radius: 10px;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
}

.logout-button:hover {
  background: #fee2e2;
  border-color: #fca5a5;
  color: #b91c1c;
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(220, 38, 38, 0.2);
}

.logout-icon {
  width: 18px;
  height: 18px;
}

.logout-text {
  display: none;
}

.navbar-auth {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.auth-link {
  padding: 0.625rem 1rem;
  color: #6b7280;
  font-size: 0.9375rem;
  font-weight: 500;
  text-decoration: none;
  border-radius: 10px;
  transition: all 0.2s ease;
}

.auth-link:hover {
  color: #667eea;
  background: #f3f4f6;
}

.auth-button {
  padding: 0.625rem 1.5rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  font-size: 0.9375rem;
  font-weight: 600;
  text-decoration: none;
  border-radius: 10px;
  box-shadow: 0 2px 8px rgba(102, 126, 234, 0.3);
  transition: all 0.3s ease;
}

.auth-button:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.auth-button:active {
  transform: translateY(0);
}

@media (min-width: 768px) {
  .user-details {
    display: block;
  }

  .logout-text {
    display: inline;
  }
}

@media (max-width: 640px) {
  .navbar-content {
    height: 64px;
  }

  .brand-text {
    font-size: 1.25rem;
  }

  .brand-logo {
    width: 40px;
    height: 40px;
  }

  .navbar-links {
    display: none;
  }
}
</style>
