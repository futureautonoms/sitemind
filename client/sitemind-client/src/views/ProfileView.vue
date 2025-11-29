<template>
  <div class="profile-container">
    <div class="profile-content">
      <!-- Profile Header -->
      <div class="profile-header">
        <div class="profile-avatar-section">
          <div class="profile-avatar">
            {{ user?.username?.charAt(0).toUpperCase() || 'U' }}
          </div>
          <button class="avatar-edit-button" @click="showAvatarEdit = true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7" />
              <path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" />
            </svg>
          </button>
        </div>
        <div class="profile-info">
          <h1 class="profile-name">{{ user?.username || 'Kullanıcı' }}</h1>
          <p class="profile-email">{{ user?.email || '' }}</p>
        </div>
      </div>

      <!-- Profile Details Card -->
      <div class="profile-card">
        <div class="card-header">
          <h2 class="card-title">Profil Bilgileri</h2>
          <p class="card-description">Hesap bilgilerinizi görüntüleyin ve düzenleyin</p>
        </div>

        <div class="form-section">
          <div class="form-group">
            <label class="form-label">Kullanıcı Adı</label>
            <div class="form-input-wrapper">
              <input
                v-model="formData.username"
                type="text"
                class="form-input"
                placeholder="Kullanıcı adınız"
                :disabled="!isEditing"
              />
              <svg class="form-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2" />
                <circle cx="12" cy="7" r="4" />
              </svg>
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">E-posta Adresi</label>
            <div class="form-input-wrapper">
              <input
                v-model="formData.email"
                type="email"
                class="form-input"
                placeholder="E-posta adresiniz"
                :disabled="!isEditing"
              />
              <svg class="form-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z" />
                <polyline points="22,6 12,13 2,6" />
              </svg>
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">Organizasyon ID</label>
            <div class="form-input-wrapper">
              <input
                :value="organizationId"
                type="text"
                class="form-input"
                disabled
              />
              <svg class="form-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" />
                <circle cx="9" cy="7" r="4" />
                <path d="M23 21v-2a4 4 0 00-3-3.87" />
                <path d="M16 3.13a4 4 0 010 7.75" />
              </svg>
            </div>
          </div>
        </div>

        <div class="card-actions">
          <button
            v-if="!isEditing"
            @click="startEditing"
            class="action-button action-button-primary"
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7" />
              <path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" />
            </svg>
            <span>Düzenle</span>
          </button>
          <div v-else class="action-buttons">
            <button
              @click="cancelEditing"
              class="action-button action-button-secondary"
            >
              <span>İptal</span>
            </button>
            <button
              @click="saveChanges"
              class="action-button action-button-primary"
              :disabled="isSaving"
            >
              <svg v-if="!isSaving" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="20 6 9 17 4 12" />
              </svg>
              <span v-if="isSaving">Kaydediliyor...</span>
              <span v-else>Kaydet</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Organization Management Card -->
      <div class="profile-card">
        <div class="card-header">
          <h2 class="card-title">Organizasyon Yönetimi</h2>
          <p class="card-description">Organizasyon bilgilerinizi görüntüleyin ve yönetin</p>
        </div>

        <div v-if="isLoadingOrganization" class="loading-state">
          <div class="loading-spinner"></div>
          <p>Yükleniyor...</p>
        </div>

        <div v-else-if="organizationError" class="error-state">
          <svg class="error-icon" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
          </svg>
          <p>{{ organizationError }}</p>
          <button @click="loadOrganization" class="retry-button">
            Tekrar Dene
          </button>
        </div>

        <div v-else-if="organization" class="form-section">
          <div class="form-group">
            <label class="form-label">Organizasyon Adı</label>
            <div class="form-input-wrapper">
              <input
                v-model="organizationForm.name"
                type="text"
                class="form-input"
                placeholder="Organizasyon adı"
                :disabled="!isEditingOrganization"
              />
              <svg class="form-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
              </svg>
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">Oluşturulma Tarihi</label>
            <div class="form-input-wrapper">
              <input
                :value="formatDate(organization.createdAt)"
                type="text"
                class="form-input"
                disabled
              />
              <svg class="form-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="12" cy="12" r="10" />
                <polyline points="12 6 12 12 16 14" />
              </svg>
            </div>
          </div>

          <div v-if="updateOrganizationError" class="form-error">
            {{ updateOrganizationError }}
          </div>
        </div>

        <div class="card-actions">
          <div v-if="!isEditingOrganization && organization" class="action-buttons">
            <button
              @click="showCreateOrganizationModal = true"
              class="action-button action-button-success"
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M12 4v16m8-8H4" />
              </svg>
              <span>Yeni Organizasyon Oluştur</span>
            </button>
            <button
              @click="startEditingOrganization"
              class="action-button action-button-primary"
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7" />
                <path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" />
              </svg>
              <span>Düzenle</span>
            </button>
          </div>
          <div v-else-if="organization" class="action-buttons">
            <button
              @click="cancelEditingOrganization"
              class="action-button action-button-secondary"
            >
              <span>İptal</span>
            </button>
            <button
              @click="saveOrganization"
              class="action-button action-button-primary"
              :disabled="isSavingOrganization"
            >
              <svg v-if="!isSavingOrganization" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="20 6 9 17 4 12" />
              </svg>
              <span v-if="isSavingOrganization">Kaydediliyor...</span>
              <span v-else>Kaydet</span>
            </button>
          </div>
        </div>

        <!-- Switch Organization Section -->
        <div v-if="organization" class="switch-organization-section">
          <div class="section-header-small">
            <h3 class="section-title-small">Organizasyonlar Arası Geçiş</h3>
            <p class="section-description-small">Diğer organizasyonlarınıza geçiş yapın</p>
          </div>

          <div v-if="isLoadingAvailableOrganizations" class="loading-state-small">
            <div class="loading-spinner-small"></div>
            <p>Yükleniyor...</p>
          </div>

          <div v-else-if="availableOrganizationsError" class="error-state-small">
            <p>{{ availableOrganizationsError }}</p>
            <button @click="loadAvailableOrganizations" class="retry-button-small">
              Tekrar Dene
            </button>
          </div>

          <div v-else-if="availableOrganizations.length === 0" class="empty-state-small">
            <p>Başka organizasyonunuz bulunmuyor.</p>
          </div>

          <div v-else class="organizations-list">
            <div
              v-for="org in availableOrganizations"
              :key="org.id"
              class="organization-item"
              :class="{ 'organization-item-active': org.id === organization.id }"
            >
              <div class="organization-item-info">
                <div class="organization-item-icon">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
                  </svg>
                </div>
                <div class="organization-item-details">
                  <h4 class="organization-item-name">{{ org.name }}</h4>
                  <p class="organization-item-date">{{ formatDate(org.createdAt) }}</p>
                </div>
              </div>
              <button
                v-if="org.id !== organization.id"
                @click="switchToOrganization(org)"
                class="organization-switch-button"
                :disabled="isSwitchingOrganization"
              >
                <span v-if="isSwitchingOrganization && switchingOrganizationId === org.id">Geçiliyor...</span>
                <span v-else>Geçiş Yap</span>
              </button>
              <div v-else class="organization-current-badge">
                <svg viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
                </svg>
                <span>Mevcut</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Delete Organization Section -->
        <div v-if="organization" class="delete-section">
          <div class="delete-warning">
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
            </svg>
            <div class="delete-warning-content">
              <h3 class="delete-warning-title">Tehlikeli Bölge</h3>
              <p class="delete-warning-text">Organizasyonu silmek, tüm verilerinizi kalıcı olarak silecektir. Bu işlem geri alınamaz.</p>
            </div>
          </div>
          <button
            @click="showDeleteOrganizationConfirm = true"
            class="action-button action-button-danger"
            :disabled="isDeletingOrganization"
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="3 6 5 6 21 6" />
              <path d="M19 6v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6m3 0V4a2 2 0 012-2h4a2 2 0 012 2v2" />
            </svg>
            <span v-if="isDeletingOrganization">Siliniyor...</span>
            <span v-else>Organizasyonu Sil</span>
          </button>
        </div>
      </div>

      <!-- Create Organization Modal -->
      <div v-if="showCreateOrganizationModal" class="modal-overlay" @click.self="showCreateOrganizationModal = false">
        <div class="modal-container">
          <div class="modal-header">
            <h2 class="modal-title">Yeni Organizasyon Oluştur</h2>
            <button @click="showCreateOrganizationModal = false" class="modal-close-button">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
          <div class="modal-content">
            <div class="form-group">
              <label class="form-label">Organizasyon Adı</label>
              <div class="form-input-wrapper">
                <input
                  v-model="newOrganizationForm.name"
                  type="text"
                  class="form-input"
                  placeholder="Yeni organizasyon adı"
                  @keyup.enter="createNewOrganization"
                />
                <svg class="form-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
                </svg>
              </div>
            </div>
            <div class="modal-info">
              <svg viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
              </svg>
              <p>Yeni organizasyon oluşturduktan sonra bu organizasyona geçiş yapacaksınız.</p>
            </div>
            <div v-if="createOrganizationError" class="form-error">
              {{ createOrganizationError }}
            </div>
          </div>
          <div class="modal-actions">
            <button
              @click="showCreateOrganizationModal = false"
              class="button-secondary"
            >
              İptal
            </button>
            <button
              @click="createNewOrganization"
              class="button-primary"
              :disabled="isCreatingOrganization"
            >
              <span v-if="isCreatingOrganization">Oluşturuluyor...</span>
              <span v-else>Oluştur</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Delete Organization Confirmation Modal -->
      <div v-if="showDeleteOrganizationConfirm" class="modal-overlay" @click.self="showDeleteOrganizationConfirm = false">
        <div class="modal-container">
          <div class="modal-header">
            <h2 class="modal-title">Organizasyonu Sil</h2>
            <button @click="showDeleteOrganizationConfirm = false" class="modal-close-button">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
          <div class="modal-content">
            <div class="modal-warning">
              <svg viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
              </svg>
              <p>Bu işlem geri alınamaz. Organizasyonunuz ve tüm verileriniz kalıcı olarak silinecektir.</p>
            </div>
            <p class="modal-text">Devam etmek istediğinize emin misiniz?</p>
            <div v-if="deleteOrganizationError" class="form-error">
              {{ deleteOrganizationError }}
            </div>
          </div>
          <div class="modal-actions">
            <button
              @click="showDeleteOrganizationConfirm = false"
              class="button-secondary"
            >
              İptal
            </button>
            <button
              @click="confirmDeleteOrganization"
              class="button-danger"
              :disabled="isDeletingOrganization"
            >
              <span v-if="isDeletingOrganization">Siliniyor...</span>
              <span v-else>Evet, Sil</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Account Settings Card -->
      <div class="profile-card">
        <div class="card-header">
          <h2 class="card-title">Hesap Ayarları</h2>
          <p class="card-description">Hesap güvenliği ve tercihleri</p>
        </div>

        <div class="settings-list">
          <div class="setting-item">
            <div class="setting-info">
              <h3 class="setting-title">Şifre Değiştir</h3>
              <p class="setting-description">Hesap güvenliğiniz için şifrenizi düzenli olarak güncelleyin</p>
            </div>
            <button class="setting-button" @click="showPasswordChange = true">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
                <path d="M7 11V7a5 5 0 0110 0v4" />
              </svg>
              <span>Değiştir</span>
            </button>
          </div>

          <div class="setting-item">
            <div class="setting-info">
              <h3 class="setting-title">Hesap Sil</h3>
              <p class="setting-description">Hesabınızı kalıcı olarak silin. Bu işlem geri alınamaz.</p>
            </div>
            <button class="setting-button setting-button-danger" @click="showDeleteConfirm = true">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="3 6 5 6 21 6" />
                <path d="M19 6v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6m3 0V4a2 2 0 012-2h4a2 2 0 012 2v2" />
              </svg>
              <span>Sil</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { organizationService } from '../api/organizationService'
