import axios from "./axios";

export function getJobList(data: any) {
    return axios({
        url: "/api/job/getJobList",
        method: "post",
        data
    });
}

export function addJob(data: any) {
    return axios({
        url: "/api/job/AddJob",
        method: "post",
        data: data
    });
}

export function login(data: any) {
    return axios({
        url: "/api/user/GetToken",
        method: "post",
        data: data
    });
}

export function excuteOnce(data: any) {
    return axios({
        url: "/api/job/ExcuteOnce",
        method: "post",
        params: data
    });
}

export function getNext5executiontimes(params: any) {
    return axios({
        url: "/api/job/getNext5executiontimes",
        method: "get",
        params: params
    });
}

export function deleteJob(params: any) {
    return axios({
        url: "/api/job/DeleteJob",
        method: "delete",
        params: params
    });
}

export function switchEnabledStatus(params: any) {
    return axios({
        url: "/api/job/SwitchEnabledStatus",
        method: "post",
        params: params
    });
}

export function lisTasks(params: any) {
    return axios({
        url: "/api/task/LisTasks",
        method: "get",
        params: params
    });
}

export function lisExecutors(params: any) {
    return axios({
        url: "/api/Executor/GetOnlineExecutor",
        method: "get",
        params: params
    });
}