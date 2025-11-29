<template>
  <div class="container">
    <!-- Sidebar -->
    <aside class="sidebar" v-if="!prohibitGroups">
      <div class="server-icon">
        <span>Chatter</span>
      </div>

      <nav class="channels">
        <div class="channel-section">
          <h3 class="channel-header">Groups</h3>
          <p class="channel-description">Join or create groups</p>
        </div>

        <ul class="group-list">
          <li
            v-for="g in groups"
            :key="g.id"
            :class="['group-item', { 'active': selectedGroupId === g.id } ]"
            @click="selectGroup(g.id)"
            title="Click to open group"
          >
            <div class="group-avatar">{{ (g.name || '').charAt(0) }}</div>
            <div class="group-meta">
              <div class="group-name">{{ g.name }}</div>
              <div class="group-owner">{{ g.ownerName || '' }}</div>
            </div>
          </li>
        </ul>

        <!-- Group creation moved to Profile → Manage Groups -->

      </nav>

      <!-- Join Group Modal -->
      <div class="modal-overlay" v-if="showJoinModal">
        <div class="modal">
          <header class="modal-header">
            <h3>Join Group</h3>
            <button class="modal-close" @click="(showJoinModal = false, joinCode = '', joinError = '')">×</button>
          </header>
          <div class="modal-body">
            <p>Enter a group join code to request membership:</p>
            <input ref="joinInputRef" v-model="joinCode" placeholder="Group code" class="form-group-input" />
            <div v-if="joinError" style="color:var(--error);margin-top:8px">{{ joinError }}</div>
          </div>
          <footer class="modal-footer">
            <button class="cancel-btn" @click="(showJoinModal = false, joinCode = '', joinError = '')">Cancel</button>
            <button class="save-button" @click="joinGroupByCode" :disabled="joining || !joinCode.trim()">{{ joining ? 'Joining...' : 'Join' }}</button>
          </footer>
        </div>
      </div>

      <!-- Join group footer (outside scroll area) -->
      <div class="join-group-footer">
        <div style="display:flex;align-items:center;gap:8px;">
          <div class="join-cta" @click="openJoinModal" title="Join group by code">
            <span class="join-plus">+</span>
            <span class="join-text">Join group</span>
          </div>
          <button class="more-btn" title="Manage groups" @click="openGroupManager">⋯</button>
        </div>
      </div>

      <!-- Group Manager Modal (opened from the three-dots button) -->
      <div class="modal-overlay" v-if="showGroupManager" @click.self="closeGroupManager">
        <div class="modal">
          <div class="modal-header">
            <h3>Group Manager</h3>
            <button class="modal-close" @click="closeGroupManager">×</button>
          </div>

          <form @submit.prevent="createGroupMG">
            <div class="form-group" style="padding:16px 20px;">
              <label for="mgName">Create New Group</label>
              <div style="display:flex;gap:8px;align-items:center;margin-top:10px;">
                <input id="mgName" v-model="mgNewName" placeholder="Group name" class="form-group-input" />
                <button type="submit" class="save-button" :disabled="mgSaving">{{ mgSaving ? 'Creating...' : 'Create' }}</button>
              </div>
            </div>
          </form>

          <div class="modal-body-manager">
            <h4>Your Groups</h4>
            <div v-if="mgLoading" class="loading">Loading groups...</div>
            <div v-else>
              <div v-if="userGroups.length === 0" class="muted">You don't own any groups yet.</div>
              <ul class="group-list-manager">
                <li v-for="g in userGroups" :key="g.id">
                  <template v-if="mgEditId === g.id">
                    <div class="gm-left">
                      <div class="gm-avatar">{{ (g.name || '').charAt(0) }}</div>
                      <div class="gm-info" style="flex:1;">
                        <input v-model="mgEditName" class="form-group-input" />
                      </div>
                    </div>
                    <div class="gm-actions">
                      <button @click="updateGroupMG" class="save-button" :disabled="mgSaving">{{ mgSaving ? 'Saving...' : 'Save' }}</button>
                      <button @click="(mgEditId = null, mgEditName = '')" class="cancel-btn">Cancel</button>
                    </div>
                  </template>
                  <template v-else>
                    <div class="gm-left">
                      <div class="gm-avatar">{{ (g.name || '').charAt(0) }}</div>
                      <div class="gm-info">
                        <div class="gm-name">{{ g.name }}</div>
                        <div class="gm-owner">Owner: {{ g.ownerName || 'You' }}</div>
                        <div v-if="g.code" class="gm-code" style="margin-top:6px;display:flex;gap:8px;align-items:center">
                          <code style="background:var(--bg-chat-dark-1);padding:6px;border-radius:6px;color:var(--text-white);font-weight:700">{{ g.code }}</code>
                          <button class="change-password-button" @click="copyCodeToClipboard(g.code, g.id)">{{ copiedCodeId === g.id ? 'Copied!' : 'Copy' }}</button>
                          <button class="delete-button" @click="revokeCode(g.id)">Revoke</button>
                        </div>
                        <div v-else style="margin-top:6px;">
                          <button class="save-button" @click="generateCode(g.id)">Generate code</button>
                        </div>
                      </div>
                    </div>
                    <div class="gm-actions">
                      <button v-if="mgEditId !== g.id" @click="startEditGroup(g)" class="change-password-button">Edit</button>
                      <button @click="deleteGroupMG(g.id)" class="delete-button">Deactivate</button>
                    </div>
                  </template>
                </li>
              </ul>
            </div>

            <!-- Inline edit appears inside the list rows now -->
            <div style="margin-top:18px"> 
              <h4>Member Of</h4>
              <div v-if="mgLoading" class="loading">Loading groups...</div>
              <div v-else>
                <div v-if="memberGroups.length === 0" class="muted">You are not a member of any groups.</div>
                <ul class="group-list-manager">
                  <li v-for="g in memberGroups" :key="g.id">
                    <div class="gm-left">
                      <div class="gm-avatar">{{ (g.name || '').charAt(0) }}</div>
                      <div class="gm-info">
                        <div class="gm-name">{{ g.name }}</div>
                        <div class="gm-owner">Owner: {{ g.ownerName || 'Unknown' }}</div>
                      </div>
                    </div>
                    <div class="gm-actions">
                      <button class="delete-button" @click="leaveGroup(g.id)" :disabled="mgLeavingId === g.id">{{ mgLeavingId === g.id ? 'Leaving...' : 'Leave' }}</button>
                    </div>
                  </li>
                </ul>
              </div>
            </div>
          </div>
        </div>
      </div>
    </aside>

    <!-- Main Chat Area -->
    <main class="chat-container">
      <!-- Header with Profile in Top Right -->
      <header class="chat-header">
        <div class="header-left">
              <h2 class="channel-title">{{ currentGroupName }}</h2>
        </div>
        
        <div class="header-right">
          <router-link to="/profile" class="profile-link">
            <div class="user-avatar-small">{{ currentUser?.name?.charAt(0) || 'U' }}</div>
            <span class="user-name-header">{{ currentUser?.name || 'User' }}</span>
          </router-link>
          <button class="logout-btn-header" @click="logout" title="Logout">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path>
              <polyline points="16 17 21 12 16 7"></polyline>
              <line x1="21" y1="12" x2="9" y2="12"></line>
            </svg>
          </button>
        </div>
      </header>

      <!-- Messages -->
      <div class="messages-area" id="messagesArea">
        <div v-if="loading" class="loading">Loading messages...</div>
        <div v-else-if="messages.length === 0" class="empty-state">
          <p>No messages yet. Start the conversation!</p>
        </div>
        <div v-else>
          <div
            v-for="(m, i) in messages"
            :key="m.id || i"
            :class="['message', { 'message--mine': m.author === currentUser?.name }]"
          >
            <div class="message-avatar">{{ (m.author || '').charAt(0) }}</div>
            <div class="message-content">
                <div class="message-header">
                    <div class="message-author">{{ m.author }}</div>
                    <div class="message-timestamp">{{ m.date }} • {{ m.time }}</div>
                </div>
              <div class="message-text">{{ m.text }}</div>
            </div>
          </div>
        </div>
      </div>

      <!-- Message Input -->
      <div class="message-input-container">
        <form id="messageForm" @submit.prevent="sendMessage">
          <input
            type="text"
            id="messageInput"
            :placeholder="currentGroupName ? `Message ${currentGroupName}` : 'Message'"
            class="message-input"
            v-model="newMessage"
            autocomplete="off"
            :disabled="sending"
          />
          <button type="submit" class="send-btn" :disabled="sending || !newMessage.trim()">
            <svg v-if="!sending" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="22" y1="2" x2="11" y2="13"></line>
              <polygon points="22 2 15 22 11 13 2 9 22 2"></polygon>
            </svg>
            <span v-else>...</span>
          </button>
        </form>
        <div v-if="error" class="error-banner">{{ error }}</div>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, nextTick } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const prohibitGroups = ref(false)
