import React, { useState, useEffect, useRef } from "react";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faArrowUp, faArrowDown, faPlus, faMinus } from "@fortawesome/free-solid-svg-icons";

function Table(props) {
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [dropdownOpen, setDropdownOpen] = useState(false);
  const [continentDropdownOpen, setContinentDropdownOpen] = useState(false);
  const [currencyDropdownOpen, setCurrencyDropdownOpen] = useState(false);
  const [populationDropdownOpen, setPopulationDropdownOpen] = useState(false);
  const [dropdownPosition, setDropdownPosition] = useState("down");
  const [selectedContinent, setSelectedContinent] = useState("ALL CONTINENTS");
  const [selectedCurrency, setSelectedCurrency] = useState("ALL CURRENCIES");

  const [minPopulation, setMinPopulation] = useState("");
  const [maxPopulation, setMaxPopulation] = useState("");

  const [sortByName, setSortByName] = useState(true);
  const [sortByCapital, setSortByCapital] = useState(false);

  const [nameArrowDirection, setNameArrowDirection] = useState(false);
  const [capitalArrowDirection, setCapitalArrowDirection] = useState(false);
  const [continentPlusOpen, setContinentPlusOpen] = useState(false);
  const [populationPlusOpen, setPopulationPlusOpen] = useState(false);
  const [currencyPlusOpen, setCurrencyPlusOpen] = useState(false);

  const dropdownRef = useRef(null);
  const continentDropdownRef = useRef(null);
  const currencyDropdownRef = useRef(null);
  const populationDropdownRef = useRef(null);

  const continentsList = ["ALL CONTINENTS", "Africa", "Asia", "Europe", "North America", "South America", "Oceania", "Antarctica"];
  // const currenciesList = ['USD', 'EUR', 'JPY', 'GBP', 'koko', 'AUD', 'BGN', 'Afghan afghani', 'ALL'];

  useEffect(() => {
    const handleClickOutside = (e) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target)) {
        setDropdownOpen(false);
      }
      if (continentDropdownRef.current && !continentDropdownRef.current.contains(e.target)) {
        setContinentDropdownOpen(false);
        setContinentPlusOpen(false);
      }
      if (currencyDropdownRef.current && !currencyDropdownRef.current.contains(e.target)) {
        setCurrencyDropdownOpen(false);
        setCurrencyPlusOpen(false);
      }
      if (populationDropdownRef.current && !populationDropdownRef.current.contains(e.target)) {
        setPopulationDropdownOpen(false);
        setPopulationPlusOpen(false);
      }
    };
    document.addEventListener("click", handleClickOutside);
    return () => {
      document.removeEventListener("click", handleClickOutside);
    };
  }, []);

  useEffect(() => {
    if (props.countries.length > 0) {
      const calculatedTotalPages = Math.ceil(props.countries.length / rowsPerPage);
      setTotalPages(calculatedTotalPages);
    }
  }, [props.countries, rowsPerPage]);

  useEffect(() => {
    if (props.newFilter) {
      setCurrentPage(1);
      props.setNewFilter(false);
    }
  }, [props.newFilter]);

  const handleRowsPerPageChange = (value) => {
    setRowsPerPage(value);
    setCurrentPage(1);
    setDropdownOpen(false);
  };

  const handleNextPage = () => {
    if (currentPage < totalPages) {
      setCurrentPage(currentPage + 1);
    }
  };

  const handlePrevPage = () => {
    if (currentPage > 1) {
      setCurrentPage(currentPage - 1);
    }
  };

  const getCurrentPageCountries = () => {
    const startIndex = (currentPage - 1) * rowsPerPage;
    const endIndex = startIndex + rowsPerPage;
    return props.countries.slice(startIndex, endIndex);
  };

  const toggleDropdown = () => {
    if (dropdownRef.current) {
      const dropdownRect = dropdownRef.current.getBoundingClientRect();
      const viewportHeight = window.innerHeight;
      if (dropdownRect.bottom + 150 > viewportHeight) {
        setDropdownPosition("up");
      } else {
        setDropdownPosition("down");
      }
    }
    setDropdownOpen(!dropdownOpen);
  };

  const getCurrencyName = (countryCurrency) => {
    if (countryCurrency) {
      const currencyCode = Object.keys(countryCurrency)[0];
      return countryCurrency[currencyCode].name;
    }
    return "No Currency";
  };

  const sortName = () => {
    setSortByName(!sortByName);
    setNameArrowDirection(!nameArrowDirection);
    props.handleSortName(sortByName);
  };

  const sortCapital = () => {
    setSortByCapital(!sortByCapital);
    setCapitalArrowDirection(!capitalArrowDirection);
    props.handleSortCapital(sortByCapital);
  };

  const toggleContinentDropdown = () => {
    setContinentDropdownOpen(!continentDropdownOpen);
    setContinentPlusOpen(!continentPlusOpen);
  };

  const toggleCurrencyDropdown = () => {
    setCurrencyDropdownOpen(!currencyDropdownOpen);
    setCurrencyPlusOpen(!currencyPlusOpen);
  };

  const togglePopulationDropdown = () => {
    setPopulationDropdownOpen(!populationDropdownOpen);
    setPopulationPlusOpen(!populationPlusOpen);
  };

  const handleContinentSelect = (continent) => {
    setSelectedContinent(continent);
    setContinentDropdownOpen(false);

    applyFilters(continent, selectedCurrency, minPopulation, maxPopulation);
    props.setFilterState((prevState) => ({
      ...prevState,
      continent: continent,
    }));
  };

  const handleCurrencySelect = (currency) => {
    setSelectedCurrency(currency);
    setCurrencyDropdownOpen(false);

    applyFilters(selectedContinent, currency, minPopulation, maxPopulation);

    props.setFilterState((prevState) => ({
      ...prevState,
      currency: currency,
    }));

    // console.log(props.filterState);
  };

  const applyPopulationFilter = () => {
    applyFilters(selectedContinent, selectedCurrency, minPopulation, maxPopulation);
    setPopulationDropdownOpen(false);

    props.setFilterState((prevState) => ({
      ...prevState,
      populationMin: minPopulation,
      populationMax: maxPopulation,
    }));
  };

  const resetPopulationFilter = () => {
    applyFilters(selectedContinent, selectedCurrency, 0, Infinity);
    setPopulationDropdownOpen(false);

    props.setFilterState((prevState) => ({
      ...prevState,
      populationMin: 0,
      populationMax: Infinity,
    }));

    setMinPopulation("");
    setMaxPopulation("");
  };

  const applyFilters = (continent, currency, minPopulation, maxPopulation) => {
    props.filterTableData(continent, currency, minPopulation, maxPopulation, props.inputValue);
  };

  const countryData = (country) => {
    props.getCountryData(country);
    props.openModal(true);
  };

  return (
    <section className="min-w-full table table-layout-fixed">
      <div className="w-full p-4">
        <div className="grid grid-cols-1 sm:grid-cols-3 md:grid-cols-6 gap-4 bg-gray-300 p-4 font-bold text-gray-700 text-center">
          <div className="cursor-pointer">Flag</div>
          <div className="cursor-pointer" onClick={sortName}>
            Name
            <FontAwesomeIcon icon={nameArrowDirection ? faArrowUp : faArrowDown} className="ml-2" />
          </div>
          <div className="cursor-pointer" onClick={sortCapital}>
            Capital
            <FontAwesomeIcon icon={capitalArrowDirection ? faArrowDown : faArrowUp} className="ml-2" />
          </div>

          <div className="relative cursor-pointer" ref={continentDropdownRef}>
            <div onClick={toggleContinentDropdown} className="flex justify-center">
              Continent
              <FontAwesomeIcon icon={continentPlusOpen ? faMinus : faPlus} className="ml-2 mt-1" />
            </div>
            {continentDropdownOpen && (
              <div className="absolute bg-white border border-gray-300 rounded mt-1 z-10 w-full">
                {continentsList.map((continent) =>
                  selectedContinent === continent ? (
                    <div key={continent} className="p-2 bg-gray-200 cursor-pointer text-center" onClick={() => handleContinentSelect(continent)}>
                      {continent}
                    </div>
                  ) : (
                    <div key={continent} className="p-2 hover:bg-gray-200 cursor-pointer text-center" onClick={() => handleContinentSelect(continent)}>
                      {continent}
                    </div>
                  )
                )}
              </div>
            )}
          </div>

          <div className="relative cursor-pointer" ref={currencyDropdownRef}>
            <div onClick={toggleCurrencyDropdown} className="flex justify-center">
              Currencies
              <FontAwesomeIcon icon={currencyPlusOpen ? faMinus : faPlus} className="ml-2 mt-1" />
            </div>
            {currencyDropdownOpen && (
              <div className="absolute bg-white border border-gray-300 rounded mt-1 z-10 w-full max-h-60 overflow-y-auto">
                {props.currencies.map((currency) =>
                  selectedCurrency === currency ? (
                    <div key={currency} className="p-2 bg-gray-200 cursor-pointer text-center" onClick={() => handleCurrencySelect(currency)}>
                      {currency}
                    </div>
                  ) : (
                    <div key={currency} className="p-2 hover:bg-gray-200 cursor-pointer text-center" onClick={() => handleCurrencySelect(currency)}>
                      {currency}
                    </div>
                  )
                )}
              </div>
            )}
          </div>

          <div className="relative cursor-pointer" ref={populationDropdownRef}>
            <div onClick={togglePopulationDropdown} className="flex justify-center">
              Population
              <FontAwesomeIcon icon={populationPlusOpen ? faMinus : faPlus} className="ml-2 mt-1" />
            </div>
            {populationDropdownOpen && (
              <div className="absolute bg-white border border-gray-300 rounded mt-1 z-10 w-full p-4">
                <div className="flex flex-col">
                  <label className="mb-2">Min Population:</label>
                  <input
                    type="number"
                    value={minPopulation}
                    onChange={(e) => setMinPopulation(e.target.value)}
                    className="border border-gray-300 p-1 rounded mb-4 w-full"
                    placeholder="Min Population"
                  />
                  <label className="mb-2">Max Population:</label>
                  <input
                    type="number"
                    value={maxPopulation}
                    onChange={(e) => setMaxPopulation(e.target.value)}
                    className="border border-gray-300 p-1 rounded w-full"
                    placeholder="Max Population"
                  />
                  <button onClick={applyPopulationFilter} className="bg-blue-600 text-white py-1 px-3 rounded mt-4 w-full">
                    Apply Filter
                  </button>
                  <button onClick={resetPopulationFilter} className="bg-blue-600 text-white py-1 px-3 rounded mt-4 w-full">
                    Reset Filter
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>

        {props.noMore !== false && (
          <>
            {getCurrentPageCountries().map((country) => (
              <div
                key={country.name.common}
                onClick={() => countryData(country)}
                className="grid grid-cols-1 sm:grid-cols-3 md:grid-cols-6 gap-4 p-4 bg-white shadow-md hover:bg-gray-100 border-b items-center cursor-pointer"
              >
                <div className="flex justify-center">
                  <img src={country.flags.png} alt={`${country.name.common} flag`} className="w-28 h-20 object-cover" />
                </div>
                <div className="text-center flex items-center justify-center">{country.name.common}</div>
                <div className="text-center flex items-center justify-center">{country.capital ? country.capital[0] : "No Capital"}</div>
                <div className="text-center flex items-center justify-center">{country.continents[0]}</div>
                <div className="text-center flex items-center justify-center">{getCurrencyName(country.currencies)}</div>
                <div className="text-center flex items-center justify-center">{country.population}</div>
              </div>
            ))}

            <div className="flex justify-between mt-4">
              <div>
                <label className="mr-2">Rows per page:</label>
                <div className="relative inline-block" ref={dropdownRef}>
                  <div onClick={toggleDropdown} className="border border-gray-300 rounded p-1 cursor-pointer">
                    {rowsPerPage}
                  </div>
                  {dropdownOpen && (
                    <div
                      className={`absolute bg-white border border-gray-300 rounded mt-1 z-10 ${
                        dropdownPosition === "up" ? "bottom-full mb-2" : "top-full mt-2"
                      }`}
                    >
                      {[10, 25, 100].map((option) => (
                        <div key={option} className="p-2 hover:bg-gray-200 cursor-pointer" onClick={() => handleRowsPerPageChange(option)}>
                          {option}
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>

              <div className="flex items-center">
                <button
                  onClick={handlePrevPage}
                  disabled={currentPage === 1}
                  className={`bg-blue-600 text-white py-1 px-3 rounded mr-2 ${currentPage === 1 ? "opacity-50 cursor-not-allowed" : "cursor-pointer"}`}
                >
                  Previous
                </button>
                <span>
                  Page <span>{currentPage}</span> of <span>{totalPages}</span>
                </span>
                <button
                  onClick={handleNextPage}
                  disabled={currentPage === totalPages}
                  className={`bg-blue-600 text-white py-1 px-3 rounded ml-2 ${currentPage === totalPages ? "opacity-50 cursor-not-allowed" : "cursor-pointer"}`}
                >
                  Next
                </button>
              </div>
            </div>
          </>
        )}

        {props.noMore === false && <div className="text-center p-4 font-bold text-red-500">No more! Please, change the filters!</div>}
      </div>
    </section>
  );
}

export default Table;
