import React from "react";
import { useState, useEffect } from "react";
import { useMemo } from 'react';
import { MaterialReactTable, useMaterialReactTable } from 'material-react-table';
import { IconButton, Divider, MenuItem, Select } from '@mui/material';
import { Add as AddIcon } from '@mui/icons-material';
import axios from './API/axios';
import { useNavigate } from 'react-router-dom';
import { Line } from 'react-chartjs-2';
import { Chart as ChartJS, LinearScale, CategoryScale, PointElement, LineElement, Tooltip, Legend } from 'chart.js';

const TEST_MODE_URL = '/test-mode';
const LOGIN_URL = '/login';
const INVESTMENT_URL = '/investment';
const PERFORMANCE_URL = '/performance';
const TRADING_TASKS_URL = '/trading-tasks';
const STRATEGY_URL = '/strategy';
const STRATEGY_SELECTION_URL = '/strategy/selection';
const STRATEGY_NAMES_URL = '/strategy/selection/names';
const ASSETS_URL = '/assets';
const tradingActionsForTaskUrl = (id) => '/trading-tasks/' + id + '/trading-actions';

const CHART_SCALE_RATIO = 6 / 5;

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Tooltip, Legend);

export const displayErrorAlert = (errorBody, customMessage = '') => {
  const errorMessage = errorBody ?
    `Error data:
    Type: ${errorBody.type}
    Title: ${errorBody.title}
    Status: ${errorBody.status}
    Trace ID: ${errorBody.traceId}
    
    Errors:
    ${errorBody.errors ? 
      `${Object.entries(errorBody.errors)
        .map(([key, value]) => `${key}: ${Array.isArray(value) ? value.join(', ') : value}`)
        .join('\n')}`
      : 'Unknown error occurred'}
    ${errorBody.Message ? 
      `Message:
      ${errorBody.Message}` : ''}
  ` : '';
  window.alert(customMessage + '\n' + errorMessage);
};

export const errorStatusString = (url, status, statusText) => {
  return ('Url: ' + axios.getUri() + url + '\nError status: ' + status + ' ' + statusText);
}

const TradingTasksTable = ({ tradingTasks, onRowClicked }) => {
  const columns = useMemo(() => [
    {
      accessorKey: 'id',
      header: 'Id'
    },
    {
      accessorKey: 'startedAt',
      header: 'Started at',
      accessorFn: (originalRow) => new Date(originalRow.startedAt),
      filterVariant: 'date-range',
      muiFilterDatePickerProps: '',
      Cell: ({ cell }) => cell.getValue().toLocaleDateString('en-GB',
        { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }
      ),
    },
    {
      header: 'Duration',
      accessorFn: (originalRow) => {
        const startDate = new Date(originalRow.startedAt);
        const endDate = new Date(originalRow.finishedAt);
        const durationInSeconds = Math.round((endDate - startDate) / 1000);
        return `${durationInSeconds}s`;
      },
      filterVariant: 'none'
    },
    {
      accessorKey: 'state',
      header: 'Status',
      accessorFn: (originalRow) => {
        switch (originalRow.state) {
          case 'Running':
            return <span style={{ color: 'blue' }}>Running</span>;
          case 'Succcess':
            return <span style={{ color: 'green' }}>Succcess</span>;
          case 'ExchangeClosed':
            return <span style={{ color: 'grey' }}>Exchange closed</span>;
          case 'ConfigDisabled':
            return <span style={{ color: 'grey' }}>Disabled in configuration</span>;
          case 'Error':
            return <span style={{ color: 'red' }}>Error</span>;
          default:
            return <span style={{ color: 'black' }}>{originalRow.state}</span>;
        }
      }
    },
    {
      accessorKey: 'stateDetails',
      header: 'Details',
      filterVariant: 'none'
    },
    {
      accessorKey: 'actions',
      header: 'Actions',
      accessorFn: (originalRow) => originalRow.actions.length,
      filterVariant: 'none'
    }
  ], []);

  const table = useMaterialReactTable({
    columns,
    data: tradingTasks,
    initialState: { showColumnFilters: false, pagination: { pageSize: 5 }, columnVisibility: { id: false } },
    enableFullScreenToggle: false,
    enableDensityToggle: false,
    enableGlobalFilter: false,
    enableHiding: false,
    muiPaginationProps: {
      rowsPerPageOptions: [5],
      showFirstButton: false,
      showLastButton: false,
    },
    muiDetailPanelProps: '',
    enableRowActions: true,
    renderRowActions: ({ row }) => (
      <IconButton onClick={() => onRowClicked(row.original.id)}>
        <AddIcon />
      </IconButton>
    )
  });

  return (
    <MaterialReactTable table={table} />
  );
};

