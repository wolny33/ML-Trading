import './App.css';
import Home from './Home';
import { Route, Routes, NavLink, BrowserRouter } from 'react-router-dom';
import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import en from 'date-fns/locale/en-GB';

const HOME_URL = '/home';
const LOGIN_URL = '/login';

function App() {
  const navigate = useNavigate();

  useEffect(() => {
      if (isLoggedIn()) {
        navigate(HOME_URL);
      }
  }, []);

  const isLoggedIn = () => {
    return true;
  };

  const logout = async () => {
    navigate(LOGIN_URL);
  };

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns} adapterLocale={en}>
      <div>
        <nav className="navbar fixed-top navbar-expand-sm m-0 bg-transprent">
          <ul className="navbar-nav mx-2">
            {isLoggedIn() ? (
              <li className="nav-item m-0 mr-auto">
                <button className="btn btn-outline-dark m-1" onClick={logout} style={{ verticalAlign: 'middle' }}>
                  Logout
                </button>
              </li>
            ) : (
              <li className="nav-item m-1">
                <NavLink className="btn btn-outline-light" to="/login">
                  Login
                </NavLink>
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
        </Routes>
      </div>
      </LocalizationProvider>
  );
}

export default App;
