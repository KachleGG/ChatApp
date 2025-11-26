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
          <p>Toggle server settings below. Changes will update `Chatter/appsettings.json` (admin-only).</p>

          <div class="settings-grid">
            <div class="setting-row">
              <div class="setting-label">Private Mode</div>
              <label class="switch">
                <input type="checkbox" v-model="config.privateMode" />
                <span class="slider" aria-hidden="true"></span>
              </label>
            </div>

            <div class="setting-row">
              <div class="setting-label">Prohibit Groups</div>
              <label class="switch">
                <input type="checkbox" v-model="config.prohibitGroups" />
                <span class="slider" aria-hidden="true"></span>
              </label>
            </div>

            <div class="setting-row">
              <div class="setting-label">Prohibit General Channel</div>
              <label class="switch">
                <input type="checkbox" v-model="config.prohibitGeneral" />
                <span class="slider" aria-hidden="true"></span>
              </label>
            </div>

            <div class="setting-row">
              <div class="setting-label">HTTP URL</div>
              <input class="text-input" type="text" v-model="config.httpUrl" placeholder="http://*:9090" />
            </div>

            <div class="setting-row">
              <div class="setting-label">HTTPS URL</div>
              <input class="text-input" type="text" v-model="config.httpsUrl" placeholder="https://*:9443" />
            </div>
          </div>

          <div class="admin-actions">
            <button class="save-btn" @click="saveConfig" :disabled="saving">Save changes</button>
            <span class="status" v-if="saveMessage">{{ saveMessage }}</span>
          </div>

          <div v-if="showRestartNotice" class="restart-note">
            <strong>Note:</strong> Port/url changes take effect only after the service is restarted.
          </div>

          <div v-if="loadingConfig" class="loading">Loading config...</div>
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
const config = ref({ prohibitGroups: false, privateMode: false, prohibitGeneral: false, httpUrl: '', httpsUrl: '' })
const originalConfig = ref<{ httpUrl: string; httpsUrl: string } | null>(null)
const showRestartNotice = ref(false)
const loadingConfig = ref(false)
const saving = ref(false)
const saveMessage = ref('')

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
  if (isAdmin.value) await fetchAdminConfig()
})

async function fetchAdminConfig() {
  loadingConfig.value = true
  try {
    const res = await fetch('/api/admin/config', { credentials: 'include' })
    if (!res.ok) {
      saveMessage.value = 'Failed to load config'
      return
    }
    const data = await res.json()
    config.value.prohibitGroups = !!data.prohibitGroups
    config.value.privateMode = !!data.privateMode
    config.value.prohibitGeneral = !!data.prohibitGeneral
    config.value.httpUrl = data.httpUrl || ''
    config.value.httpsUrl = data.httpsUrl || ''
      // Keep a copy of original ports so we can detect changes that require a restart
      originalConfig.value = { httpUrl: config.value.httpUrl, httpsUrl: config.value.httpsUrl }
  } catch (e) {
    saveMessage.value = 'Error loading config'
    console.warn(e)
  } finally {
    loadingConfig.value = false
  }
}

async function saveConfig() {
  saveMessage.value = ''
  saving.value = true
  try {
    const payload = {
      ProhibitGroups: config.value.prohibitGroups,
      PrivateMode: config.value.privateMode,
      ProhibitGeneral: config.value.prohibitGeneral,
      HttpUrl: config.value.httpUrl,
      HttpsUrl: config.value.httpsUrl,
    }
    const res = await fetch('/api/admin/config', {
      method: 'PUT',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    })
    if (!res.ok) {
      const txt = await res.text().catch(() => '')
      saveMessage.value = `Save failed: ${res.status} ${txt}`
      return
    }

    // Indicate success; if ports changed, show restart note
    const portsChanged = 
      (originalConfig.value?.httpUrl ?? '') !== (config.value.httpUrl ?? '') ||
      (originalConfig.value?.httpsUrl ?? '') !== (config.value.httpsUrl ?? '')

    saveMessage.value = portsChanged
      ? 'Saved successfully. Port/url changes require service restart to take effect.'
      : 'Saved successfully.'

    showRestartNotice.value = portsChanged
  } catch (e) {
    saveMessage.value = 'Network error while saving'
  } finally {
    saving.value = false
    setTimeout(() => (saveMessage.value = ''), 4000)
  }
}
</script>
<style scoped>
.admin-page {
  min-height: 100vh;
  background-color: var(--bg-chat-dark-2);
  color: var(--text-white);
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
  background-color: var(--bg-chat-dark-1);
  border-bottom: 1px solid var(--border-white-5);
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
  color: var(--text-purple-70);
  text-decoration: none;
  transition: all 0.2s ease;
}

.back-arrow:hover {
  background-color: var(--bg-chat-sidebar-2);
  color: var(--text-white);
}

.back-arrow svg {
  width: 24px;
  height: 24px;
}

