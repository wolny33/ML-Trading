import './App.css';
import Home from './Home';
import Login from './Login';
import { Route, Routes, NavLink } from 'react-router-dom';
import { useEffect, useState } from 'react';
import axios from './API/axios';
import { useNavigate } from 'react-router-dom';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import en from 'date-fns/locale/en-GB';

const HOME_URL = '/home';
const LOGIN_URL = '/login';

function App() {
  const navigate = useNavigate();

  const [isLoggedIn, setIsLoggedIn] = useState(false);

  useEffect(() => {
    if(localStorage.getItem("isLoggedIn") && localStorage.getItem("pwd")){
      setIsLoggedIn(true);
    }
  });

  useEffect(() => {
    const savedUserName = localStorage.getItem("userName");
    const savedPwd = localStorage.getItem("pwd");
    if(savedUserName && savedPwd){
        axios.get('',
            {
                auth: {
                    username: savedUserName, //'foo'
                    password: savedPwd //'bar'
                }
            }
        ).then(() => {
            setIsLoggedIn(true);
            navigate(HOME_URL);
        }).catch(() => {
            navigate(LOGIN_URL);
        });
    } else {
      navigate(LOGIN_URL);
    }
  }, []);

  const logout = async () => {
    localStorage.clear();
    setIsLoggedIn(false);
    navigate(LOGIN_URL);
  };

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns} adapterLocale={en}>
      <div>
        <nav className="navbar fixed-top navbar-expand-sm m-0 bg-gray-200">
          <ul className="navbar-nav mx-2">
            {isLoggedIn && (
              <li className="nav-item m-0 mr-auto">
                <button className="btn btn-outline-dark m-1" onClick={logout} style={{ verticalAlign: 'middle' }}>
                  Logout
                </button>
              </li>
            )}
          </ul>
          <div className="navbar-text mx-auto">
            <h1 className="text-4xl font-bold text-gray-800 text-center" style={{marginRight: "100px"}}>
                Autonomic Trading Bot
            </h1>
          </div>
        </nav>
        <Routes>
            <Route path="/home" element={<Home />} />
            <Route path='/login' element={<Login/>} />
        </Routes>
      </div>
      </LocalizationProvider>
  );
}

export default App;
