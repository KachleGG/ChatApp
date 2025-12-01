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
              <div class="setting-label">User Group Limit</div>
              <input class="text-input" type="number" v-model.number="config.userGroupLimit" placeholder="5" />
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

        <div class="admin-section" style="margin-top:16px">
          <h2>Backup Management</h2>
          <p>Create and manage database backups. Schedule and retention settings are saved to server config.</p>
          <div class="settings-grid">
            <div class="setting-row">
              <div class="setting-label">Enable Backups</div>
              <label class="switch">
                <input type="checkbox" v-model="config.backupEnabled" />
                <span class="slider" aria-hidden="true"></span>
              </label>
            </div>

            <div class="setting-row">
              <div class="setting-label">Schedule</div>
              <div style="display:flex;align-items:center;gap:8px">
                <input class="text-input" type="text" v-model="config.backupSchedule" placeholder="interval:60 or daily:02:00 or weekly:Mon:03:00" />
                <button class="save-btn" @click="openCronBuilder(); showCronBuilder = !showCronBuilder" type="button" style="margin-left:6px">Build Cron</button>
              </div>
            </div>

            <!-- Cron Builder inline removed; modal popup will be used instead -->

            <div class="setting-row">
              <div class="setting-label">Backup Path</div>
              <input class="text-input" type="text" v-model="config.backupPath" placeholder="Optional path (server)" />
            </div>

            <div class="setting-row">
              <div class="setting-label">Retention (keep)</div>
              <input class="text-input" type="number" v-model.number="config.backupRetention" placeholder="5" />
            </div>
          </div>

          <div style="display:flex;gap:8px;align-items:center;margin-top:12px">
            <button class="save-btn" @click="saveConfig" :disabled="saving">Save changes</button>
            <button class="save-btn" @click="createBackupNow" :disabled="creatingBackup">{{ creatingBackup ? 'Creating...' : 'Create Backup Now' }}</button>
            <span style="color:var(--text-purple-70);margin-left:8px">Backups: <strong>{{ backups.length }}</strong></span>
          </div>

          <div v-if="status.enabled" style="margin-top:8px;color:var(--text-purple-70);display:flex;gap:12px;align-items:center">
            <div><strong>Next run:</strong> <span v-if="status.nextRun">{{ formatUtcLocal(status.nextRun) }}</span><span v-else>—</span></div>
          </div>

          <div v-if="backupsLoading" class="loading" style="margin-top:12px">Loading backups...</div>
          <div v-else style="margin-top:12px">
            <table style="width:100%;border-collapse:collapse">
              <thead>
                <tr style="text-align:left;color:var(--text-purple-70)">
                  <th>File</th><th>Timestamp (UTC)</th><th style="width:120px">Size</th><th style="width:240px"></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="b in backups" :key="b.fileName" style="border-top:1px solid var(--border-white-5)">
                  <td style="padding:8px 6px"><code>{{ b.fileName }}</code></td>
                  <td style="padding:8px 6px">{{ new Date(b.timestamp).toISOString() }}</td>
                  <td style="padding:8px 6px">{{ (b.size/1024).toFixed(1) }} KB</td>
                  <td style="padding:8px 6px;text-align:right">
                    <button class="save-btn" @click="downloadBackup(b.fileName)">Download</button>
                    <button class="save-btn" @click="restoreBackup(b.fileName)">Restore</button>
                    <button class="delete-button" @click="deleteBackup(b.fileName)">Delete</button>
                  </td>
                </tr>
                <tr v-if="backups.length === 0">
                  <td colspan="4" style="padding:12px;color:var(--text-purple-70)">No backups found.</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  </div>
  <!-- Schedule Help Modal (top-most, full-screen) -->
  <div v-if="showScheduleHelp" class="modal-backdrop" role="dialog" aria-modal="true" @click.self="closeScheduleHelp">
    <div class="modal-panel" tabindex="-1">
      <div class="modal-header">
        <h3>Backup Schedule &amp; Backup Settings Help</h3>
        <button class="modal-close" @click="closeScheduleHelp" aria-label="Close help">✕</button>
      </div>

      <div class="modal-content">
        <p>This modal explains the schedule formats accepted by the server and practical guidance for backups.</p>

        <h4>Supported schedule formats</h4>
        <ul>
          <li><code>interval:{minutes}</code> — run every N minutes. Example: <code>interval:60</code> runs hourly.</li>
          <li><code>daily:HH:mm</code> — run once per day at the specified 24‑hour time. Example: <code>daily:02:00</code>.</li>
          <li><code>weekly:Day:HH:mm</code> — run weekly on the specified day and time. Day accepts short names like <code>Mon</code>, <code>Tue</code>, etc. Example: <code>weekly:Sun:04:30</code>.</li>
        </ul>

        <h4>Rules &amp; validation</h4>
        <ul>
          <li>Interval must be a positive integer number of minutes (e.g. <code>interval:15</code>).</li>
          <li>Daily and weekly times must be valid times in <code>HH:mm</code> (00:00 — 23:59).</li>
          <li>Weekly day accepts 3‑letter or full names (Mon, Monday).</li>
          <li>Server will validate the schedule before saving; invalid values will be rejected.</li>
        </ul>

        <h4>Examples (copyable)</h4>
        <div class="examples">
          <div class="example-row"><code>interval:15</code><button class="copy-btn" @click="copyExample('interval:15')">Copy</button><div class="example-desc">Small frequent backups (every 15 minutes).</div></div>
          <div class="example-row"><code>daily:02:00</code><button class="copy-btn" @click="copyExample('daily:02:00')">Copy</button><div class="example-desc">Nightly backup at 02:00 UTC.</div></div>
          <div class="example-row"><code>weekly:Sun:04:30</code><button class="copy-btn" @click="copyExample('weekly:Sun:04:30')">Copy</button><div class="example-desc">Weekly maintenance backup on Sunday.</div></div>
        </div>

        <h4>Retention and path guidance</h4>
        <ul>
          <li><strong>Retention:</strong> set how many recent backups to keep (integer &gt;= 1). Older backups beyond this count will be deleted.</li>
          <li><strong>Backup Path:</strong> if set, backups are stored on that path. The server process must have write permission there and sufficient disk space.</li>
          <li><strong>WAL mode:</strong> the backup system attempts to snapshot the live SQLite DB safely; however ensure the server has enough temporary disk space for snapshots.</li>
        </ul>

        <h4>Troubleshooting</h4>
        <ul>
          <li>If backups fail, check server logs and ensure the configured path is writable by the service account.</li>
          <li>Restores overwrite the live database — consider taking a manual copy before restoring in production.</li>
        </ul>

        <div style="display:flex;gap:8px;justify-content:flex-end;margin-top:16px">
          <button class="save-btn" @click="closeScheduleHelp">Got it</button>
        </div>
      </div>
    </div>
  </div>

  <!-- Comprehensive Cron Builder Modal -->
  <div v-if="showCronBuilder" class="modal-backdrop" role="dialog" aria-modal="true" @click.self="showCronBuilder = false">
    <div class="modal-panel" tabindex="-1">
      <div class="modal-header">
        <h3>Cron Builder</h3>
        <button class="modal-close" @click="showCronBuilder = false" aria-label="Close">✕</button>
      </div>

      <div class="modal-content">
        <p>Create a cron expression using the controls below. Preview and validate before applying.</p>

        <div style="display:flex;gap:12px;align-items:center;margin-top:8px">
          <label style="font-weight:600">Mode</label>
          <select v-model="cbMode" class="text-input" style="width:220px">
            <option v-for="m in cbModeList" :key="m.value" :value="m.value">{{ m.label }}</option>
          </select>
          <div style="margin-left:auto;color:var(--text-purple-70);display:flex;gap:10px;align-items:center">
            <div>Preview:</div>
            <code style="background:transparent;padding:0 6px">{{ previewCron }}</code>
            <div style="font-size:13px;color:var(--text-purple-60)">Time: <strong>{{ previewTimeDisplay }}</strong></div>
          </div>
        </div>

        <!-- Preset -->
          <div v-if="cbMode === 'preset'" style="margin-top:12px;display:flex;gap:12px;align-items:center">
          <label style="font-weight:600">Preset</label>
          <select v-model="presetType" class="text-input text-select" style="width:200px" @focus="onSelectFocus('presetType')" @blur="onSelectBlur"
                  :class="{ 'select-focused': selectFocused === 'presetType' }">
            <option value="everyNmin">Every N minutes</option>
            <option value="everyNhours">Every N hours</option>
            <option value="hourlyAt">Hourly at minute</option>
          </select>
          <input type="number" v-model.number="presetN" class="text-input" style="width:120px" min="1" />
          <div style="color:var(--text-purple-70)">units</div>
        </div>

        <!-- Hourly -->
        <div v-if="cbMode === 'hourly'" style="margin-top:12px;display:flex;gap:12px;align-items:center">
          <label style="font-weight:600">Minute</label>
          <input type="number" v-model.number="hourlyMinute" class="text-input" style="width:120px" min="0" max="59" />
        </div>

        <!-- Daily -->
        <div v-if="cbMode === 'daily'" style="margin-top:12px;display:flex;gap:12px;align-items:center">
          <label style="font-weight:600">Time (UTC)</label>
          <div style="display:flex;gap:8px;align-items:center">
            <select v-model.number="dailyHour" class="text-input text-select" style="width:90px">
              <option v-for="h in 24" :key="h" :value="h-1">{{ (h-1) < 10 ? '0'+(h-1) : (h-1) }}</option>
            </select>
            <select v-model.number="dailyMinuteNum" class="text-input text-select" style="width:90px">
              <option v-for="m in 60" :key="m" :value="m-1">{{ (m-1) < 10 ? '0'+(m-1) : (m-1) }}</option>
            </select>
            <div style="color:var(--text-purple-70);font-size:13px">24h: <strong>{{ previewTimeDisplay }}</strong></div>
          </div>
        </div>

        <!-- Weekly -->
        <div v-if="cbMode === 'weekly'" style="margin-top:12px">
          <div style="display:flex;gap:12px;align-items:center;margin-bottom:8px">
            <label style="font-weight:600">Time (UTC)</label>
            <div style="display:flex;gap:8px;align-items:center">
              <select v-model.number="weeklyHour" class="text-input text-select" style="width:90px">
                <option v-for="h in 24" :key="h" :value="h-1">{{ (h-1) < 10 ? '0'+(h-1) : (h-1) }}</option>
              </select>
              <select v-model.number="weeklyMinuteNum" class="text-input text-select" style="width:90px">
                <option v-for="m in 60" :key="m" :value="m-1">{{ (m-1) < 10 ? '0'+(m-1) : (m-1) }}</option>
              </select>
              <div style="color:var(--text-purple-70);font-size:13px">24h: <strong>{{ previewTimeDisplay }}</strong></div>
            </div>
          </div>
          <div style="display:flex;gap:8px;flex-wrap:wrap">
            <label v-for="(d, idx) in cronDayNames" :key="d" style="display:inline-flex;align-items:center;gap:6px">
              <input type="checkbox" v-model="weeklyDays[idx]" /> {{ d }}
            </label>
          </div>
        </div>

        <!-- Monthly -->
        <div v-if="cbMode === 'monthly'" style="margin-top:12px">
          <div style="display:flex;gap:12px;align-items:center;margin-bottom:8px">
            <label style="font-weight:600">Option</label>
            <select v-model="monthlyOption" class="text-input" style="width:160px">
              <option value="dom">Day of month</option>
              <option value="nthWeekday">Nth weekday (advanced)</option>
            </select>
          </div>
          <div v-if="monthlyOption === 'dom'" style="display:flex;gap:12px;align-items:center">
            <label style="font-weight:600">Day</label>
            <input type="number" v-model.number="monthlyDay" class="text-input" style="width:120px" min="1" max="31" />
            <label style="font-weight:600">Time</label>
            <div style="display:flex;gap:8px;align-items:center">
              <select v-model.number="monthlyHour" class="text-input text-select" style="width:90px">
                <option v-for="h in 24" :key="h" :value="h-1">{{ (h-1) < 10 ? '0'+(h-1) : (h-1) }}</option>
              </select>
              <select v-model.number="monthlyMinuteNum" class="text-input text-select" style="width:90px">
                <option v-for="m in 60" :key="m" :value="m-1">{{ (m-1) < 10 ? '0'+(m-1) : (m-1) }}</option>
              </select>
              <div style="color:var(--text-purple-70);font-size:13px">24h: <strong>{{ previewTimeDisplay }}</strong></div>
            </div>
          </div>
          <div v-else style="display:flex;gap:12px;align-items:center">
            <label style="font-weight:600">Ordinal</label>
            <select v-model="monthlyOrdinal" class="text-input" style="width:140px">
              <option value="first">First</option>
              <option value="second">Second</option>
              <option value="third">Third</option>
              <option value="fourth">Fourth</option>
              <option value="last">Last</option>
            </select>
            <label style="font-weight:600">Weekday</label>
            <select v-model.number="monthlyWeekday" class="text-input text-select" style="width:120px" @focus="onSelectFocus('monthlyWeekday')" @blur="onSelectBlur"
                    :class="{ 'select-focused': selectFocused === 'monthlyWeekday' }">
              <option v-for="(d, i) in cronDayNames" :key="d" :value="i">{{ d }}</option>
            </select>
          </div>
        </div>

        <!-- Yearly -->
        <div v-if="cbMode === 'yearly'" style="margin-top:12px;display:flex;gap:12px;align-items:center">
          <label style="font-weight:600">Month</label>
          <input type="number" v-model.number="yearlyMonth" class="text-input" style="width:120px" min="1" max="12" />
          <label style="font-weight:600">Day</label>
          <input type="number" v-model.number="yearlyDay" class="text-input" style="width:120px" min="1" max="31" />
          <label style="font-weight:600">Time</label>
          <div style="display:flex;gap:8px;align-items:center">
            <select v-model.number="yearlyHour" class="text-input text-select" style="width:90px">
              <option v-for="h in 24" :key="h" :value="h-1">{{ (h-1) < 10 ? '0'+(h-1) : (h-1) }}</option>
            </select>
            <select v-model.number="yearlyMinuteNum" class="text-input text-select" style="width:90px">
              <option v-for="m in 60" :key="m" :value="m-1">{{ (m-1) < 10 ? '0'+(m-1) : (m-1) }}</option>
            </select>
          </div>
        </div>

        <!-- Custom -->
        <div v-if="cbMode === 'custom'" style="margin-top:12px">
          <label style="font-weight:600">Raw Cron</label>
          <input class="text-input" type="text" v-model="customCron" placeholder="e.g. 0 2 * * *" />
          <div style="margin-top:8px;color:var(--text-purple-70);font-size:13px">Use standard 5-field cron: <code>minute hour day month day-of-week</code></div>
        </div>

        <div style="display:flex;gap:8px;justify-content:flex-end;margin-top:16px">
          <div style="flex:1;color:var(--text-purple-70);align-self:center">Validation: <strong style="color:var(--text-white)">{{ cronValidation?.message || '—' }}</strong></div>
          <button class="save-btn" @click="applyCronBuilder">Apply</button>
          <button class="delete-button" @click="showCronBuilder = false">Cancel</button>
        </div>
      </div>
    </div>
  </div>
 
