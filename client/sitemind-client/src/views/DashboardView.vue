<template>
  <div class="dashboard-container">
    <div class="dashboard-content">
      <!-- Welcome Section -->
      <div class="welcome-section">
        <div class="welcome-content">
          <h1 class="welcome-title">
            Hoş geldiniz, <span class="welcome-name">{{ user?.username || 'Kullanıcı' }}</span>!
          </h1>
          <p class="welcome-subtitle">
            SiteMind dashboard'unuza hoş geldiniz. Web sitelerinizi yönetmeye başlayın.
          </p>
        </div>
        <div class="welcome-decoration">
          <div class="decoration-circle"></div>
        </div>
      </div>

      <!-- Organization Info Card -->
      <div class="section-card organization-card">
        <div class="organization-header">
          <div class="organization-info">
            <div class="organization-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
              </svg>
            </div>
            <div class="organization-details">
              <h3 class="organization-title">Organizasyon</h3>
              <p v-if="isLoadingOrganization" class="organization-name">Yükleniyor...</p>
              <p v-else-if="organization" class="organization-name">{{ organization.name }}</p>
              <p v-else class="organization-name">Organizasyon bilgisi yüklenemedi</p>
              <p v-if="organization" class="organization-date">
                Oluşturulma: {{ formatDate(organization.createdAt) }}
              </p>
            </div>
          </div>
          <router-link to="/profile" class="organization-settings-button">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M12 15a3 3 0 100-6 3 3 0 000 6z" />
              <path d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-2 2 2 2 0 01-2-2v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83 0 2 2 0 010-2.83l.06-.06a1.65 1.65 0 00.33-1.82 1.65 1.65 0 00-1.51-1H3a2 2 0 01-2-2 2 2 0 012-2h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 010-2.83 2 2 0 012.83 0l.06.06a1.65 1.65 0 001.82.33H9a1.65 1.65 0 001-1.51V3a2 2 0 012-2 2 2 0 012 2v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 0 2 2 0 010 2.83l-.06.06a1.65 1.65 0 00-.33 1.82V9a1.65 1.65 0 001.51 1H21a2 2 0 012 2 2 2 0 01-2 2h-.09a1.65 1.65 0 00-1.51 1z" />
            </svg>
            <span>Ayarlar</span>
          </router-link>
        </div>
      </div>

      <!-- Stats Cards -->
      <div class="stats-grid">
        <!-- Total Websites Card -->
        <div class="stat-card stat-card-indigo">
          <div class="stat-content">
            <div class="stat-info">
              <p class="stat-label">Toplam Web Sitesi</p>
              <p class="stat-value">{{ stats.totalWebsites || 0 }}</p>
            </div>
            <div class="stat-icon stat-icon-indigo">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M21 12a9 9 0 01-9 9m9-9a9 9 0 00-9-9m9 9H3m9 9a9 9 0 01-9-9m9 9c1.657 0 3-4.03 3-9s-1.343-9-3-9m0 18c-1.657 0-3-4.03-3-9s1.343-9 3-9m-9 9a9 9 0 019-9" />
              </svg>
            </div>
          </div>
          <div class="stat-footer">
            <span class="stat-trend">+0% bu ay</span>
          </div>
        </div>

        <!-- Active Websites Card -->
        <div class="stat-card stat-card-green">
          <div class="stat-content">
            <div class="stat-info">
              <p class="stat-label">Aktif Web Siteleri</p>
              <p class="stat-value">{{ stats.activeWebsites || 0 }}</p>
            </div>
            <div class="stat-icon stat-icon-green">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
          </div>
          <div class="stat-footer">
            <span class="stat-trend">+0% bu ay</span>
          </div>
        </div>

        <!-- Total Pages Card -->
        <div class="stat-card stat-card-purple">
          <div class="stat-content">
            <div class="stat-info">
              <p class="stat-label">Toplam Sayfa</p>
              <p class="stat-value">{{ stats.totalPages || 0 }}</p>
            </div>
            <div class="stat-icon stat-icon-purple">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            </div>
          </div>
          <div class="stat-footer">
            <span class="stat-trend">+0% bu ay</span>
          </div>
        </div>
      </div>

      <!-- Quick Actions -->
      <div class="section-card">
        <div class="section-header">
          <h2 class="section-title">Hızlı İşlemler</h2>
          <p class="section-description">Sık kullanılan işlemlere hızlıca erişin</p>
        </div>
        <div class="actions-grid">
          <button @click="openAddWebsiteModal" class="action-button action-button-indigo">
            <div class="action-icon action-icon-indigo">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M12 4v16m8-8H4" />
              </svg>
            </div>
            <div class="action-content">
              <p class="action-title">Yeni Web Sitesi Ekle</p>
              <p class="action-description">Yeni bir web sitesi ekleyin</p>
            </div>
            <svg class="action-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M5 12h14m-7-7l7 7-7 7" />
            </svg>
          </button>

          <button class="action-button action-button-green">
            <div class="action-icon action-icon-green">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
              </svg>
            </div>
            <div class="action-content">
              <p class="action-title">İçerik İndir</p>
              <p class="action-description">Web sitelerinden içerik indirin</p>
            </div>
            <svg class="action-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M5 12h14m-7-7l7 7-7 7" />
            </svg>
          </button>

          <button class="action-button action-button-purple">
            <div class="action-icon action-icon-purple">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
              </svg>
            </div>
            <div class="action-content">
              <p class="action-title">Raporları Görüntüle</p>
              <p class="action-description">Analitik raporları inceleyin</p>
            </div>
            <svg class="action-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M5 12h14m-7-7l7 7-7 7" />
            </svg>
          </button>
        </div>
      </div>

      <!-- Websites List -->
      <div class="section-card">
        <div class="section-header">
          <h2 class="section-title">Web Sitelerim</h2>
          <p class="section-description">Eklediğiniz web sitelerini yönetin</p>
        </div>
        <div v-if="isLoadingWebsites" class="loading-state">
          <div class="loading-spinner"></div>
          <p>Yükleniyor...</p>
        </div>
        <div v-else-if="websites.length === 0" class="empty-state">
          <div class="empty-icon">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M21 12a9 9 0 01-9 9m9-9a9 9 0 00-9-9m9 9H3m9 9a9 9 0 01-9-9m9 9c1.657 0 3-4.03 3-9s-1.343-9-3-9m0 18c-1.657 0-3-4.03-3-9s1.343-9 3-9m-9 9a9 9 0 019-9" />
            </svg>
          </div>
          <p class="empty-text">Henüz web sitesi eklenmemiş</p>
          <p class="empty-subtext">Yeni bir web sitesi ekleyerek başlayın</p>
          <button @click="openAddWebsiteModal" class="empty-action-button">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M12 4v16m8-8H4" />
            </svg>
            Yeni Web Sitesi Ekle
          </button>
        </div>
        <div v-else class="websites-grid">
          <div v-for="website in websites" :key="website.id" class="website-card">
            <div class="website-card-header">
              <div class="website-info">
                <h3 class="website-name">{{ website.name }}</h3>
                <a :href="website.baseUrl" target="_blank" class="website-url" @click.stop>
                  {{ website.baseUrl }}
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M18 13v6a2 2 0 01-2 2H5a2 2 0 01-2-2V8a2 2 0 012-2h6m4-3h6v6m-11 5l5-5m0 0v5m0-5h-5" />
                  </svg>
                </a>
              </div>
              <div class="website-status" :class="getStatusClass(website.status)">
                <span class="status-dot"></span>
                <span class="status-text">{{ getStatusText(website.status) }}</span>
              </div>
            </div>
            <div class="website-card-body">
              <div class="website-stats">
                <div class="website-stat">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                  </svg>
                  <span>{{ website.pages?.length || 0 }} Sayfa</span>
                </div>
                <div v-if="website.lastCrawledAt" class="website-stat">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  <span>{{ formatDate(website.lastCrawledAt) }}</span>
                </div>
              </div>
            </div>
            <div class="website-card-actions">
              <button @click="viewWebsite(website)" class="action-btn action-btn-view" title="Görüntüle">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
                  <circle cx="12" cy="12" r="3" />
                </svg>
              </button>
              <button @click="editWebsite(website)" class="action-btn action-btn-edit" title="Düzenle">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7" />
                  <path d="M18.5 2.5a2.121 2.121 0 011.414 3.414L11 13.5l-4 1 1-4 7.586-7.586a2.121 2.121 0 013.414-1.414z" />
                </svg>
              </button>
              <button @click="deleteWebsite(website)" class="action-btn action-btn-delete" title="Sil">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M3 6h18m-2 0v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6m3 0V4a2 2 0 012-2h4a2 2 0 012 2v2" />
                </svg>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Recent Activity -->
      <div class="section-card">
        <div class="section-header">
          <h2 class="section-title">Son Aktiviteler</h2>
          <p class="section-description">Son yapılan işlemlerin özeti</p>
        </div>
        <div class="activities-list">
          <div v-if="activities.length === 0" class="empty-state">
            <div class="empty-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            </div>
            <p class="empty-text">Henüz aktivite yok</p>
            <p class="empty-subtext">İşlem yaptığınızda burada görünecek</p>
          </div>
          <div v-for="(activity, index) in activities" :key="index" class="activity-item">
            <div class="activity-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M13 10V3L4 14h7v7l9-11h-7z" />
              </svg>
            </div>
            <div class="activity-content">
              <p class="activity-title">{{ activity.title }}</p>
              <p class="activity-time">{{ activity.time }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Add Website Modal -->
    <div v-if="showAddWebsiteModal" class="modal-overlay" @click.self="closeAddWebsiteModal">
      <div class="modal-container">
        <div class="modal-header">
          <h2 class="modal-title">{{ editingWebsiteId ? 'Web Sitesi Düzenle' : 'Yeni Web Sitesi Ekle' }}</h2>
          <button @click="closeAddWebsiteModal" class="modal-close-button">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
        <form @submit.prevent="handleAddWebsite" class="modal-form">
          <div class="form-group">
            <label for="website-name" class="form-label">Web Sitesi Adı</label>
            <input
              id="website-name"
              v-model="newWebsite.name"
              type="text"
              class="form-input"
              placeholder="Örn: SiteMind Blog"
              required
            />
          </div>
          <div class="form-group">
            <label for="website-url" class="form-label">Web Sitesi URL</label>
            <input
              id="website-url"
              v-model="newWebsite.baseUrl"
              type="url"
              class="form-input"
              placeholder="https://example.com"
              required
            />
          </div>
          <div v-if="addWebsiteError" class="form-error">
            {{ addWebsiteError }}
          </div>
          <div class="modal-actions">
            <button type="button" @click="closeAddWebsiteModal" class="button-secondary">
              İptal
            </button>
            <button type="submit" class="button-primary" :disabled="isAddingWebsite">
              <span v-if="isAddingWebsite">{{ editingWebsiteId ? 'Güncelleniyor...' : 'Ekleniyor...' }}</span>
              <span v-else>{{ editingWebsiteId ? 'Güncelle' : 'Ekle' }}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { websiteService } from '../api/websiteService'
import { organizationService } from '../api/organizationService'

const authStore = useAuthStore()

const user = computed(() => authStore.user)

const stats = ref({
  totalWebsites: 0,
  activeWebsites: 0,
  totalPages: 0
})

const activities = ref([])
const websites = ref([])
const isLoadingWebsites = ref(false)

const organization = ref(null)
const isLoadingOrganization = ref(false)

const showAddWebsiteModal = ref(false)
const editingWebsiteId = ref(null)
const newWebsite = ref({
  name: '',
  baseUrl: ''
})
const isAddingWebsite = ref(false)
const addWebsiteError = ref('')

const openAddWebsiteModal = () => {
  editingWebsiteId.value = null
  showAddWebsiteModal.value = true
  newWebsite.value = { name: '', baseUrl: '' }
  addWebsiteError.value = ''
}

const closeAddWebsiteModal = () => {
  showAddWebsiteModal.value = false
  editingWebsiteId.value = null
  newWebsite.value = { name: '', baseUrl: '' }
  addWebsiteError.value = ''
}

const handleAddWebsite = async () => {
  if (!newWebsite.value.name || !newWebsite.value.baseUrl) {
    addWebsiteError.value = 'Lütfen tüm alanları doldurun.'
    return
  }

  isAddingWebsite.value = true
  addWebsiteError.value = ''

  try {
    if (editingWebsiteId.value) {
      // Güncelleme
      await websiteService.updateWebsite(editingWebsiteId.value, newWebsite.value.name, newWebsite.value.baseUrl)
      activities.value.unshift({
        title: `${newWebsite.value.name} web sitesi güncellendi`,
        time: 'Az önce'
      })
    } else {
      // Yeni ekleme
      await websiteService.createWebsite(newWebsite.value.name, newWebsite.value.baseUrl)
      activities.value.unshift({
        title: `${newWebsite.value.name} web sitesi eklendi`,
        time: 'Az önce'
      })
    }
    
    // Başarılı oldu, modal'ı kapat ve istatistikleri güncelle
    closeAddWebsiteModal()
    
    // İstatistikleri ve web sitelerini yeniden yükle
    await Promise.all([loadStats(), loadWebsites()])
  } catch (error) {
    addWebsiteError.value = error.response?.data?.error || error.message || (editingWebsiteId.value ? 'Web sitesi güncellenirken bir hata oluştu.' : 'Web sitesi eklenirken bir hata oluştu.')
  } finally {
    isAddingWebsite.value = false
  }
}

const loadStats = async () => {
  try {
    const websitesData = await websiteService.getWebsites()
    stats.value = {
      totalWebsites: websitesData.length,
      activeWebsites: websitesData.filter(w => w.status === 1).length, // WebsiteStatus.Active = 1
      totalPages: websitesData.reduce((sum, w) => sum + (w.pages?.length || 0), 0)
    }
  } catch (error) {
    console.error('İstatistikler yüklenirken hata:', error)
  }
}

const loadWebsites = async () => {
  isLoadingWebsites.value = true
  try {
    websites.value = await websiteService.getWebsites()
  } catch (error) {
    console.error('Web siteleri yüklenirken hata:', error)
  } finally {
    isLoadingWebsites.value = false
  }
}

const getStatusText = (status) => {
  const statusMap = {
    0: 'Oluşturuldu',
    1: 'Aktif',
    2: 'Hata',
    3: 'Taranıyor'
  }
  return statusMap[status] || 'Bilinmiyor'
}

const getStatusClass = (status) => {
  const classMap = {
    0: 'status-created',
    1: 'status-active',
    2: 'status-error',
    3: 'status-crawling'
  }
  return classMap[status] || 'status-unknown'
}

const formatDate = (dateString) => {
  if (!dateString) return 'Henüz taranmadı'
  const date = new Date(dateString)
  const now = new Date()
  const diffMs = now - date
  const diffMins = Math.floor(diffMs / 60000)
  const diffHours = Math.floor(diffMs / 3600000)
  const diffDays = Math.floor(diffMs / 86400000)

  if (diffMins < 1) return 'Az önce'
  if (diffMins < 60) return `${diffMins} dakika önce`
  if (diffHours < 24) return `${diffHours} saat önce`
  if (diffDays < 7) return `${diffDays} gün önce`
  
  return date.toLocaleDateString('tr-TR', { day: 'numeric', month: 'short', year: 'numeric' })
}

const viewWebsite = (website) => {
  // TODO: Web sitesi detay sayfasına yönlendir
  console.log('View website:', website)
  // window.open(website.baseUrl, '_blank')
}

const editWebsite = (website) => {
  newWebsite.value = {
    name: website.name,
    baseUrl: website.baseUrl
  }
  editingWebsiteId.value = website.id
  showAddWebsiteModal.value = true
  addWebsiteError.value = ''
}

const deleteWebsite = async (website) => {
  if (!confirm(`${website.name} web sitesini silmek istediğinize emin misiniz?`)) {
    return
  }

  try {
    await websiteService.deleteWebsite(website.id)
    await loadWebsites()
    await loadStats()
    activities.value.unshift({
      title: `${website.name} web sitesi silindi`,
      time: 'Az önce'
    })
  } catch (error) {
    console.error('Web sitesi silinirken hata:', error)
    alert('Web sitesi silinirken bir hata oluştu.')
  }
}

const loadOrganization = async () => {
  isLoadingOrganization.value = true
  try {
    organization.value = await organizationService.getCurrentOrganization()
  } catch (error) {
    console.error('Organization yüklenirken hata:', error)
    organization.value = null
  } finally {
    isLoadingOrganization.value = false
  }
}

onMounted(() => {
  loadStats()
  loadWebsites()
  loadOrganization()
  activities.value = []
})
</script>

<style scoped>
.dashboard-container {
  min-height: calc(100vh - 64px);
  background: linear-gradient(to bottom, #f9fafb, #f3f4f6);
  padding: 2rem 1rem;
}

.dashboard-content {
  max-width: 1280px;
  margin: 0 auto;
}

.welcome-section {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 24px;
  padding: 3rem 2.5rem;
  margin-bottom: 2rem;
  position: relative;
  overflow: hidden;
  box-shadow: 0 10px 40px rgba(102, 126, 234, 0.3);
}

.welcome-content {
  position: relative;
  z-index: 1;
}

.welcome-title {
  font-size: 2.25rem;
  font-weight: 800;
  color: white;
  margin: 0 0 0.75rem 0;
  letter-spacing: -0.02em;
}

.welcome-name {
  background: linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
}

.welcome-subtitle {
  font-size: 1.125rem;
  color: rgba(255, 255, 255, 0.9);
  margin: 0;
}

.welcome-decoration {
  position: absolute;
  top: -50%;
  right: -10%;
  width: 400px;
  height: 400px;
  background: radial-gradient(circle, rgba(255, 255, 255, 0.1) 0%, transparent 70%);
  border-radius: 50%;
}

.decoration-circle {
  width: 100%;
  height: 100%;
  background: radial-gradient(circle, rgba(255, 255, 255, 0.15) 0%, transparent 70%);
  border-radius: 50%;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}

.stat-card {
  background: white;
  border-radius: 20px;
  padding: 1.75rem;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.05);
  border: 1px solid #e5e7eb;
  transition: all 0.3s ease;
  position: relative;
  overflow: hidden;
}

.stat-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 4px;
  background: linear-gradient(90deg, transparent, currentColor, transparent);
  opacity: 0;
  transition: opacity 0.3s ease;
}

