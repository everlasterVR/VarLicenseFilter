public class SecondaryLicenseInfo
{
    public License License { get; set; }

    string _activeAfterDayString;
    string _activeAfterMonthString;
    string _activeAfterYearString;

    public int ActiveAfterDay { get; private set; }
    public int ActiveAfterMonth { get; private set; }
    public int ActiveAfterYear { get; private set; }

    public string ActiveAfterDayString
    {
        set
        {
            _activeAfterDayString = value;
            int dayInt;
            ActiveAfterDay = int.TryParse(value, out dayInt) ? dayInt : -1;
        }
    }

    public string ActiveAfterMonthString
    {
        set
        {
            _activeAfterMonthString = value;
            ActiveAfterMonth = MonthStringToMonthInt(value);
        }
    }

    public string ActiveAfterYearString
    {
        set
        {
            _activeAfterYearString = value;
            int yearInt;
            ActiveAfterYear = int.TryParse(value, out yearInt) ? yearInt : -1;
        }
    }

    public bool ActiveAfterDateIsValidDate()
    {
        return ActiveAfterDay != -1 && ActiveAfterMonth != -1 && ActiveAfterYear != -1;
    }

    public string GetActiveAfterDateString()
    {
        return $"{_activeAfterDayString} {_activeAfterMonthString} {_activeAfterYearString}";
    }

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
