<template>
  <div class="container">
    <!-- Sidebar -->
    <aside class="sidebar" v-if="!prohibitGroups">
      <div class="server-icon">
        <span>AC</span>
      </div>
      
      <nav class="channels">
        <div class="channel-section">
          <h3 class="channel-header">Anarchy</h3>
          <p class="channel-description">Welcome to the chat</p>
        </div>
      </nav>
    </aside>

    <!-- Main Chat Area -->
    <main class="chat-container">
      <!-- Header with Profile in Top Right -->
      <header class="chat-header">
        <div class="header-left">
          <h2 class="channel-title">General</h2>
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
            <div class="message-avatar">{{ m.author.charAt(0) }}</div>
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
      <div v-if="!prohibitGeneral" class="message-input-container">
        <form id="messageForm" @submit.prevent="sendMessage">
          <input
            type="text"
            id="messageInput"
            placeholder="Message General"
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
      <div v-else class="message-disabled-note">
        Sending messages to the General channel is disabled by an administrator.
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const prohibitGroups = ref(false)
const prohibitGeneral = ref(false)
const messages = ref<{ id: number; author: string; text: string; time: string; sentAt?: string; date?: string }[]>([])
const newMessage = ref('')
const loading = ref(true)
const sending = ref(false)
const error = ref('')
const currentUser = ref<{ id: number; name: string; email: string; isAdmin: boolean } | null>(null)

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
    const res = await fetch('/api/messages?limit=50', { credentials: 'include' })
    if (!res.ok) return
    const data = await res.json()
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

onMounted(async () => {
  const user = await getCurrentUser()
  if (!user) {
    router.push({ name: 'Login' })
    return
  }
  currentUser.value = user
  await fetchConfig()
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
  } catch (e) {
    console.warn('Failed to fetch config:', e)
  }
}

async function sendMessage() {
  error.value = ''
  const text = newMessage.value.trim()
  if (!text) return

  if (!currentUser.value) {
    router.push({ name: 'Login' })
    return
  }

  sending.value = true
  try {
    const payload = { Message: text, UserId: currentUser.value.id }
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
  block-size: clamp(48px, 8vh, 56px);
  background-color: var(--accent-purple);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: var(--font-lg);
  font-weight: bold;
  color: var(--text-primary);
  border-block-end: 1px solid var(--border-color);
}

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