const prohibitGeneral = ref(false)
const groups = ref<{ id: number; name: string; ownerId: number; ownerName?: string; isDeactivated?: boolean }[]>([])
const selectedGroupId = ref<number>(1)
const currentGroupName = ref('General')
const userGroupLimit = ref<number | null>(null)
const ownerGroupCount = ref<number>(0)
const messages = ref<{ id: number; author: string; text: string; time: string; sentAt?: string; date?: string }[]>([])
const newMessage = ref('')
const loading = ref(true)
const sending = ref(false)
const error = ref('')
const currentUser = ref<{ id: number; name: string; email: string; isAdmin: boolean } | null>(null)

// Join group UI state (modal)
const showJoinModal = ref(false)
const joinCode = ref('')
const joining = ref(false)
const joinError = ref('')
const joinInputRef = ref<HTMLInputElement | null>(null)

function openJoinModal() {
  joinError.value = ''
  showJoinModal.value = true
  nextTick(() => {
    joinInputRef.value?.focus()
  })
}

// --- Group Manager (opened from sidebar three-dots) ---
const showGroupManager = ref(false)
const mgLoading = ref(false)
const userGroups = ref<Array<{ id: number; name: string; ownerId: number; ownerName?: string; isDeactivated?: boolean; code?: string | null; codeGeneratedAt?: string | null }>>([])
const memberGroups = ref<Array<{ id: number; name: string; ownerId: number; ownerName?: string; isDeactivated?: boolean; code?: string | null }>>([])
const mgNewName = ref('')
const mgEditId = ref<number | null>(null)
const mgEditName = ref('')
const mgSaving = ref(false)
const copiedCodeId = ref<number | null>(null)
const mgLeavingId = ref<number | null>(null)

