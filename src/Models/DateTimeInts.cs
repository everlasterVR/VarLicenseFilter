using System;

struct DateTimeInts
{
    public int Day { get; }
    public int Month { get; }
    public int Year { get; }

    public DateTimeInts(DateTime date)
    {
        Day = date.Day;
        Month = date.Month;
        Year = date.Year;
    }
}