import { useRouter } from 'vue-router'

const authStore = useAuthStore()
const router = useRouter()

const user = computed(() => authStore.user)
const organizationId = computed(() => authStore.organizationId)

const isEditing = ref(false)
const isSaving = ref(false)
const showAvatarEdit = ref(false)
const showPasswordChange = ref(false)
const showDeleteConfirm = ref(false)

const formData = ref({
  username: '',
  email: ''
})

// Organization state
const organization = ref(null)
const isLoadingOrganization = ref(false)
const organizationError = ref('')
const isEditingOrganization = ref(false)
const isSavingOrganization = ref(false)
const updateOrganizationError = ref('')
const showDeleteOrganizationConfirm = ref(false)
const isDeletingOrganization = ref(false)
const deleteOrganizationError = ref('')
const showCreateOrganizationModal = ref(false)
const isCreatingOrganization = ref(false)
const createOrganizationError = ref('')
const availableOrganizations = ref([])
const isLoadingAvailableOrganizations = ref(false)
const availableOrganizationsError = ref('')
const isSwitchingOrganization = ref(false)
const switchingOrganizationId = ref(null)

const organizationForm = ref({
  name: ''
})

const newOrganizationForm = ref({
  name: ''
})

const loadOrganization = async () => {
  isLoadingOrganization.value = true
  organizationError.value = ''
  try {
    organization.value = await organizationService.getCurrentOrganization()
    organizationForm.value = {
      name: organization.value.name || ''
    }
    // Load available organizations after current organization is loaded
    await loadAvailableOrganizations()
  } catch (error) {
    console.error('Organization yüklenirken hata:', error)
    organizationError.value = error.response?.data?.error || 'Organizasyon bilgisi yüklenemedi'
    organization.value = null
  } finally {
    isLoadingOrganization.value = false
  }
}

