import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import axios from './API/axios';
import { MemoryRouter } from 'react-router-dom';
import Home from './Home';
import { expect } from '@jest/globals';

jest.mock('./API/axios');

jest.mock('react-chartjs-2', () => ({
    Line: () => null,
}));

jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: jest.fn(),
}));

const userName = 'admin';
const pwd = 'password';

const setupAxiosMocks = () => {
    localStorage.setItem("userName", userName);
    localStorage.setItem("pwd", pwd);
    localStorage.setItem("isLoggedIn", 'true');

    axios.get.mockImplementationOnce(() =>
      Promise.resolve({ data: { "equityValue": 10000 } })
    );

    axios.get.mockImplementationOnce(() =>
      Promise.resolve({ data: { "enabled": true } })
    );

    axios.get.mockImplementationOnce(() =>
      Promise.resolve({ data: { "enabled": true } })
    );

    axios.get.mockImplementationOnce(() =>
      Promise.resolve({ data: { 
        "maxStocksBuyCount": 4,
        "minDaysDecreasing": 2,
        "minDaysIncreasing": 4,
        "topGrowingSymbolsBuyRatio": 0.4 
      }})
    );

    axios.get.mockImplementationOnce(() =>
        Promise.resolve({ data: [
            {
                "return": -1.2660278300515748,
                "time": "2023-11-19T00:00:00+01:00"
            },
            {
                "return": -0.9435926253181691,
                "time": "2023-11-20T00:00:00+01:00"
            },
        ]})
    );

    axios.get.mockImplementationOnce(() =>
        Promise.resolve({ data: [
            {
              "id": "f2eee7d8-4344-43b7-8cb5-e5bb47dbb87d",
              "startedAt": "2023-11-19T012:00:00+01:00",
              "finishedAt": "2023-11-19T12:01:00+01:00",
              "state": "Success",
              "stateDetails": "Finished successfully"
            }]
        })
    );

    axios.get.mockImplementationOnce(() =>
        Promise.resolve({ data: [
            {
              "id": "f2eee7d8-4344-43b7-8cb5-e5bb47dbb88d",
              "status": "Accepted",
              "createdAt": "2023-11-19T00:00:00+01:00",
              "executedAt": "2023-11-19T00:00:00+01:00",
              "price": 95.02,
              "averageFillPrice": 95.00,
              "quantity": 0.89,
              "symbol": "AMZN",
              "inForce": "Day",
              "orderType": "LimitBuy"
            }]
        })
    );
};

  test('renders correctly', async () => {
    setupAxiosMocks();
    render(
      <MemoryRouter>
        <Home />
      </MemoryRouter>
    );
    await waitFor(() => {
        expect(axios.get).toHaveBeenCalledTimes(7);
    });
    expect(axios.get).toHaveBeenCalledWith('/assets', expect.anything());
    expect(axios.get).toHaveBeenCalledWith('/test-mode', expect.anything());
    expect(axios.get).toHaveBeenCalledWith('/investment', expect.anything());
    expect(axios.get).toHaveBeenCalledWith('/strategy', expect.anything());
    expect(axios.get).toHaveBeenCalledWith('/performance', expect.anything());
    expect(axios.get).toHaveBeenCalledWith('/trading-tasks', expect.anything());
    expect(axios.get).toHaveBeenCalledWith('/trading-tasks/f2eee7d8-4344-43b7-8cb5-e5bb47dbb87d/trading-actions', expect.anything());

    const heading = await waitFor(() => screen.getByText('Returns chart'));
    expect(heading).toBeInTheDocument();
  });

  test('handles switching test mode', async () => {
    setupAxiosMocks();
    axios.put.mockImplementationOnce(() =>
      Promise.resolve({ data: { "enabled": false } })
    );
    const confirmSpy = jest.spyOn(window, 'confirm');
    confirmSpy.mockReturnValue(true);
    render(
      <MemoryRouter>
        <Home />
      </MemoryRouter>
    );
    await act(async () => {
        expect(axios.get).toHaveBeenCalledTimes(6);
    });
    const switchTestModeButton = screen.getByTestId('test-mode-on-button');
    
    await act(async () => {
        fireEvent.click(switchTestModeButton);
    });
    confirmSpy.mockRestore();
    await waitFor(() => {
        expect(axios.put).toHaveBeenCalledWith('/test-mode',
          { 
            "enable": false 
          },
          { 
            auth: { 
                username: userName, 
                password: pwd 
            }
          }
        );
    });
    const updatedSwitchTestModeButton = screen.getByTestId('test-mode-off-button');
    expect(updatedSwitchTestModeButton).toBeInTheDocument();
  });

  test('handles switching investment', async () => {
    setupAxiosMocks();
    axios.put.mockImplementationOnce(() =>
      Promise.resolve({ data: { "enabled": false } })
    );
    const confirmSpy = jest.spyOn(window, 'confirm');
    confirmSpy.mockReturnValue(true);
    render(
      <MemoryRouter>
        <Home />
      </MemoryRouter>
    );
    await act(async () => {
        expect(axios.get).toHaveBeenCalledTimes(6);
    });
    const switchTestModeButton = screen.getByTestId("investment-on-button");
    
    await act(async () => {
        fireEvent.click(switchTestModeButton);
    });
    confirmSpy.mockRestore();
    await waitFor(() => {
        expect(axios.put).toHaveBeenCalledWith('/investment',
          { 
            "enable": false 
            },
            { auth: 
            { 
                username: userName, 
                password: pwd 
            }
            }
        );
    });
    const updatedSwitchTestModeButton = screen.getByTestId('investment-off-button');
    expect(updatedSwitchTestModeButton).toBeInTheDocument();
  });

  test('alert with api call error', async () => {
    localStorage.clear();
    const alertMock = jest.spyOn(window, 'alert').mockImplementation(() => {});
    axios.get.mockRejectedValue({ response: { status: 400 } });
    render(
        <MemoryRouter>
          <Home/>
        </MemoryRouter>
    );
    await waitFor(() => {
        expect(axios.get).toHaveBeenCalled();
        expect(alertMock).toHaveBeenCalled();
    });
    alertMock.mockRestore();
  });