</template>

<script setup lang="ts">
import { ref, onMounted, computed, watch, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const loading = ref(true)
const isAdmin = ref(false)
const config = ref({ privateMode: false, prohibitGroups: false, prohibitGeneral: false, userGroupLimit: 5 as number | null, httpUrl: '', httpsUrl: '', backupEnabled: false, backupSchedule: '', backupPath: '', backupRetention: 5 })
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
const backups = ref<Array<{ fileName:string; timestamp:string; size:number }>>([])
const backupsLoading = ref(false)
const creatingBackup = ref(false)
const showScheduleHelp = ref(false)
const showCronBuilder = ref(false)
const cronMode = ref<'daily'|'weekly'>('daily')
const cronTime = ref('02:00')
const cronDays = ref<Array<boolean>>([false, false, false, false, false, false, false]) // Sun..Sat
const cronDayNames = ['Sun','Mon','Tue','Wed','Thu','Fri','Sat']
const status = ref({ enabled:false, schedule:'', retention:5, lastRun: null as string | null, nextRun: null as string | null })

function formatUtcLocal(iso: string | null) {
  if (!iso) return '—'
  try {
    const d = new Date(iso)
    return d.toLocaleString()
  } catch { return iso }
}

// Comprehensive Cron Builder state
const cbMode = ref<'preset'|'daily'|'weekly'|'monthly'|'yearly'|'custom'|'hourly'>('daily')
// preset options
const presetType = ref<'everyNmin'|'everyNhours'|'hourlyAt'>('everyNmin')
const presetN = ref<number>(15)
const hourlyMinute = ref<number>(0)

// daily/weekly/monthly/yearly time fields
const dailyTime = ref('02:00')
const weeklyTime = ref('02:00')
// explicit hour/minute fields to avoid locale-dependent <input type=time> behaviour
const dailyHour = ref<number>(2)
const dailyMinuteNum = ref<number>(0)
const weeklyHour = ref<number>(2)
const weeklyMinuteNum = ref<number>(0)
const monthlyHour = ref<number>(2)
const monthlyMinuteNum = ref<number>(0)
const yearlyHour = ref<number>(2)
const yearlyMinuteNum = ref<number>(0)
const weeklyDays = ref<Array<boolean>>([false,false,false,false,false,false,false])

// monthly options
const monthlyOption = ref<'dom'|'nthWeekday'>('dom')
const monthlyDay = ref<number>(1)
const monthlyOrdinal = ref<'first'|'second'|'third'|'fourth'|'last'>('first')
const monthlyWeekday = ref<number>(1) // 0=Sun
const monthlyTime = ref('02:00')

// yearly
const yearlyMonth = ref<number>(1)
const yearlyDay = ref<number>(1)
const yearlyTime = ref('02:00')

// custom
const customCron = ref('')

// validation state for preview
const cronValidation = ref<{ valid: boolean; message?: string } | null>(null)

// UI helpers for select focus and time formatting
const selectFocused = ref<string | null>(null)
function onSelectFocus(name: string) { selectFocused.value = name }
function onSelectBlur() { selectFocused.value = null }

function pad2(n: number) { return n < 10 ? '0' + n : String(n) }
function formatTimeHHmm(val: string) {
  if (!val) return '--:--'
  // Expect format HH:mm already (from <input type=time>), but normalize
  const parts = (val || '').split(':')
  if (parts.length < 2) return val
  const hh = Number(parts[0])
  const mm = Number(parts[1])
  if (isNaN(hh) || isNaN(mm)) return val
  return `${pad2(hh)}:${pad2(mm)}`
}

const previewTimeDisplay = computed(() => {
  try {
    if (cbMode.value === 'daily') return `${pad2(dailyHour.value)}:${pad2(dailyMinuteNum.value)}`
    if (cbMode.value === 'weekly') return `${pad2(weeklyHour.value)}:${pad2(weeklyMinuteNum.value)}`
    if (cbMode.value === 'monthly') return `${pad2(monthlyHour.value)}:${pad2(monthlyMinuteNum.value)}`
    if (cbMode.value === 'yearly') return `${pad2(yearlyHour.value)}:${pad2(yearlyMinuteNum.value)}`
    if (cbMode.value === 'hourly') return `:${pad2(hourlyMinute.value)} (every hour)`
    return `${pad2(dailyHour.value)}:${pad2(dailyMinuteNum.value)}`
  } catch { return '--:--' }
})

// Custom combobox (Mode) state + helpers
const cbModeList = [
  { value: 'preset', label: 'Presets' },
  { value: 'hourly', label: 'Hourly' },
  { value: 'daily', label: 'Daily' },
  { value: 'weekly', label: 'Weekly' },
  { value: 'monthly', label: 'Monthly' },
  { value: 'yearly', label: 'Yearly' },
  { value: 'custom', label: 'Custom (raw cron)' },
]

const cbModeLabels = cbModeList.reduce((acc: Record<string,string>, m) => { acc[m.value] = m.label; return acc }, {} as Record<string,string>)
const cbModeOpen = ref(false)
const cbCombo = ref<HTMLElement | null>(null)

function toggleCbMode() { cbModeOpen.value = !cbModeOpen.value }
function setCbMode(v: string) { cbMode.value = v as any; cbModeOpen.value = false }

function onCbKeyDown(e: KeyboardEvent) {
  if (e.key === 'ArrowDown' || e.key === 'Enter') {
    cbModeOpen.value = true
    // focus the first item in the dropdown if present
    setTimeout(() => {
      const el = cbCombo.value?.querySelector('.combo-dropdown .combo-item') as HTMLElement | null
      el?.focus()
    }, 0)
  }
  if (e.key === 'Escape') cbModeOpen.value = false
}

function onDocClickForCb(e: MouseEvent) {
  if (!cbCombo.value) return
  const target = e.target as Node
  if (!cbCombo.value.contains(target)) cbModeOpen.value = false
}

onMounted(() => { document.addEventListener('click', onDocClickForCb) })
onUnmounted(() => { document.removeEventListener('click', onDocClickForCb) })

const previewCron = computed(() => {
  try {
    if (cbMode.value === 'preset') {
      if (presetType.value === 'everyNmin') return `*/${Math.max(1, Math.floor(presetN.value))} * * * *`
      if (presetType.value === 'everyNhours') return `0 */${Math.max(1, Math.floor(presetN.value))} * * *`
      // hourlyAt
      return `${Math.max(0, Math.floor(hourlyMinute.value))} * * * *`
    }
    if (cbMode.value === 'hourly') return `${Math.max(0, Math.floor(hourlyMinute.value))} * * * *`
    if (cbMode.value === 'daily') {
      const hh = Number(dailyHour.value)
      const mm = Number(dailyMinuteNum.value)
      return `${mm} ${hh} * * *`
    }
    if (cbMode.value === 'weekly') {
      const hh = Number(weeklyHour.value)
      const mm = Number(weeklyMinuteNum.value)
      // Use day names (Sun,Mon,...) in the cron DOW field so the expression
      // is clearer and compatible with common cron parsers that accept names.
      const selected = weeklyDays.value.map((v, i) => v ? cronDayNames[i] : null).filter(Boolean)
      const dow = selected.length ? selected.join(',') : '*'
      return `${mm} ${hh} * * ${dow}`
    }
    if (cbMode.value === 'monthly') {
      const hh = Number(monthlyHour.value)
      const mm = Number(monthlyMinuteNum.value)
      if (monthlyOption.value === 'dom') {
        const d = Math.max(1, Math.min(31, monthlyDay.value))
        return `${mm} ${hh} ${d} * *`
      }
      // nth weekday -> translate to cron 'day-of-month' is not expressive; use nearest day pattern with L/ or complex form — fallback to custom pattern using ? not supported
      // We'll emit a cron that runs every day at time and rely on server-side custom if user needs exact nth-weekday.
      return `${Number(mm)} ${Number(hh)} * * *`
    }
    if (cbMode.value === 'yearly') {
      const hh = Number(yearlyHour.value)
      const mm = Number(yearlyMinuteNum.value)
      const mon = Math.max(1, Math.min(12, yearlyMonth.value))
      const day = Math.max(1, Math.min(31, yearlyDay.value))
      return `${mm} ${hh} ${day} ${mon} *`
    }
    // custom
    if (cbMode.value === 'custom') return customCron.value.trim()
    return ''
  } catch {
    return ''
  }
})



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
  if (isAdmin.value) await fetchBackupStatus()
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
    config.value.userGroupLimit = typeof data.userGroupLimit === 'number' ? data.userGroupLimit : 5
    config.value.backupEnabled = !!data.backupEnabled
    config.value.backupSchedule = data.backupSchedule || ''
    config.value.backupPath = data.backupPath || ''
    config.value.backupRetention = typeof data.backupRetention === 'number' ? data.backupRetention : 5
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
      await loadBackups()
    }
  }
}

