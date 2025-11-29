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

        <div class="admin-section" style="margin-top:16px">
          <h2>Invite Codes (Private Mode)</h2>
          <p>Create invite codes that allow limited registrations while Private Mode is enabled.</p>

            <div class="invite-form-grid" style="margin-bottom:12px">
              <div class="invite-form-row">
                <label class="invite-label">Note</label>
                <input class="text-input" v-model="inviteNote" placeholder="Note (optional)" />
                <div class="invite-help">Optional note for admins to remember the invite purpose (visible only to admins).</div>
              </div>

              <div class="invite-form-row">
                <label class="invite-label">Max Uses</label>
                <input type="number" class="text-input" v-model.number="inviteMaxUses" min="0" style="width:120px" />
                <div class="invite-help">Number of registrations allowed for this invite (0 = unlimited)</div>
              </div>

              <div class="invite-form-row">
                <label class="invite-label">Expires</label>
                <select v-model.number="inviteExpiresSeconds" class="text-input" style="width:160px">
                  <option :value="0">No expiry</option>
                  <option :value="86400">1 day</option>
                  <option :value="604800">1 week</option>
                  <option :value="2592000">30 days</option>
                </select>
                <div class="invite-help">Expiry time for the invite (0 = no expiry)</div>
              </div>

              <div class="invite-form-row invite-create-col">
                <label class="invite-label">&nbsp;</label>
                <div style="display:flex;gap:8px;align-items:center">
                  <button class="save-btn" @click="createInvite" :disabled="creatingInvite">{{ creatingInvite ? 'Creating...' : 'Create Invite' }}</button>
                </div>
              </div>
            </div>

          <div v-if="invitesLoading" class="loading">Loading invites...</div>
          <div v-else>
            <table style="width:100%;border-collapse:collapse">
              <thead>
                <tr style="text-align:left;color:var(--text-purple-70)">
                  <th>Code</th><th>Uses</th><th>Expires</th><th>Note</th><th></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="inv in invites" :key="inv.code" style="border-top:1px solid var(--border-white-5)">
                  <td style="padding:8px 6px"><code>{{ inv.code }}</code></td>
                  <td style="padding:8px 6px">{{ inv.usesCount }} / {{ inv.maxUses === 0 ? '∞' : inv.maxUses }}</td>
                  <td style="padding:8px 6px">{{ inv.expiresAt ? new Date(inv.expiresAt).toLocaleString() : '—' }}</td>
                  <td style="padding:8px 6px">{{ inv.note || '—' }}</td>
                  <td style="padding:8px 6px;text-align:right">
                    <button class="save-btn" @click="copyInviteCode(inv.code)">Copy</button>
                    <button class="delete-button" @click="revokeInvite(inv.code)">Revoke</button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div class="admin-section" style="margin-top:16px">
          <h2>User Management</h2>
          <p>Promote or demote users. The owner account (Id = 1) cannot be demoted.</p>

          <div class="user-controls" style="display:flex;gap:12px;align-items:center;margin-top:8px;margin-bottom:8px;flex-wrap:wrap">
            <div style="display:flex;gap:8px;align-items:center">
              <label style="color:var(--text-purple-70);font-weight:600">Find by</label>
              <select v-model="searchField" class="text-input" style="width:140px">
                <option value="name">Username</option>
                <option value="email">Email</option>
                <option value="id">ID</option>
              </select>
              <input v-model="searchQuery" class="text-input" placeholder="Search" style="width:240px" />
              <button class="save-btn" @click="page = 1">Apply</button>
            </div>

            <div style="margin-left:auto;display:flex;gap:8px;align-items:center">
              <label style="color:var(--text-purple-70);font-weight:600">Per page</label>
              <select v-model.number="pageSize" class="text-input" style="width:100px">
                <option :value="5">5</option>
                <option :value="10">10</option>
                <option :value="25">25</option>
                <option :value="50">50</option>
              </select>
            </div>
          </div>

          <div v-if="usersLoading" class="loading">Loading users...</div>
          <div v-else>
            <div style="margin-bottom:8px;color:var(--text-purple-70)">Showing {{ pagedTotal }} users (filtered {{ filteredTotal }})</div>
            <table style="width:100%;border-collapse:collapse;margin-top:8px">
              <thead>
                <tr style="text-align:left;color:var(--text-purple-70)">
                  <th style="width:64px">ID</th><th>Name</th><th>Email</th><th style="width:80px">Admin</th><th style="width:160px"></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="u in visibleUsers" :key="u.id" style="border-top:1px solid var(--border-white-5)">
                  <td style="padding:8px 6px">{{ u.id }}</td>
                  <td style="padding:8px 6px">{{ u.name }}</td>
                  <td style="padding:8px 6px">{{ u.email }}</td>
                  <td style="padding:8px 6px">{{ u.isAdmin ? 'Yes' : 'No' }}</td>
                  <td style="padding:8px 6px;text-align:right">
                    <button v-if="!u.isAdmin" class="save-btn" @click="promoteUser(u.id)">Promote</button>
                    <button v-else class="delete-button" @click="demoteUser(u.id)" :disabled="u.id === 1">Demote</button>
                  </td>
                </tr>
                <tr v-if="visibleUsers.length === 0">
                  <td colspan="5" style="padding:12px;color:var(--text-purple-70)">No users match the current filter.</td>
                </tr>
              </tbody>
            </table>

            <div style="display:flex;justify-content:space-between;align-items:center;margin-top:12px">
              <div>
                <button class="save-btn" @click="prevPage" :disabled="page === 1">Prev</button>
                <button class="save-btn" @click="nextPage" :disabled="page >= pageCount">Next</button>
              </div>
              <div style="color:var(--text-purple-70)">Page {{ page }} / {{ pageCount }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const loading = ref(true)
const isAdmin = ref(false)
const config = ref({ privateMode: false, prohibitGroups: false, prohibitGeneral: false, httpUrl: '', httpsUrl: '' })
const originalConfig = ref<{ httpUrl: string; httpsUrl: string } | null>(null)
const showRestartNotice = ref(false)
const loadingConfig = ref(false)
const saving = ref(false)
const saveMessage = ref('')
const invites = ref<Array<any>>([])
const invitesLoading = ref(false)
const creatingInvite = ref(false)
const inviteNote = ref('')
const inviteMaxUses = ref<number>(1)
const inviteExpiresSeconds = ref<number>(0)
const users = ref<Array<{ id:number; name:string; email:string; isAdmin:boolean; isDeactivated:boolean }>>([])
const usersLoading = ref(false)

// Search & pagination
const searchQuery = ref('')
const searchField = ref<'name'|'email'|'id'>('name')
const page = ref(1)
const pageSize = ref<number>(10)

const filteredUsers = computed(() => {
  const q = (searchQuery.value || '').toString().trim().toLowerCase()
  if (!q) return users.value.slice()
  if (searchField.value === 'id') {
    const n = Number(q)
    if (isNaN(n)) return []
    return users.value.filter(u => u.id === n)
  }
  return users.value.filter(u => ((searchField.value === 'name' ? (u.name || '') : (u.email || ''))).toLowerCase().includes(q))
})

const filteredTotal = computed(() => filteredUsers.value.length)
const pageCount = computed(() => Math.max(1, Math.ceil(filteredUsers.value.length / pageSize.value)))
const visibleUsers = computed(() => {
  const start = (page.value - 1) * pageSize.value
  return filteredUsers.value.slice(start, start + pageSize.value)
})
const pagedTotal = computed(() => visibleUsers.value.length)

watch([searchQuery, searchField, pageSize], () => { page.value = 1 })

function prevPage() {
  if (page.value > 1) page.value--
}

function nextPage() {
  if (page.value < pageCount.value) page.value++
}

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
    config.value.privateMode = !!data.privateMode
    config.value.prohibitGroups = !!data.prohibitGroups
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
    // load invites after config loads
    if (isAdmin.value) {
      await loadInvites()
      await loadUsers()
    }
  }
}