const loadAvailableOrganizations = async () => {
  isLoadingAvailableOrganizations.value = true
  availableOrganizationsError.value = ''
  try {
    availableOrganizations.value = await organizationService.getAvailableOrganizations()
  } catch (error) {
    console.error('Available organizations yüklenirken hata:', error)
    availableOrganizationsError.value = error.response?.data?.error || 'Organizasyonlar yüklenemedi'
    availableOrganizations.value = []
  } finally {
    isLoadingAvailableOrganizations.value = false
  }
}

const switchToOrganization = async (targetOrganization) => {
  if (!targetOrganization || !targetOrganization.id) return

  isSwitchingOrganization.value = true
  switchingOrganizationId.value = targetOrganization.id
  
  try {
    // Switch to target organization
    const switchResult = await organizationService.switchOrganization(targetOrganization.id)
    
    // Update auth store with new token and organization ID
    if (switchResult.token) {
      authStore.updateToken(switchResult.token)
    }
    authStore.updateOrganizationId(targetOrganization.id.toString())
    
    // Reload organization data and available organizations
    await Promise.all([
      loadOrganization(),
      loadAvailableOrganizations()
    ])
    
    // Show success message
    alert(`${targetOrganization.name} organizasyonuna geçiş yapıldı!`)
  } catch (error) {
    console.error('Organization geçişi sırasında hata:', error)
    alert(error.response?.data?.error || 'Organizasyon geçişi sırasında bir hata oluştu')
  } finally {
    isSwitchingOrganization.value = false
    switchingOrganizationId.value = null
  }
}

