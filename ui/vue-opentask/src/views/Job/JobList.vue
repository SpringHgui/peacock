<style>
.el-dropdown {
  top: 4px;
}
</style>
<template>
  <div>
    <el-form :inline="true" :model="searchForm" @submit.native.prevent="fetchData(1)">
      <el-form-item label="任务名称:">
        <el-input v-model="searchForm.name" placeholder="任务名称" clearable></el-input>
      </el-form-item>
      <el-form-item>
        <el-button type="primary" native-type="submit">查询</el-button>
        <el-button type="primary" @click="newJobVisable = true">创建任务</el-button>
      </el-form-item>
    </el-form>

    <el-table :data="rows" :row-class-name="tableRowClassName">
      <el-table-column prop="jobId" label="任务ID" align="center"></el-table-column>
      <el-table-column prop="name" label="任务名称" align="center"></el-table-column>
      <el-table-column prop="content" label="JobHandler" align="center"></el-table-column>
      <el-table-column prop="description" label="描述" align="center"></el-table-column>
      <el-table-column prop="jobParams" label="参数" align="center"></el-table-column>
      <el-table-column prop="groupName" label="执行组" align="center">
        <template #default="scope">
          <div>{{ scope.row.groupName }}</div>
          <el-popover placement="right" :width="400" trigger="click">
            <template #reference>
              <el-link type="primary">{{ scope.row.onlineExecutors.length }}个在线</el-link>
            </template>

            <el-table :data="scope.row.onlineExecutors">
              <el-table-column property="clientId" label="客户端编号" />
              <el-table-column property="startTime" label="连接时间" />
            </el-table>
          </el-popover>
        </template>
      </el-table-column>
      <el-table-column prop="timeType" label="时间类型" align="center"></el-table-column>
      <el-table-column prop="timeExpression" label="时间表达式" align="center"></el-table-column>
      <el-table-column prop="executeMode" label="执行模式" align="center"></el-table-column>
      <el-table-column prop="enabled" label="启用状态" align="center">
        <template #default="scope">
          <!-- <el-icon v-if="!scope.row.enabled">
            <VideoPlay />
          </el-icon>
          <el-icon v-if="scope.row.enabled">
            <VideoPause />
          </el-icon> -->
          <el-switch :value="scope.row.enabled" inline-prompt :active-icon="Check" :inactive-icon="Close"
            @click="switchJobStatus(scope.row)" />
          {{ scope.row.enabled ? "启用中" : "禁用中" }}
        </template>
      </el-table-column>
      <el-table-column label="操作">
        <template #default="scope">
          <el-button size="small" type="primary" link @click="runOnce(scope.$index, scope.row)">运行一次</el-button>
          <el-divider direction="vertical" />
          <el-dropdown trigger="click">
            <el-button size="small" type="primary" link>更多</el-button>
            <template #dropdown>
              <el-dropdown-menu style="top: 4px;">
                <el-dropdown-item @click="deleteJob(scope.row)">删除任务</el-dropdown-item>
                <el-dropdown-item @click="checkcron(scope.row)">验证cron</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>

        </template>
      </el-table-column>
    </el-table>
    <!-- 分页 -->

    <el-pagination @size-change="handleSizeChange" @current-change="handleCurrentChange"
      :current-page="searchForm.pageNumber" :page-sizes="[15, 20, 50, 100]" :page-size="searchForm.pageSize"
      layout="total, sizes, prev, pager, next, jumper" :total="Count">
    </el-pagination>

    <el-drawer title="创建任务" size="40%" v-model="newJobVisable" direction="rtl">
      <div style="margin: 15px;height: 100%;position: relative;">
        <el-steps :active="step">
          <el-step title="基本配置"></el-step>
          <el-step title="定时配置"></el-step>
          <el-step title="通知配置"></el-step>
        </el-steps>

        <el-form :model="newJob" label-width="100px">
          <div v-if="step == 1">
            <el-form-item label="任务名:">
              <el-input v-model="newJob.name" placeholder="任务名" clearable></el-input>
            </el-form-item>
            <el-form-item label="执行组:">
              <el-autocomplete v-model="newJob.groupName" placeholder="执行组" :fetch-suggestions="querySearchGroup">
                <template #default="{ item }">
                  <div class="value">{{ item.value }}</div>
                </template>
              </el-autocomplete>
            </el-form-item>
            <el-form-item label="Handler:" v-show="newJob.groupName">
              <el-autocomplete v-model="newJob.content" placeholder="JobHandler" :fetch-suggestions="querySearchHandler">
                <template #default="{ item }">
                  <div class="value">{{ item.value }}</div>
                </template>
              </el-autocomplete>
              <!-- <el-input v-model="newJob.content" placeholder="JobHandler的类名" clearable></el-input> -->
            </el-form-item>
            <el-form-item label="任务描述:">
              <el-input type="textarea" v-model="newJob.description" placeholder="描述" clearable></el-input>
            </el-form-item>
            <el-form-item label="执行参数:">
              <el-input type="textarea" v-model="newJob.jobParams" placeholder="执行参数（可选）" clearable></el-input>
            </el-form-item>
            <el-form-item label="执行模式:">
              <el-select v-model="newJob.executeMode" placeholder="请选择执行模式">
                <el-option label="单机执行" value="alone"></el-option>
                <el-option label="广播/分片" value="broadcast"></el-option>
              </el-select>
            </el-form-item>

            <el-form-item label="重试次数">
              <!-- <el-input v-model="newJob.maxAttempt" type="number" placeholder="实例失败重试次数" clearable> <template
                  #append>次</template></el-input> -->

              <el-input-number v-model="newJob.maxAttempt" :min="0" :max="10" />
            </el-form-item>
            <el-form-item label="重试间隔">
              <el-input v-model="newJob.attemptInterval" type="number" placeholder="实例失败重试间隔 单位s" clearable> <template
                  #append>秒</template></el-input>
            </el-form-item>
            <el-form-item label="并发限制">
              <el-input-number v-model="newJob.maxThread" placeholder="最大并发线程" type="number" :min="0"
                :max="10"></el-input-number> 个线程
            </el-form-item>
          </div>
          <div v-else-if="step == 2">
            <el-form-item label="时间类型">
              <el-select v-model="newJob.timeType" placeholder="请选择执行模式">
                <el-option label="cron" value="cron"></el-option>
              </el-select>
            </el-form-item>
            <el-form-item label="cron表达式">
              <el-input v-model="newJob.timeExpression" placeholder="cron表达式" clearable></el-input>
              <el-row>
                <el-col :span="24">
                  <div v-show="showcronTool">
                    <el-input v-model="cron.per" type="number" style="width: 100px;">
                      <template #prepend>每</template>
                    </el-input>
                    <el-select v-model="cron.cycle" class="m-2" style="width: 80px;">
                      <el-option v-for="item in options" :key="item.value" :label="item.label" :value="item.value" />
                    </el-select>
                  </div>
                </el-col>
                <el-col :span="24">
                  <el-button type="primary" v-show="!showcronTool" @click="showcronTool = true">使用生成工具</el-button>
                  <el-button type="primary" v-show="showcronTool" @click="usecron">确定</el-button>
                  <el-button>验证cron</el-button>
                  <el-link type="primary" href="http://cron.ciding.cc/" target="_blank">cron表达式工具</el-link>
                </el-col>
              </el-row>
            </el-form-item>
          </div>
          <div v-else>
            <!-- <el-form-item label="超时时间">
              <el-input v-model="newJob.timeout" type="number" placeholder="超时时间 单位s" clearable></el-input>
            </el-form-item> -->
            <el-form-item label="失败报警">
              <el-radio-group v-model="newJob.alarmType">
                <el-radio :label="0">无</el-radio>
                <el-radio :label="1">企业微信机器人</el-radio>
              </el-radio-group>
            </el-form-item>
            <el-form-item label="webhook" v-show="newJob.alarmType > 0">
              <el-input v-model="newJob.alarmContent" type="text" placeholder="企业微信机器人webhook地址" clearable></el-input>
            </el-form-item>
          </div>
        </el-form>
        <!-- 页脚 -->
        <div style="position: absolute; bottom: 40px; width: 100%;">
          <el-divider></el-divider>
          <el-button v-show="step != 1" @click="step--">上一步</el-button>
          <el-button type="primary" v-show="step != 3" @click="next()">下一步</el-button>
          <el-button v-show="step == 1" @click="newJobVisable = false">取消</el-button>
          <el-button type="primary" v-show="step == 3" @click="createJob">创建任务</el-button>
        </div>
      </div>
    </el-drawer>
    <!-- cron最近5次执行时间展示 -->
    <el-dialog v-model="cronCheckVisible" title="验证cron" width="30%" align-center>
      <h3>最近5次执行时间</h3>
      <div style="padding: 5px 0;" v-for="item in next5cron" :key="item">{{ item }}</div>
      <template #footer>
        <span class="dialog-footer">
          <el-button type="primary" @click="cronCheckVisible = false">
            确定
          </el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script lang="ts">
