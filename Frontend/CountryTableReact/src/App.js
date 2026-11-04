import React, { useState, useEffect } from "react";
import _ from "lodash";
import SearchBar from "./componets/SearchBar";
import Table from "./componets/Table";
import Modal from "./componets/Modal";

function App() {
  const [countries, setCountries] = useState([]);
  const [displayedCountries, setDisplayedCountries] = useState([]);
  const [modalData, setModalData] = useState({});
  const [modalOpened, setModalOpened] = useState(false);
  const [currencies, setCurrencies] = useState([]);
  const [newFilter, setNewFilter] = useState(false);

  const [filterState, setFilterState] = useState({
    continent: "ALL CONTINENTS",
    currency: "ALL CURRENCIES",
    populationMin: 0,
    populationMax: Infinity,
    inputValue: "",
  });

  useEffect(() => {
    fetch("https://restcountries.com/v3.1/all")
      .then((response) => response.json())
      .then((data) => {
        const sortedCountries = _.sortBy(data, "name.common");
        const uniqueCurrencies = ["ALL CURRENCIES"];

        sortedCountries.forEach((country) => {
          if (country.currencies) {
            const currencyCodes = Object.keys(country.currencies);

            currencyCodes.forEach((code) => {
              if (!uniqueCurrencies.includes(code)) {
                uniqueCurrencies.push(code);
              }
            });

            // console.log(currencyNames[0]);
          }
        });

        setCurrencies(uniqueCurrencies);
        setCountries(sortedCountries);
        setDisplayedCountries(sortedCountries);
      })
      .catch((error) => console.log(error));
  }, []);

  const updateInputValue = (newInputValue) => {
    setFilterState((prevState) => ({
      ...prevState,
      inputValue: newInputValue,
    }));

    filterTableData(filterState.continent, filterState.currency, filterState.populationMin, filterState.populationMax, newInputValue);
  };

  const handleSortName = (sortDirection) => {
    if (!sortDirection) {
      const sortedCountries = _.sortBy(displayedCountries, "name.common");
      setDisplayedCountries(sortedCountries);
    } else {
      const sortedCountries = _.reverse(_.sortBy(displayedCountries, "name.common"));
      setDisplayedCountries(sortedCountries);
    }
  };

  const handleSortCapital = (sortDirection) => {
    if (!sortDirection) {
      const sortedCountries = _.sortBy(displayedCountries, "capital[0]");
      setDisplayedCountries(sortedCountries);
    } else {
      const sortedCountries = _.reverse(_.sortBy(displayedCountries, "capital[0]"));
      setDisplayedCountries(sortedCountries);
    }
  };

  const filterTableData = (continent, currency, populationMin, populationMax, inputValue) => {
    const searchValue = inputValue ? inputValue.toLowerCase() : "";
    // console.log(inputValue);

    const filteredCountries = _.filter(countries, (country) => {
      const countryName = country.name.common.toLowerCase();
      const capitalName = country.capital && country.capital[0] ? country.capital[0].toLowerCase() : "";

      const inContinent = continent === "ALL CONTINENTS" || (country.continents && country.continents[0].includes(continent));

      const usesCurrency = !currency || currency === "ALL CURRENCIES" || (country.currencies && Object.keys(country.currencies).includes(currency));

      const withinPopulationRange = (!populationMin || country.population >= populationMin) && (!populationMax || country.population <= populationMax);

      const searchFilter = countryName.includes(searchValue) || capitalName.includes(searchValue);

      return inContinent && usesCurrency && withinPopulationRange && searchFilter;
    });

    setDisplayedCountries(filteredCountries);
    console.log(displayedCountries.length);

    setNewFilter(true);
  };

  const closeModal = () => {
    setModalOpened(false);
  };

  return (
    <div className="bg-gray-50">
      <header className="bg-blue-600 text-white py-6 shadow-lg">
        <h1 className="text-4xl font-extrabold text-center tracking-wider">Search Country</h1>
      </header>

      <main className="container mx-auto mt-6 p-4">
        <SearchBar findCountry={updateInputValue} />

        <Table
          countries={displayedCountries}
          getCountryData={setModalData}
          openModal={setModalOpened}
          handleSortName={handleSortName}
          handleSortCapital={handleSortCapital}
          filterTableData={filterTableData}
          filterState={filterState}
          setFilterState={setFilterState}
          currencies={currencies}
          newFilter={newFilter}
          setNewFilter={setNewFilter}
          inputValue={filterState.inputValue}
          noMore={displayedCountries.length !== 0 ? true : false}
        />

        <Modal modalStatus={modalOpened} closeModal={closeModal} countryData={modalData} />
      </main>
    </div>
  );
}

export default App;