function openGroupManager() {
  showGroupManager.value = true
  loadUserGroups()
}

function closeGroupManager() {
  showGroupManager.value = false
  mgNewName.value = ''
  mgEditId.value = null
  mgEditName.value = ''
}

async function loadUserGroups() {
  if (!currentUser.value) return
  mgLoading.value = true
  try {
    const res = await fetch('/api/groups', { credentials: 'include' })
    if (!res.ok) {
      userGroups.value = []
      return
    }
    const data = await res.json()
    // Owner groups
    userGroups.value = data.filter((g: any) => g.ownerId === currentUser.value!.id && !g.isDeactivated)
    // Member groups (exclude owned groups) - groups where the user is a member but not the owner
    memberGroups.value = data.filter((g: any) => g.ownerId !== currentUser.value!.id && !g.isDeactivated)
  } catch (e) {
    console.warn('Failed to load user groups', e)
    userGroups.value = []
  } finally {
    mgLoading.value = false
  }
}

async function leaveGroup(groupId: number) {
  if (!confirm('Leave this group? You will lose access to its messages.')) return
  mgLeavingId.value = groupId
  try {
    const res = await fetch(`/api/groups/${groupId}/leave`, { method: 'POST', credentials: 'include' })
    if (!res.ok) {
      const j = await res.json().catch(() => null)
      alert(j?.message || `Failed to leave group: ${res.status}`)
      return
    }
    // Refresh lists
    await loadUserGroups()
    await loadGroups()
  } catch (e) {
    console.warn('Leave group failed', e)
    alert('Network error while leaving group')
  } finally {
    mgLeavingId.value = null
  }
}

async function createGroupMG() {
  if (!mgNewName.value.trim() || !currentUser.value) return
  mgSaving.value = true
  try {
    const res = await fetch('/api/groups', {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name: mgNewName.value.trim() })
    })
    if (!res.ok) {
      const j = await res.json().catch(() => null)
      const msg = j?.message || `Create failed: ${res.status}`
      alert(msg)
      return
    }
    mgNewName.value = ''
    await loadUserGroups()
    // Also reload all groups in the sidebar
    await loadGroups()
  } catch (e) {
    console.warn('Create group failed', e)
    alert('Network error while creating group')
  } finally {
    mgSaving.value = false
  }
}

function startEditGroup(g: { id: number; name: string }) {
  mgEditId.value = g.id
  mgEditName.value = g.name
}

async function generateCode(groupId: number) {
  try {
    const res = await fetch(`/api/groups/${groupId}/code`, { method: 'POST', credentials: 'include' })
    if (!res.ok) {
      const j = await res.json().catch(() => null)
      alert(j?.message || `Failed to generate code: ${res.status}`)
      return
    }
    const data = await res.json().catch(() => null)
    // update local copy
    const g = userGroups.value.find(x => x.id === groupId)
    if (g) {
      g.code = data?.code || null
      g.codeGeneratedAt = data?.generatedAt || null
    }
    await loadGroups()
  } catch (e) {
    console.warn('Generate code failed', e)
    alert('Network error while generating code')
  }
}

async function revokeCode(groupId: number) {
  if (!confirm('Revoke join code? Users will no longer be able to join using the current code.')) return
  try {
    const res = await fetch(`/api/groups/${groupId}/code`, { method: 'DELETE', credentials: 'include' })
    if (!res.ok) {
      const j = await res.json().catch(() => null)
      alert(j?.message || `Failed to revoke code: ${res.status}`)
      return
    }
    const g = userGroups.value.find(x => x.id === groupId)
    if (g) { g.code = null; g.codeGeneratedAt = null }
    await loadGroups()
  } catch (e) {
    console.warn('Revoke code failed', e)
    alert('Network error while revoking code')
  }
}

function copyCodeToClipboard(code?: string | null, groupId?: number) {
  if (!code) return
  if (navigator.clipboard && navigator.clipboard.writeText) {
    navigator.clipboard.writeText(code).then(() => {
      copiedCodeId.value = groupId ?? null
      setTimeout(() => { copiedCodeId.value = null }, 1500)
    }).catch(() => {
      // fallback handled below
      fallbackCopy(code, groupId)
    })
  } else {
    // fallback
    fallbackCopy(code, groupId)
  }
}

function fallbackCopy(code: string, groupId?: number) {
  const el = document.createElement('textarea')
  el.value = code
  document.body.appendChild(el)
  el.select()
  try {
    document.execCommand('copy')
    copiedCodeId.value = groupId ?? null
    setTimeout(() => { copiedCodeId.value = null }, 1500)
  } catch (e) {
    console.warn('Fallback copy failed', e)
  }
  document.body.removeChild(el)
}

