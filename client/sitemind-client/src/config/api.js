// API Configuration
// In production, this will be set via environment variables
// Development: uses relative paths (/api)
// Production: uses full API domain (https://sitemindapi.futureautonoms.com)

const getApiBaseUrl = () => {
  // If environment variable is set, use it
  if (import.meta.env.VITE_API_BASE_URL) {
    return import.meta.env.VITE_API_BASE_URL;
  }
  
  // Check if we're in production
  if (import.meta.env.PROD) {
    // Production: use API domain with /api prefix
    return 'https://sitemindapi.futureautonoms.com/api';
  }
  
  // Development: use relative path (proxy through nginx)
  return '/api';
};

export const API_BASE_URL = getApiBaseUrl();

export default {
  API_BASE_URL
};

