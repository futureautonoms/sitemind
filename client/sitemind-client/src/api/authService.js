import axios from 'axios'

const API_BASE_URL = '/api/auth'

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

export const authService = {
  async login(username, password) {
    const response = await apiClient.post('/login', {
      username,
      password
    })
    return response.data
  },

  async register(email, username, password, organizationName) {
    const response = await apiClient.post('/register', {
      email,
      username,
      password,
      organizationName
    })
    return response.data
  }
}

export default apiClient

