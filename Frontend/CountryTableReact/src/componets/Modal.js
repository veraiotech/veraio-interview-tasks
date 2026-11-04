import React from 'react'

function Modal(props) {

  if (!props.countryData || !props.countryData.name || !props.countryData.flags) {
    return null;
  }

  const getCurrencyName = (countryCurrency) => {
    if (countryCurrency) {
      const currencyCode = Object.keys(countryCurrency)[0];
      return countryCurrency[currencyCode].name;
    }
    return "No Currency";
  };

  const handleOutsideClick = (e) => {
    if (e.target.id === 'modal') props.closeModal();
    // console.log(props.countryData);
  };

  return (
    <div
      id='modal'
      onClick={handleOutsideClick}
      className={`fixed inset-0 flex items-center justify-center bg-black bg-opacity-50 ${props.modalStatus ? "" : "hidden"}`}>
      <div className="bg-white rounded-lg p-6 w-11/12 md:w-1/2 lg:w-1/3 flex flex-col items-center">
        <button onClick={props.closeModal} className="absolute top-2 right-2 text-gray-600 hover:text-gray-900 text-xl">✖</button>
        <h2 className="text-2xl font-bold mb-2 text-center">{props.countryData.name.common}</h2>
        <img className="w-32 h-auto mb-4" src={props.countryData.flags.png} alt="Country Flag" />
        <p className="text-lg text-center"><strong>Capital:</strong> {props.countryData.capital ? props.countryData.capital[0] : "No Capital"}</p>
        <p className="text-lg text-center"><strong>Continent:</strong> {props.countryData.continents[0]}</p>
        <p className="text-lg text-center"><strong>Currency:</strong> {getCurrencyName(props.countryData.currencies)}</p>
        <p className="text-lg text-center"><strong>Population:</strong> {props.countryData.population}</p>
      </div>
    </div>
  )
}

export default Modal