const TradingActionsTable = ({ tradingActions }) => {
  const columns = useMemo(() => [
    {
      accessorKey: 'id',
      header: 'Id',
    },
    {
      accessorKey: 'status',
      header: 'Status',
      accessorFn: (originalRow) => {
        if(originalRow.status === 'Error'){
          return 'Error [' + originalRow.error.code + ']: ' + originalRow.error.message;
        }

        return originalRow.status;
      }
    },
    {
      accessorKey: 'createdAt',
      header: 'Created',
      accessorFn: (originalRow) => new Date(originalRow.createdAt),
      filterVariant: 'date-range',
      muiFilterDatePickerProps: '',
      Cell: ({ cell }) => cell.getValue().toLocaleDateString('en-GB',
        { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }
      ),
    },
    {
      accessorKey: 'executedAt',
      header: 'Executed',
      accessorFn: (originalRow) => originalRow.executedAt === null ? null : new Date(originalRow.executedAt),
      filterVariant: 'date-range',
      muiFilterDatePickerProps: '',
      Cell: ({ cell }) => {
        const value = cell.getValue();
        if (value === null)
          return "Not executed yet";
        return value.toLocaleDateString('en-GB',
          { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }
        )
      },
    },
    {
      accessorKey: 'orderType',
      header: 'Order Type',
      size: 200,
      filterVariant: 'multi-select',
      filterSelectOptions: ["MarketBuy", "LimitBuy", "MarketSell", "LimitSell"],
    },
    {
      accessorKey: 'symbol',
      header: 'Symbol',
      size: 200,
    },
    {
      accessorKey: 'price',
      header: 'Price',
      Cell: ({ cell }) => {
        const priceValue = cell.getValue();
        if (priceValue === null || priceValue === undefined)
          return null;
        return priceValue.toLocaleString('en-US', {
          style: 'currency',
          currency: 'USD',
        });
      },
      filterVariant: 'range-slider',
      filterFn: (row, id, filterValues) => {
        if (row.getValue(id) === null || row.getValue(id) === undefined)
          return false;
        const value = row.getValue(id);
        return (value >= filterValues[0] && value <= filterValues[1]);
      },
      muiFilterSliderProps: {
        min: 0,
        max: 1000,
        step: 10,
        valueLabelFormat: (value) =>
          value.toLocaleString('en-US', {
            style: 'currency',
            currency: 'USD',
          }),
      },
    },
    {
      accessorKey: 'averageFillPrice',
      header: 'Fill price',
      Cell: ({ cell }) => {
        const priceValue = cell.getValue();
        if (priceValue === null || priceValue === undefined)
          return null;
        return priceValue.toLocaleString('en-US', {
          style: 'currency',
          currency: 'USD',
        });
      },
      filterVariant: 'range-slider',
      filterFn: (row, id, filterValues) => {
        if (row.getValue(id) === null || row.getValue(id) === undefined)
          return false;
        const value = row.getValue(id);
        return (value >= filterValues[0] && value <= filterValues[1]);
      },
      muiFilterSliderProps: {
        min: 0,
        max: 1000,
        step: 10,
        valueLabelFormat: (value) =>
          value.toLocaleString('en-US', {
            style: 'currency',
            currency: 'USD',
          }),
      },
    },
    {
      accessorKey: 'quantity',
      header: 'Quantity',
      size: 80,
    },
  ], []);

  const table = useMaterialReactTable({
    columns,
    data: tradingActions,
    initialState: { showColumnFilters: false, pagination: { pageSize: 5 }, columnVisibility: { id: false } },
    enableFullScreenToggle: false,
    enableDensityToggle: false,
    enableGlobalFilter: false,
    enableHiding: false,
    muiPaginationProps: {
      rowsPerPageOptions: [5],
      showFirstButton: false,
      showLastButton: false,
    },
    muiDetailPanelProps: ''
  });

  return (
    <MaterialReactTable table={table} />
  );
};