async function updateGroupMG() {
  if (mgEditId.value === null) return
  mgSaving.value = true
  try {
    const res = await fetch(`/api/groups/${mgEditId.value}`, {
      method: 'PUT',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name: mgEditName.value.trim() })
    })
    if (!res.ok) {
      const j = await res.json().catch(() => null)
      const msg = j?.message || `Update failed: ${res.status}`
      alert(msg)
      return
    }
    mgEditId.value = null
    mgEditName.value = ''
    await loadUserGroups()
    await loadGroups()
  } catch (e) {
    console.warn('Update group failed', e)
    alert('Network error while updating group')
  } finally {
    mgSaving.value = false
  }
}

async function deleteGroupMG(id: number) {
  if (!confirm('Are you sure you want to deactivate this group?')) return
  try {
    const res = await fetch(`/api/groups/${id}`, {
      method: 'DELETE',
      credentials: 'include'
    })
    if (!res.ok) {
      const j = await res.json().catch(() => null)
      const msg = j?.message || `Delete failed: ${res.status}`
      alert(msg)
      return
    }
    await loadUserGroups()
    await loadGroups()
  } catch (e) {
    console.warn('Delete group failed', e)
    alert('Network error while deleting group')
  }
}

async function joinGroupByCode() {
  if (!joinCode.value.trim()) return
  joining.value = true
  try {
    const res = await fetch('/api/groups/join', {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ code: joinCode.value.trim() })
    })
    if (!res.ok) {
      const j = await res.json().catch(() => null)
      const msg = j?.message || `Join failed: ${res.status}`
      joinError.value = msg
      return
    }
    const joined = await res.json().catch(() => null)
    // reload groups and select joined group if provided
    await loadGroups()
    if (joined && joined.id) {
      selectedGroupId.value = joined.id
      await fetchMessages()
    }
    showJoinModal.value = false
    joinCode.value = ''
    joinError.value = ''
  } catch (e) {
    console.warn('Join group failed', e)
    joinError.value = 'Network error while joining group'
  } finally {
    joining.value = false
  }
}

async function getCurrentUser() {
  try {
    const res = await fetch('/api/auth/check', { credentials: 'include' })
    if (!res.ok) return null
    const data = await res.json()
    if (data && data.authenticated && data.user) {
      return data.user
    }
    return null
  } catch (e) {
    return null
  }
}

function parseUtcIsoToLocal(iso?: string) {
  if (!iso) return ''
  // If the string lacks timezone info, treat it as UTC by appending 'Z'.
  // Consider strings ending with 'Z' or with an offset (+HH:MM or +HHMM) as timezone-aware.
  const hasTZ = /[Zz]$|[+-]\d{2}:?\d{2}$/.test(iso)
  const isoWithTZ = hasTZ ? iso : iso + 'Z'
  const date = new Date(isoWithTZ)
  if (isNaN(date.getTime())) return ''
  return date.toLocaleString()
}

function formatDateDDMMYYYY(iso?: string) {
  if (!iso) return ''
  const hasTZ = /[Zz]$|[+-]\d{2}:?\d{2}$/.test(iso)
  const isoWithTZ = hasTZ ? iso : iso + 'Z'
  const date = new Date(isoWithTZ)
  if (isNaN(date.getTime())) return ''
  const dd = String(date.getDate()).padStart(2, '0')
  const mm = String(date.getMonth() + 1).padStart(2, '0')
  const yyyy = date.getFullYear()
  return `${dd}.${mm}.${yyyy}`
}

function formatTimeHHMM(iso?: string) {
  if (!iso) return ''
  const hasTZ = /[Zz]$|[+-]\d{2}:?\d{2}$/.test(iso)
  const isoWithTZ = hasTZ ? iso : iso + 'Z'
  const date = new Date(isoWithTZ)
  if (isNaN(date.getTime())) return ''
  const hh = String(date.getHours()).padStart(2, '0')
  const mm = String(date.getMinutes()).padStart(2, '0')
  return `${hh}:${mm}`
}

async function fetchMessages() {
  try {
    // If General is prohibited, or no group is selected (0), do not fetch messages for General
    if (selectedGroupId.value === 0 || (selectedGroupId.value === 1 && prohibitGeneral.value)) {
      messages.value = []
      return
    }
    const data = await (await fetch(`/api/messages?limit=50&groupId=${selectedGroupId.value}`, { credentials: 'include' })).json()
    // Backend returns [{ id, text, sentFrom: { id, name }, sentAt }]
    messages.value = data.map((m: any) => ({
      id: m.id,
      author: m.sentFrom?.name || 'Unknown',
      text: m.text,
      sentAt: m.sentAt,
      // display local time (24h) and formatted date
      time: m.sentAt ? formatTimeHHMM(m.sentAt) : '',
      date: m.sentAt ? formatDateDDMMYYYY(m.sentAt) : ''
    }))
  } catch (e) {
    console.error('Failed to fetch messages:', e)
  }
}

function canSendToGroup(groupId: number) {
  // No selection
  if (!groupId || groupId === 0) return false
  // General channel allowed only when not prohibited
  if (groupId === 1) return !prohibitGeneral.value
  // Admins may post anywhere (UX-level assumption)
  if (currentUser.value?.isAdmin) return true
  // Owner of group
  if (userGroups.value.some(g => g.id === groupId)) return true
  // Member of group
  if (memberGroups.value.some(g => g.id === groupId)) return true
  return false
}

onMounted(async () => {
  const user = await getCurrentUser()
  if (!user) {
    router.push({ name: 'Login' })
    return
  }
  currentUser.value = user
  await fetchConfig()
  await loadGroups()
  await fetchMessages()
  loading.value = false
})

