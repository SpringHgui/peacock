import { createRouter, createWebHistory } from 'vue-router'
// import HomeView from '../views/HomeView.vue'
import { useUserStore } from "../stores/user";

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('../views/Job/JobList.vue')
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('../views/Account/Login.vue'),
      meta: {
        allowAnonymous: true
      }
    },
    {
      path: '/job/joblist',
      name: 'joblist',
      // route level code-splitting
      // this generates a separate chunk (About.[hash].js) for this route
      // which is lazy-loaded when the route is visited.
      component: () => import('../views/Job/JobList.vue')
    },
    {
      path: '/task/tasklist',
      name: 'taskList',
      component: () => import('../views/Task/TaskList.vue')
    },
    {
      path: '/Executor/ExecutorList',
      name: 'executorList',
      component: () => import('../views/Executor/ExecutorList.vue')
    }
  ]
})

router.beforeEach((to, from, next) => {
  var user = useUserStore();
  if (!user.jwtToken && !to.meta.allowAnonymous) {
    next({ name: "login", query: { callback: to.fullPath } });
  }
  return next();
})

export default router