async function loadInvites() {
  invitesLoading.value = true
  try {
    const res = await fetch('/api/invites', { credentials: 'include' })
    if (!res.ok) { invites.value = []; return }
    invites.value = await res.json()
  } catch (e) { invites.value = []; console.warn(e) }
  finally { invitesLoading.value = false }
}

async function createInvite() {
  creatingInvite.value = true
  try {
    const payload = { maxUses: inviteMaxUses.value || 0, expiresInSeconds: inviteExpiresSeconds.value || 0, note: inviteNote.value }
    const res = await fetch('/api/invites', { method: 'POST', credentials: 'include', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
    if (!res.ok) {
      const txt = await res.text().catch(() => '')
      alert(`Create invite failed: ${res.status} ${txt}`)
      return
    }
    const data = await res.json()
    await loadInvites()
    // show copy via prompt
    try { await navigator.clipboard.writeText(data.code); alert('Invite code copied to clipboard') } catch { /* ignore */ }
    inviteNote.value = ''
    inviteMaxUses.value = 1
    inviteExpiresSeconds.value = 0
  } catch (e) { console.warn(e); alert('Network error creating invite') }
  finally { creatingInvite.value = false }
}

function copyInviteCode(code: string) {
  if (!code) return
  if (navigator.clipboard && navigator.clipboard.writeText) {
    navigator.clipboard.writeText(code).then(() => alert('Invite code copied'))
  } else {
    const el = document.createElement('textarea'); el.value = code; document.body.appendChild(el); el.select(); document.execCommand('copy'); document.body.removeChild(el);
    alert('Invite code copied')
  }
}

async function revokeInvite(code: string) {
  if (!confirm('Revoke invite?')) return
  try {
    const res = await fetch(`/api/invites/${code}/revoke`, { method: 'POST', credentials: 'include' })
    if (!res.ok) { const txt = await res.text().catch(() => ''); alert(`Revoke failed: ${res.status} ${txt}`); return }
    await loadInvites()
  } catch (e) { console.warn(e); alert('Network error while revoking invite') }
}

async function loadUsers() {
  usersLoading.value = true
  try {
    const res = await fetch('/api/admin/users', { credentials: 'include' })
    if (!res.ok) { users.value = []; return }
    users.value = await res.json()
  } catch (e) { users.value = []; console.warn(e) }
  finally { usersLoading.value = false }
}

async function promoteUser(id: number) {
  if (!confirm('Promote this user to admin?')) return
  try {
    const res = await fetch(`/api/admin/users/${id}/promote`, { method: 'POST', credentials: 'include' })
    if (!res.ok) {
      const txt = await res.text().catch(() => '')
      alert(`Promote failed: ${res.status} ${txt}`)
      return
    }
    await loadUsers()
  } catch (e) {
    console.warn(e)
    alert('Network error while promoting user')
  }
}

async function demoteUser(id: number) {
  if (id === 1) { alert('The owner account cannot be demoted.'); return }
  if (!confirm('Demote this admin to regular user?')) return
  try {
    const res = await fetch(`/api/admin/users/${id}/demote`, { method: 'POST', credentials: 'include' })
    if (!res.ok) {
      const txt = await res.text().catch(() => '')
      alert(`Demote failed: ${res.status} ${txt}`)
      return
    }
    await loadUsers()
  } catch (e) {
    console.warn(e)
    alert('Network error while demoting user')
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

/* Revoke / delete invite button styling */
.delete-button {
  background: transparent;
  color: var(--border-red-30);
  border: 1px solid var(--border-red-30);
  padding: 6px 10px;
  border-radius: 6px;
  cursor: pointer;
  margin-left: 8px;
  transition: background-color 0.12s ease, color 0.12s ease, transform 0.06s ease;
}
.delete-button:hover {
  background: var(--border-red-30);
  color: var(--text-white);
}
.delete-button:active { transform: translateY(1px); }
.delete-button:disabled { opacity: 0.5; cursor: not-allowed; }

.restart-note { margin-top:12px; padding:10px; background: var(--bg-chat-sidebar-1); border-left:4px solid var(--warning-amber); color: var(--warning-amber); border-radius:4px }

/* Invite form grid (table-like layout) */
.invite-form-grid {
  display: grid;
  grid-template-columns: 140px 1fr 260px;
  gap: 8px 12px;
  align-items: center;
}
.invite-form-row {
  display: contents;
}
.invite-label {
  color: var(--text-purple-70);
  font-weight: 600;
  padding-right: 8px;
  align-self: start;
}
.invite-help {
  font-size: 13px;
  color: var(--text-purple-70);
  margin-top: 6px;
}
.invite-create-col { grid-column: 2 / 3; }

@media (max-width: 720px) {
  .invite-form-grid { grid-template-columns: 1fr; }
  .invite-form-row { display: block; }
  .invite-label { margin-bottom: 6px; }
  .invite-help { margin-top: 6px; }
  .invite-create-col { grid-column: auto; margin-top: 6px; }
}
</style>