async function fetchConfig() {
  try {
    const res = await fetch('/api/config', { credentials: 'include' })
    if (!res.ok) return
    const data = await res.json()
    // server now returns `prohibitGroups` (true = hide groups panel)
    prohibitGroups.value = !!data.prohibitGroups
    // server may return `prohibitGeneral` to block posting to General channel
    prohibitGeneral.value = !!data.prohibitGeneral
    // user group limit (optional)
    userGroupLimit.value = typeof data.userGroupLimit === 'number' ? data.userGroupLimit : null
  } catch (e) {
    console.warn('Failed to fetch config:', e)
  }
}

async function loadGroups() {
  try {
    const res = await fetch('/api/groups', { credentials: 'include' })
    if (!res.ok) {
      groups.value = []
      return
    }
    let data = await res.json()
    // Do not auto-insert a default 'General' group client-side. The server is the source
    // of truth for groups and should return any groups that exist. This prevents a
    // blank or placeholder channel appearing in the sidebar when the server intentionally
    // omits General (for example when it's prohibited) or when data is incomplete.
    groups.value = data
    // If selected group doesn't exist, select General
    if (!groups.value.find(g => g.id === selectedGroupId.value)) {
      if (groups.value.length > 0) {
        selectedGroupId.value = groups.value[0]!.id
      } else {
        // No groups available (General may be prohibited) — clear selection
        selectedGroupId.value = 0
      }
    }
    const g = groups.value.find(g => g.id === selectedGroupId.value)
    currentGroupName.value = g ? g.name : ''
    // compute owner's active group count
    if (currentUser.value) {
      ownerGroupCount.value = groups.value.filter(gr => gr.ownerId === currentUser.value!.id && !gr.isDeactivated).length
    } else {
      ownerGroupCount.value = 0
    }
  } catch (e) {
    console.warn('Failed to load groups', e)
    groups.value = []
  }
}

function selectGroup(id: number) {
  // If attempting to select General while it's prohibited, ignore selection
  if (id === 1 && prohibitGeneral.value) {
    // if there are other groups, pick the first one; otherwise clear selection
    const alt = groups.value.find(g => g.id !== 1)
    if (alt) {
      selectedGroupId.value = alt.id
    } else {
      selectedGroupId.value = 0
      currentGroupName.value = ''
      messages.value = []
      return
    }
  } else {
    selectedGroupId.value = id
  }
  const g = groups.value.find(g => g.id === selectedGroupId.value)
  currentGroupName.value = g ? g.name : ''
  loading.value = true
  fetchMessages().finally(() => (loading.value = false))
}

// Group creation is handled in the Profile -> Manage Groups dialog

async function sendMessage() {
  error.value = ''
  const text = newMessage.value.trim()
  if (!text) return

  if (!currentUser.value) {
    router.push({ name: 'Login' })
    return
  }

  // Friendly client-side check: ensure user is composing into a valid group
  if (!canSendToGroup(selectedGroupId.value)) {
    error.value = "You are not composing into a group. Select, create, or join a group first.";
    return
  }

  sending.value = true
  try {
    const payload = { Message: text, UserId: currentUser.value.id, GroupId: selectedGroupId.value }
    const res = await fetch('/api/messages', {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    })

    if (!res.ok) {
      const txt = await res.text().catch(() => '')
      error.value = `Send failed: ${res.status} ${txt}`
      return
    }

    const data = await res.json()
    // Backend returns { id, text, sentFrom: { id, name }, sentAt }
    messages.value.push({
      id: data.id,
      author: data.sentFrom?.name || currentUser.value.name,
      text: data.text,
      sentAt: data.sentAt,
      time: data.sentAt ? formatTimeHHMM(data.sentAt) : new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', hour12: false }),
      date: data.sentAt ? formatDateDDMMYYYY(data.sentAt) : ''
    })
    newMessage.value = ''
  } catch (e) {
    error.value = 'Network error while sending message'
  } finally {
    sending.value = false
  }
}

async function logout() {
  try {
    await fetch('/api/auth/logout', { method: 'POST', credentials: 'include' })
  } catch (e) {
    // ignore
  }
  currentUser.value = null
  router.push({ name: 'Login' })
}
</script>
<style>
/* CSS Reset and Variables */
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

:root {
  /* Alias legacy view variables to the new global theme variables */
  --primary-dark: var(--bg-chat-dark-1);
  --secondary-dark: var(--bg-chat-sidebar-1);
  --tertiary-dark: var(--bg-chat-dark-2);
  --accent-purple: var(--brand-purple-primary);
  --accent-hover: var(--brand-purple-dark);
  --text-primary: var(--text-white);
  --text-secondary: var(--text-purple-90);
  --text-muted: var(--text-purple-60);
  --message-hover: var(--message-hover);
  --border-color: var(--border-white-10);
  --success: var(--profile-status-online);
  --error: var(--border-red-30);

  --sidebar-width: clamp(240px, 20vw, 280px);
  --header-height: clamp(56px, 8vh, 64px);
  --spacing-xs: clamp(0.25rem, 0.5vw, 0.5rem);
  --spacing-sm: clamp(0.5rem, 1vw, 0.75rem);
  --spacing-md: clamp(0.75rem, 1.5vw, 1rem);
  --spacing-lg: clamp(1rem, 2vw, 1.5rem);
  --font-sm: clamp(0.75rem, 2vw, 0.875rem);
  --font-md: clamp(0.875rem, 2.5vw, 1rem);
  --font-lg: clamp(1rem, 3vw, 1.125rem);
}

