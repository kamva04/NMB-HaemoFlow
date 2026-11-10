using System.Globalization;

namespace NMB_HLabSys.Helpers
{
    public static class SouthAfricanIdNumber
    {
        public static bool TryGetDateOfBirth(string? idNumber, out DateTime dateOfBirth)
        {
            dateOfBirth = default;

            if (string.IsNullOrWhiteSpace(idNumber) ||
                idNumber.Length != 13 ||
                !idNumber.All(char.IsDigit) ||
                !int.TryParse(idNumber[..6], NumberStyles.None, CultureInfo.InvariantCulture, out var datePart))
            {
                return false;
            }

            var year = datePart / 10000;
            var month = (datePart / 100) % 100;
            var day = datePart % 100;
            var currentTwoDigitYear = DateTime.Today.Year % 100;
            var century = year <= currentTwoDigitYear ? DateTime.Today.Year / 100 : DateTime.Today.Year / 100 - 1;

            try
            {
                dateOfBirth = new DateTime(century * 100 + year, month, day);
                return dateOfBirth <= DateTime.Today;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }
    }
}