async function loadBackups() {
  backupsLoading.value = true
  try {
    const res = await fetch('/api/admin/backups', { credentials: 'include' })
    if (!res.ok) { backups.value = []; return }
    backups.value = await res.json()
  } catch (e) { backups.value = []; console.warn(e) }
  finally { backupsLoading.value = false }
}

async function fetchBackupStatus() {
  try {
    const res = await fetch('/api/admin/backups/status', { credentials: 'include' })
    if (!res.ok) { return }
    const data = await res.json()
    status.value.enabled = !!data.enabled
    status.value.schedule = data.schedule || ''
    status.value.retention = typeof data.retention === 'number' ? data.retention : 5
    status.value.lastRun = data.lastRun || null
    status.value.nextRun = data.nextRun || null
  } catch (e) { console.warn(e) }
}

// refresh backup status periodically while on the page
let statusInterval: number | undefined
watch(isAdmin, (v) => {
  if (v) {
    // refresh every 30 seconds
    statusInterval = window.setInterval(fetchBackupStatus, 30000)
  } else {
    if (statusInterval) { clearInterval(statusInterval); statusInterval = undefined }
  }
})

async function createBackupNow() {
  if (!confirm('Create backup now?')) return
  creatingBackup.value = true
  try {
    const res = await fetch('/api/admin/backups/create', { method: 'POST', credentials: 'include' })
    if (!res.ok) { const txt = await res.text().catch(() => ''); alert(`Create backup failed: ${res.status} ${txt}`); return }
    await loadBackups()
    // refresh status so UI shows updated lastRun/nextRun immediately
    await fetchBackupStatus()
    alert('Backup created')
  } catch (e) { console.warn(e); alert('Network error creating backup') }
  finally { creatingBackup.value = false }
}

