using System;

struct DateTimeInts
{
    public readonly int day;
    public readonly int month;
    public readonly int year;

    public DateTimeInts(DateTime date)
    {
        day = date.Day;
        month = date.Month;
        year = date.Year;
    }
}
