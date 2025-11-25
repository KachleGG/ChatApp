<template>
  <div class="profile-page">
    <div class="profile-container">
      <div class="profile-header">
        <div class="header-left-section">
          <router-link to="/" class="back-arrow" title="Back to Chat">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="19" y1="12" x2="5" y2="12"></line>
              <polyline points="12 19 5 12 12 5"></polyline>
            </svg>
          </router-link>
          <h1>Profile Settings</h1>
        </div>
      </div>

      <div v-if="loading" class="loading">Loading profile...</div>

      <div v-else class="profile-content">
        <div class="profile-section">
          <h2>Account Information</h2>
          <div class="info-grid">
            <div class="info-item">
              <label>Name</label>
              <p>{{ user?.name }}</p>
            </div>
            <div class="info-item">
              <label>Email</label>
              <p>{{ user?.email }}</p>
            </div>
            <div class="info-item">
              <label>Role</label>
              <p>{{ user?.isAdmin ? 'Administrator' : 'User' }}</p>
            </div>
          </div>
        </div>

        <div v-if="user?.isAdmin" class="profile-section admin-section">
          <h2>Administration</h2>
          <p>You have administrator privileges.</p>
          <router-link to="/admin" class="admin-button">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="3"></circle>
              <path d="M12 1v6m0 6v6m5.2-13.2l-3.4 3.4m-3.6 3.6l-3.4 3.4m13.2-5.2l-3.4-3.4m-3.6 3.6l-3.4-3.4"></path>
            </svg>
            Go to Admin Panel
          </router-link>
        </div>

        <div class="profile-section">
          <h2>Edit Profile</h2>
          <form @submit.prevent="handleUpdate">
            <div class="form-group">
              <label for="name">Name</label>
              <input type="text" id="name" v-model="formData.name" required />
            </div>

            <div class="form-group">
              <label for="email">Email</label>
              <input type="email" id="email" v-model="formData.email" required />
            </div>

            <div class="button-group">
              <button type="submit" class="save-button" :disabled="saving">
                {{ saving ? 'Saving...' : 'Save Changes' }}
              </button>
              <button type="button" class="change-password-button" @click="showPasswordDialog = true">
                Change Password
              </button>
            </div>

            <div v-if="message" class="message" :class="{ error: isError, success: !isError }">
              {{ message }}
            </div>
          </form>
        </div>

        <div class="profile-section danger-section">
          <h2>Actions</h2>
          <div class="danger-actions">
            <button @click="handleLogout" class="logout-button">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path>
                <polyline points="16 17 21 12 16 7"></polyline>
                <line x1="21" y1="12" x2="9" y2="12"></line>
              </svg>
              Logout
            </button>
            <button @click="handleDeactivate" class="delete-button" :disabled="deleting">
              {{ deleting ? 'Deactivating...' : 'Deactivate Account' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Password Change Dialog -->
    <div v-if="showPasswordDialog" class="dialog-overlay" @click.self="closePasswordDialog">
      <div class="dialog">
        <div class="dialog-header">
          <h3>Change Password</h3>
          <button @click="closePasswordDialog" class="close-btn">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="18" y1="6" x2="6" y2="18"></line>
              <line x1="6" y1="6" x2="18" y2="18"></line>
            </svg>
          </button>
        </div>
        <form @submit.prevent="handlePasswordChange">
          <div class="form-group">
            <label for="oldPassword">Current Password</label>
            <div class="input-with-toggle">
              <input :type="showOldPassword ? 'text' : 'password'" id="oldPassword" v-model="passwordForm.oldPassword" required autocomplete="current-password" />
              <button type="button" class="password-toggle" @click="showOldPassword = !showOldPassword" :aria-pressed="showOldPassword" :title="showOldPassword ? 'Hide password' : 'Show password'">
                <svg v-if="!showOldPassword" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8S1 12 1 12z"></path><circle cx="12" cy="12" r="3"></circle></svg>
                <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17.94 17.94A10.97 10.97 0 0 1 12 20c-7 0-11-8-11-8a21.35 21.35 0 0 1 5.12-6.27"></path><path d="M1 1l22 22"></path></svg>
              </button>
            </div>
          </div>
          <div class="form-group">
            <label for="newPassword">New Password</label>
            <div class="input-with-toggle">
              <input :type="showNewPassword ? 'text' : 'password'" id="newPassword" v-model="passwordForm.newPassword" required minlength="6" autocomplete="new-password" />
              <button type="button" class="password-toggle" @click="showNewPassword = !showNewPassword" :aria-pressed="showNewPassword" :title="showNewPassword ? 'Hide password' : 'Show password'">
                <svg v-if="!showNewPassword" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8S1 12 1 12z"></path><circle cx="12" cy="12" r="3"></circle></svg>
                <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17.94 17.94A10.97 10.97 0 0 1 12 20c-7 0-11-8-11-8a21.35 21.35 0 0 1 5.12-6.27"></path><path d="M1 1l22 22"></path></svg>
              </button>
            </div>
          </div>
          <div class="form-group">
            <label for="confirmPassword">Confirm New Password</label>
            <div class="input-with-toggle">
              <input :type="showConfirmPassword ? 'text' : 'password'" id="confirmPassword" v-model="passwordForm.confirmPassword" required autocomplete="new-password" />
              <button type="button" class="password-toggle" @click="showConfirmPassword = !showConfirmPassword" :aria-pressed="showConfirmPassword" :title="showConfirmPassword ? 'Hide password' : 'Show password'">
                <svg v-if="!showConfirmPassword" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8S1 12 1 12z"></path><circle cx="12" cy="12" r="3"></circle></svg>
                <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17.94 17.94A10.97 10.97 0 0 1 12 20c-7 0-11-8-11-8a21.35 21.35 0 0 1 5.12-6.27"></path><path d="M1 1l22 22"></path></svg>
              </button>
            </div>
          </div>
          <div v-if="passwordError" class="message error">{{ passwordError }}</div>
          <div class="dialog-actions">
            <button type="button" @click="closePasswordDialog" class="cancel-btn">Cancel</button>
            <button type="submit" class="save-button" :disabled="changingPassword">
              {{ changingPassword ? 'Changing...' : 'Change Password' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const loading = ref(true)
const saving = ref(false)
const deleting = ref(false)
const changingPassword = ref(false)
const message = ref('')
const isError = ref(false)
const passwordError = ref('')
const user = ref<{ id: number; name: string; email: string; isAdmin: boolean } | null>(null)
const formData = ref({ name: '', email: '' })

const showPasswordDialog = ref(false)
const passwordForm = ref({
  oldPassword: '',
  newPassword: '',
  confirmPassword: ''
})
const showOldPassword = ref(false)
const showNewPassword = ref(false)
const showConfirmPassword = ref(false)

async function fetchProfile() {
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
    user.value = data.user
    formData.value = { name: data.user.name, email: data.user.email }
  } catch (e) {
    router.push({ name: 'Login' })
  }
}

onMounted(async () => {
  await fetchProfile()
  loading.value = false
})

async function handleUpdate() {
  if (!user.value) return
  message.value = ''
  saving.value = true
  isError.value = false

  try {
    const payload: any = {
      Name: formData.value.name,
      Email: formData.value.email,
    }

    const res = await fetch(`/api/users/${user.value.id}`, {
      method: 'PUT',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    })

    if (!res.ok) {
      const data = await res.json().catch(() => ({}))
      message.value = data.message || `Update failed: ${res.status}`
      isError.value = true
      return
    }

    const updated = await res.json()
    user.value = { ...user.value, ...updated }
    message.value = 'Profile updated successfully!'
    isError.value = false
  } catch (e) {
    message.value = 'Network error'
    isError.value = true
  } finally {
    saving.value = false
  }
}

async function handlePasswordChange() {
  passwordError.value = ''

  if (passwordForm.value.newPassword !== passwordForm.value.confirmPassword) {
    passwordError.value = 'New passwords do not match'
    return
  }

  if (passwordForm.value.newPassword.length < 6) {
    passwordError.value = 'Password must be at least 6 characters'
    return
  }

  try {
    changingPassword.value = true
    
    // Update with new password — include current password for server-side verification
    const updateResponse = await fetch(`/api/users/${user.value?.id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({
        Name: formData.value.name,
        Email: formData.value.email,
        Password: passwordForm.value.newPassword,
        CurrentPassword: passwordForm.value.oldPassword
      })
    })

    if (!updateResponse.ok) {
      // Try to extract server error message
      const txt = await updateResponse.text().catch(() => '')
      console.error('Password update failed:', updateResponse.status, txt)
      passwordError.value = 'Current password is incorrect or update failed'
      return
    }

    closePasswordDialog()
    message.value = 'Password changed successfully!'
    isError.value = false
  } catch (err) {
    console.error('Failed to change password:', err)
    passwordError.value = 'Failed to change password'
  } finally {
    changingPassword.value = false
  }
}

function closePasswordDialog() {
  showPasswordDialog.value = false
  passwordForm.value = {
    oldPassword: '',
    newPassword: '',
    confirmPassword: ''
  }
  passwordError.value = ''
}

async function handleLogout() {
  try {
    await fetch('/api/auth/logout', {
      method: 'POST',
      credentials: 'include'
    })
    router.push({ name: 'Login' })
  } catch (err) {
    console.error('Logout failed:', err)
  }
}

async function handleDeactivate() {
  if (!user.value) return
  if (!confirm('Are you sure you want to deactivate your account? This action cannot be undone.')) {
    return
  }

  deleting.value = true
  try {
    const res = await fetch(`/api/users/${user.value.id}`, {
      method: 'DELETE',
      credentials: 'include',
    })

    if (!res.ok) {
      const data = await res.json().catch(() => ({}))
      message.value = data.message || 'Failed to deactivate account'
      isError.value = true
      return
    }

    router.push({ name: 'Login' })
  } catch (e) {
    message.value = 'Network error'
    isError.value = true
  } finally {
    deleting.value = false
  }
}
</script>

<style scoped>
.profile-page {
  min-height: 100vh;
  background-color: #36393f;
  color: #dcddde;
  display: flex;
  justify-content: center;
}

.profile-container {
  width: 100%;
  max-width: 900px;
  display: flex;
  flex-direction: column;
}

.profile-header {
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

.profile-header h1 {
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

.profile-content {
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 20px;
  flex: 1;
}

.profile-section {
  background-color: #2f3136;
  border-radius: 8px;
  padding: 24px;
  border: 1px solid #202225;
}

.profile-section h2 {
  font-size: 16px;
  font-weight: 600;
  color: #fff;
  margin: 0 0 16px 0;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 16px;
}

.info-item label {
  display: block;
  font-size: 12px;
  font-weight: 600;
  color: #b9bbbe;
  text-transform: uppercase;
  margin-bottom: 8px;
}

.info-item p {
  font-size: 14px;
  color: #dcddde;
  margin: 0;
}

.admin-section {
  background-color: #2b2d31;
  border-left: 4px solid #5865f2;
}

.admin-section p {
  color: #b9bbbe;
  margin: 0 0 12px 0;
  font-size: 14px;
}

.admin-button {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 10px 16px;
  background-color: #5865f2;
  color: #fff;
  border-radius: 4px;
  text-decoration: none;
  font-size: 14px;
  font-weight: 500;
  transition: background-color 0.2s ease;
}

.admin-button:hover {
  background-color: #4752c4;
}

.admin-button svg {
  width: 20px;
  height: 20px;
}

.form-group {
  margin-bottom: 16px;
}

.form-group label {
  display: block;
  font-size: 12px;
  font-weight: 600;
  color: #b9bbbe;
  text-transform: uppercase;
  margin-bottom: 8px;
}

.form-group input {
  width: 100%;
  padding: 10px 12px;
  background-color: #1e1f22;
  border: 1px solid #1e1f22;
  border-radius: 4px;
  color: #dcddde;
  font-size: 14px;
  transition: border-color 0.2s ease;
}

.form-group input:focus {
  outline: none;
  border-color: #5865f2;
}

.button-group {
  display: flex;
  gap: 8px;
}

.save-button,
.change-password-button {
  padding: 10px 20px;
  border-radius: 4px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  border: none;
}

.save-button {
  background-color: #5865f2;
  color: #fff;
}

.save-button:hover:not(:disabled) {
  background-color: #4752c4;
}

.save-button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.change-password-button {
  background-color: #4e5058;
  color: #fff;
}

.change-password-button:hover {
  background-color: #5d6169;
}

.message {
  margin-top: 16px;
  padding: 10px 12px;
  border-radius: 4px;
  font-size: 14px;
}

.message.error {
  background-color: #ed4245;
  color: #fff;
}

.message.success {
  background-color: #3ba55d;
  color: #fff;
}

.danger-section {
  background-color: #2b2d31;
  border-left: 4px solid #ed4245;
}

.danger-actions {
  display: flex;
  gap: 8px;
}

.logout-button,
.delete-button {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 10px 20px;
  border-radius: 4px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  border: none;
}

.logout-button {
  background-color: #4e5058;
  color: #fff;
}

.logout-button:hover {
  background-color: #5d6169;
}

.logout-button svg {
  width: 18px;
  height: 18px;
}

.delete-button {
  background-color: #ed4245;
  color: #fff;
}

.delete-button:hover:not(:disabled) {
  background-color: #c03537;
}

.delete-button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.dialog-overlay {
  position: fixed;
  inset: 0;
  background-color: rgba(0, 0, 0, 0.85);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 16px;
}

.dialog {
  background-color: #2f3136;
  border-radius: 8px;
  width: 100%;
  max-width: 500px;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.24);
}

.dialog-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px;
  border-bottom: 1px solid #202225;
}

.dialog-header h3 {
  font-size: 20px;
  font-weight: 600;
  color: #fff;
  margin: 0;
}

.close-btn {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background-color: transparent;
  border: none;
  color: #b9bbbe;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
  padding: 0;
}

.close-btn:hover {
  background-color: #4e5058;
  color: #fff;
}

.close-btn svg {
  width: 20px;
  height: 20px;
}

.dialog form {
  padding: 20px;
}

.dialog-actions {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
  margin-top: 20px;
}

.cancel-btn {
  padding: 10px 20px;
  background-color: transparent;
  border: 1px solid #4e5058;
  border-radius: 4px;
  color: #fff;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
}

.cancel-btn:hover {
  background-color: #4e5058;
}

.profile-page::-webkit-scrollbar {
  width: 8px;
}

.profile-page::-webkit-scrollbar-track {
  background: #2f3136;
}

.profile-page::-webkit-scrollbar-thumb {
  background-color: #202225;
  border-radius: 4px;
}

.profile-page::-webkit-scrollbar-thumb:hover {
  background-color: #1e1f22;
}

.dialog::-webkit-scrollbar {
  width: 8px;
}

.dialog::-webkit-scrollbar-track {
  background: #2f3136;
}

.dialog::-webkit-scrollbar-thumb {
  background-color: #202225;
  border-radius: 4px;
}
</style>

