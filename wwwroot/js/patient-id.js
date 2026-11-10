(function () {
    function populateDateOfBirth() {
        var id = document.getElementById('SAIDNumber');
        var date = document.getElementById('DateOfBirth');
        if (!id || !date || !/^\d{6}/.test(id.value)) {
            return;
        }

        var year = parseInt(id.value.substring(0, 2), 10);
        var month = parseInt(id.value.substring(2, 4), 10);
        var day = parseInt(id.value.substring(4, 6), 10);
        var today = new Date();
        var currentTwoDigitYear = today.getFullYear() % 100;
        var fullYear = (year <= currentTwoDigitYear ? 2000 : 1900) + year;
        var candidate = new Date(fullYear, month - 1, day);

        if (candidate.getFullYear() === fullYear &&
            candidate.getMonth() === month - 1 &&
            candidate.getDate() === day &&
            candidate <= today) {
            date.value = fullYear + '-' +
                String(month).padStart(2, '0') + '-' +
                String(day).padStart(2, '0');
        }
    }

    var id = document.getElementById('SAIDNumber');
    if (id) {
        id.addEventListener('input', populateDateOfBirth);
        populateDateOfBirth();
    }
})();
