## Description

Code challenge for interactive react table view. User can see grid view (table) off all countries (paged and with sort). Above the grid there is search bar from where the user can search for particular country. Both search bar and table row provides details views by clicking on each of them.

## Requirements

- Set up a basic React.js application (there is no limits, you can use create-react-app, react-boilerplate or custom set up)

- Create a service which use a backend API provided by

  - API1. All countries - GET `https://restcountries.com/v3.1/all`
  - API2. Search by name - GET `https://restcontries.com/rest/v3.1/name/:nameOfCountry`
  - More info about the API: https://restcountries.com

- Implement your own reusable component for grid view (table) which accept config object for columns definition. Provide sorting of columns and paging.

- Present in a responsive grid view (table) the results of API1 with columns (flag, name, capital, continents, currencies, cca3, population).

- Implement details view (modal) with the same fields present in the table. Show that details view when the user presses and holds left mouse button over table’s row for several seconds. Implement small countdown (or progress) indicator so that the user can see how long he should keep pressing until the details view is shown.

- Implement an autocomplete filter box for the countries using API2 (on each symbol entered in the filter box a suggestions dropdown with up to 10 items is displayed).

- By clicking on any suggestion from the search bar a details view (modal) is opened as in 3. from above.

## After complete task

When you are done with the implementation please make a new branch (checkout it from this one with feature/ prefix name) and push the code there.
We use [gitflow](https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow) to support clean and simple repos.