.admin-header h1 {
  font-size: 20px;
  font-weight: 600;
  color: var(--text-white);
  margin: 0;
}

.loading {
  text-align: center;
  padding: 40px;
  color: var(--text-purple-70);
  font-size: 14px;
}

.unauthorized {
  max-width: 600px;
  margin: 40px auto;
  text-align: center;
  background: var(--bg-chat-sidebar-1);
  padding: 40px;
  border-radius: 8px;
  border: 2px solid var(--border-red-30);
}

.unauthorized-icon {
  margin-bottom: 16px;
}

.unauthorized-icon svg {
  width: 64px;
  height: 64px;
  color: var(--border-red-30);
}

.unauthorized h2 {
  font-size: 24px;
  margin-bottom: 16px;
  color: var(--border-red-30);
}

.unauthorized p {
  margin-bottom: 24px;
  color: var(--text-purple-70);
  font-size: 14px;
}

.home-link {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 10px 20px;
  background-color: var(--brand-blue-primary);
  color: var(--text-white);
  text-decoration: none;
  border-radius: 4px;
  font-size: 14px;
  font-weight: 500;
  transition: background-color 0.2s ease;
}

.home-link:hover {
  filter: brightness(0.92);
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
  background: var(--bg-chat-sidebar-1);
  padding: 24px;
  border-radius: 8px;
  margin-bottom: 20px;
  border-left: 4px solid var(--brand-blue-primary);
}

.admin-section h2 {
  font-size: 20px;
  font-weight: 600;
  margin-bottom: 16px;
  color: var(--text-white);
}

.admin-section p {
  color: var(--text-purple-70);
  margin-bottom: 16px;
  font-size: 14px;
  line-height: 1.5;
}

.placeholder-info {
  background: var(--bg-chat-dark-1);
  padding: 16px;
  border-radius: 4px;
  border-left: 4px solid var(--brand-blue-primary);
}

.placeholder-info h3 {
  font-size: 16px;
  margin-bottom: 12px;
  color: var(--brand-blue-primary);
}

.placeholder-info ul {
  list-style: none;
  padding: 0;
  margin: 0;
}

.placeholder-info li {
  padding: 8px 0;
  color: var(--text-purple-70);
  border-bottom: 1px solid var(--bg-chat-sidebar-1);
  font-size: 14px;
  display: flex;
  align-items: center;
}

.placeholder-info li:last-child {
  border-bottom: none;
}

.placeholder-info li:before {
  content: '→';
  color: var(--brand-blue-primary);
  margin-right: 8px;
  font-weight: 600;
}

.admin-page::-webkit-scrollbar {
  width: 8px;
}

.admin-page::-webkit-scrollbar-track {
  background: var(--bg-chat-sidebar-1);
}

.admin-page::-webkit-scrollbar-thumb {
  background-color: var(--bg-chat-dark-2);
  border-radius: 4px;
}

.admin-page::-webkit-scrollbar-thumb:hover {
  background-color: var(--bg-chat-dark-1);
}

/* Settings grid and switches */
.settings-grid {
  display: grid;
  grid-template-columns: 1fr 240px;
  gap: 12px 20px;
  align-items: center;
  margin-top: 12px;
}

.setting-row {
  display: contents;
}

.setting-label {
  color: var(--text-purple-70);
  font-weight: 600;
}

.text-input {
  width: 100%;
  padding: 10px 12px;
  border-radius: 6px;
  background: var(--bg-chat-dark-2);
  border: 1px solid var(--bg-chat-sidebar-1);
  color: var(--text-white);
}

.switch {
  position: relative;
  display: inline-block;
  width: 52px;
  height: 28px;
}

.switch input { display: none; }

.slider {
  position: absolute;
  cursor: pointer;
  top: 0; left: 0; right: 0; bottom: 0;
  background-color: var(--border-white-20);
  transition: 0.18s;
  border-radius: 28px;
  box-shadow: inset 0 1px 2px rgba(0,0,0,0.2);
}

.slider:before {
  position: absolute;
  content: "";
  height: 22px;
  width: 22px;
  left: 4px;
  top: 3px;
  background-color: white;
  transition: 0.18s;
  border-radius: 50%;
}

.switch input:checked + .slider {
  background-color: var(--brand-blue-primary);
}

.switch input:checked + .slider:before {
  transform: translateX(24px);
}

.admin-actions { margin-top: 18px; display:flex; gap: 12px; align-items:center }
.save-btn { background: var(--brand-blue-primary); color: var(--text-white); padding: 8px 14px; border-radius:6px; border:none; cursor:pointer }
.save-btn:disabled { opacity: .5; cursor: not-allowed }
.status { color: var(--text-purple-70) }

.restart-note { margin-top:12px; padding:10px; background: var(--bg-chat-sidebar-1); border-left:4px solid var(--warning-amber); color: var(--warning-amber); border-radius:4px }
</style>

