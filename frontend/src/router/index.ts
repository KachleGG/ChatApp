import { createRouter, createWebHistory } from 'vue-router'
// Load api lazily to avoid startup failures if the module throws during import
let api: typeof import('../api') | null = null

async function getApi() {
  if (!api) {
    try {
      api = await import('../api')
    } catch (e) {
      console.error('Failed to load api module:', e)
      api = null
    }
  }
  return api
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'Home',
      component: () => import('../views/HomeView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/login',
      name: 'Login',
      component: () => import('../views/LoginView.vue'),
      meta: { requiresAuth: false }
    },
    {
      path: '/register',
      name: 'Register',
      component: () => import('../views/RegisterView.vue'),
      meta: { requiresAuth: false }
    },
    {
      path: '/profile',
      name: 'Profile',
      component: () => import('../views/ProfileView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/admin',
      name: 'Admin',
      component: () => import('../views/AdminView.vue'),
      meta: { requiresAuth: true, requiresAdmin: true }
    }
  ],
})

// Navigation guard to check authentication
// Always check authentication and redirect unauthenticated users immediately
// except when visiting the public `Login` or `Register` routes.
router.beforeEach(async (to, from, next) => {
  const routeName = (to.name || '').toString()
  const publicPages = ['Login', 'Register']

  // Allow navigation to login/register without auth check
  if (publicPages.includes(routeName)) {
    // If user is trying to access the Register page, check server config for private mode
    if (routeName === 'Register') {
      try {
        const cfgRes = await fetch('/api/config')
        if (cfgRes.ok) {
          const cfg = await cfgRes.json()
          if (cfg?.privateMode) {
            // Registration is disabled in private mode - redirect to Login
            next({ name: 'Login' })
            return
          }
        }
      } catch (e) {
        // If config can't be fetched, be conservative and disallow registration
        next({ name: 'Login' })
        return
      }
    }
    next()
    return
  }

  try {
    const _api = await getApi()
    // If api couldn't be loaded, treat as unauthenticated and send to login
    if (!_api) {
      next({ name: 'Login', query: { redirect: to.fullPath } })
      return
    }

    const { authenticated } = await _api.checkAuth()
    if (!authenticated) {
      // Immediately redirect unauthenticated users to login
      next({ name: 'Login', query: { redirect: to.fullPath } })
      return
    }

    // Check if route requires admin and user is admin
    if (to.meta.requiresAdmin) {
      const checkRes = await fetch('/api/auth/check', { credentials: 'include' })
      if (checkRes.ok) {
        const data = await checkRes.json()
        if (!data.user?.isAdmin) {
          // Not admin - redirect to home
          next({ name: 'Home' })
          return
        }
      }
    }

    next()
  } catch (error) {
    console.error('Auth check failed:', error)
    next({ name: 'Login', query: { redirect: to.fullPath } })
  }
})

export default router
