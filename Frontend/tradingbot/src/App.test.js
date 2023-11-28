import { render, screen, fireEvent } from '@testing-library/react';
import App from './App';
import { MemoryRouter } from 'react-router-dom';

test('renders navbar with correct title', () => {
  render(
    <MemoryRouter>
      <App />
    </MemoryRouter>
  );

  const titleElement = screen.getByText('Autonomic Trading Bot');
  expect(titleElement).toBeInTheDocument();
});

test('renders Logout button when user is logged in', () => {
  localStorage.setItem("userName", "admin");
  localStorage.setItem("pwd", "password");
  localStorage.setItem("isLoggedIn", 'true');
  render(
    <MemoryRouter>
      <App />
    </MemoryRouter>
  );

  const logoutButton = screen.getByText('Logout');
  expect(logoutButton).toBeInTheDocument();
});

test('does not render Logout button when user is not logged in', () => {
  localStorage.removeItem("userName");
  localStorage.removeItem("pwd");
  localStorage.removeItem("isLoggedIn");
  render(
    <MemoryRouter>
      <App />
    </MemoryRouter>
  );

  const logoutButton = screen.queryByText('Logout');
  expect(logoutButton).toBeNull();
});


test('logs out user when Logout button is clicked', () => {
  localStorage.setItem("userName", "admin");
  localStorage.setItem("pwd", "password");
  localStorage.setItem("isLoggedIn", 'true');
  render(
    <MemoryRouter>
      <App />
    </MemoryRouter>
  );

  const logoutButton = screen.getByText('Logout');
  fireEvent.click(logoutButton);

  const loginText = screen.getByText('Please Sign In');
  expect(loginText).toBeInTheDocument();
});
