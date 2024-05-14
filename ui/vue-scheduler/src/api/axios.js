import axios from "axios";
import { ElLoading } from 'element-plus'
import { useUserStore } from "../stores/user";

var loading;
const instance = axios.create({
  headers: { 'content-type': 'application/json' },
  baseURL:
    import.meta.env.MODE === "development" ?
      "https://localhost:7148/"
      // "https://jobadmin-zsc.bx.com.cn/"
      : null,
  timeout: 20000
});

// http request 拦截器
instance.interceptors.request.use(
  config => {
    // console.log("interceptors", config);
    if (config.showLoding === undefined || config.showLoding) {
      loading = ElLoading.service({
        lock: true,
        text: config.toast || "加载中...",
        background: "rgba(0,0,0,0.2)"
      });
    }

    var user = useUserStore();
    config.headers.Authorization = user.jwtToken;
    return config;
  },
  err => {
    console.log("request err:", err);
    return Promise.reject(err);
  }
);

instance.interceptors.response.use(
  response => {
    if (loading) {
      loading.close();
    }
    if (response.status === 200) {
      if (!response.data.success && response.data.message == "invalid_token") {
        var user = useUserStore();
        user.removeToken();
      }

      return Promise.resolve(response.data);
    } else {
      return Promise.reject(response);
    }
  },
  error => {
    if (loading) {
      loading.close();
    }
    console.dir(error);
    return Promise.reject(error.message);
  }
);

export default instance;
