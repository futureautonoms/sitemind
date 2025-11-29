import axios from 'axios'
import { API_BASE_URL as BASE_URL } from '../config/api.js'

const API_BASE_URL = `${BASE_URL}/websites`

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

export const websiteService = {
  async getWebsites() {
    const response = await apiClient.get('/')
    return response.data
  },

  async getWebsite(id) {
    const response = await apiClient.get(`/${id}`)
    return response.data
  },

  async createWebsite(name, baseUrl) {
    const response = await apiClient.post('/', {
      name,
      baseUrl
    })
    return response.data
  },

  async updateWebsite(id, name, baseUrl) {
    const response = await apiClient.put(`/${id}`, {
      name,
      baseUrl
    })
    return response.data
  },

  async deleteWebsite(id) {
    const response = await apiClient.delete(`/${id}`)
    return response.data
  }
}

export default apiClient

