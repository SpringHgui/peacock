<style></style>
<template>
    <div>
        <el-form :inline="true" :model="searchForm" @submit.native.prevent="fetchData(1)">
            <el-form-item label="任务ID:">
                <el-input v-model="searchForm.jobId" placeholder="任务ID" clearable></el-input>
            </el-form-item>
            <el-form-item>
                <el-button type="primary" native-type="submit">查询</el-button>
            </el-form-item>
        </el-form>

        <el-table :data="rows">
            <el-table-column prop="taskId" label="调度ID" align="center"></el-table-column>
            <el-table-column prop="jobId" label="任务ID" align="center"></el-table-column>
            <el-table-column prop="startTime" label="调度时间" align="center"></el-table-column>
            <el-table-column prop="endTime" label="完成时间" align="center"></el-table-column>
            <el-table-column prop="result" label="描述" align="center"></el-table-column> 
            <el-table-column prop="clientId" label="工作节点" align="center"></el-table-column>
            <el-table-column prop="status" label="结果" align="center">
                <template #default="scope">
                    <el-icon v-show="scope.row.status == 2" style="color: green;">
                        <CircleCheck />
                    </el-icon>
                    <el-icon v-show="scope.row.status == 3 || scope.row.status == 0" style="color: red;">
                        <CircleClose />
                    </el-icon>
                    <el-icon v-show="scope.row.status == 1" style="color: #c17d10;">
                        <VideoPause />
                    </el-icon>
                </template>
            </el-table-column>
        </el-table>
        <!-- 分页 -->

        <el-pagination @size-change="handleSizeChange" @current-change="handleCurrentChange"
            :current-page="searchForm.pageNumber" :page-sizes="[15, 20, 50, 100]" :page-size="searchForm.pageSize"
            layout="total, sizes, prev, pager, next, jumper" :total="count">
        </el-pagination>
    </div>
</template>

<script lang="ts">
import { lisTasks } from "@/api/api";

export default {
    data() {
        return {
            searchForm: {
                jobId: null,
                pageNumber: 1,
                pageSize: 15,
            },
            rows: [],
            count: 0
        };
    },
    created() {
        console.log("created");
        this.fetchData(1);
    },
    methods: {

        // 获取数据
        fetchData(pageNumber: any) {
            var that = this;
            this.searchForm.pageNumber = pageNumber;

            lisTasks(this.searchForm)
                .then((res: any) => {
                    console.log(res);
                    if (res.success) {
                        that.rows = res.data.rows;
                        that.count = res.data.count;
                    } else {
                        console.log(this);

                        // that.$message({
                        //   message: res.message || "ok",
                        //   type: "error",
                        // });
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