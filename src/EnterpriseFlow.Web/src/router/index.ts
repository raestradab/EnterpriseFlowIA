import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('../views/auth/LoginView.vue'),
      meta: { public: true },
    },
    {
      path: '/register',
      name: 'register',
      component: () => import('../views/auth/RegisterTenantView.vue'),
      meta: { public: true },
    },
    {
      path: '/',
      component: () => import('../layouts/DefaultLayout.vue'),
      children: [
        { path: '', name: 'dashboard', component: () => import('../views/DashboardView.vue') },
        {
          path: 'companies',
          name: 'companies',
          component: () => import('../views/companies/CompaniesListView.vue'),
          meta: { permission: 'companies.read' },
        },
        {
          path: 'clients',
          name: 'clients',
          component: () => import('../views/clients/ClientsListView.vue'),
          meta: { permission: 'clients.read' },
        },
        {
          path: 'projects',
          name: 'projects',
          component: () => import('../views/projects/ProjectsListView.vue'),
          meta: { permission: 'projects.read' },
        },
        {
          path: 'projects/:id',
          name: 'project-detail',
          component: () => import('../views/projects/ProjectDetailView.vue'),
          meta: { permission: 'projects.read' },
        },
        {
          path: 'tasks',
          name: 'tasks',
          component: () => import('../views/tasks/TasksListView.vue'),
          meta: { permission: 'tasks.read' },
        },
        {
          path: 'catalogs',
          name: 'catalogs',
          component: () => import('../views/catalogs/CatalogsListView.vue'),
          meta: { permission: 'catalogs.read' },
        },
        {
          path: 'catalogs/:id',
          name: 'catalog-detail',
          component: () => import('../views/catalogs/CatalogDetailView.vue'),
          meta: { permission: 'catalogs.read' },
        },
        {
          path: 'workflows',
          name: 'workflows',
          component: () => import('../views/workflows/WorkflowsListView.vue'),
          meta: { permission: 'workflows.read' },
        },
        {
          path: 'workflows/:id',
          name: 'workflow-detail',
          component: () => import('../views/workflows/WorkflowDetailView.vue'),
          meta: { permission: 'workflows.read' },
        },
        {
          // No meta.permission — AssistantEndpoints.cs only requires authentication (ADR-0013:
          // the security boundary lives in each tool the assistant can invoke, not the endpoint).
          path: 'assistant',
          name: 'assistant',
          component: () => import('../views/assistant/AssistantView.vue'),
        },
      ],
    },
  ],
})

router.beforeEach((to) => {
  const auth = useAuthStore()

  if (!to.meta.public && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if ((to.name === 'login' || to.name === 'register') && auth.isAuthenticated) {
    return { name: 'dashboard' }
  }

  return true
})

export default router
