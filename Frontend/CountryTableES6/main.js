const apiData = "https://restcountries.com/v3.1/all";

var formInfo = document.getElementById('inputText');
var visibleSection = document.getElementById('apiResultSection');
var table = document.getElementById('results');
var noFindsTxt = document.getElementById('notFind');

var rowsPerPageSelect = document.getElementById('rowsPerPage');
var prevPageButton = document.getElementById('prevPage');
var nextPageButton = document.getElementById('nextPage');
var currentPageSpan = document.getElementById('currentPage');
var totalPagesSpan = document.getElementById('totalPages');

var modal = document.getElementById('modal');
var closeModalButton = document.getElementById('closeModal');
var modalCountryName = document.getElementById('modalCountryName');
var modalCountryFlag = document.getElementById('modalCountryFlag');
var modalCountryCapital = document.getElementById('modalCountryCapital');
var modalCountryContinent = document.getElementById('modalCountryContinent');
var modalCountryCurrency = document.getElementById('modalCountryCurrency');
var modalCountryCCA = document.getElementById('modalCountryCCA');
var modalCountryPopulation = document.getElementById('modalCountryPopulation');

var sortByName = document.getElementById('sortByName');
var sortByCapital = document.getElementById('sortByCapital');
var sortByContinent = document.getElementById('sortByContinent');
var sortByCurrency = document.getElementById('sortByCurrency');
var sortByCCA = document.getElementById('sortByCCA');
var sortByPopulation = document.getElementById('sortByPopulation');
var sortByDefault = document.getElementById('sortByDefault');

var results = [];
var displayedResults = [];
var sortDirections = {
    name: false,
    capital: false,
    continent: false,
    currency: false,
    cca: false,
    population: false,
};
var currentPage = 1;
var rowsPerPage = 10;

formInfo.addEventListener("input", e => {
    const value = e.target.value.toLowerCase();
    displayedResults = results.filter(result => 
        result.name.toLowerCase().includes(value)
    );
    renderTable();
    noFindsTxt.classList.toggle("hide", displayedResults.length !== 0);
});

rowsPerPageSelect.addEventListener('change', e => {
    rowsPerPage = parseInt(e.target.value);
    currentPage = 1;
    renderTable();
});

prevPageButton.addEventListener('click', () => {
    if (currentPage > 1) {
        currentPage--;
        renderTable();
    }
});

nextPageButton.addEventListener('click', () => {
    if (currentPage < Math.ceil(displayedResults.length / rowsPerPage)) {
        currentPage++;
        renderTable();
    }
});

closeModalButton.addEventListener('click', () => {
    modal.classList.add('hidden');
});

modal.addEventListener('click', (e) => {
    if (e.target === modal) {
        modal.classList.add('hidden');
    }
});

sortByName.addEventListener('click', () => sortTable('name'));
sortByCapital.addEventListener('click', () => sortTable('capital'));
sortByContinent.addEventListener('click', () => sortTable('continent'));
sortByCurrency.addEventListener('click', () => sortTable('currency'));
sortByCCA.addEventListener('click', () => sortTable('cca'));
sortByPopulation.addEventListener('click', () => sortTable('population'));
// sortByDefault.addEventListener('click', () => fetchData());

async function fetchData() {
    try {
        const response = await fetch(apiData);
        const data = await response.json();

        results = data.map(element => {
            const countryFlag = element.flags.png;
            const countryName = element.name.common;
            const countryCapital = element.capital ? element.capital[0] : "No Capital";
            const countryContinent = element.continents[0];
            const countryCCA = element.cca3;
            const countryPopulation = element.population;

            let countryCurrency = "No Currency";
            if (element.currencies) {
                const currencyCode = Object.keys(element.currencies)[0];
                countryCurrency = element.currencies[currencyCode].name;
            }

            return {
                name: countryName,
                capital: countryCapital,
                continent: countryContinent,
                currency: countryCurrency,
                cca: countryCCA,
                population: countryPopulation,
                flag: countryFlag
            };
        });

        displayedResults = [...results]; 
        renderTable();

    } catch (error) {
        console.log(error);
    }
}

function renderTable() {
    const start = (currentPage - 1) * rowsPerPage;
    const end = start + rowsPerPage;
    const paginatedResults = displayedResults.slice(start, end);

    table.innerHTML = '';
    paginatedResults.forEach(element => {
        const createTr = document.createElement('tr');
        createTr.className = "hover:bg-gray-100 cursor-pointer";
        createTr.innerHTML = `
            <td class="py-4 px-2 border-b"><img class="w-12 h-auto" src="${element.flag}" alt="Flag"/></td>
            <td class="py-4 px-2 border-b">${element.name}</td>
            <td class="py-4 px-2 border-b">${element.capital}</td>
            <td class="py-4 px-2 border-b">${element.continent}</td>
            <td class="py-4 px-2 border-b">${element.currency}</td>
            <td class="py-4 px-2 border-b">${element.cca}</td>
            <td class="py-4 px-2 border-b">${element.population}</td>
        `;

        createTr.addEventListener('click', () => {
            showModal(element);
        });

        table.appendChild(createTr);
    });

    updatePaginationControls();
}

function updatePaginationControls() {
    const totalPages = Math.ceil(displayedResults.length / rowsPerPage);
    totalPagesSpan.textContent = totalPages;

    currentPageSpan.textContent = currentPage;

    prevPageButton.disabled = currentPage === 1;
    nextPageButton.disabled = currentPage === totalPages || totalPages === 0;
}

function showModal(country) {
    modalCountryName.textContent = country.name;
    modalCountryFlag.src = country.flag;
    modalCountryCapital.textContent = country.capital;
    modalCountryContinent.textContent = country.continent;
    modalCountryCurrency.textContent = country.currency;
    modalCountryCCA.textContent = country.cca;
    modalCountryPopulation.textContent = country.population;
    modal.classList.remove('hidden');
}

function sortTable(criteria) {
    sortDirections[criteria] = !sortDirections[criteria];
    const direction = sortDirections[criteria] ? 1 : -1;

    document.querySelectorAll('.rotate').forEach(arrow => {
        arrow.classList.remove('asc');
    });

    const currentArrow = document.getElementById(`arrow${capitalizeFirstLetter(criteria)}`);
    if (currentArrow) {
        currentArrow.classList.toggle('asc', sortDirections[criteria]);
    }

    displayedResults.sort((a, b) => {
        if (criteria === 'population') {
            return direction * (a[criteria] - b[criteria]);
        } else {
            return direction * a[criteria].localeCompare(b[criteria]);
        }
    });

    renderTable();
}

function capitalizeFirstLetter(string) {
    return string.charAt(0).toUpperCase() + string.slice(1);
}

fetchData();
