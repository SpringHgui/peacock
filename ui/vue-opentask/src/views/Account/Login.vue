
<template>
    <div>
        <h1>登录</h1>
        <el-form ref="formRef" :model="dynamicValidateForm" label-width="120px" class="demo-dynamic" @submit.native.prevent="submitForm()">
            <el-form-item prop="userName" label="用户名" :rules="[
                {
                    required: true,
                    message: 'Please input email address',
                    trigger: 'blur',
                },
            ]">
                <el-input v-model="dynamicValidateForm.userName" />
            </el-form-item>
            <el-form-item label="密码" :rules="{
                required: true,
                message: 'domain can not be null',
                trigger: 'blur',
            }">
                <el-input type="password" v-model="dynamicValidateForm.password" />
            </el-form-item>
            <el-form-item>
                <el-button type="primary" @click="submitForm()" native-type="submit">登录</el-button>
            </el-form-item>
        </el-form>
    </div>
</template>

<script lang="ts">
import type { ResultData } from "@/api/ResultData";
import { login } from "../../api/api";
import { useUserStore } from "../../stores/user";
import { ElMessage, ElMessageBox } from 'element-plus'

export default {
    data() {
        return {
            dynamicValidateForm: {
                userName: "",
                password: ""
            }
        }
    },
    methods: {
        submitForm() {
            console.log("登录");
            login(this.dynamicValidateForm).then(res => {
                if (res.success) {
                    var user = useUserStore();
                    user.setToken(res.data);
                    this.$router.push({ name: "home" });
                } else {
                    ElMessage.error(res.message);
                }
            })
        }
    }
}
</script>


