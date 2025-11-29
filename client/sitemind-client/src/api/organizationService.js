import axios from 'axios'

const API_BASE_URL = '/api/organizations'

// Create axios instance
const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json'
  }
})

// Add token and organization ID to requests if available
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    
    // Add organization ID header for multi-tenancy support
    const organizationId = localStorage.getItem('organizationId')
    if (organizationId) {
      config.headers['X-Organization-Id'] = organizationId
    }
    
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// Handle token expiration
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('token')
      localStorage.removeItem('organizationId')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

export const organizationService = {
  // Get current organization (from token/header)
  async getCurrentOrganization() {
    const response = await apiClient.get('/')
    return response.data
  },

  // Get organization by ID
  async getOrganization(id) {
    const response = await apiClient.get(`/${id}`)
    return response.data
  },

  // Create new organization (public endpoint, no auth required)
  async createOrganization(name) {
    const response = await apiClient.post('/', {
      name
    })
    return response.data
  },

  // Update organization
  async updateOrganization(id, name) {
    const response = await apiClient.put(`/${id}`, {
      name
    })
    return response.data
  },

  // Delete organization
  async deleteOrganization(id) {
    const response = await apiClient.delete(`/${id}`)
    return response.data
  },

  // Switch to another organization
  async switchOrganization(organizationId) {
    const response = await apiClient.post('/switch', {
      organizationId
    })
    return response.data
  },

  // Get available organizations for current user
  async getAvailableOrganizations() {
    const response = await apiClient.get('/available')
    return response.data
  }
}

export default apiClient