function downloadBackup(fileName: string) {
  // Open the download endpoint in a new tab to trigger download
  const url = `/api/admin/backups/download/${encodeURIComponent(fileName)}`
  window.open(url, '_blank')
}

async function restoreBackup(fileName: string) {
  if (!confirm('Restore this backup? This will overwrite the current database file.')) return
  try {
    const res = await fetch(`/api/admin/backups/restore/${encodeURIComponent(fileName)}`, { method: 'POST', credentials: 'include' })
    if (!res.ok) { const txt = await res.text().catch(() => ''); alert(`Restore failed: ${res.status} ${txt}`); return }
    alert('Restore completed (server may need restart).')
  } catch (e) { console.warn(e); alert('Network error restoring backup') }
}

async function deleteBackup(fileName: string) {
  if (!confirm('Delete this backup?')) return
  try {
    const res = await fetch(`/api/admin/backups/${encodeURIComponent(fileName)}`, { method: 'DELETE', credentials: 'include' })
    if (!res.ok) { const txt = await res.text().catch(() => ''); alert(`Delete failed: ${res.status} ${txt}`); return }
    await loadBackups()
  } catch (e) { console.warn(e); alert('Network error deleting backup') }
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
    // validate schedule with server-side validator (if provided)
    if (config.value.backupSchedule && config.value.backupSchedule.trim() !== '') {
      try {
        const vres = await fetch('/api/admin/validate-schedule', { method: 'POST', credentials: 'include', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ schedule: config.value.backupSchedule }) })
        if (vres.ok) {
          const v = await vres.json()
          if (!v.valid) { saveMessage.value = `Schedule invalid: ${v.message}`; return }
        } else {
          const t = await vres.text().catch(() => ''); saveMessage.value = `Schedule validation failed: ${vres.status} ${t}`; return
        }
      } catch (e) { saveMessage.value = 'Schedule validation request failed'; console.warn(e); return }
    }
    const payload = {
      ProhibitGroups: config.value.prohibitGroups,
      PrivateMode: config.value.privateMode,
      ProhibitGeneral: config.value.prohibitGeneral,
      UserGroupLimit: config.value.userGroupLimit,
      HttpUrl: config.value.httpUrl,
      HttpsUrl: config.value.httpsUrl,
      BackupEnabled: config.value.backupEnabled,
      BackupSchedule: config.value.backupSchedule,
      BackupPath: config.value.backupPath,
      BackupRetention: config.value.backupRetention,
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
    // Refresh backups status in case enabling/disabling backups changed
    try { await fetchBackupStatus() } catch {}
    setTimeout(() => (saveMessage.value = ''), 4000)
  }
}