body {
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", "Roboto", "Oxygen", "Ubuntu", "Cantarell", "Fira Sans",
    "Droid Sans", "Helvetica Neue", sans-serif;
  background-color: var(--tertiary-dark);
  color: var(--text-primary);
}

.container {
  display: flex;
  block-size: 100vh;
  inline-size: 100%;
}

/* Sidebar */
.sidebar {
  inline-size: var(--sidebar-width);
  background-color: var(--secondary-dark);
  display: flex;
  flex-direction: column;
  border-inline-end: 1px solid var(--border-color);
  overflow-y: auto;
}

.server-icon {
  inline-size: 100%;
  block-size: var(--header-height);
  background-color: var(--accent-purple);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: var(--font-lg);
  font-weight: bold;
  color: var(--text-primary);
  border-block-end: 1px solid var(--border-color);
}

.group-list {
  list-style: none;
  margin-top: 8px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.group-item {
  display: flex;
  gap: 12px;
  align-items: center;
  padding: 8px;
  border-radius: 8px;
  cursor: pointer;
  transition: background-color 0.15s;
}

.group-item:hover { background-color: rgba(255,255,255,0.02); }

.group-item.active { background-color: rgba(114,137,218,0.06); border-left: 4px solid var(--accent-purple); }

.group-avatar {
  inline-size: 36px;
  block-size: 36px;
  border-radius: 8px;
  background-color: var(--accent-purple);
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
}

.group-meta { display: flex; flex-direction: column; }
.group-name { font-weight: 600; }
.group-owner { font-size: 0.8rem; color: var(--text-muted); }

/* create-group UI moved to Profile -> Manage Groups dialog */

.join-group-footer {
  position: sticky;
  bottom: 0;
  padding: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(180deg, rgba(0,0,0,0.0), rgba(0,0,0,0.06));
}
.join-cta {
  display:flex;gap:8px;align-items:center;cursor:pointer;padding:8px 12px;border-radius:8px;transition:background 0.15s;border:1px solid transparent
}
.join-cta:hover { background: rgba(114,137,218,0.06); }
.join-plus { display:inline-flex;width:28px;height:28px;border-radius:6px;background:var(--accent-purple);color:var(--text-primary);align-items:center;justify-content:center;font-weight:700 }
.join-text { color:var(--text-primary);font-weight:600 }
.join-input-wrap { display:flex;gap:8px;align-items:center }
.join-input { padding:8px;border-radius:6px;border:1px solid var(--border-color); background:var(--secondary-dark); color:var(--text-primary) }
 .more-btn {
    background: transparent;
    border: 1px solid transparent;
    color: var(--text-purple-70);
    padding: 6px 10px;
    border-radius: 8px;
    cursor: pointer;
    font-size: 18px;
    line-height: 1;
  }
  .more-btn:hover { background: rgba(255,255,255,0.02); color: var(--text-white); border-color: var(--border-color) }

/* Modal styles (used for Join Group) */
  .modal-overlay {
  position: fixed;
  inset: 0;
  background-color: rgba(0,0,0,0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1200;
  padding: 16px;
}
  .modal {
    background-color: var(--bg-chat-sidebar-1);
    border-radius: 10px;
    width: 100%;
    max-width: 520px;
    box-shadow: 0 16px 48px rgba(0,0,0,0.6);
    overflow: hidden;
    border: 1px solid var(--border-white-5);
    display: flex;
    flex-direction: column;
  }
  .modal-header { display:flex;align-items:center;justify-content:space-between;padding:18px 20px;border-bottom:1px solid var(--border-white-5); background: linear-gradient(180deg, rgba(255,255,255,0.02), transparent) }
  .modal-header h3 { margin:0;font-size:18px;color:var(--text-white);font-weight:700 }
  .modal-body { padding:18px 20px; display:flex; flex-direction:column; gap:12px }
  .modal-footer { padding:14px 20px; display:flex; gap:8px; justify-content:flex-end; border-top:1px solid var(--border-white-5); background: var(--bg-chat-sidebar-1) }
  .modal-close { background:transparent;border:none;font-size:20px;cursor:pointer;color:var(--text-purple-70);padding:6px;border-radius:6px }
  .modal-close:hover { background:var(--bg-chat-sidebar-2); color:var(--text-white) }

  /* Group Manager specific styles */
  .modal .form-group { padding: 12px 20px; }
  .modal h4 { color: var(--text-white); margin: 8px 0; font-size: 15px; }
  .modal .muted { color: var(--text-muted); font-size: 13px }

  .group-list-manager { list-style: none; padding: 0; margin: 12px 0; }
  .group-list-manager li { display:flex; align-items:center; justify-content:space-between; gap:12px; padding:10px 0; border-bottom:1px solid var(--border-white-5); }
  .group-list-manager .gm-left { display:flex; gap:12px; align-items:center }
  .group-list-manager .gm-avatar { width:40px; height:40px; border-radius:8px; background:var(--bg-chat-dark-1); display:flex; align-items:center; justify-content:center; color:var(--text-white); font-weight:700 }
  .group-list-manager .gm-info { display:flex; flex-direction:column }
  .group-list-manager .gm-name { font-weight:700; color:var(--text-white) }
  .group-list-manager .gm-owner { font-size:12px; color:var(--text-purple-70) }

  .group-list-manager .gm-actions { display:flex; gap:8px; align-items:center }
  .group-list-manager .gm-actions .change-password-button,
  .group-list-manager .gm-actions .delete-button { padding:6px 10px; font-size:13px }

  /* Group action button colors */
  .group-list-manager .gm-actions .change-password-button {
    background-color: var(--bg-chat-sidebar-2);
    color: var(--text-white);
    border: none;
    border-radius: 6px;
  }
  .group-list-manager .gm-actions .change-password-button:hover { filter: brightness(0.95); }

  .group-list-manager .gm-actions .delete-button {
    background-color: var(--border-red-30);
    color: var(--text-white);
    border: none;
    border-radius: 6px;
  }
  .group-list-manager .gm-actions .delete-button:hover { filter: brightness(0.9); }

  .modal .edit-row { display:flex; gap:8px; align-items:center; margin-top:10px }
  .modal .edit-row .form-group-input { max-width: 360px }

  /* Make the manager body scrollable when lots of groups exist */
  .modal .modal-body-manager {
    padding: 0 20px 20px 20px;
    max-height: calc(70vh - 120px);
    overflow-y: auto;
    display: flex;
    flex-direction: column;
    gap: 8px;
  }

  /* Ensure list uses compact rows inside scrollable area */
  .modal .group-list-manager { margin: 8px 0 0 0; padding: 0; }

  /* Modal-scoped button colors for manager actions (fallback selectors) */
  .modal .change-password-button { background-color: var(--bg-chat-sidebar-2); color: var(--text-white); border: none; border-radius: 6px; padding:6px 10px }
  .modal .change-password-button:hover { filter: brightness(0.95); }
  .modal .delete-button { background-color: var(--border-red-30); color: var(--text-white); border: none; border-radius: 6px; padding:6px 10px }
  .modal .delete-button:hover { filter: brightness(0.9); }

  /* Loading state inside modal */
  .modal .loading { padding: 14px 20px; color: var(--text-purple-70) }

  @media (max-width: 640px) {
    .modal { max-width: 92%; }
    .group-list-manager .gm-avatar { width:34px; height:34px }
    .group-list-manager .gm-name { font-size: 14px }
  }

  /* Input used throughout app */
  .form-group-input {
    width: 100%;
    box-sizing: border-box;
    padding: 10px 12px;
    border-radius: 6px;
    border: 1px solid var(--input-border);
    background: var(--input-bg);
    color: var(--text-white);
    font-size: 14px;
  }
  .form-group-input:focus { outline: none; border-color: var(--brand-blue-primary); box-shadow: 0 0 0 3px rgba(114,137,218,0.06) }

  .modal .save-button { padding: 8px 14px; font-size: 14px }
  .modal .cancel-btn { padding: 8px 12px; font-size: 14px }

  /* Modal button colors (consistent across Join + Group Manager) */
  .modal .save-button {
    background-color: var(--brand-blue-primary);
    color: var(--text-white);
    border: none;
    border-radius: 6px;
  }
  .modal .save-button:hover:not(:disabled) { filter: brightness(0.95); }

  .modal .cancel-btn {
    background: transparent;
    color: var(--text-white);
    border: 1px solid var(--border-white-10);
    border-radius: 6px;
  }
  .modal .cancel-btn:hover { background: var(--bg-chat-sidebar-2); }

    .channels {
      flex: 1;
      padding-block: var(--spacing-lg);
      padding-inline: var(--spacing-md);
      overflow-y: auto;
    }

.channel-section {
  padding-inline: var(--spacing-sm);
}

.channel-header {
  font-size: var(--font-lg);
  font-weight: 700;
  color: var(--text-primary);
  padding-block-end: var(--spacing-sm);
  margin-block-end: var(--spacing-xs);
}

.channel-description {
  font-size: var(--font-sm);
  color: var(--text-muted);
  line-height: 1.4;
}

/* Chat Container */
.chat-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  background-color: var(--primary-dark);
  min-inline-size: 0;
}

/* Header with Profile */
.chat-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-inline: var(--spacing-lg);
  padding-block: var(--spacing-md);
  border-block-end: 1px solid var(--border-color);
  block-size: var(--header-height);
  background-color: var(--secondary-dark);
}

