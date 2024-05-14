<script lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import { useUserStore } from "./stores/user";
import { mapState } from 'pinia'
export default {

  computed: {
    ...mapState(useUserStore, ['jwtToken']),
  },
}
</script>

<template>
  <div class="common-layout">
    <el-container>
      <el-header style="border-bottom: 1px solid #e1e1e1;">
        <div style="font-size: 1.3rem;line-height: 60px;">
          分布式任务调度平台
        </div>
      </el-header>
      <el-container>
        <el-aside width="200px" v-if="jwtToken">
          <el-menu router default-active="/">
            <el-menu-item index="/job/joblist">
              <el-icon>
                <Timer />
              </el-icon>
              <span>任务管理</span>
            </el-menu-item>
            <el-menu-item index="/task/tasklist">
              <el-icon>
                <Clock />
              </el-icon>
              <span>调度列表</span>
            </el-menu-item>
            <el-menu-item index="/Executor/ExecutorList">
              <el-icon>
                <Operation />
              </el-icon>
              <span>执行器列表</span>
            </el-menu-item>
            <el-menu-item index="3">
              <el-icon>
                <Tickets />
              </el-icon>
              <span>日志查询</span>
            </el-menu-item>
            <el-menu-item index="4">
              <el-icon>
                <Bell />
              </el-icon>
              <span>报警设置</span>
            </el-menu-item>
            <!-- <el-menu-item index="3" disabled>
                                          <el-icon>
                                            <document />
                                                  </el-icon>
                                                  <span>Navigator Three</span>
                                                </el-menu-item>
                                                <el-menu-item index="4">
                                                                    <el-icon>
                                                                      <setting />
                                                                    </el-icon>
                                                                    <span>Navigator Four</span>
                                                                  </el-menu-item> -->
          </el-menu>
        </el-aside>
        <el-container>
          <el-main>
            <RouterView />
          </el-main>
          <el-footer>
            <div class="copyright">
              保信科技 Copyright © 2017 - 2023 All Rights Reserved.
            </div>
          </el-footer>
        </el-container>
      </el-container>
    </el-container>
  </div>
</template>

<style>
.copyright {
  text-align: center;
  font-size: .8rem;
  color: gray;
}

.el-pagination {
  float: right;
  padding: 10px 0;
}

.el-menu {
  border-right: none;
}
 
</style>