// Modal helpers for schedule help
function closeScheduleHelp() {
  showScheduleHelp.value = false
}

function copyExample(text: string) {
  if (!text) return
  if (navigator.clipboard && navigator.clipboard.writeText) {
    navigator.clipboard.writeText(text).then(() => alert('Copied to clipboard'))
  } else {
    const el = document.createElement('textarea'); el.value = text; document.body.appendChild(el); el.select(); document.execCommand('copy'); document.body.removeChild(el);
    alert('Copied to clipboard')
  }
}

function openCronBuilder() {
  // Initialize builder from current schedule if possible
  const s = (config.value.backupSchedule || '').toString().trim()
  // Reset builder state
  cbMode.value = 'daily'
  presetType.value = 'everyNmin'
  presetN.value = 15
  hourlyMinute.value = 0
  dailyTime.value = '02:00'
  dailyHour.value = 2
  dailyMinuteNum.value = 0
  weeklyTime.value = '02:00'
  weeklyHour.value = 2
  weeklyMinuteNum.value = 0
  weeklyDays.value = [false,false,false,false,false,false,false]
  monthlyOption.value = 'dom'
  monthlyDay.value = 1
  monthlyOrdinal.value = 'first'
  monthlyWeekday.value = 1
  monthlyTime.value = '02:00'
  monthlyHour.value = 2
  monthlyMinuteNum.value = 0
  yearlyMonth.value = 1
  yearlyDay.value = 1
  yearlyTime.value = '02:00'
  yearlyHour.value = 2
  yearlyMinuteNum.value = 0
  customCron.value = ''

  const sRaw = (config.value.backupSchedule || '').toString().trim()
  if (!sRaw) return
  // If it looks like a 5-field cron, initialize custom mode with the expression
  const parts = sRaw.split(/\s+/)
  if (parts.length >= 5) {
    cbMode.value = 'custom'
    customCron.value = sRaw
    return
  }
  // otherwise place it in custom as-is
  cbMode.value = 'custom'
  customCron.value = sRaw
}