.stat-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 24px rgba(0, 0, 0, 0.1);
}

.stat-card:hover::before {
  opacity: 1;
}

.stat-card-indigo::before {
  background: linear-gradient(90deg, transparent, #667eea, transparent);
}

.stat-card-green::before {
  background: linear-gradient(90deg, transparent, #10b981, transparent);
}

.stat-card-purple::before {
  background: linear-gradient(90deg, transparent, #8b5cf6, transparent);
}

.stat-content {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1rem;
}

.stat-info {
  flex: 1;
}

.stat-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #6b7280;
  margin: 0 0 0.5rem 0;
}

.stat-value {
  font-size: 2.5rem;
  font-weight: 800;
  color: #1f2937;
  margin: 0;
  line-height: 1;
}

.stat-icon {
  width: 56px;
  height: 56px;
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.stat-icon svg {
  width: 28px;
  height: 28px;
}

.stat-icon-indigo {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.stat-icon-green {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  color: white;
}

.stat-icon-purple {
  background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%);
  color: white;
}

.stat-footer {
  padding-top: 1rem;
  border-top: 1px solid #f3f4f6;
}

.stat-trend {
  font-size: 0.8125rem;
  color: #10b981;
  font-weight: 500;
}

.section-card {
  background: white;
  border-radius: 20px;
  padding: 2rem;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.05);
  border: 1px solid #e5e7eb;
  margin-bottom: 2rem;
}

.organization-card {
  background: linear-gradient(135deg, #f0f9ff 0%, #e0f2fe 100%);
  border: 2px solid #bae6fd;
}

.organization-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1.5rem;
}

.organization-info {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  flex: 1;
}

.organization-icon {
  width: 64px;
  height: 64px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  flex-shrink: 0;
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
}

.organization-icon svg {
  width: 32px;
  height: 32px;
}

.organization-details {
  flex: 1;
  min-width: 0;
}

.organization-title {
  font-size: 0.875rem;
  font-weight: 600;
  color: #6b7280;
  margin: 0 0 0.5rem 0;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.organization-name {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1f2937;
  margin: 0 0 0.25rem 0;
  word-break: break-word;
}

.organization-date {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

.organization-settings-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1.25rem;
  background: white;
  color: #667eea;
  border: 2px solid #667eea;
  border-radius: 12px;
  font-size: 0.9375rem;
  font-weight: 600;
  text-decoration: none;
  transition: all 0.3s ease;
  box-shadow: 0 2px 8px rgba(102, 126, 234, 0.2);
  flex-shrink: 0;
}

.organization-settings-button:hover {
  background: #667eea;
  color: white;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.organization-settings-button svg {
  width: 18px;
  height: 18px;
}

.section-header {
  margin-bottom: 1.5rem;
}

.section-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1f2937;
  margin: 0 0 0.5rem 0;
  letter-spacing: -0.01em;
}

.section-description {
  font-size: 0.9375rem;
  color: #6b7280;
  margin: 0;
}

.actions-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 1rem;
}

.action-button {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1.25rem;
  background: white;
  border: 2px solid #e5e7eb;
  border-radius: 16px;
  cursor: pointer;
  transition: all 0.3s ease;
  text-align: left;
  position: relative;
  overflow: hidden;
}

.action-button::before {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(102, 126, 234, 0.05), transparent);
  transition: left 0.5s;
}

.action-button:hover::before {
  left: 100%;
}

.action-button:hover {
  border-color: currentColor;
  transform: translateX(4px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.action-button-indigo {
  color: #667eea;
}

.action-button-green {
  color: #10b981;
}

.action-button-purple {
  color: #8b5cf6;
}

.action-icon {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: transform 0.3s ease;
}

.action-button:hover .action-icon {
  transform: scale(1.1);
}

.action-icon svg {
  width: 24px;
  height: 24px;
}

.action-icon-indigo {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.action-icon-green {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  color: white;
}

.action-icon-purple {
  background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%);
  color: white;
}

.action-content {
  flex: 1;
}

.action-title {
  font-size: 1rem;
  font-weight: 600;
  color: #1f2937;
  margin: 0 0 0.25rem 0;
}

.action-description {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

.action-arrow {
  width: 20px;
  height: 20px;
  color: #9ca3af;
  flex-shrink: 0;
  transition: transform 0.3s ease;
}

.action-button:hover .action-arrow {
  transform: translateX(4px);
  color: currentColor;
}

.activities-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.empty-state {
  text-align: center;
  padding: 3rem 1rem;
}

.empty-icon {
  width: 64px;
  height: 64px;
  margin: 0 auto 1rem;
  color: #d1d5db;
}

.empty-icon svg {
  width: 100%;
  height: 100%;
}

.empty-text {
  font-size: 1rem;
  font-weight: 600;
  color: #6b7280;
  margin: 0 0 0.5rem 0;
}

.empty-subtext {
  font-size: 0.875rem;
  color: #9ca3af;
  margin: 0;
}

.activity-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem;
  background: #f9fafb;
  border-radius: 12px;
  transition: all 0.2s ease;
  border: 1px solid transparent;
}

.activity-item:hover {
  background: #f3f4f6;
  border-color: #e5e7eb;
  transform: translateX(4px);
}

.activity-icon {
  width: 40px;
  height: 40px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  flex-shrink: 0;
}

.activity-icon svg {
  width: 20px;
  height: 20px;
}

.activity-content {
  flex: 1;
}

.activity-title {
  font-size: 0.9375rem;
  font-weight: 600;
  color: #1f2937;
  margin: 0 0 0.25rem 0;
}

.activity-time {
  font-size: 0.8125rem;
  color: #6b7280;
  margin: 0;
}

/* Websites List Styles */
.loading-state {
  text-align: center;
  padding: 3rem 1rem;
  color: #6b7280;
}

.loading-spinner {
  width: 40px;
  height: 40px;
  margin: 0 auto 1rem;
  border: 3px solid #e5e7eb;
  border-top-color: #667eea;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.empty-action-button {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  margin-top: 1rem;
  padding: 0.75rem 1.5rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 12px;
  font-size: 0.9375rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 8px rgba(102, 126, 234, 0.3);
}

.empty-action-button:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.empty-action-button svg {
  width: 20px;
  height: 20px;
}

.websites-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 1.5rem;
}

.website-card {
  background: white;
  border: 2px solid #e5e7eb;
  border-radius: 16px;
  padding: 1.5rem;
  transition: all 0.3s ease;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.website-card:hover {
  border-color: #667eea;
  box-shadow: 0 8px 24px rgba(102, 126, 234, 0.15);
  transform: translateY(-2px);
}

.website-card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 1rem;
}

.website-info {
  flex: 1;
  min-width: 0;
}

.website-name {
  font-size: 1.125rem;
  font-weight: 700;
  color: #1f2937;
  margin: 0 0 0.5rem 0;
  word-break: break-word;
}

.website-url {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
  font-size: 0.875rem;
  color: #667eea;
  text-decoration: none;
  word-break: break-all;
  transition: color 0.2s ease;
}

.website-url:hover {
  color: #764ba2;
  text-decoration: underline;
}

.website-url svg {
  width: 14px;
  height: 14px;
  flex-shrink: 0;
}

.website-status {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.375rem 0.75rem;
  border-radius: 8px;
  font-size: 0.8125rem;
  font-weight: 600;
  white-space: nowrap;
  flex-shrink: 0;
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;
}

.status-created {
  background: #fef3c7;
  color: #92400e;
}

.status-created .status-dot {
  background: #f59e0b;
}

.status-active {
  background: #d1fae5;
  color: #065f46;
}

.status-active .status-dot {
  background: #10b981;
}

.status-error {
  background: #fee2e2;
  color: #991b1b;
}

.status-error .status-dot {
  background: #ef4444;
}

.status-crawling {
  background: #dbeafe;
  color: #1e40af;
}

.status-crawling .status-dot {
  background: #3b82f6;
  animation: pulse 2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.5;
  }
}

.status-text {
  font-size: 0.8125rem;
}

.website-card-body {
  padding-top: 0.75rem;
  border-top: 1px solid #f3f4f6;
}

.website-stats {
  display: flex;
  gap: 1.5rem;
  flex-wrap: wrap;
}

.website-stat {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: #6b7280;
}

.website-stat svg {
  width: 16px;
  height: 16px;
  color: #9ca3af;
}

.website-card-actions {
  display: flex;
  gap: 0.5rem;
  padding-top: 0.75rem;
  border-top: 1px solid #f3f4f6;
}

.action-btn {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0.625rem;
  border: 2px solid #e5e7eb;
  border-radius: 10px;
  background: white;
  cursor: pointer;
  transition: all 0.2s ease;
}

.action-btn svg {
  width: 18px;
  height: 18px;
}

.action-btn-view {
  color: #667eea;
  border-color: #e0e7ff;
}

.action-btn-view:hover {
  background: #eef2ff;
  border-color: #667eea;
}

.action-btn-edit {
  color: #10b981;
  border-color: #d1fae5;
}

.action-btn-edit:hover {
  background: #d1fae5;
  border-color: #10b981;
}

.action-btn-delete {
  color: #ef4444;
  border-color: #fee2e2;
}

.action-btn-delete:hover {
  background: #fee2e2;
  border-color: #ef4444;
}

/* Modal Styles */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
  backdrop-filter: blur(4px);
}

