import React from 'react'

function SearchBar(props) {

  const findCountry = (e) => {
    // console.log(e.target.value);
    props.findCountry(e.target.value);
  }
  return (
    <section className="formField mb-4">
      <input type="text" onChange={findCountry} placeholder="Name of country ..." className="w-full p-2 border border-gray-300 rounded" />
    </section>
  )
}

export default SearchBar