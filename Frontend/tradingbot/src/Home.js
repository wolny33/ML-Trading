import React from "react";
import { useState, useEffect } from "react";
import { useMemo } from 'react';
import { MaterialReactTable, useMaterialReactTable } from 'material-react-table';
import { Box, Typography } from '@mui/material';

const Home = () => {

  const [tradingAcionsData, setTradingAcionsData] = useState([]);
  const [isTestModeOn, setIsTestModeOn] = useState(true);
  const [isInvestmentOn, setIsInvestmentOn] = useState(true);
  const [showStrategyOptions, setshowStrategyOptions] = useState(true);

  useEffect(() => {
    setTradingAcionsData(  [{
      date: '2022-03-13',
      actionType: 'BUY',
      symbol: 'AAPL',
      price: 100,
      quantity: 2
    },
    {
      date: '2022-03-13',
      actionType: 'BUY',
      symbol: 'AAPL',
      price: 100,
      quantity: 2
    },
    {
      date: '2022-03-13',
      actionType: 'BUY',
      symbol: 'AAPL',
      price: 100,
      quantity: 2
    },
    {
      date: '2022-03-13',
      actionType: 'BUY',
      symbol: 'AAPL',
      price: 100,
      quantity: 2
    },
    {
      date: '2022-03-13',
      actionType: 'SELL',
      symbol: 'AAPL',
      price: 100,
      quantity: 2
    },
    {
      date: '2022-03-13',
      actionType: 'SELL',
      symbol: 'AAPL',
      price: 100,
      quantity: 2
    },
    {
      date: '2022-03-13',
      actionType: 'SELL',
      symbol: 'AAPL',
      price: 100,
      quantity: 2
    },
    {
      date: '2022-03-13',
      actionType: 'SELL',
      symbol: 'AAPL',
      price: 100,
      quantity: 2
    },
    {
      date: '2022-03-13',
      actionType: 'SELL',
      symbol: 'AAPL',
      price: 100,
      quantity: 2
    },
    {
      date: '2022-03-13',
      actionType: 'SELL',
      symbol: 'AAPL',
      price: 100,
      quantity: 2
    },
    {
      date: '2022-03-13',
      actionType: 'SELL',
      symbol: 'AAPL',
      price: 100,
      quantity: 2
    },
    {
      date: '2022-03-13',
      actionType: 'SELL',
      symbol: 'AAPL',
      price: 100,
      quantity: 2
    },
    {
      date: '2022-03-13',
      actionType: 'SELL',
      symbol: 'AAPL',
      price: 100,
      quantity: 2
    },
    {
      date: '2022-03-13',
      actionType: 'SELL',
      symbol: 'AAPL',
      price: 100,
      quantity: 2
    },
    {
      date: '2022-03-13',
      actionType: 'SELL',
      symbol: 'AAPL',
      price: 100,
      quantity: 2
    },
  ]);
  }, []);

  const handleSwitchTestModeClick = () => {
    setIsTestModeOn(!isTestModeOn);
  }

  const handleSwitchInvestmentClick = () => {
    setIsInvestmentOn(!isInvestmentOn);
  }

  const handleStrategyOptionstClick = () => {
    setshowStrategyOptions(!showStrategyOptions);
  }

  const columns = useMemo( () => [
      {
        id: 'date',
        header: 'Date',
        accessorFn: (originalRow) => new Date(originalRow.date),
        filterVariant: 'date-range',
        muiFilterDatePickerProps: '',
        Cell: ({ cell }) => cell.getValue().toLocaleDateString(),
      },
      {
        accessorKey: 'actionType',
        header: 'Action Type',
        size: 200,
        filterVariant: 'multi-select',
        filterSelectOptions: ["BUY", "SELL"],
      },
      {
        accessorKey: 'symbol',
        header: 'Symbol',
        size: 200,
      },
      {
        accessorKey: 'price',
        header: 'Price',
        Cell: ({ cell }) =>
          cell.getValue().toLocaleString('en-US', {
            style: 'currency',
            currency: 'USD',
          }),
        filterVariant: 'range-slider',
        filterFn: 'betweenInclusive',
        muiFilterSliderProps: {
          min: 0,
          max: 10000,
          step: 100,
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
    ],
    [],
  );

  const table = useMaterialReactTable({
    columns,
    data: tradingAcionsData,
    initialState: { showColumnFilters: false, pagination: { pageSize: 5 } },
    enableFullScreenToggle: false,
    enableDensityToggle: false,
    enableGlobalFilter: false,
    enableHiding: false,
    muiPaginationProps: {
      rowsPerPageOptions: [5],
      showFirstButton: false,
      showLastButton: false,
    },
    muiDetailPanelProps:'',
    renderDetailPanel: ({ row }) => (
      <Box
        sx={{
          display: 'grid',
          margin: 'auto',
          gridTemplateColumns: '1fr 1fr',
          width: '100%',
        }}
      >
        <Typography>Details:</Typography>
      </Box>
    ),
  });

    return(
        <div className="mx-auto items-center justify-center h-screen" style={{ marginTop: '100px', marginBottom: "200px" }}>
        <div className="container p-8 bg-gray-100 rounded-xl" style={{marginBottom: "50px" }}>
          {/* <h1 className="text-4xl font-bold text-gray-800 mb-6 text-center">
                Autonomic Trading Bot
          </h1> */}
          <div className="mb-8">
            <div className="bg-white p-6 rounded-xl shadow-lg">
            <h2 className="text-2xl font-semibold text-gray-700 mb-4 text-center">
                Returns chart
            </h2>
              <div className="h-64 bg-gray-200 rounded-xl flex items-center justify-center">
                {/* Placeholder for chart */}
                <p className="text-center">Chart Placeholder</p>
              </div>
            </div>
          </div>
          <div className="bg-white p-6 rounded-xl shadow-lg overflow-x-auto w-min ml-0 whitespace-nowrap">
            <h3 className="text-md text-gray-700 text-left">
              Current Balance
            </h3>
            <h3 className="text-2xl font-bold text-gray-700 text-left">
              USD 10,000.00
            </h3>
          </div>


          <div className="bg-white p-6 rounded-xl shadow-lg overflow-x-auto mt-4">
                <h3 className="text-2xl font-semibold text-gray-700 mb-4 text-center">
                    Trading history
                </h3>
          <MaterialReactTable table={table} />
          </div>
          {/* <div>
            <div className="bg-white p-6 rounded-xl shadow-lg overflow-x-auto">
                <h3 className="text-2xl font-semibold text-gray-700 mb-4 text-center">
                    Trading history
                </h3>
              <table className="mx-auto">
                <thead>
                  <tr className="bg-gray-200">
                    <th className="text-center py-2 px-4">Date</th>
                    <th className="text-center py-2 px-4">Order Type</th>
                    <th className="text-center py-2 px-4">Symbol</th>
                    <th className="text-center py-2 px-4">Price [$]</th>
                    <th className="text-center py-2 px-4">Quantity</th>
                    <th className="text-center py-2 px-4"></th>
                  </tr>
                </thead>
                <tbody>
                  <tr className="even:bg-gray-200 even:rounded-xl">
                    <td className="text-center py-2 px-4 font-semibold">13 Mar 2022</td>
                    <td className="text-center py-2 px-4 font-semibold">BUY</td>
                    <td className="text-center py-2 px-4 font-semibold">AAPL</td>
                    <td className="text-center py-2 px-4 font-semibold">100</td>
                    <td className="text-center py-2 px-4 font-semibold">23</td>
                    <td className="text-center py-2 px-4 font-semibold">
                      <button className="text-center bg-blue-500 hover:bg-blue-700 text-white font-bold py-1 px-3 rounded">
                        Details
                      </button>
                    </td>
                  </tr>
                  <tr className="even:bg-gray-200 even:rounded-xl">
                    <td className="text-center py-2 px-4 font-semibold">13 Mar 2022</td>
                    <td className="text-center py-2 px-4 font-semibold">BUY</td>
                    <td className="text-center py-2 px-4 font-semibold">AAPL</td>
                    <td className="text-center py-2 px-4 font-semibold">100</td>
                    <td className="text-center py-2 px-4 font-semibold">23</td>
                    <td className="text-center py-2 px-4 font-semibold">
                      <button className="text-center bg-blue-500 hover:bg-blue-700 text-white font-bold py-1 px-3 rounded">
                        Details
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div> */}
            <div>
            {showStrategyOptions ? (
              <button className="text-center bg-gray-300 hover:bg-gray-400 text-black py-1 px-3 rounded" style={{ marginTop: "30px" }} onClick={handleStrategyOptionstClick}>
                Show Strategy Options
              </button>
            ):(
              <button className="text-center bg-gray-300 hover:bg-gray-400 text-black py-1 px-3 rounded" style={{ marginTop: "30px" }} onClick={handleStrategyOptionstClick}>
                Hide Strategy Options
              </button>
            )}
            </div>
            <div>
            {isTestModeOn ? (
              <button className="flex items-center bg-gray-300 hover:bg-gray-400 text-black py-1 px-3 rounded" style={{ marginTop: "30px" }} onClick={handleSwitchTestModeClick}>
                <svg viewBox="0 0 32 32" fill="currentColor" height="1.5em" width="1.5em">
                  <path d="M21 9H9a6 6 0 00-6 6 6 6 0 006 6h12a6 6 0 006-6 6 6 0 00-6-6m0 10a4 4 0 01-4-4 4 4 0 014-4 4 4 0 014 4 4 4 0 01-4 4z" />
                </svg>
                <span className="ml-2">Test mode on</span>
              </button>            
            ): (
              <button className="flex items-center bg-gray-300 hover:bg-gray-400 text-black py-1 px-3 rounded" style={{ marginTop: "30px" }} onClick={handleSwitchTestModeClick}>
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
              <button className="flex items-center bg-green-400 hover:bg-gray-400 text-black py-1 px-3 rounded" style={{ marginTop: "30px" }} onClick={handleSwitchInvestmentClick}>
                <svg viewBox="0 0 32 32" fill="currentColor" height="1.5em" width="1.5em">
                  <path d="M21 9H9a6 6 0 00-6 6 6 6 0 006 6h12a6 6 0 006-6 6 6 0 00-6-6m0 10a4 4 0 01-4-4 4 4 0 014-4 4 4 0 014 4 4 4 0 01-4 4z" />
                </svg>
                <span className="ml-2">Investment on</span>
              </button>            
            ): (
              <button className="flex items-center bg-red-400 hover:bg-gray-400 text-black py-1 px-3 rounded" style={{ marginTop: "30px" }} onClick={handleSwitchInvestmentClick}>
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