const startEditingOrganization = () => {
  if (organization.value) {
    organizationForm.value = {
      name: organization.value.name || ''
    }
  }
  isEditingOrganization.value = true
  updateOrganizationError.value = ''
}

const cancelEditingOrganization = () => {
  if (organization.value) {
    organizationForm.value = {
      name: organization.value.name || ''
    }
  }
  isEditingOrganization.value = false
  updateOrganizationError.value = ''
}

const saveOrganization = async () => {
  if (!organization.value || !organizationForm.value.name.trim()) {
    updateOrganizationError.value = 'Organizasyon adı boş olamaz'
    return
  }

  isSavingOrganization.value = true
  updateOrganizationError.value = ''
  
  try {
    const updated = await organizationService.updateOrganization(
      organization.value.id,
      organizationForm.value.name.trim()
    )
    organization.value = updated
    isEditingOrganization.value = false
  } catch (error) {
    console.error('Organization güncellenirken hata:', error)
    updateOrganizationError.value = error.response?.data?.error || 'Organizasyon güncellenirken bir hata oluştu'
  } finally {
    isSavingOrganization.value = false
  }
}

const confirmDeleteOrganization = async () => {
  if (!organization.value) return

  isDeletingOrganization.value = true
  deleteOrganizationError.value = ''
  
  try {
    await organizationService.deleteOrganization(organization.value.id)
    // Organization silindi, logout yap
    authStore.logout()
    router.push('/login')
  } catch (error) {
    console.error('Organization silinirken hata:', error)
    deleteOrganizationError.value = error.response?.data?.error || 'Organizasyon silinirken bir hata oluştu'
  } finally {
    isDeletingOrganization.value = false
  }
}