.header-left {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
}

.channel-title {
  font-size: var(--font-lg);
  font-weight: 600;
  color: var(--text-primary);
}

.header-right {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
}

.profile-link {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding-inline: var(--spacing-md);
  padding-block: var(--spacing-sm);
  border-radius: 4px;
  text-decoration: none;
  color: var(--text-primary);
  transition: background-color 0.2s;
}

.profile-link:hover {
  background-color: var(--primary-dark);
}

.user-avatar-small {
  inline-size: clamp(28px, 5vw, 32px);
  block-size: clamp(28px, 5vw, 32px);
  border-radius: 50%;
  background-color: var(--accent-purple);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: var(--font-sm);
  font-weight: bold;
}

.user-name-header {
  font-size: var(--font-md);
  font-weight: 500;
  white-space: nowrap;
}

.logout-btn-header {
  background: none;
  border: none;
  color: var(--text-secondary);
  cursor: pointer;
  padding: var(--spacing-sm);
  border-radius: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
}

.logout-btn-header:hover {
  background-color: var(--primary-dark);
  color: var(--error);
}

/* Messages Area */
.messages-area {
  flex: 1;
  overflow-y: auto;
  padding: var(--spacing-lg);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.loading,
.empty-state {
  text-align: center;
  color: var(--text-muted);
  padding-block: var(--spacing-lg);
  font-size: var(--font-md);
}

.message {
  display: flex;
  gap: var(--spacing-md);
  padding-inline: var(--spacing-md);
  padding-block: var(--spacing-sm);
  border-radius: 4px;
  transition: background-color 0.2s;
}

.message:hover {
  background-color: var(--message-hover);
}

.message--mine {
  background-color: rgba(114,137,218,0.06);
  border-inline-start: 4px solid var(--accent-purple);
}

.message-avatar {
  inline-size: clamp(32px, 6vw, 40px);
  block-size: clamp(32px, 6vw, 40px);
  border-radius: 50%;
  background-color: var(--accent-purple);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: var(--font-sm);
  font-weight: bold;
  flex-shrink: 0;
}

.message-content {
  flex: 1;
  min-inline-size: 0;
}

.message-header {
  display: flex;
  gap: var(--spacing-sm);
  align-items: baseline;
  margin-block-end: 4px;
  flex-wrap: wrap;
}

.message-author {
  font-weight: 500;
  color: var(--text-primary);
  font-size: var(--font-md);
}

.message-timestamp {
  font-size: var(--font-sm);
  color: var(--text-muted);
}

.message-text {
  color: var(--text-primary);
  font-size: var(--font-md);
  line-height: 1.4;
  word-wrap: break-word;
  overflow-wrap: break-word;
}

/* Message Input */
.message-input-container {
  padding: var(--spacing-lg);
  border-block-start: 1px solid var(--border-color);
}

#messageForm {
  display: flex;
  gap: var(--spacing-sm);
}

