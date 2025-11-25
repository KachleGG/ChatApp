<template>
  <div class="admin-page">
    <div class="admin-container">
      <div class="admin-header">
        <div class="header-left-section">
          <router-link to="/profile" class="back-arrow" title="Back to Profile">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="19" y1="12" x2="5" y2="12"></line>
              <polyline points="12 19 5 12 12 5"></polyline>
            </svg>
          </router-link>
          <h1>Admin Panel</h1>
        </div>
      </div>

      <div v-if="loading" class="loading">Loading...</div>

      <div v-else-if="!isAdmin" class="unauthorized">
        <div class="unauthorized-icon">
          <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="12" y1="8" x2="12" y2="12"></line>
            <line x1="12" y1="16" x2="12.01" y2="16"></line>
          </svg>
        </div>
        <h2>Access Denied</h2>
        <p>You do not have permission to access this page.</p>
        <router-link to="/" class="home-link">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
            <polyline points="9 22 9 12 15 12 15 22"></polyline>
          </svg>
          Go to Home
        </router-link>
      </div>

      <div v-else class="admin-content">
        <div class="admin-section">
          <h2>Administration Dashboard</h2>
          <p>This page is under construction. Admin features will be added here.</p>
          
          <div class="placeholder-info">
            <h3>Coming Soon:</h3>
            <ul>
              <li>User management</li>
              <li>Message moderation</li>
              <li>System statistics</li>
              <li>Server configuration</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const loading = ref(true)
const isAdmin = ref(false)

async function checkAdmin() {
  try {
    const res = await fetch('/api/auth/check', { credentials: 'include' })
    if (!res.ok) {
      router.push({ name: 'Login' })
      return
    }
    const data = await res.json()
    if (!data.authenticated || !data.user) {
      router.push({ name: 'Login' })
      return
    }
    isAdmin.value = data.user.isAdmin || false
    if (!isAdmin.value) {
      // Not admin - could redirect or show unauthorized message
      // We'll show unauthorized message in template
    }
  } catch (e) {
    router.push({ name: 'Login' })
  }
}

onMounted(async () => {
  await checkAdmin()
  loading.value = false
})
</script>

<style scoped>
.admin-page {
  min-height: 100vh;
  background-color: #36393f;
  color: #dcddde;
  display: flex;
  justify-content: center;
}

.admin-container {
  width: 100%;
  max-width: 1200px;
  display: flex;
  flex-direction: column;
}

.admin-header {
  height: 64px;
  background-color: #202225;
  border-bottom: 1px solid #1e1f22;
  padding: 0 24px;
  display: flex;
  align-items: center;
  position: sticky;
  top: 0;
  z-index: 10;
}

.header-left-section {
  display: flex;
  align-items: center;
  gap: 16px;
}

.back-arrow {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background-color: transparent;
  color: #b9bbbe;
  text-decoration: none;
  transition: all 0.2s ease;
}

.back-arrow:hover {
  background-color: #4e5058;
  color: #fff;
}

.back-arrow svg {
  width: 24px;
  height: 24px;
}

.admin-header h1 {
  font-size: 20px;
  font-weight: 600;
  color: #fff;
  margin: 0;
}

.loading {
  text-align: center;
  padding: 40px;
  color: #b9bbbe;
  font-size: 14px;
}

.unauthorized {
  max-width: 600px;
  margin: 40px auto;
  text-align: center;
  background: #2f3136;
  padding: 40px;
  border-radius: 8px;
  border: 2px solid #ed4245;
}

.unauthorized-icon {
  margin-bottom: 16px;
}

.unauthorized-icon svg {
  width: 64px;
  height: 64px;
  color: #ed4245;
}

.unauthorized h2 {
  font-size: 24px;
  margin-bottom: 16px;
  color: #ed4245;
}

.unauthorized p {
  margin-bottom: 24px;
  color: #b9bbbe;
  font-size: 14px;
}

.home-link {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 10px 20px;
  background-color: #5865f2;
  color: #fff;
  text-decoration: none;
  border-radius: 4px;
  font-size: 14px;
  font-weight: 500;
  transition: background-color 0.2s ease;
}

.home-link:hover {
  background-color: #4752c4;
}

.home-link svg {
  width: 18px;
  height: 18px;
}

.admin-content {
  padding: 24px;
  flex: 1;
}

.admin-section {
  background: #2f3136;
  padding: 24px;
  border-radius: 8px;
  margin-bottom: 20px;
  border-left: 4px solid #5865f2;
}

.admin-section h2 {
  font-size: 20px;
  font-weight: 600;
  margin-bottom: 16px;
  color: #fff;
}

.admin-section p {
  color: #b9bbbe;
  margin-bottom: 16px;
  font-size: 14px;
  line-height: 1.5;
}

.placeholder-info {
  background: #202225;
  padding: 16px;
  border-radius: 4px;
  border-left: 4px solid #5865f2;
}

.placeholder-info h3 {
  font-size: 16px;
  margin-bottom: 12px;
  color: #5865f2;
}

.placeholder-info ul {
  list-style: none;
  padding: 0;
  margin: 0;
}

.placeholder-info li {
  padding: 8px 0;
  color: #b9bbbe;
  border-bottom: 1px solid #2f3136;
  font-size: 14px;
  display: flex;
  align-items: center;
}

.placeholder-info li:last-child {
  border-bottom: none;
}

.placeholder-info li:before {
  content: '→';
  color: #5865f2;
  margin-right: 8px;
  font-weight: 600;
}

.admin-page::-webkit-scrollbar {
  width: 8px;
}

.admin-page::-webkit-scrollbar-track {
  background: #2f3136;
}

.admin-page::-webkit-scrollbar-thumb {
  background-color: #202225;
  border-radius: 4px;
}

.admin-page::-webkit-scrollbar-thumb:hover {
  background-color: #1e1f22;
}
</style>

