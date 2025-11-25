<template>
  <div class="auth-body">
    <div class="auth-container">
      <div class="auth-box">
        <h1 class="auth-title">Anarchy Chat</h1>
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

          <button type="submit" class="auth-button">Create Account</button>
        </form>

        <p class="auth-footer">Already have an account? <router-link to="/login">Login here</router-link></p>

        <div id="errorMessage" class="error-message" :class="{ show: error }">{{ error }}</div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const username = ref('')
const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const error = ref('')
const showPassword = ref(false)
const showConfirmPassword = ref(false)

async function handleRegister() {
  error.value = ''
  if (password.value !== confirmPassword.value) {
    error.value = 'Passwords do not match'
    return
  }
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
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: #ffffff;
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
  background: #2f3136;
  padding: 40px;
  border-radius: 8px;
  width: 100%;
  max-width: 400px;
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.3);
}

.auth-title {
  font-size: 32px;
  font-weight: 700;
  text-align: center;
  margin-bottom: 8px;
  color: #ffffff;
}

.auth-subtitle {
  font-size: 18px;
  text-align: center;
  color: #b9bbbe;
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
  color: #72767d;
  margin-bottom: 8px;
}

.form-group input {
  width: 100%;
  padding: 12px;
  background-color: #202225;
  border: 1px solid #202225;
  border-radius: 4px;
  color: #ffffff;
  font-size: 14px;
  transition: border-color 0.2s ease;
}

.form-group input:focus {
  outline: none;
  border-color: #7289da;
}

.form-group input::placeholder {
  color: #72767d;
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
  color: #b9bbbe;
  padding: 4px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
}

.password-toggle:hover { color: #fff; }

.auth-button {
  width: 100%;
  padding: 12px;
  background-color: #7289da;
  border: none;
  border-radius: 4px;
  color: #ffffff;
  font-weight: 600;
  font-size: 14px;
  cursor: pointer;
  transition: background-color 0.2s ease;
}

.auth-button:hover {
  background-color: #5b7fd4;
}

.auth-footer {
  text-align: center;
  margin-top: 20px;
  color: #b9bbbe;
  font-size: 14px;
}

.auth-footer a {
  color: #7289da;
  text-decoration: none;
  font-weight: 600;
}

.auth-footer a:hover {
  text-decoration: underline;
}

.error-message {
  margin-top: 16px;
  padding: 12px;
  background-color: rgba(240, 71, 71, 0.1);
  border: 1px solid #f04747;
  border-radius: 4px;
  color: #f04747;
  font-size: 13px;
  display: none;
}

.error-message.show {
  display: block;
}
</style>
