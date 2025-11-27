<template>
  <div class="auth-body">
    <div class="auth-container">
      <div class="auth-box">
        <h1 class="auth-title">Chatter</h1>
        <p class="auth-subtitle">Create Account</p>
        
        <form id="registerForm" @submit.prevent="handleRegister">
          <div class="form-group">
            <label for="username">Username</label>
            <input type="text" id="username" placeholder="Choose a username" required v-model="username" autocomplete="username" />
          </div>

          <div class="form-group">
            <label for="email">Email</label>
            <input type="email" id="email" placeholder="Enter your email" required v-model="email" autocomplete="email" />
          </div>

          <div class="form-group">
            <label for="password">Password</label>
            <div class="input-with-toggle">
              <input :type="showPassword ? 'text' : 'password'" id="password" placeholder="Create a password" required v-model="password" autocomplete="new-password" />
              <button type="button" class="password-toggle" @click="showPassword = !showPassword" :aria-pressed="showPassword" :title="showPassword ? 'Hide password' : 'Show password'">
                <svg v-if="!showPassword" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8S1 12 1 12z"></path><circle cx="12" cy="12" r="3"></circle></svg>
                <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17.94 17.94A10.97 10.97 0 0 1 12 20c-7 0-11-8-11-8a21.35 21.35 0 0 1 5.12-6.27"></path><path d="M1 1l22 22"></path></svg>
              </button>
            </div>
          </div>

          <div class="form-group">
            <label for="confirmPassword">Confirm Password</label>
            <div class="input-with-toggle">
              <input :type="showConfirmPassword ? 'text' : 'password'" id="confirmPassword" placeholder="Confirm your password" required v-model="confirmPassword" autocomplete="new-password" />
              <button type="button" class="password-toggle" @click="showConfirmPassword = !showConfirmPassword" :aria-pressed="showConfirmPassword" :title="showConfirmPassword ? 'Hide password' : 'Show password'">
                <svg v-if="!showConfirmPassword" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8S1 12 1 12z"></path><circle cx="12" cy="12" r="3"></circle></svg>
                <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17.94 17.94A10.97 10.97 0 0 1 12 20c-7 0-11-8-11-8a21.35 21.35 0 0 1 5.12-6.27"></path><path d="M1 1l22 22"></path></svg>
              </button>
            </div>
          </div>

          <button type="submit" class="auth-button" :disabled="registerLoading" :aria-busy="registerLoading">
            <svg v-if="!registerLoading" class="btn-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
              <path d="M16 11c1.657 0 3-1.343 3-3s-1.343-3-3-3-3 1.343-3 3 1.343 3 3 3z"></path>
              <path d="M2 21v-2a4 4 0 0 1 4-4h4"></path>
              <path d="M19 16v6"></path>
              <path d="M22 19h-6"></path>
            </svg>
            <svg v-else class="spinner" width="16" height="16" viewBox="0 0 50 50" aria-hidden="true">
              <circle cx="25" cy="25" r="20" fill="none" stroke="currentColor" stroke-width="4" stroke-linecap="round" stroke-dasharray="31.415, 31.415"></circle>
            </svg>
            <span class="btn-label">{{ registerLoading ? 'Creating...' : 'Create Account' }}</span>
          </button>
        </form>

        <p class="auth-footer">Already have an account? <router-link to="/login">Login here</router-link></p>

        <div id="errorMessage" class="error-message" :class="{ show: error }">{{ error }}</div>
      </div>
    </div>
  </div>
</template>


<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const username = ref('')
const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const error = ref('')
const showPassword = ref(false)
const showConfirmPassword = ref(false)
const registerLoading = ref(false)

onMounted(async () => {
  try {
    const res = await fetch('/api/config')
    if (res.ok) {
      const cfg = await res.json()
      if (cfg?.privateMode) {
        // Redirect to login since registration is disabled
        router.replace({ name: 'Login' })
      }
    }
  } catch (e) {
    // ignore - allow registration if config can't be fetched
  }
})

