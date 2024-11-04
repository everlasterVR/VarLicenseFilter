public class SecondaryLicenseInfo
{
    public License license;

    string _activeAfterDayString;
    string _activeAfterMonthString;
    string _activeAfterYearString;

    public int activeAfterDay { get; private set; }
    public int activeAfterMonth { get; private set; }
    public int activeAfterYear { get; private set; }

    public string activeAfterDayString
    {
        set
        {
            _activeAfterDayString = value;
            int dayInt;
            activeAfterDay = int.TryParse(value, out dayInt) ? dayInt : -1;
        }
    }

    public string activeAfterMonthString
    {
        set
        {
            _activeAfterMonthString = value;
            activeAfterMonth = MonthStringToMonthInt(value);
        }
    }

    public string activeAfterYearString
    {
        set
        {
            _activeAfterYearString = value;
            int yearInt;
            activeAfterYear = int.TryParse(value, out yearInt) ? yearInt : -1;
        }
    }

    public bool ActiveAfterDateIsValidDate() =>
        activeAfterDay != -1 && activeAfterMonth != -1 && activeAfterYear != -1;

    public string GetActiveAfterDateString() =>
        $"{_activeAfterDayString} {_activeAfterMonthString} {_activeAfterYearString}";

    static int MonthStringToMonthInt(string monthString)
    {
        switch(monthString.ToLower())
        {
            case "jan":
                return 1;
            case "feb":
                return 2;
            case "mar":
                return 3;
            case "apr":
                return 4;
            case "may":
                return 5;
            case "jun":
                return 6;
            case "jul":
                return 7;
            case "aug":
                return 8;
            case "sep":
                return 9;
            case "oct":
                return 10;
            case "nov":
                return 11;
            case "dec":
                return 12;
            default:
                return -1;
        }
    }
}