const Home = () => {

  const navigate = useNavigate();

  const [userName, setUserName] = useState('');
  const [pwd, setPwd] = useState('');
  const [tradingTasksData, setTradingTasksData] = useState([]);
  const [selectedTaskId, setSelectedTaskId] = useState(null);
  const [isTestModeOn, setIsTestModeOn] = useState(true);
  const [isInvestmentOn, setIsInvestmentOn] = useState(true);
  const [showStrategyOptions, setshowStrategyOptions] = useState(true);
  const [strategyParameters, setStrategyParameters] = useState({});
  const [performanceData, setPerformanceData] = useState([]);
  const [equityValue, setEquityValue] = useState(0);

  const [editingStrategyParameters, setEditingStrategyParameters] = useState(false);
  const [newStrategyParameters, setNewStrategyParameters] = useState({});

  const [strategySelection, setStrategySelection] = useState('Basic strategy');
  const [newStrategySelection, setNewStrategySelection] = useState('Basic strategy');
  const [validStrategyNames, setValidStrategyNames] = useState([]);

  const [maxChartValue, setMaxChartValue] = useState(0);
  const [areTasksReady, setTasksReady] = useState(false);

  useEffect(() => {
    const storedUserName = localStorage.getItem("userName");
    const storedPwd = localStorage.getItem("pwd");
    setUserName(localStorage.getItem("userName"));
    setPwd(localStorage.getItem("pwd"));

    axios.get(ASSETS_URL,
      {
        auth: {
          username: storedUserName,
          password: storedPwd
        }
      }
    ).then(result => {
      setEquityValue(result.data.equityValue);
    }).catch(err => {
      if (!err?.response || err.response?.status === 401) {
        logout();
      } else {
        displayErrorAlert(err.response?.data, errorStatusString(err.response?.config?.url, err.response.status, err.response.statusText));
      }
    });

    axios.get(TEST_MODE_URL,
      {
        auth: {
          username: storedUserName,
          password: storedPwd
        }
      }
    ).then(result => {
      setIsTestModeOn(result.data.enabled);
    }).catch(err => {
      if (!err?.response || err.response?.status === 401) {
        logout();
      } else {
        displayErrorAlert(err.response?.data, errorStatusString(err.response?.config?.url, err.response.status, err.response.statusText));
      }
    });

    axios.get(INVESTMENT_URL,
      {
        auth: {
          username: storedUserName,
          password: storedPwd
        }
      }
    ).then(result => {
      setIsInvestmentOn(result.data.enabled);
    }).catch(err => {
      if (!err?.response || err.response?.status === 401) {
        logout();
      } else {
        displayErrorAlert(err.response?.data, errorStatusString(err.response?.config?.url, err.response.status, err.response.statusText));
      }
    });

    axios.get(STRATEGY_URL,
      {
        auth: {
          username: storedUserName,
          password: storedPwd
        }
      }
    ).then(result => {
      setStrategyParameters(result.data);
      setNewStrategyParameters(result.data);
    }).catch(err => {
      if (!err?.response || err.response?.status === 401) {
        logout();
      } else {
        displayErrorAlert(err.response?.data, errorStatusString(err.response?.config?.url, err.response.status, err.response.statusText));
      }
    });

    axios.get(PERFORMANCE_URL,
      {
        auth: {
          username: storedUserName,
          password: storedPwd
        }
      }
    ).then(result => {
      setPerformanceData(result.data);
      setMaxChartValue(Math.max(...result.data.map((row) => Math.abs(row.return))));
    }).catch(err => {
      if (!err?.response || err.response?.status === 401) {
        logout();
      } else {
        displayErrorAlert(err.response?.data, errorStatusString(err.response?.config?.url, err.response.status, err.response.statusText));
      }
    });

    const getActionsForTask = (taskId) => axios.get(
      tradingActionsForTaskUrl(taskId),
      {
        auth: {
          username: storedUserName,
          password: storedPwd
        }
      }
    ).then(result => result.data)
      .catch(err => {
        displayErrorAlert(err.response?.data, errorStatusString(err.response?.config?.url, err.response.status, err.response.statusText));
      });

    axios.get(TRADING_TASKS_URL,
      {
        auth: {
          username: storedUserName,
          password: storedPwd
        }
      }
    ).then(result => Promise.all(result.data.map(
      tradingTask => getActionsForTask(tradingTask.id)
        .then(actions => tradingTask.actions = actions)
    )).then(_ => result))
      .then(result => setTradingTasksData(result.data))
      .then(_ => setTasksReady(true))
      .catch(err => {
        if (!err?.response || err.response?.status === 401) {
          logout();
        } else {
          displayErrorAlert(err.response?.data, errorStatusString(err.response?.config?.url, err.response.status, err.response.statusText));
        }
      });

    axios.get(STRATEGY_NAMES_URL,
      {
        auth: {
          username: storedUserName,
          password: storedPwd
        }
      }
    ).then(result => {
      setValidStrategyNames(result.data.names);
    }).catch(err => {
      if (!err?.response || err.response?.status === 401) {
        logout();
      } else {
        displayErrorAlert(err.response?.data, errorStatusString(err.response?.config?.url, err.response.status, err.response.statusText));
      }
    });

    axios.get(STRATEGY_SELECTION_URL,
      {
        auth: {
          username: storedUserName,
          password: storedPwd
        }
      }
    ).then(result => {
      setStrategySelection(result.data.name);
      setNewStrategySelection(result.data.name);
    }).catch(err => {
      if (!err?.response || err.response?.status === 401) {
        logout();
      } else {
        displayErrorAlert(err.response?.data, errorStatusString(err.response?.config?.url, err.response.status, err.response.statusText));
      }
    });
  }, []);

  const logout = () => {
    localStorage.clear();
    navigate(LOGIN_URL);
  };

  const handleSwitchTestModeClick = async () => {
    let message = "Are you sure you want to trun on the test mode?";
    if (isTestModeOn) {
      message = "Are you sure you want to trun off the test mode?";
    }
    const result = window.confirm(message);
    if (result) {
      try {
        const response = await axios.put(TEST_MODE_URL,
          {
            "enable": !isTestModeOn
          },
          {
            auth: {
              username: userName,
              password: pwd
            }
          }
        );
        setIsTestModeOn(response.data.enabled);
      } catch (err) {
        if (!err?.response || err.response?.status === 401) {
          logout();
        } else {
          displayErrorAlert(err.response?.data, "Switching test mode failed, please try again... \n" + errorStatusString(err.response?.config?.url, err.response.status, err.response.statusText));
        }
      }
    }
  }

  const handleSwitchInvestmentClick = async () => {
    let message = "Are you sure you want to trun on the investment?";
    if (isInvestmentOn) {
      message = "Are you sure you want to trun off the investment?";
    }
    const result = window.confirm(message);
    if (result) {
      try {
        const response = await axios.put(INVESTMENT_URL,
          {
            "enable": !isInvestmentOn
          },
          {
            auth: {
              username: userName,
              password: pwd
            }
          }
        );
        setIsInvestmentOn(response.data.enabled);
      } catch (err) {
        if (!err?.response || err.response?.status === 401) {
          logout();
        } else {
          displayErrorAlert(err.response?.data, "Switching investment failed, please try again... \n" + errorStatusString(err.response?.config?.url, err.response.status, err.response.statusText));
        }
      }
    }
  }

  const handleStrategyOptionstClick = () => {
    setshowStrategyOptions(!showStrategyOptions);
  }

  const handleEditStrategyParametersClick = () => {
    setEditingStrategyParameters(true);
  };

  const handleConfirmEditStrategyParametersClick = async () => {
    try {
      const response = await axios.put(STRATEGY_URL,
        newStrategyParameters,
        {
          auth: {
            username: userName,
            password: pwd
          },
          headers: {
            'Content-Type': 'application/json',
          },
          withCredentials: false
        }
      );
      setStrategyParameters(response.data);
      setNewStrategyParameters(response.data);

      const selectionResponse = await axios.put(STRATEGY_SELECTION_URL,
        {
          name: newStrategySelection
        },
        {
          auth: {
            username: userName,
            password: pwd
          }
        },
        {
          headers: {
            'Content-Type': 'application/json',
          },
          withCredentials: false
        }
      );
      setStrategySelection(selectionResponse.data.name);
      setNewStrategySelection(selectionResponse.data.name);

    } catch (err) {
      if (!err?.response || err.response?.status === 401) {
        logout();
      } else {
        displayErrorAlert(err.response?.data, "Changing strategy parameters failed, please try again... \n" + errorStatusString(err.response?.config?.url, err.response.status, err.response.statusText));
      }
    }
    setEditingStrategyParameters(false);
  };

  const handleCancelEditStrategyParametersClick = () => {
    setNewStrategyParameters(strategyParameters);
    setEditingStrategyParameters(false);
  }

  const clipNumberToRange = (min, max) => (value) => Math.min(Math.max(isNaN(value) ? 0 : value, min), max);

  const handleGeneralParameterChange = (field, value) => {
    setNewStrategyParameters((prevParameters) => ({
      ...prevParameters,
      [field]: value
    }));
  }

  const handleStrategyParameterChange = (strategy, field, value) => {
    setNewStrategyParameters((prevParameters) => ({
      ...prevParameters,
      [strategy]: {
        ...prevParameters[strategy],
        [field]: value
      },
    }));
  };

  const countInitialAccountValue = () => {
    if (performanceData.length > 0)
      return equityValue / (1 + performanceData[performanceData.length - 1].return);
    else
      return 0;
  }

  const chartData = {
    labels: performanceData.map((data) => new Date(data.time).toISOString().split('T')[0]),
    datasets: [
      {
        label: 'Returns',
        data: performanceData.map((data) => data.return),
        fill: false,
        borderColor: 'rgb(75, 192, 192)',
        tension: 0.1,
      },
    ],
  };

  const options = {
    scales: {
      y: {
        type: 'linear',
        beginAtZero: false,
        min: -maxChartValue * CHART_SCALE_RATIO,
        max: maxChartValue * CHART_SCALE_RATIO,
      },
    },
  };

  if (!areTasksReady) {
    return <div>Loading...</div>;
  }

  return (
    <div className="mx-auto items-center justify-center h-screen" style={{ marginTop: '100px', marginBottom: "200px" }}>
      <div className="container p-8 bg-gray-100 rounded-xl" style={{ marginBottom: "50px" }}>
        <div className="mb-8">
          <div className="bg-white p-6 rounded-xl shadow-lg">
            <h2 className="text-2xl font-semibold text-gray-700 mb-4 text-center">
              Returns chart
            </h2>
            <div className="h-64 bg-gray-200 rounded-xl flex items-center justify-center">
              <Line data={chartData} options={options} />
            </div>
          </div>
        </div>
        <div className="flex">
          <div className="bg-white p-6 rounded-xl shadow-lg overflow-x-auto w-min ml-0 whitespace-nowrap">
            <h3 className="text-md text-gray-700 text-left">
              Current Balance
            </h3>
            <h3 className="text-2xl font-bold text-gray-700 text-left">
              USD {equityValue.toFixed(2)}
            </h3>
          </div>
          <div className={`p-6 rounded-xl shadow-lg overflow-x-auto w-min ml-5 whitespace-nowrap ${(equityValue - countInitialAccountValue()) >= 0 ? 'bg-green-200' : 'bg-red-200'}`}>
            <h3 className="text-md text-gray-700 text-left">
              {(equityValue - countInitialAccountValue()) >= 0 ? 'Current income' : 'Current loss'}
            </h3>
            <h3 className={`text-2xl font-bold text-gray-700 text-left `}>
              USD {(Math.abs(equityValue - countInitialAccountValue())).toFixed(2)}
            </h3>
          </div>
        </div>
        <div className="bg-white p-6 rounded-xl shadow-lg overflow-x-auto mt-4">
          <h3 className="text-2xl font-semibold text-gray-700 mb-4 text-center">
            Trading task history
          </h3>
          <TradingTasksTable tradingTasks={tradingTasksData} onRowClicked={setSelectedTaskId} />
          <h3 className="text-2xl font-semibold text-gray-700 mb-4 text-center" style={{ paddingTop: '20px' }}>
            Trading actions
          </h3>
          {selectedTaskId !== null ? (
            <TradingActionsTable tradingActions={tradingTasksData.find((task) => task.id === selectedTaskId).actions} />
          ) : (
            <div style={{ border: '1px solid gray', width: '100%', fontStyle: 'italic', color: 'gray', textAlign: 'center' }}>
              No task selected
            </div>
          )}
        </div>
        <div>
          {showStrategyOptions ? (
            <button className="text-center bg-gray-300 hover:bg-gray-400 text-black py-1 px-3 rounded" style={{ marginTop: "30px" }} onClick={handleStrategyOptionstClick}>
              Show Strategy Options
            </button>
          ) : (
            <div className="bg-white p-6 rounded-xl shadow-lg overflow-x-auto mt-4">
              {editingStrategyParameters ? (
                <div>
                  <h3 className="text-1xl font-bold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Selected strategy:
                  </h3>
                  <Select
                    labelId="strategy-select-label"
                    id="strategy-select"
                    value={newStrategySelection}
                    label="Strategy"
                    onChange={(e) => setNewStrategySelection(e.target.value)}
                  >
                    {validStrategyNames.map((strategyName) => (
                      <MenuItem value={strategyName}>
                        {strategyName}
                      </MenuItem>
                    ))}
                  </Select>
                  <h3 className="mb-2">Prediction damping for limit orders:</h3>
                  <input
                    type="text"
                    value={newStrategyParameters.limitPriceDamping}
                    onChange={(e) => handleGeneralParameterChange('limitPriceDamping', clipNumberToRange(0, 1)(e.target.value))}
                    className="border border-gray-300 p-0.5 mb-2 mr-1.5"
                    style={{ width: "150px", height: "35px" }}
                  />
                  <Divider variant="middle"/>
                  <h3 className="text-1xl font-bold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Basic strategy parameters
                  </h3>
                  <h3 className="mb-2">Max Stocks Buy Count:</h3>
                  <input
                    type="text"
                    value={newStrategyParameters.basic.maxStocksBuyCount}
                    onChange={(e) => handleStrategyParameterChange('basic', 'maxStocksBuyCount', clipNumberToRange(1, 100)(Math.floor(e.target.value)))}
                    className="border border-gray-300 p-0.5 mb-2 mr-1.5"
                    style={{ width: "150px", height: "35px" }}
                  />
                  <h3 className="mb-2">Min Days Decreasing:</h3>
                  <input
                    type="text"
                    value={newStrategyParameters.basic.minDaysDecreasing}
                    onChange={(e) => handleStrategyParameterChange('basic', 'minDaysDecreasing', clipNumberToRange(1, 5)(Math.floor(e.target.value)))}
                    className="border border-gray-300 p-0.5 mb-2 mr-1.5"
                    style={{ width: "150px", height: "35px" }}
                  />
                  <h3 className="mb-2">Min Days Increasing:</h3>
                  <input
                    type="text"
                    value={newStrategyParameters.basic.minDaysIncreasing}
                    onChange={(e) => handleStrategyParameterChange('basic', 'minDaysIncreasing', clipNumberToRange(1, 5)(Math.floor(e.target.value)))}
                    className="border border-gray-300 p-0.5 mb-2 mr-1.5"
                    style={{ width: "150px", height: "35px" }}
                  />
                  <h3 className="mb-2">Top Growing Symbols Buy Ratio:</h3>
                  <input
                    type="text"
                    value={newStrategyParameters.basic.topGrowingSymbolsBuyRatio}
                    onChange={(e) => handleStrategyParameterChange('basic', 'topGrowingSymbolsBuyRatio', clipNumberToRange(0.01, 1)(e.target.value))}
                    className="border border-gray-300 p-0.5 mb-2 mr-1.5"
                    style={{ width: "150px", height: "35px" }}
                  />
                  <Divider variant="middle"/>
                  <h3 className="text-1xl font-bold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Overreaction strategy parameters
                  </h3>
                  <h3 className="mb-2">Evaluation frequency:</h3>
                  <input
                    type="text"
                    value={newStrategyParameters.buyLosers.evaluationFrequencyInDays}
                    onChange={(e) => handleStrategyParameterChange('buyLosers', 'evaluationFrequencyInDays', clipNumberToRange(1, 100)(Math.floor(e.target.value)))}
                    className="border border-gray-300 p-0.5 mb-2 mr-1.5"
                    style={{ width: "150px", height: "35px" }}
                  />
                  <h3 className="mb-2">Analysis length:</h3>
                  <input
                    type="text"
                    value={newStrategyParameters.buyLosers.analysisLengthInDays}
                    onChange={(e) => handleStrategyParameterChange('buyLosers', 'analysisLengthInDays', clipNumberToRange(7, 365)(Math.floor(e.target.value)))}
                    className="border border-gray-300 p-0.5 mb-2 mr-1.5"
                    style={{ width: "150px", height: "35px" }}
                  />
                  <Divider variant="middle" />
                  <h3 className="text-1xl font-bold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Trend following strategy parameters
                  </h3>
                  <h3 className="mb-2">Evaluation frequency:</h3>
                  <input
                    type="text"
                    value={newStrategyParameters.buyWinners.evaluationFrequencyInDays}
                    onChange={(e) => handleStrategyParameterChange('buyWinners', 'evaluationFrequencyInDays', clipNumberToRange(1, 100)(Math.floor(e.target.value)))}
                    className="border border-gray-300 p-0.5 mb-2 mr-1.5"
                    style={{ width: "150px", height: "35px" }}
                  />
                  <h3 className="mb-2">Analysis length:</h3>
                  <input
                    type="text"
                    value={newStrategyParameters.buyWinners.analysisLengthInDays}
                    onChange={(e) => handleStrategyParameterChange('buyWinners', 'analysisLengthInDays', clipNumberToRange(7, 365)(Math.floor(e.target.value)))}
                    className="border border-gray-300 p-0.5 mb-2 mr-1.5"
                    style={{ width: "150px", height: "35px" }}
                  />
                  <h3 className="mb-2">Simultaneous Evaluations:</h3>
                  <input
                    type="text"
                    value={newStrategyParameters.buyWinners.simultaneousEvaluations}
                    onChange={(e) => handleStrategyParameterChange('buyWinners', 'simultaneousEvaluations', clipNumberToRange(1, 12)(Math.floor(e.target.value)))}
                    className="border border-gray-300 p-0.5 mb-2 mr-1.5"
                    style={{ width: "150px", height: "35px" }}
                  />
                  <h3 className="mb-2">Wait time before buying:</h3>
                  <input
                    type="text"
                    value={newStrategyParameters.buyWinners.buyWaitTimeInDays}
                    onChange={(e) => handleStrategyParameterChange('buyWinners', 'buyWaitTimeInDays', clipNumberToRange(0, 30)(Math.floor(e.target.value)))}
                    className="border border-gray-300 p-0.5 mb-2 mr-1.5"
                    style={{ width: "150px", height: "35px" }}
                  />
                  <Divider variant="middle" />
                  <h3 className="text-1xl font-bold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    PCA strategy parameters
                  </h3>
                  <h3 className="mb-2">Analysis length:</h3>
                  <input
                    type="text"
                    value={newStrategyParameters.pca.analysisLengthInDays}
                    onChange={(e) => handleStrategyParameterChange('pca', 'analysisLengthInDays', clipNumberToRange(7, 365)(Math.floor(e.target.value)))}
                    className="border border-gray-300 p-0.5 mb-2 mr-1.5"
                    style={{ width: "150px", height: "35px" }}
                  />
                  <h3 className="mb-2">Decomposition expiration:</h3>
                  <input
                    type="text"
                    value={newStrategyParameters.pca.decompositionExpirationInDays}
                    onChange={(e) => handleStrategyParameterChange('pca', 'decompositionExpirationInDays', clipNumberToRange(0, 365)(Math.floor(e.target.value)))}
                    className="border border-gray-300 p-0.5 mb-2 mr-1.5"
                    style={{ width: "150px", height: "35px" }}
                  />
                  <h3 className="mb-2">Variance fraction covered by decomposition:</h3>
                  <input
                    type="text"
                    value={newStrategyParameters.pca.varianceFraction}
                    onChange={(e) => handleStrategyParameterChange('pca', 'varianceFraction', clipNumberToRange(0.01, 1)(e.target.value))}
                    className="border border-gray-300 p-0.5 mb-2 mr-1.5"
                    style={{ width: "150px", height: "35px" }}
                  />
                  <h3 className="mb-2">Undervaluation threshold:</h3>
                  <input
                    type="text"
                    value={newStrategyParameters.pca.undervaluedThreshold}
                    onChange={(e) => handleStrategyParameterChange('pca', 'undervaluedThreshold', clipNumberToRange(0, 10)(e.target.value))}
                    className="border border-gray-300 p-0.5 mb-2 mr-1.5"
                    style={{ width: "150px", height: "35px" }}
                  />
                  <h3 className="mb-2">Diversification threshold:</h3>
                  <input
                    type="text"
                    value={newStrategyParameters.pca.diverseThreshold}
                    onChange={(e) => handleStrategyParameterChange('pca', 'diverseThreshold', clipNumberToRange(0, 1)(e.target.value))}
                    className="border border-gray-300 p-0.5 mb-2 mr-1.5"
                    style={{ width: "150px", height: "35px" }}
                  />
                  <h3 className="mb-2">Ignored symbol threshold:</h3>
                  <input
                    type="text"
                    value={newStrategyParameters.pca.ignoredThreshold}
                    onChange={(e) => handleStrategyParameterChange('pca', 'ignoredThreshold', clipNumberToRange(0, 1)(e.target.value))}
                    className="border border-gray-300 p-0.5 mb-2 mr-1.5"
                    style={{ width: "150px", height: "35px" }}
                  />
                  <div>
                    <button className="bg-blue-500 hover:bg-blue-700 text-white py-1 px-3 rounded mr-2" onClick={handleConfirmEditStrategyParametersClick}>
                      Confirm
                    </button>
                    <button className="bg-gray-300 hover:bg-gray-400 text-black py-1 px-3 rounded" onClick={handleCancelEditStrategyParametersClick}>
                      Cancel
                    </button>
                  </div>
                </div>
              ) : (
                <div>
                  <button className="bg-gray-300 hover:bg-gray-400 text-black py-1 px-3 rounded mr-3" onClick={handleStrategyOptionstClick}>
                    Hide Strategy Options
                  </button>
                  <button className="bg-gray-300 hover:bg-gray-400 text-black py-1 px-3 rounded" onClick={handleEditStrategyParametersClick}>
                    Edit Strategy Options
                  </button>
                  <h3 className="text-1xl font-bold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Selected strategy: {strategySelection}
                  </h3>
                  <h3 className="text-1xl font-semibold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Prediction damping for limit orders: {strategyParameters.limitPriceDamping}
                  </h3>
                  <Divider variant="middle" />
                  <h3 className="text-1xl font-bold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Basic strategy parameters
                  </h3>
                  <h3 className="text-1xl font-semibold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Max Stocks Buy Count: {strategyParameters.basic.maxStocksBuyCount}
                  </h3>
                  <h3 className="text-1xl font-semibold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Min Days Decreasing: {strategyParameters.basic.minDaysDecreasing}
                  </h3>
                  <h3 className="text-1xl font-semibold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Min Days Increasing: {strategyParameters.basic.minDaysIncreasing}
                  </h3>
                  <h3 className="text-1xl font-semibold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Top Growing Symbols Buy Ratio: {strategyParameters.basic.topGrowingSymbolsBuyRatio}
                  </h3>
                  <Divider variant="middle" />
                  <h3 className="text-1xl font-bold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Overreaction strategy parameters
                  </h3>
                  <h3 className="text-1xl font-semibold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Evaluation frequency: {strategyParameters.buyLosers.evaluationFrequencyInDays}
                  </h3>
                  <h3 className="text-1xl font-semibold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Analysis length: {strategyParameters.buyLosers.analysisLengthInDays}
                  </h3>
                  <Divider variant="middle" />
                  <h3 className="text-1xl font-bold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Trend following strategy parameters
                  </h3>
                  <h3 className="text-1xl font-semibold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Evaluation frequency: {strategyParameters.buyWinners.evaluationFrequencyInDays}
                  </h3>
                  <h3 className="text-1xl font-semibold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Analysis length: {strategyParameters.buyWinners.analysisLengthInDays}
                  </h3>
                  <h3 className="text-1xl font-semibold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Simultaneous evaluations: {strategyParameters.buyWinners.simultaneousEvaluations}
                  </h3>
                  <h3 className="text-1xl font-semibold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Wait time before buying: {strategyParameters.buyWinners.buyWaitTimeInDays}
                  </h3>
                  <Divider variant="middle" />
                  <h3 className="text-1xl font-bold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    PCA strategy parameters
                  </h3>
                  <h3 className="text-1xl font-semibold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Analysis length: {strategyParameters.pca.analysisLengthInDays}
                  </h3>
                  <h3 className="text-1xl font-semibold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Decomposition expiration: {strategyParameters.pca.decompositionExpirationInDays}
                  </h3>
                  <h3 className="text-1xl font-semibold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Variance freaction covered by decomposition: {strategyParameters.pca.varianceFraction}
                  </h3>
                  <h3 className="text-1xl font-semibold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Undervaluation threshold: {strategyParameters.pca.undervaluedThreshold}
                  </h3>
                  <h3 className="text-1xl font-semibold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Diversification threshold: {strategyParameters.pca.diverseThreshold}
                  </h3>
                  <h3 className="text-1xl font-semibold text-gray-700 mb-4" style={{ marginTop: "30px" }}>
                    Ignored symbol threshold: {strategyParameters.pca.ignoredThreshold}
                  </h3>
                </div>
              )}
            </div>
          )}
        </div>
        <div>
          {isTestModeOn ? (
            <button data-testid="test-mode-on-button" className="flex items-center bg-gray-300 hover:bg-gray-400 text-black py-1 px-3 rounded" style={{ marginTop: "30px" }} onClick={handleSwitchTestModeClick}>
              <svg viewBox="0 0 32 32" fill="currentColor" height="1.5em" width="1.5em">
                <path d="M21 9H9a6 6 0 00-6 6 6 6 0 006 6h12a6 6 0 006-6 6 6 0 00-6-6m0 10a4 4 0 01-4-4 4 4 0 014-4 4 4 0 014 4 4 4 0 01-4 4z" />
              </svg>
              <span className="ml-2">Test mode on</span>
            </button>
          ) : (
            <button data-testid="test-mode-off-button" className="flex items-center bg-gray-300 hover:bg-gray-400 text-black py-1 px-3 rounded" style={{ marginTop: "30px" }} onClick={handleSwitchTestModeClick}>
              <svg viewBox="0 0 24 24" fill="currentColor" height="1.5em" width="1.3em">
                <path d="M17 7H7a5 5 0 00-5 5 5 5 0 005 5h10a5 5 0 005-5 5 5 0 00-5-5M7 15a3 3 0 01-3-3 3 3 0 013-3 3 3 0 013 3 3 3 0 01-3 3z" />
              </svg>
              <span className="ml-2">Test mode off</span>
            </button>
          )
          }
        </div>
        <div>
          {isInvestmentOn ? (
            <button data-testid="investment-on-button" className="flex items-center bg-green-400 hover:bg-gray-400 text-black py-1 px-3 rounded" style={{ marginTop: "30px" }} onClick={handleSwitchInvestmentClick}>
              <svg viewBox="0 0 32 32" fill="currentColor" height="1.5em" width="1.5em">
                <path d="M21 9H9a6 6 0 00-6 6 6 6 0 006 6h12a6 6 0 006-6 6 6 0 00-6-6m0 10a4 4 0 01-4-4 4 4 0 014-4 4 4 0 014 4 4 4 0 01-4 4z" />
              </svg>
              <span className="ml-2">Investment on</span>
            </button>
          ) : (
            <button data-testid="investment-off-button" className="flex items-center bg-red-400 hover:bg-gray-400 text-black py-1 px-3 rounded" style={{ marginTop: "30px" }} onClick={handleSwitchInvestmentClick}>
              <svg viewBox="0 0 24 24" fill="currentColor" height="1.5em" width="1.3em">
                <path d="M17 7H7a5 5 0 00-5 5 5 5 0 005 5h10a5 5 0 005-5 5 5 0 00-5-5M7 15a3 3 0 01-3-3 3 3 0 013-3 3 3 0 013 3 3 3 0 01-3 3z" />
              </svg>
              <span className="ml-2">Investment off</span>
            </button>
          )
          }
        </div>
      </div>
    </div>
  )
}

export default Home;