<template>
  <body class="auth-body">
    <div class="auth-container">
      <div class="auth-box">
        <h1 class="auth-title">Anarchy Chat</h1>
        <p class="auth-subtitle">Welcome Back</p>

        <form id="loginForm" @submit.prevent="handleLogin">
          <div class="form-group">
            <label for="email">Email</label>
            <input type="email" id="email" placeholder="Enter your email" required v-model="email" autocomplete="email" />
          </div>

          <div class="form-group">
            <label for="password">Password</label>
            <input type="password" id="password" placeholder="Enter your password" required v-model="password" autocomplete="current-password" />
          </div>

          <button type="submit" class="auth-button">Login</button>
        </form>

        <p class="auth-footer">Don't have an account? <router-link to="/register">Register here</router-link></p>

        <div id="errorMessage" class="error-message" :class="{ show: error }">{{ error }}</div>
      </div>
    </div>
  </body>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const email = ref('')
const password = ref('')
const error = ref('')

async function handleLogin() {
  error.value = ''
  try {
    const res = await fetch('/api/auth/login', {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ Username: email.value, Password: password.value }),
    })
    if (!res.ok) {
      const txt = await res.text()
      error.value = `Login failed: ${res.status} ${txt}`
      return
    }
    // On success redirect to home
    router.push({ name: 'Home' })
  } catch (e) {
    error.value = 'Network error'
  }
}
</script>

<style>
/* Reuse same auth styles from HomeView file to keep look consistent */
*{margin:0;padding:0;box-sizing:border-box}
:root{--primary-dark:#36393f;--secondary-dark:#2f3136;--tertiary-dark:#202225;--accent-purple:#7289da;--text-primary:#ffffff;--text-secondary:#b9bbbe;--text-muted:#72767d;--message-hover:#35393f;--border-color:#202225}
body{font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",Roboto, Oxygen,Ubuntu,Cantarell,Fira Sans,"Droid Sans","Helvetica Neue",sans-serif;background:linear-gradient(135deg,#667eea 0%,#764ba2 100%);color:var(--text-primary);}
.auth-container{width:100%;display:flex;justify-content:center;align-items:center;min-height:100vh;padding:20px}
.auth-box{background:var(--secondary-dark);padding:40px;border-radius:8px;width:100%;max-width:400px;box-shadow:0 8px 16px rgba(0,0,0,0.3)}
.auth-title{font-size:32px;font-weight:700;text-align:center;margin-bottom:8px;color:var(--text-primary)}
.auth-subtitle{font-size:18px;text-align:center;color:var(--text-secondary);margin-bottom:32px}
.form-group{margin-bottom:20px}
.form-group label{display:block;font-size:12px;font-weight:700;text-transform:uppercase;color:var(--text-muted);margin-bottom:8px}
.form-group input{width:100%;padding:12px;background-color:var(--tertiary-dark);border:1px solid var(--border-color);border-radius:4px;color:var(--text-primary)}
.auth-button{width:100%;padding:12px;background-color:var(--accent-purple);border:none;border-radius:4px;color:var(--text-primary);font-weight:600}
.auth-footer{ text-align:center;margin-top:20px;color:var(--text-secondary)}
.error-message{margin-top:16px;padding:12px;background-color:rgba(240,71,71,0.1);border:1px solid #f04747;border-radius:4px;color:#f04747;font-size:13px;display:none}
.error-message.show{display:block}
</style>