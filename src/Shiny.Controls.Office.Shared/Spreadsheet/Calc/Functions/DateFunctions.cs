namespace Shiny.Controls.Office.Spreadsheet.Calc;

static class DateFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        registry.Add("TODAY", 0, 0, a => CalcValue.From(Math.Floor(ExcelDate.FromDateTime(a.Context.Now.Date))));
        registry.Add("NOW", 0, 0, a => CalcValue.From(ExcelDate.FromDateTime(a.Context.Now)));

        registry.Add("DATE", 3, 3, a =>
        {
            try
            {
                return CalcValue.From(ExcelDate.FromParts(a.Integer(0), a.Integer(1), a.Integer(2)));
            }
            catch (ArgumentOutOfRangeException)
            {
                return CalcValue.Error(CellError.Num);
            }
        });

        registry.Add("TIME", 3, 3, a =>
        {
            var hours = a.Integer(0);
            var minutes = a.Integer(1);
            var seconds = a.Integer(2);
            var total = (hours * 3600d + minutes * 60d + seconds) / 86400d;

            // TIME wraps at a day rather than overflowing into a date.
            return CalcValue.From(total - Math.Floor(total));
        });

        registry.Add("YEAR", 1, 1, a => Part(a, DatePart.Year));
        registry.Add("MONTH", 1, 1, a => Part(a, DatePart.Month));
        registry.Add("DAY", 1, 1, a => Part(a, DatePart.Day));
        registry.Add("HOUR", 1, 1, a => TimePart(a, x => x.Hours));
        registry.Add("MINUTE", 1, 1, a => TimePart(a, x => x.Minutes));
        registry.Add("SECOND", 1, 1, a => TimePart(a, x => x.Seconds));

        registry.Add("WEEKDAY", 1, 2, a =>
        {
            if (!ExcelDate.TryToDateTime(a.Number(0), out var date))
                return CalcValue.Error(CellError.Num);

            var day = (int)date.DayOfWeek; // Sunday = 0
            return CalcValue.From((double)(a.IntegerOrDefault(1, 1) switch
            {
                1 => day + 1,      // Sunday = 1
                2 => (day + 6) % 7 + 1, // Monday = 1
                3 => (day + 6) % 7,     // Monday = 0
                _ => day + 1
            }));
        });

        registry.Add("EOMONTH", 2, 2, a =>
        {
            if (!ExcelDate.TryToDateTime(a.Number(0), out var date))
                return CalcValue.Error(CellError.Num);

            try
            {
                var shifted = date.Date.AddMonths(a.Integer(1));
                var lastDay = new DateTime(shifted.Year, shifted.Month, DateTime.DaysInMonth(shifted.Year, shifted.Month));
                return CalcValue.From(Math.Floor(ExcelDate.FromDateTime(lastDay)));
            }
            catch (ArgumentOutOfRangeException)
            {
                return CalcValue.Error(CellError.Num);
            }
        });

        registry.Add("EDATE", 2, 2, a =>
        {
            if (!ExcelDate.TryToDateTime(a.Number(0), out var date))
                return CalcValue.Error(CellError.Num);

            try
            {
                return CalcValue.From(Math.Floor(ExcelDate.FromDateTime(date.Date.AddMonths(a.Integer(1)))));
            }
            catch (ArgumentOutOfRangeException)
            {
                return CalcValue.Error(CellError.Num);
            }
        });

        registry.Add("DAYS", 2, 2, a => CalcValue.From(Math.Floor(a.Number(0)) - Math.Floor(a.Number(1))));

        registry.Add("DATEVALUE", 1, 1, a =>
            ExcelDate.TryParse(a.Text(0), out var serial)
                ? CalcValue.From(Math.Floor(serial))
                : CalcValue.Error(CellError.Value));
    }

    enum DatePart { Year, Month, Day }

    static CalcValue Part(CalcArguments a, DatePart part)
    {
        var serial = a.Number(0);
        if (!ExcelDate.TryToDateTime(serial, out var date))
            return CalcValue.Error(CellError.Num);

        // Serial 60 is Excel's phantom 29 February 1900. Report the date the spreadsheet believes in
        // rather than the real one it maps onto, or DAY(60) disagrees with what the cell displays.
        if (Math.Floor(serial) == ExcelDate.PhantomLeapDaySerial)
        {
            return CalcValue.From((double)(part switch
            {
                DatePart.Year => 1900,
                DatePart.Month => 2,
                _ => 29
            }));
        }

        return CalcValue.From((double)(part switch
        {
            DatePart.Year => date.Year,
            DatePart.Month => date.Month,
            _ => date.Day
        }));
    }

    static CalcValue TimePart(CalcArguments a, Func<TimeSpan, int> selector)
    {
        var serial = a.Number(0);
        if (serial < 0)
            return CalcValue.Error(CellError.Num);

        var fraction = serial - Math.Floor(serial);

        // Round to the nearest second before splitting, or 0.5 comes out as 11:59:59.
        var seconds = Math.Round(fraction * 86400d, MidpointRounding.AwayFromZero);
        return CalcValue.From((double)selector(TimeSpan.FromSeconds(seconds)));
    }
}
