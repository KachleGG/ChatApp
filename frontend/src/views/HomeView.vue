<template>
  <div class="container">
    <!-- Sidebar -->
    <aside class="sidebar">
      <div class="server-icon">
        <span>AC</span>
      </div>
      <nav class="channels">
        <div class="channel-section">
          <h3 class="channel-header">CHANNELS</h3>
          <ul>
            <li><a href="#" class="channel-link active" data-channel="general">#general</a></li>
            <li><a href="#" class="channel-link" data-channel="random">#random</a></li>
            <li><a href="#" class="channel-link" data-channel="off-topic">#off-topic</a></li>
            <li><a href="#" class="channel-link" data-channel="memes">#memes</a></li>
          </ul>
        </div>
      </nav>

      <div class="user-profile">
        <div class="user-avatar">U</div>
        <div class="user-info">
          <p class="user-name">User</p>
          <p class="user-status">Online</p>
        </div>
        <button class="logout-btn" @click="logout">⎋</button>
      </div>
    </aside>

    <!-- Main Chat Area -->
    <main class="chat-container">
      <!-- Header -->
      <header class="chat-header">
        <h2 id="channel-title">#general</h2>
        <div class="header-icons">
          <button class="icon-btn">🔍</button>
          <button class="icon-btn">ℹ️</button>
        </div>
      </header>

      <!-- Messages -->
      <div class="messages-area" id="messagesArea">
        <div v-if="loading" class="loading">Loading messages...</div>
        <div v-else>
          <div v-for="(m, i) in messages" :key="i" class="message">
            <div class="message-avatar">{{ m.author.charAt(0) }}</div>
            <div class="message-content">
              <div class="message-header">
                <div class="message-author">{{ m.author }}</div>
                <div class="message-timestamp">{{ m.time }}</div>
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
            placeholder="Message #general"
            class="message-input"
            v-model="newMessage"
            autocomplete="off"
          />
          <button type="submit" class="send-btn">Send</button>
        </form>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const messages = ref<{ author: string; text: string; time: string }[]>([])
const newMessage = ref('')
const loading = ref(false)
const sending = ref(false)
const error = ref('')
const currentUser = ref<{ id: number; name: string } | null>(null)

async function getCurrentUser() {
  try {
    const res = await fetch('/api/auth/check', { credentials: 'include' })
    if (!res.ok) return null
    const data = await res.json()
    // Expecting backend to return something like { authenticated: true, user: { id, name } }
    if (data && data.authenticated && data.user) {
      return { id: data.user.id, name: data.user.name }
    }
    return null
  } catch (e) {
    return null
  }
}

async function sendMessage() {
  error.value = ''
  const text = newMessage.value.trim()
  if (!text) return

  // Ensure we have a current user
  if (!currentUser.value) {
    const user = await getCurrentUser()
    if (!user) {
      // Not authenticated -> redirect to login
      router.push({ name: 'Login' })
      return
    }
    currentUser.value = user
  }

  sending.value = true
  try {
    const payload = { Message: text, UserId: currentUser.value!.id }
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

    // Append message locally
    messages.value.push({ author: currentUser.value!.name || 'You', text, time: new Date().toLocaleTimeString() })
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
/* Shared styles for chat app (global within the frontend) */
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

:root {
  --primary-dark: #36393f;
  --secondary-dark: #2f3136;
  --tertiary-dark: #202225;
  --accent-purple: #7289da;
  --text-primary: #ffffff;
  --text-secondary: #b9bbbe;
  --text-muted: #72767d;
  --message-hover: #35393f;
  --border-color: #202225;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", "Roboto", "Oxygen", "Ubuntu", "Cantarell", "Fira Sans",
    "Droid Sans", "Helvetica Neue", sans-serif;
  background-color: var(--tertiary-dark);
  color: var(--text-primary);
  overflow: hidden;
}

.container {
  display: flex;
  height: 100vh;
  width: 100%;
}

.sidebar {
  width: 240px;
  background-color: var(--secondary-dark);
  display: flex;
  flex-direction: column;
  border-right: 1px solid var(--border-color);
  overflow-y: auto;
}

.server-icon {
  width: 100%;
  height: 50px;
  background-color: var(--accent-purple);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  font-weight: bold;
  color: var(--text-primary);
  border-bottom: 1px solid var(--border-color);
}

.channels {
  flex: 1;
  padding: 16px 0;
  overflow-y: auto;
}

.channel-header {
  font-size: 12px;
  font-weight: 700;
  color: var(--text-muted);
  padding: 0 16px 8px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.channel-link {
  display: block;
  padding: 8px 16px;
  color: var(--text-secondary);
  text-decoration: none;
  border-radius: 4px;
  transition: all 0.2s ease;
  font-size: 15px;
}

.channel-link.active { background-color: var(--primary-dark); color: var(--accent-purple); }

.user-profile { display:flex; align-items:center; gap:12px; padding:12px 16px; background:var(--primary-dark); border-top:1px solid var(--border-color); margin:auto 8px 8px; border-radius:4px }
.user-avatar{width:32px;height:32px;border-radius:50%;background:var(--accent-purple);display:flex;align-items:center;justify-content:center;font-weight:bold}

.chat-container { flex:1; display:flex; flex-direction:column; background-color:var(--primary-dark) }
.chat-header{display:flex;justify-content:space-between;align-items:center;padding:16px;border-bottom:1px solid var(--border-color);height:60px}
.messages-area{flex:1;overflow-y:auto;padding:16px;display:flex;flex-direction:column;gap:8px}
.message{display:flex;gap:12px;padding:8px 12px;border-radius:4px}
.message-avatar{width:32px;height:32px;border-radius:50%;background-color:var(--accent-purple);display:flex;align-items:center;justify-content:center}
.message-content{flex:1}
.message-text{color:var(--text-primary)}

.message-input-container{padding:16px;border-top:1px solid var(--border-color)}
.message-input{flex:1;background-color:var(--secondary-dark);border:1px solid var(--border-color);border-radius:4px;padding:12px 16px;color:var(--text-primary)}
.send-btn{background-color:var(--accent-purple);border:none;border-radius:4px;padding:12px 20px;color:var(--text-primary);cursor:pointer}

@media (max-width:768px) { .sidebar{width:60px} }
</style>