<style></style>
<template>
    <div>
        <el-form :inline="true" :model="searchForm" @submit.native.prevent="fetchData(1)">
            <el-form-item label="执行组">
                <el-input v-model="searchForm.groupName" placeholder="分组名" clearable></el-input>
            </el-form-item>
            <el-form-item>
                <el-button type="primary" native-type="submit">查询</el-button>
            </el-form-item>
        </el-form>

        <el-table :data="rows">
            <el-table-column prop="groupName" label="执行组" align="center"></el-table-column>
            <el-table-column prop="clientId" label="clientId" align="center"></el-table-column>
            <el-table-column prop="startTime" label="连接时间" align="center"></el-table-column>
            <el-table-column prop="handelrs" label="处理器" align="center">
                <template #default="scope">
                    <el-popover placement="right" :width="400" trigger="click">
                        <template #reference>
                            <el-link type="primary">{{ scope.row.handelrs?.length || 0 }}个JobHandler</el-link>
                        </template>
                        <h4>JobHandler名</h4>
                        <div v-if="scope.row.handelrs">
                            <div v-for="item in scope.row.handelrs" :key="item">
                                {{ item }}
                            </div>
                        </div>
                    </el-popover>
                </template>
            </el-table-column>
        </el-table>
        <!-- 分页 -->

        <!-- <el-pagination @size-change="handleSizeChange" @current-change="handleCurrentChange"
            :current-page="searchForm.pageNumber" :page-sizes="[15, 20, 50, 100]" :page-size="searchForm.pageSize"
            layout="total, sizes, prev, pager, next, jumper" :total="count">
        </el-pagination> -->
    </div>
</template>

<script lang="ts">
import { lisExecutors } from "@/api/api";
import { ElMessage, ElMessageBox } from 'element-plus'

export default {
    data() {
        return {
            searchForm: {
                groupName: null,
            },
            rows: [],
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
            lisExecutors(this.searchForm)
                .then((res: any) => {
                    console.log(res);
                    if (res.success) {
                        that.rows = res.data;
                    } else {
                        console.log(this);

                        ElMessage({
                            message: res.message || "ok",
                            type: "error",
                        });
                    }
                })
                .catch((e: any) => {
                    console.error(e);
                });
        }
    }
};
</script>