async function validateCronExpression(expr: string) {
  cronValidation.value = null
  if (!expr || expr.trim() === '') { cronValidation.value = { valid: false, message: 'Empty expression' }; return false }
  try {
    const res = await fetch('/api/admin/validate-schedule', { method: 'POST', credentials: 'include', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ schedule: expr }) })
    if (!res.ok) {
      cronValidation.value = { valid: false, message: `Validation endpoint error: ${res.status}` }
      return false
    }
    const j = await res.json()
    cronValidation.value = { valid: !!j.valid, message: j.message }
    return !!j.valid
  } catch (e) {
    cronValidation.value = { valid: false, message: 'Network error validating expression' }
    return false
  }
}

async function applyCronBuilder() {
  const expr = previewCron.value || ''
  if (!expr) { alert('Cannot build cron expression from current inputs'); return }
  const ok = await validateCronExpression(expr)
  if (!ok) {
    if (!confirm(`Expression appears invalid: ${cronValidation.value?.message || 'unknown'}. Apply anyway?`)) return
  }
  config.value.backupSchedule = expr
  showCronBuilder.value = false
}

function onKeyDownForModal(e: KeyboardEvent) {
  if (e.key === 'Escape') closeScheduleHelp()
}

watch(showScheduleHelp, (v) => {
  if (v) window.addEventListener('keydown', onKeyDownForModal)
  else window.removeEventListener('keydown', onKeyDownForModal)
})