async function handleRegister() {
  error.value = ''
  if (password.value !== confirmPassword.value) {
    error.value = 'Passwords do not match'
    return
  }
  registerLoading.value = true
  try {
    const res = await fetch('/api/users', {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      // UsersController expects CreateUserRequest with fields: Name, Email, Password
      body: JSON.stringify({ Name: username.value, Email: email.value, Password: password.value }),
    })
    if (!res.ok) {
      const txt = await res.text()
      error.value = `Register failed: ${res.status} ${txt}`
      return
    }
    router.push({ name: 'Login' })
  } catch (e) {
    error.value = 'Network error'
  } finally {
    registerLoading.value = false
  }
}
</script>
<style scoped>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

.auth-body {
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Oxygen, Ubuntu, Cantarell, "Fira Sans", "Droid Sans", "Helvetica Neue", sans-serif;
  background: var(--gradient-bg-auth);
  color: var(--text-white);
  min-height: 100vh;
}

.auth-container {
  width: 100%;
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  padding: 20px;
}

.auth-box {
  background: var(--profile-bg);
  padding: 40px;
  border-radius: 8px;
  width: 100%;
  max-width: 400px;
  box-shadow: 0 8px 24px var(--shadow-black-50);
  border: 1px solid var(--border-white-10);
}

.auth-title {
  font-size: 32px;
  font-weight: 700;
  text-align: center;
  margin-bottom: 8px;
  color: var(--text-white);
}

.auth-subtitle {
  font-size: 18px;
  text-align: center;
  color: var(--text-purple-70);
  margin-bottom: 32px;
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  font-size: 12px;
  font-weight: 700;
  text-transform: uppercase;
  color: var(--text-purple-40);
  margin-bottom: 8px;
}

.form-group input {
  width: 100%;
  padding: 12px;
  background-color: var(--input-bg);
  border: 1px solid var(--input-border);
  border-radius: 4px;
  color: var(--text-white);
  font-size: 14px;
  transition: border-color 0.2s ease;
}

.form-group input:focus {
  outline: none;
  border-color: var(--input-border-focus);
}

.form-group input::placeholder {
  color: var(--input-placeholder);
}

.input-with-toggle {
  position: relative;
}

.input-with-toggle input {
  padding-right: 40px;
}

.password-toggle {
  position: absolute;
  right: 6px;
  top: 50%;
  transform: translateY(-50%);
  background: transparent;
  border: none;
  color: var(--text-purple-70);
  padding: 4px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
}

.password-toggle:hover { color: var(--text-white); }

.password-toggle:active {
  transform: translateY(1px);
  opacity: 0.9;
}

.password-toggle:focus {
  outline: 2px solid rgba(114,137,218,0.14);
  outline-offset: 2px;
}

.auth-button {
  width: 100%;
  padding: 12px;
  background: var(--gradient-primary);
  border: none;
  border-radius: 4px;
  color: var(--text-white);
  font-weight: 600;
  font-size: 14px;
  cursor: pointer;
  transition: background 0.12s ease, transform 0.06s ease, box-shadow 0.06s ease;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

.auth-button:hover {
  background: var(--gradient-primary-hover);
}

.spinner { animation: spin 1s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }

.auth-button:active {
  transform: translateY(1px) scale(0.998);
  box-shadow: inset 0 2px 6px rgba(0,0,0,0.25);
}

.auth-button:focus {
  outline: 3px solid rgba(59,130,246,0.18);
  outline-offset: 2px;
}

.btn-icon {
  display: inline-block;
  vertical-align: middle;
  color: var(--text-white);
}

.btn-label {
  display: inline-block;
}

.auth-footer {
  text-align: center;
  margin-top: 20px;
  color: var(--text-purple-70);
  font-size: 14px;
}

.auth-footer a {
  color: var(--brand-purple-primary);
  text-decoration: none;
  font-weight: 600;
}

.auth-footer a:hover {
  text-decoration: underline;
}

.error-message {
  margin-top: 16px;
  padding: 12px;
  background-color: var(--btn-logout-bg-start);
  border: 1px solid var(--border-red-30);
  border-radius: 4px;
  color: var(--btn-logout-text);
  font-size: 13px;
  display: none;
}

.error-message.show {
  display: block;
}
</style>