import { ref, computed } from 'vue'
import { defineStore } from 'pinia'

export const useUserStore = defineStore('user', {
    state: () => {
        return { token: "" }
    },
    // 也可以定义为
    // state: () => ({ count: 0 })
    getters: {
        jwtToken: (state) => state.token || localStorage.getItem("token"),
    },
    actions: {
        setToken(tokenStr: string) {
            this.token = tokenStr;
            localStorage.setItem("token", this.token);
        },
        removeToken() {
            this.token = "";
            localStorage.removeItem("token");
        }
    },
})