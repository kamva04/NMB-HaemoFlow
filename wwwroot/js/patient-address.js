(() => {
    const province = document.getElementById('Province');
    const town = document.getElementById('Town');
    if (!province || !town) return;

    const townsByProvince = {
        'Eastern Cape': ['Cradock', 'Despatch', 'East London', 'Gqeberha', 'Grahamstown (Makhanda)', 'Humansdorp', 'Jeffreys Bay', 'King William’s Town (Qonce)', 'Komani', 'Mdantsane', 'Mthatha', 'Port Alfred', 'Port Elizabeth', 'Queenstown', 'Uitenhage (Kariega)'],
        'Free State': ['Bethlehem', 'Bloemfontein', 'Harrismith', 'Kroonstad', 'Parys', 'Sasolburg', 'Welkom'],
        'Gauteng': ['Alberton', 'Benoni', 'Boksburg', 'Centurion', 'Germiston', 'Johannesburg', 'Kempton Park', 'Krugersdorp', 'Pretoria', 'Randburg', 'Roodepoort', 'Soweto', 'Vanderbijlpark'],
        'KwaZulu-Natal': ['Durban', 'Empangeni', 'Howick', 'Ladysmith', 'Newcastle', 'Pietermaritzburg', 'Pinetown', 'Port Shepstone', 'Richards Bay', 'Ulundi'],
        'Limpopo': ['Giyani', 'Lephalale', 'Louis Trichardt (Makhado)', 'Mokopane', 'Polokwane', 'Tzaneen', 'Thohoyandou'],
        'Mpumalanga': ['Barberton', 'Bethal', 'Ermelo', 'Mbombela', 'Middelburg', 'Secunda', 'Standerton', 'Witbank (Emalahleni)'],
        'North West': ['Brits', 'Klerksdorp', 'Mahikeng', 'Potchefstroom', 'Rustenburg', 'Vryburg'],
        'Northern Cape': ['De Aar', 'Kimberley', 'Kuruman', 'Springbok', 'Upington'],
        'Western Cape': ['Cape Town', 'George', 'Hermanus', 'Mossel Bay', 'Oudtshoorn', 'Paarl', 'Stellenbosch', 'Swellendam', 'Worcester']
    };

    const selectedTown = town.dataset.selectedTown || town.value;
    const populateTowns = (preserveSelection) => {
        const previousTown = preserveSelection ? (town.value || selectedTown) : '';
        const availableTowns = townsByProvince[province.value] || [];
        town.replaceChildren(new Option(
            province.value ? 'Select town' : 'Select a province first',
            ''
        ));

        availableTowns.forEach(name => town.add(new Option(name, name)));
        if (previousTown && !availableTowns.includes(previousTown)) {
            town.add(new Option(previousTown, previousTown));
        }

        town.disabled = !province.value;
        town.value = previousTown || '';
    };

    province.addEventListener('change', () => populateTowns(false));
    populateTowns(true);
})();