const createNewOrganization = async () => {
  if (!newOrganizationForm.value.name.trim()) {
    createOrganizationError.value = 'Organizasyon adı boş olamaz'
    return
  }

  isCreatingOrganization.value = true
  createOrganizationError.value = ''
  
  try {
    // Create new organization
    const newOrg = await organizationService.createOrganization(newOrganizationForm.value.name.trim())
    
    // Switch to new organization (this returns new token)
    const switchResult = await organizationService.switchOrganization(newOrg.id)
    
    // Update auth store with new token and organization ID
    if (switchResult.token) {
      authStore.updateToken(switchResult.token)
    }
    authStore.updateOrganizationId(newOrg.id.toString())
    
    // Reload organization data and available organizations
    await Promise.all([
      loadOrganization(),
      loadAvailableOrganizations()
    ])
    
    // Close modal
    showCreateOrganizationModal.value = false
    newOrganizationForm.value = { name: '' }
    
    // Show success message (you can add a toast notification here)
    alert('Yeni organizasyon oluşturuldu ve geçiş yapıldı!')
  } catch (error) {
    console.error('Organization oluşturulurken hata:', error)
    createOrganizationError.value = error.response?.data?.error || 'Organizasyon oluşturulurken bir hata oluştu'
  } finally {
    isCreatingOrganization.value = false
  }
}