.modal-container {
  background: white;
  border-radius: 20px;
  width: 100%;
  max-width: 500px;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
  animation: modalSlideIn 0.3s ease-out;
}

@keyframes modalSlideIn {
  from {
    opacity: 0;
    transform: translateY(-20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem 2rem;
  border-bottom: 1px solid #e5e7eb;
}

.modal-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1f2937;
  margin: 0;
  letter-spacing: -0.01em;
}

.modal-close-button {
  width: 36px;
  height: 36px;
  border: none;
  background: #f3f4f6;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s ease;
  color: #6b7280;
}

.modal-close-button:hover {
  background: #e5e7eb;
  color: #1f2937;
}

.modal-close-button svg {
  width: 20px;
  height: 20px;
}

.modal-form {
  padding: 2rem;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-label {
  display: block;
  font-size: 0.9375rem;
  font-weight: 600;
  color: #374151;
  margin-bottom: 0.5rem;
}

.form-input {
  width: 100%;
  padding: 0.75rem 1rem;
  border: 2px solid #e5e7eb;
  border-radius: 12px;
  font-size: 1rem;
  transition: all 0.2s ease;
  background: white;
  color: #1f2937;
  box-sizing: border-box;
}

.form-input:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.form-input::placeholder {
  color: #9ca3af;
}

.form-error {
  padding: 0.75rem 1rem;
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 12px;
  color: #dc2626;
  font-size: 0.875rem;
  margin-bottom: 1rem;
}

.modal-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
  margin-top: 2rem;
}

.button-primary {
  padding: 0.75rem 1.5rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 12px;
  font-size: 0.9375rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 8px rgba(102, 126, 234, 0.3);
}

.button-primary:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.button-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.button-secondary {
  padding: 0.75rem 1.5rem;
  background: white;
  color: #6b7280;
  border: 2px solid #e5e7eb;
  border-radius: 12px;
  font-size: 0.9375rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.button-secondary:hover {
  background: #f9fafb;
  border-color: #d1d5db;
  color: #374151;
}

@media (max-width: 768px) {
  .dashboard-container {
    padding: 1rem 0.5rem;
  }

  .welcome-section {
    padding: 2rem 1.5rem;
  }

  .welcome-title {
    font-size: 1.75rem;
  }

  .stats-grid {
    grid-template-columns: 1fr;
  }

  .actions-grid {
    grid-template-columns: 1fr;
  }

  .section-card {
    padding: 1.5rem;
  }

  .websites-grid {
    grid-template-columns: 1fr;
  }

  .website-card-header {
    flex-direction: column;
    align-items: flex-start;
  }

  .website-status {
    align-self: flex-start;
  }

  .modal-container {
    max-width: 100%;
    margin: 1rem;
  }

  .modal-header {
    padding: 1.25rem 1.5rem;
  }

  .modal-form {
    padding: 1.5rem;
  }

  .modal-actions {
    flex-direction: column-reverse;
  }

  .button-primary,
  .button-secondary {
    width: 100%;
  }

  .organization-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 1rem;
  }

  .organization-settings-button {
    width: 100%;
    justify-content: center;
  }

  .organization-info {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
