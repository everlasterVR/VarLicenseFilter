using System;

struct DateTimeInts
{
    public int day { get; }
    public int month { get; }
    public int year { get; }

    public DateTimeInts(DateTime date)
    {
        day = date.Day;
        month = date.Month;
        year = date.Year;
    }
}