const formatDate = (dateString) => {
  if (!dateString) return 'Bilinmiyor'
  const date = new Date(dateString)
  return date.toLocaleDateString('tr-TR', { 
    day: 'numeric', 
    month: 'long', 
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

onMounted(() => {
  if (user.value) {
    formData.value = {
      username: user.value.username || '',
      email: user.value.email || ''
    }
  }
  loadOrganization()
})

const startEditing = () => {
  isEditing.value = true
}

const cancelEditing = () => {
  if (user.value) {
    formData.value = {
      username: user.value.username || '',
      email: user.value.email || ''
    }
  }
  isEditing.value = false
}

const saveChanges = async () => {
  isSaving.value = true
  try {
    // TODO: API'ye profil güncelleme isteği gönder
    // await authService.updateProfile(formData.value)
    
    // Şimdilik mock - gerçek API entegrasyonu yapılacak
    await new Promise(resolve => setTimeout(resolve, 1000))
    
    // Store'u güncelle
    authStore.updateUser({
      username: formData.value.username,
      email: formData.value.email
    })
    
    isEditing.value = false
  } catch (error) {
    console.error('Profil güncelleme hatası:', error)
    // TODO: Hata mesajı göster
  } finally {
    isSaving.value = false
  }
}
</script>

<style scoped>
.profile-container {
  min-height: calc(100vh - 64px);
  background: linear-gradient(to bottom, #f9fafb, #f3f4f6);
  padding: 2rem 1rem;
}

.profile-content {
  max-width: 800px;
  margin: 0 auto;
}

.profile-header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 24px;
  padding: 3rem 2.5rem;
  margin-bottom: 2rem;
  position: relative;
  overflow: hidden;
  box-shadow: 0 10px 40px rgba(102, 126, 234, 0.3);
  display: flex;
  align-items: center;
  gap: 2rem;
}

.profile-avatar-section {
  position: relative;
  flex-shrink: 0;
}

.profile-avatar {
  width: 120px;
  height: 120px;
  background: rgba(255, 255, 255, 0.2);
  backdrop-filter: blur(10px);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-weight: 700;
  font-size: 3rem;
  border: 4px solid rgba(255, 255, 255, 0.3);
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
}

.avatar-edit-button {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 40px;
  height: 40px;
  background: white;
  border: 3px solid #667eea;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.avatar-edit-button:hover {
  transform: scale(1.1);
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.2);
}

.avatar-edit-button svg {
  width: 20px;
  height: 20px;
  color: #667eea;
}

.profile-info {
  flex: 1;
  color: white;
}

.profile-name {
  font-size: 2.5rem;
  font-weight: 800;
  margin: 0 0 0.5rem 0;
  letter-spacing: -0.02em;
}

.profile-email {
  font-size: 1.125rem;
  color: rgba(255, 255, 255, 0.9);
  margin: 0;
}

.profile-card {
  background: white;
  border-radius: 20px;
  padding: 2rem;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.05);
  border: 1px solid #e5e7eb;
  margin-bottom: 2rem;
}

.card-header {
  margin-bottom: 2rem;
}

.card-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1f2937;
  margin: 0 0 0.5rem 0;
  letter-spacing: -0.01em;
}

.card-description {
  font-size: 0.9375rem;
  color: #6b7280;
  margin: 0;
}

.form-section {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: #374151;
}

.form-input-wrapper {
  position: relative;
}

.form-input {
  width: 100%;
  padding: 0.875rem 1rem 0.875rem 3rem;
  border: 2px solid #e5e7eb;
  border-radius: 12px;
  font-size: 0.9375rem;
  color: #1f2937;
  transition: all 0.2s ease;
  background: white;
}

.form-input:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.form-input:disabled {
  background: #f9fafb;
  color: #6b7280;
  cursor: not-allowed;
}

.form-icon {
  position: absolute;
  left: 1rem;
  top: 50%;
  transform: translateY(-50%);
  width: 20px;
  height: 20px;
  color: #9ca3af;
}

.card-actions {
  margin-top: 2rem;
  padding-top: 2rem;
  border-top: 1px solid #e5e7eb;
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
}

.action-buttons {
  display: flex;
  gap: 1rem;
}

.action-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 12px;
  font-size: 0.9375rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
}

.action-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.action-button-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  box-shadow: 0 2px 8px rgba(102, 126, 234, 0.3);
}

.action-button-primary:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.action-button-primary svg {
  width: 18px;
  height: 18px;
}

.action-button-success {
  background: #d1fae5;
  color: #059669;
  border: 2px solid #a7f3d0;
}

.action-button-success:hover:not(:disabled) {
  background: #059669;
  color: white;
  border-color: #059669;
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(5, 150, 105, 0.3);
}

.action-button-success svg {
  width: 18px;
  height: 18px;
}

.action-button-secondary {
  background: white;
  color: #6b7280;
  border: 2px solid #e5e7eb;
}

.action-button-secondary:hover {
  background: #f9fafb;
  border-color: #d1d5db;
}

.settings-list {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.setting-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  background: #f9fafb;
  border-radius: 16px;
  border: 1px solid #e5e7eb;
  transition: all 0.2s ease;
}

.setting-item:hover {
  background: #f3f4f6;
  border-color: #d1d5db;
}

.setting-info {
  flex: 1;
}

.setting-title {
  font-size: 1rem;
  font-weight: 600;
  color: #1f2937;
  margin: 0 0 0.25rem 0;
}

