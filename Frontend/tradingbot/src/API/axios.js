import axios from "axios";

export default axios.create({
    // baseURL: 'http://localhost:7042'
    baseURL: 'https://httpbin.org/basic-auth/foo/bars'
});