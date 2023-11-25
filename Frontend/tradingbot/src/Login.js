import { useRef, useState, useEffect } from 'react';
import axios from './API/axios';
import { useNavigate } from 'react-router-dom'
import { BounceLoader } from "react-spinners";

const LOGIN_URL = '/investment';
const HOME_URL = '/home';

const Login = () => {

    const navigate = useNavigate();

    const userNameRef = useRef();
    const errRef = useRef();

    const [userName, setUserName] = useState('');
    const [pwd, setPwd] = useState('');
    const [errMsg, setErrMsg] = useState('');

    const [isLoading, setIsLoading] = useState(false);

    useEffect(() => {
        userNameRef.current.focus();
    }, [])

    useEffect(() => {
        setErrMsg('');
    }, [userName, pwd])

    const handleSubmit = async (e) => {
        e.preventDefault();
        setErrMsg('');
        try{
            setIsLoading(true);
            await axios.get(LOGIN_URL,
                {
                    auth: {
                        username: userName,
                        password: pwd
                    }
                }
            );
            localStorage.setItem("userName", userName);
            localStorage.setItem("pwd", pwd);
            localStorage.setItem("isLoggedIn", 'true');
            setUserName('');
            setPwd('');
            setIsLoading(false);
            navigate(HOME_URL);
        }catch(err){
            if(!err?.response) {
                setErrMsg('No Server Response')
            } else if(err.response?.status === 400) {
                setErrMsg('Login Failed');
            } else if(err.response?.status === 404){
                setErrMsg('Account does not exist');
            } else if(err.response?.status === 401 ){
                setErrMsg('Incorrect login credentials');
            } else {
                setErrMsg('Login Failed');
            }
            setIsLoading(false);
            errRef.current.focus();
        }
    }

    return (
    <div>
        <section className="container-fluid justify-content-center" style={{marginTop:"200px"}}>
            <p ref={errRef} className={errMsg ? "errmsg" : 
            "offscreen"} aria-live="assertive">{errMsg}</p>
            <h1 className='login' style={{ display: 'flex', alignItems: 'center', justifyContent: 'center' }}>Please sign In</h1>
            <form onSubmit={handleSubmit}>
                <label htmlFor="userName" className='login'>Username:</label>
                <input 
                    type = "text"
                    id="userName"
                    ref={userNameRef}
                    autoComplete="off"
                    onChange={(e) => setUserName(e.target.value)}
                    value={userName}
                    required
                />

                <label htmlFor="password" className='login'>Password:</label>
                <input 
                    type = "password"
                    id="password"
                    onChange={(e) => setPwd(e.target.value)}
                    value={pwd}
                    required
                />
                <button className="text-black bg-gray-300 hover:bg-gray-400 text-black py-2 px-3 rounded">Sign In</button>
            </form>
        </section>
        {isLoading && (
            <div className="loading-container">
              <h4>Loging in, please wait...</h4>
              <BounceLoader color="lightpink" />
            </div>
        )}
    </div>
    )
}

export default Login