import axios from "axios";
import {getToken, removeToken} from "./auth.js";

const baseURL = "https://localhost:7007/api";

const authLogin = async (username, password) => {
    const response = await axios.post(`${baseURL}/auth/login`, { username, password })

    const ok = response.status
    const token = response.data.token

    return { ok, token }
}

const authRegister = async (username, password) => {
    const response = await axios.post(`${baseURL}/auth/register`, { username, password })
    const ok = response.status

    return ok
}

// ------------------------------------------------------------------------------------------

const createAuthAxios = (onUnauthorized) => {
    const token = getToken();

    const instance = axios.create({
        baseURL,
        headers: {
            Authorization: `Bearer ${token}`
        },
        responseType: "arraybuffer"
    });

    instance.interceptors.response.use(
        (response) => response,
        (error) => {
            if (error?.response?.status === 401) {
                removeToken()
                onUnauthorized?.();
            }
            return Promise.reject(error);
        }
    );

    return instance;
}

const Download = async (path, authAxios) => {
    return await authAxios.get(`/file/download?path=${encodeURIComponent(path)}`);
}

const List = async (path, authAxios) => {
    return await authAxios.get(`/directory/list?path=${encodeURIComponent(path)}`, { responseType: "json" });
}

const Create = async (path, authAxios) => {
    return await authAxios.post(`/directory/create?path=${encodeURIComponent(path)}`, null, { responseType: "text" });
}

const Upload = async (path, data, authAxios) => {
    return await authAxios.post(`/file/upload?path=${encodeURIComponent(path)}`, data, { responseType: "text" });
}

const Delete = async (nodeType, path, authAxios) => {
    await authAxios.delete(`/${nodeType}/delete?path=${encodeURIComponent(path)}`, { responseType: "text" });
}

const Move_Copy = async (nodeType, actionMode, oldPath, newPath, authAxios) => {
    await authAxios.post(
        `/${nodeType}/${actionMode}?oldPath=${encodeURIComponent(oldPath)}&newPath=${encodeURIComponent(newPath)}`,
        null,
        { responseType: "text" }
    );
}

const Rename = async (nodeType, oldPath, newPath, authAxios) => {
    await authAxios.post(
       `/${nodeType}/rename?oldPath=${encodeURIComponent(oldPath)}&newPath=${encodeURIComponent(newPath)}`,
    );
}

const api = {
    createAuthAxios,
    authLogin,
    authRegister,
    List,
    Create,
    Delete,
    Upload,
    Download,
    Move_Copy,
    Rename
}


export default api;