import { lisExecutors, getJobList, addJob, excuteOnce, getNext5executiontimes, deleteJob, switchEnabledStatus } from "@/api/api";
import { ElMessage, ElMessageBox } from 'element-plus'
import { Check, Close } from '@element-plus/icons-vue'

export default {
  data() {
    return {
      Check: Check,
      Close: Close,
      step: 1,
      searchForm: {
        name: "",
        pageNumber: 1,
        pageSize: 15,
      },
      options: [{
        value: 'Option1',
        label: '分钟',
      },
      {
        value: 'Option2',
        label: '小时',
      },
      {
        value: 'Option3',
        label: '天',
      },
      {
        value: 'Option4',
        label: '周',
      },
      {
        value: 'Option5',
        label: '月',
      },],
      cron: {
        per: "",
        cycle: ""
      },
      showcronTool: false,
      newJob: {
        name: "",
        content: "",
        description: "任务描述",
        groupName: "default",
        timeExpression: "0 0 0 */1 * ?",
        timeType: "cron",
        executeMode: "alone",
        attemptInterval: 60,
        maxAttempt: 1,
        alarmType: 1,
        timeout: 0,
        maxThread: 0,
        jobParams: "",
        alarmContent: "https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key=9137a8d9-0182-48e8-9b72-a70ab97d6043"
      },
      rows: [],
      loading: false,
      Count: 0,
      newJobVisable: false,
      next5cron: [],
      cronCheckVisible: false,
      executors: []
    };
  },
  computed: {

  },
  created() {
    console.log("created");
    this.fetchData(1);
    this.fetchExcuters();
  },
  methods: {
    querySearchGroup(input: string, cb: any) {
      cb(this.executors.map(x => { return { value: x.groupName } }));
    },
    querySearchHandler(input: string, cb: any) {
      var targetGroupExecutors = this.executors.filter(x => { return x.groupName == this.newJob.groupName })

      const set = new Set();
      for (let index = 0; index < targetGroupExecutors.length; index++) {
        const element = targetGroupExecutors[index];
        for (let index = 0; index < element.handelrs.length; index++) {
          const executor = element.handelrs[index];
          set.add(executor);
        }
      }
      var res = [];
      set.forEach(x => {
        res.push({ value: x });
      });
      cb(res);
    },
    fetchExcuters() {
      lisExecutors(this.searchForm)
        .then((res: any) => {
          console.log(res);
          if (res.success) {
            this.executors = res.data;
          } else {
            ElMessage({
              message: res.message || "ok",
              type: "error",
            });
          }
        })
        .catch((e: any) => {
          ElMessage({
            message: e.message || "ok",
            type: "error",
          });
        });
    },
    switchJobStatus(row: any) {
      switchEnabledStatus({ jobId: row.jobId }).then(res => {
        if (res.success) {
          ElMessage({
            message: '操作成功',
            type: 'success',
          })
          this.fetchData(this.searchForm.pageNumber);
        } else {
          ElMessage.error(res.message);
        }
      })
    },
    deleteJob(row: any) {
      ElMessageBox.confirm(
        '删除后不可恢复，你确定要删除该定时任务吗?',
        '警告',
        {
          confirmButtonText: '确定',
          cancelButtonText: '取消',
          type: 'warning',
        }
      )
        .then(() => {
          deleteJob({ jobId: row.jobId }).then(res => {
            if (res.success) {
              ElMessage({
                message: '删除成功',
                type: 'success',
              })
              this.fetchData(this.searchForm.pageNumber);
            } else {
              ElMessage.error(res.message);
            }
          })
        })
        .catch(() => {
          ElMessage({
            type: 'info',
            message: 'Delete canceled',
          })
        })

    },
    checkcron(row: any) {
      getNext5executiontimes({ jobId: row.jobId }).then(res => {
        if (res.success) {
          this.cronCheckVisible = true;
          this.next5cron = res.data;
        } else {
          ElMessage.error(res.message);
        }
      })
    },
    usecron() {
      this.showcronTool = false;
    },
    runOnce(index: number, row: any) {
      excuteOnce({ jobId: row.jobId }).then(res => {
        if (res.success) {
          ElMessage({
            message: '指令已发送',
            type: 'success',
          })
        } else {
          // @ts-ignore
          ElMessage.error(res.message);
        }
      })
    },
    createJob() {
      console.log(this.newJob);
      addJob(this.newJob).then((res: any) => {
        if (res.success) {
          this.newJobVisable = false;
          this.newJob = {
            maxThread: 0,
            name: "",
            content: "",
            description: "任务描述",
            groupName: "default",
            timeExpression: "0 0 0 */1 * ?",
            timeType: "cron",
            executeMode: "alone",
            attemptInterval: 60,
            maxAttempt: 1,
            alarmType: 1,
            timeout: 0,
            jobParams: "",
            alarmContent: "https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key=9137a8d9-0182-48e8-9b72-a70ab97d6043"
          }
          this.fetchData(this.searchForm.pageNumber);
        } else {
          ElMessage.error(res.message);
        }
      }).catch((err: any) => {
        ElMessage.error(err.message);
      })
    },
    next() {
      // TODO:表单验证
      this.step++;
      console.log(this.step);
    },
    tableRowClassName({ row, rowIndex }: any) {
      if (row.isOffline) {
        return "warning-row";
      } else if (rowIndex === 3) {
        return "success-row";
      }
      return "";
    },
    // 获取数据
    fetchData(pageNumber: any) {
      var that = this;

      this.searchForm.pageNumber = pageNumber;

      getJobList(this.searchForm)
        .then((res: any) => {
          if (res.success) {
            that.rows = res.data.rows;
            that.Count = res.data.count;

            for (let index = 0; index < that.rows.length; index++) {
              const element = that.rows[index];
              var targetGroupExecutors = this.executors.filter(x => { return x.groupName == element.groupName })
              element.onlineExecutors = targetGroupExecutors;
            }
          } else {
            ElMessage({
              message: res.message || "ok",
              type: "error",
            });
          }
        })
        .catch((e: any) => {
          console.error(e);
        });
    },
    // 分页-每页条数
    handleSizeChange(val: number) {
      // console.log(`每页 ${val} 条`);
      this.searchForm.pageSize = val;
      this.fetchData(1);
    },
    // 当前页
    handleCurrentChange(val: number) {
      // console.log(`当前页: ${val}`);
      this.searchForm.pageNumber = val;
      this.fetchData(val);
    },
  },
};
</script>