onUnmounted(() => { window.removeEventListener('keydown', onKeyDownForModal) })
</script>
<!-- removed duplicate modal (moved into main template) -->
<style scoped>
.info-icon {
  background: transparent;
  border: 1px solid var(--bg-chat-sidebar-1);
  color: var(--text-purple-70);
  width: 28px; height: 28px; border-radius: 50%; display:inline-flex; align-items:center; justify-content:center; cursor:pointer;
}
.help-popup {
  margin-top:8px; padding:10px; background:var(--bg-chat-dark-1); border:1px solid var(--bg-chat-sidebar-1); border-radius:6px; color:var(--text-white); max-width:520px; font-size:13px;
}

/* Modal styles for schedule help */
.modal-backdrop {
  position:fixed; inset:0; background:rgba(3,6,12,0.75); display:flex; align-items:center; justify-content:center; z-index:9999;
}
.modal-panel {
  width:calc(100% - 48px); max-width:980px; max-height:90vh; overflow:auto; background:var(--bg-chat-sidebar-1); border-radius:10px; padding:18px; box-shadow:0 10px 30px rgba(0,0,0,0.6); color:var(--text-white);
}
.modal-header { display:flex; align-items:center; justify-content:space-between; gap:12px; margin-bottom:12px }
.modal-header h3 { margin:0; font-size:18px }
.modal-close { background:transparent; border:none; color:var(--text-white); font-size:18px; cursor:pointer }
.modal-content h4 { margin-top:12px; margin-bottom:8px }
.examples { display:flex; flex-direction:column; gap:8px }
.example-row { display:flex; gap:8px; align-items:center }
.example-desc { color:var(--text-purple-70); font-size:13px; margin-left:8px }
.copy-btn { margin-left:8px; padding:6px 8px; border-radius:6px; background:var(--bg-chat-dark-2); color:var(--text-white); border:1px solid var(--bg-chat-sidebar-1); cursor:pointer }
</style>
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