.message-input {
  flex: 1;
  background-color: var(--secondary-dark);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  padding-inline: var(--spacing-lg);
  padding-block: var(--spacing-md);
  color: var(--text-primary);
  font-size: var(--font-md);
  font-family: inherit;
  transition: border-color 0.2s, background-color 0.2s;
}

.message-input::placeholder {
  color: var(--text-muted);
}

.message-input:focus {
  outline: none;
  border-color: var(--accent-purple);
  background-color: var(--bg-chat-dark-2);
}

.message-input:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.send-btn {
  background-color: var(--accent-purple);
  border: none;
  border-radius: 8px;
  padding-inline: clamp(1rem, 3vw, 1.5rem);
  padding-block: var(--spacing-md);
  color: var(--text-primary);
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
  min-inline-size: clamp(44px, 8vw, 56px);
}

.send-btn:hover:not(:disabled) {
  background-color: var(--accent-hover);
}

.send-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.error-banner {
  margin-block-start: var(--spacing-sm);
  padding: var(--spacing-sm);
  background-color: rgba(240, 71, 71, 0.1);
  border: 1px solid var(--error);
  border-radius: 4px;
  color: var(--error);
  font-size: var(--font-sm);
}

/* Notice shown when General channel is prohibited */
.message-disabled-note {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: calc(var(--spacing-md) + 2px);
  margin: calc(var(--spacing-md));
  border-radius: 8px;
  background: linear-gradient(90deg, rgba(255,255,255,0.02), transparent);
  border-left: 4px solid var(--warning-amber); /* warning accent */
  color: var(--text-muted);
  font-size: var(--font-md);
}

.message-disabled-note svg {
  inline-size: 28px;
  block-size: 28px;
  color: var(--warning-amber);
  flex-shrink: 0;
}

.message-disabled-note .note-text {
  color: var(--text-secondary);
  font-size: 0.95rem;
  line-height: 1.3;
}

/* Scrollbar */
::-webkit-scrollbar {
  inline-size: 8px;
}

::-webkit-scrollbar-track {
  background: transparent;
}

::-webkit-scrollbar-thumb {
  background: var(--text-muted);
  border-radius: 4px;
}

::-webkit-scrollbar-thumb:hover {
  background: var(--text-secondary);
}

/* Responsive - Tablet */
@media (max-width: 768px) {
  :root {
    --sidebar-width: 200px;
  }
  
  .user-name-header {
    display: none;
  }
  
  .channel-description {
    font-size: 0.75rem;
  }
}

/* Responsive - Mobile */
@media (max-width: 480px) {
  :root {
    --sidebar-width: 60px;
  }
  
  .sidebar {
    padding: 0;
  }
  
  .channel-header,
  .channel-description {
    display: none;
  }
  
  .channels {
    padding: var(--spacing-sm);
  }
  
  .user-name-header {
    display: none;
  }
  
  .profile-link {
    padding-inline: var(--spacing-sm);
  }
}
</style>