using System.Text.RegularExpressions;

namespace Ovn9.Garage3.Common;

// Samlar valideringsfunktioner för appens indata, bland annat registreringsnummer
public static class Validator
{
    // Mönstret kräver exakt tre bokstäver följda av tre siffror, utan extra tecken före eller efter
    public static bool IsValidRegistrationNumber(string registrationNumber) => Regex.IsMatch(registrationNumber, @"\A[A-Za-z]{3}[0-9]{3}\z");
}