.text-select {
  -webkit-appearance: none;
  -moz-appearance: none;
  appearance: none;
  background-image: linear-gradient(45deg, transparent 50%, var(--text-white) 50%), linear-gradient(135deg, var(--text-white) 50%, transparent 50%);
  background-position: calc(100% - 18px) calc(1em + 2px), calc(100% - 13px) calc(1em + 2px);
  background-size: 6px 6px, 6px 6px;
  background-repeat: no-repeat;
  padding-right: 36px;
  cursor: pointer;
}

.select-focused {
  box-shadow: 0 0 0 3px rgba(100, 120, 255, 0.15);
  border-color: var(--brand-blue-primary);
}

/* Custom combobox styles */
.combo { position: relative; display: inline-block; }
.combo-button {
  width: 100%;
  text-align: left;
  padding: 10px 12px;
  border-radius: 6px;
  background: var(--bg-chat-dark-2);
  border: 1px solid var(--bg-chat-sidebar-1);
  color: var(--text-white);
  cursor: pointer;
  display:flex;align-items:center;justify-content:space-between;
}
.combo-button:focus { outline: none; box-shadow: 0 0 0 3px rgba(100,120,255,0.15); border-color:var(--brand-blue-primary) }
.combo-caret { margin-left:8px; color:var(--text-purple-70) }
.combo-dropdown {
  position: absolute;
  left: 0; right: 0;
  margin-top:6px;
  max-height:220px; overflow:auto;
  background:var(--bg-chat-sidebar-1);
  border:1px solid var(--bg-chat-sidebar-1);
  border-radius:8px;
  box-shadow:0 8px 20px rgba(0,0,0,0.6);
  z-index: 1200;
  padding:6px 6px;
}
.combo-item { padding:8px 10px; border-radius:6px; cursor:pointer; color:var(--text-white); }
.combo-item:hover, .combo-item:focus { background:var(--bg-chat-dark-2); outline:none }

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