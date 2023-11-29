import { act, render, screen, fireEvent, waitFor } from '@testing-library/react';
import axios from './API/axios';
import { MemoryRouter } from 'react-router-dom';
import Login from './Login';

jest.mock('./API/axios');

const correctUsername = 'admin';
const correctPassword = 'password';

test('logs in successfully on correct credentials', async () => {
    axios.get.mockResolvedValue({});
    localStorage.clear();
    render(
      <MemoryRouter>
        <Login />
      </MemoryRouter>
    );

    const signInButton = screen.getByTestId('signin-button');
    const userNameInput = screen.getByTestId('username-input');
    const passwordInput = screen.getByTestId('password-input');

    fireEvent.change(userNameInput, { target: { value: correctUsername } });
    fireEvent.change(passwordInput, { target: { value: correctPassword } });

    await act(async () => {
      fireEvent.click(signInButton);
    });

    expect(axios.get).toHaveBeenCalledWith('/investment', {
      auth: {
        username: correctUsername,
        password: correctPassword,
      },
    });

    expect(localStorage.getItem('userName')).toBe(correctUsername);
    expect(localStorage.getItem('pwd')).toBe(correctPassword);
    expect(localStorage.getItem('isLoggedIn')).toBe('true');

  });
  
  
  test('handles login failure with incorrect credentials', async () => {
    axios.get.mockRejectedValue({ response: { status: 401 } });
    localStorage.clear();
    render(
      <MemoryRouter>
        <Login />
      </MemoryRouter>
    );

    const signInButton = screen.getByTestId('signin-button');
    const userNameInput = screen.getByTestId('username-input');
    const passwordInput = screen.getByTestId('password-input');

    const incorrectUsername = 'incorrectUsername';
    const incorrectPassword = 'incorrectPassword';

    fireEvent.change(userNameInput, { target: { value: incorrectUsername } });
    fireEvent.change(passwordInput, { target: { value: incorrectPassword } });

    await act(async () => {
      fireEvent.click(signInButton);
    });

    expect(axios.get).toHaveBeenCalledWith('/investment', {
      auth: {
        username: incorrectUsername,
        password: incorrectPassword,
      },
    });

    const errorText = await screen.findByText('Incorrect login credentials');
    expect(errorText).toBeInTheDocument();

    expect(localStorage.getItem('userName')).toBeNull();
    expect(localStorage.getItem('pwd')).toBeNull();
    expect(localStorage.getItem('isLoggedIn')).toBeNull();
  });

  test('handles no response from API', async () => {
    axios.get.mockImplementationOnce(() => Promise.reject());
    localStorage.clear();
    render(
      <MemoryRouter>
        <Login />
      </MemoryRouter>
    );
  
    const signInButton = screen.getByTestId('signin-button');
    const userNameInput = screen.getByTestId('username-input');
    const passwordInput = screen.getByTestId('password-input');
  
    fireEvent.change(userNameInput, { target: { value: correctUsername } });
    fireEvent.change(passwordInput, { target: { value: correctPassword } });
  
    await act(async () => {
      fireEvent.click(signInButton);
    });

    expect(axios.get).toHaveBeenCalledWith('/investment', {
        auth: {
          username: correctUsername,
          password: correctPassword,
        },
    });
  
    const errorText = await screen.findByText('No Server Response');
    expect(errorText).toBeInTheDocument();
  
    expect(localStorage.getItem('userName')).toBeNull();
    expect(localStorage.getItem('pwd')).toBeNull();
    expect(localStorage.getItem('isLoggedIn')).toBeNull();
  });

  test('handles different response status from API', async () => {
    axios.get.mockRejectedValue({ response: { status: 500 } });
    const alertMock = jest.spyOn(window, 'alert').mockImplementation(() => {});
    localStorage.clear();
    render(
      <MemoryRouter>
        <Login />
      </MemoryRouter>
    );
  
    const signInButton = screen.getByTestId('signin-button');
    const userNameInput = screen.getByTestId('username-input');
    const passwordInput = screen.getByTestId('password-input');
  
    fireEvent.change(userNameInput, { target: { value: correctUsername } });
    fireEvent.change(passwordInput, { target: { value: correctPassword } });
  
    await act(async () => {
      fireEvent.click(signInButton);
    });

    expect(axios.get).toHaveBeenCalledWith('/investment', {
        auth: {
          username: correctUsername,
          password: correctPassword,
        },
    });
  
    const errorText = await screen.findByText('Login Failed');
    expect(errorText).toBeInTheDocument();

    expect(alertMock).toHaveBeenCalled();
    alertMock.mockRestore();
  
    expect(localStorage.getItem('userName')).toBeNull();
    expect(localStorage.getItem('pwd')).toBeNull();
    expect(localStorage.getItem('isLoggedIn')).toBeNull();
  });