.setting-description {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

.setting-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.625rem 1rem;
  background: white;
  color: #667eea;
  border: 2px solid #667eea;
  border-radius: 10px;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.setting-button:hover {
  background: #667eea;
  color: white;
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(102, 126, 234, 0.3);
}

.setting-button svg {
  width: 18px;
  height: 18px;
}

.setting-button-danger {
  color: #dc2626;
  border-color: #dc2626;
}

.setting-button-danger:hover {
  background: #dc2626;
  color: white;
  box-shadow: 0 2px 8px rgba(220, 38, 38, 0.3);
}

/* Organization Management Styles */
.loading-state,
.error-state {
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

.error-state {
  color: #dc2626;
}

.error-icon {
  width: 48px;
  height: 48px;
  margin: 0 auto 1rem;
  color: #dc2626;
}

.retry-button {
  margin-top: 1rem;
  padding: 0.625rem 1.25rem;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 10px;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.retry-button:hover {
  background: #764ba2;
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(102, 126, 234, 0.3);
}

.form-error {
  padding: 0.75rem 1rem;
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 12px;
  color: #dc2626;
  font-size: 0.875rem;
  margin-top: 1rem;
}

.switch-organization-section {
  margin-top: 2rem;
  padding-top: 2rem;
  border-top: 1px solid #e5e7eb;
}

.section-header-small {
  margin-bottom: 1.5rem;
}

.section-title-small {
  font-size: 1.125rem;
  font-weight: 700;
  color: #1f2937;
  margin: 0 0 0.5rem 0;
}

.section-description-small {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

.loading-state-small,
.error-state-small,
.empty-state-small {
  text-align: center;
  padding: 2rem 1rem;
  color: #6b7280;
  font-size: 0.875rem;
}

.loading-spinner-small {
  width: 24px;
  height: 24px;
  margin: 0 auto 0.75rem;
  border: 2px solid #e5e7eb;
  border-top-color: #667eea;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

.error-state-small {
  color: #dc2626;
}

.retry-button-small {
  margin-top: 0.75rem;
  padding: 0.5rem 1rem;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 0.8125rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.retry-button-small:hover {
  background: #764ba2;
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(102, 126, 234, 0.3);
}

.organizations-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.organization-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem;
  background: #f9fafb;
  border: 2px solid #e5e7eb;
  border-radius: 12px;
  transition: all 0.2s ease;
}

.organization-item:hover {
  background: #f3f4f6;
  border-color: #d1d5db;
  transform: translateX(4px);
}

.organization-item-active {
  background: #eef2ff;
  border-color: #667eea;
}

.organization-item-info {
  display: flex;
  align-items: center;
  gap: 1rem;
  flex: 1;
  min-width: 0;
}

.organization-item-icon {
  width: 48px;
  height: 48px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  flex-shrink: 0;
  box-shadow: 0 2px 8px rgba(102, 126, 234, 0.2);
}

.organization-item-active .organization-item-icon {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  box-shadow: 0 2px 8px rgba(16, 185, 129, 0.2);
}

.organization-item-icon svg {
  width: 24px;
  height: 24px;
}

.organization-item-details {
  flex: 1;
  min-width: 0;
}

.organization-item-name {
  font-size: 1rem;
  font-weight: 600;
  color: #1f2937;
  margin: 0 0 0.25rem 0;
  word-break: break-word;
}

.organization-item-date {
  font-size: 0.8125rem;
  color: #6b7280;
  margin: 0;
}

.organization-switch-button {
  padding: 0.625rem 1.25rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 10px;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 8px rgba(102, 126, 234, 0.3);
  flex-shrink: 0;
}

.organization-switch-button:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.organization-switch-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.organization-current-badge {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.625rem 1.25rem;
  background: #d1fae5;
  color: #059669;
  border: 2px solid #a7f3d0;
  border-radius: 10px;
  font-size: 0.875rem;
  font-weight: 600;
  flex-shrink: 0;
}

.organization-current-badge svg {
  width: 18px;
  height: 18px;
}

.delete-section {
  margin-top: 2rem;
  padding-top: 2rem;
  border-top: 1px solid #e5e7eb;
}

.delete-warning {
  display: flex;
  gap: 1rem;
  padding: 1.25rem;
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 12px;
  margin-bottom: 1rem;
}

.delete-warning svg {
  width: 24px;
  height: 24px;
  color: #dc2626;
  flex-shrink: 0;
}

.delete-warning-content {
  flex: 1;
}

.delete-warning-title {
  font-size: 1rem;
  font-weight: 600;
  color: #991b1b;
  margin: 0 0 0.5rem 0;
}

.delete-warning-text {
  font-size: 0.875rem;
  color: #7f1d1d;
  margin: 0;
  line-height: 1.5;
}

.action-button-danger {
  background: #fee2e2;
  color: #dc2626;
  border: 2px solid #fecaca;
}

.action-button-danger:hover:not(:disabled) {
  background: #dc2626;
  color: white;
  border-color: #dc2626;
  transform: translateY(-1px);
  box-shadow: 0 2px 8px rgba(220, 38, 38, 0.3);
}

.action-button-danger:disabled {
  opacity: 0.6;
  cursor: not-allowed;
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

.modal-content {
  padding: 2rem;
}

.modal-warning {
  display: flex;
  gap: 1rem;
  padding: 1.25rem;
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 12px;
  margin-bottom: 1.5rem;
}

.modal-warning svg {
  width: 24px;
  height: 24px;
  color: #dc2626;
  flex-shrink: 0;
}

.modal-warning p {
  font-size: 0.9375rem;
  color: #7f1d1d;
  margin: 0;
  line-height: 1.5;
}

.modal-text {
  font-size: 1rem;
  color: #374151;
  margin: 0 0 1rem 0;
  text-align: center;
}

.modal-info {
  display: flex;
  gap: 0.75rem;
  padding: 1rem;
  background: #eff6ff;
  border: 1px solid #bfdbfe;
  border-radius: 12px;
  margin-top: 1rem;
}

.modal-info svg {
  width: 20px;
  height: 20px;
  color: #3b82f6;
  flex-shrink: 0;
  margin-top: 0.125rem;
}

.modal-info p {
  font-size: 0.875rem;
  color: #1e40af;
  margin: 0;
  line-height: 1.5;
}

.modal-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
  padding: 1.5rem 2rem;
  border-top: 1px solid #e5e7eb;
}

.button-danger {
  padding: 0.75rem 1.5rem;
  background: #dc2626;
  color: white;
  border: none;
  border-radius: 12px;
  font-size: 0.9375rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 8px rgba(220, 38, 38, 0.3);
}

.button-danger:hover:not(:disabled) {
  background: #b91c1c;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(220, 38, 38, 0.4);
}

.button-danger:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

@media (max-width: 768px) {
  .profile-container {
    padding: 1rem 0.5rem;
  }

  .profile-header {
    flex-direction: column;
    text-align: center;
    padding: 2rem 1.5rem;
  }

  .profile-name {
    font-size: 2rem;
  }

  .profile-card {
    padding: 1.5rem;
  }

  .setting-item {
    flex-direction: column;
    align-items: flex-start;
    gap: 1rem;
  }

  .setting-button {
    width: 100%;
    justify-content: center;
  }

  .card-actions {
    flex-direction: column;
  }

  .action-buttons {
    width: 100%;
    flex-direction: column;
  }

  .action-button {
    width: 100%;
    justify-content: center;
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

  .organization-item {
    flex-direction: column;
    align-items: flex-start;
    gap: 1rem;
  }

  .organization-switch-button,
  .organization-current-badge {
    width: 100%;
    justify-content: center;
  }

  .modal-container {
    max-width: 100%;
    margin: 1rem;
  }

  .modal-content {
    padding: 1.5rem;
  }

  .modal-actions {
    flex-direction: column-reverse;
    padding: 1.25rem 1.5rem;
  }

  .button-danger,
  .button-secondary {
    width: 100%;
  }
}
</style>

