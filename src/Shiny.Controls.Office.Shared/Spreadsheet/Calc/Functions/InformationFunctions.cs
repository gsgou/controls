namespace Shiny.Controls.Office.Spreadsheet.Calc;

static class InformationFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        // The IS* family must not propagate errors — inspecting an error is the entire point.
        registry.Add("ISERROR", 1, 1, a => CalcValue.From(a.Scalar(0).IsError));

        registry.Add("ISERR", 1, 1, a =>
        {
            var value = a.Scalar(0);
            return CalcValue.From(value.IsError && value.AsError() != CellError.NotAvailable);
        });

        registry.Add("ISNA", 1, 1, a =>
        {
            var value = a.Scalar(0);
            return CalcValue.From(value.IsError && value.AsError() == CellError.NotAvailable);
        });

        registry.Add("ISBLANK", 1, 1, a => CalcValue.From(a.Scalar(0).IsBlank));
        registry.Add("ISNUMBER", 1, 1, a => CalcValue.From(a.Scalar(0).Kind == CellValueKind.Number));
        registry.Add("ISTEXT", 1, 1, a => CalcValue.From(a.Scalar(0).Kind == CellValueKind.Text));
        registry.Add("ISNONTEXT", 1, 1, a => CalcValue.From(a.Scalar(0).Kind != CellValueKind.Text));
        registry.Add("ISLOGICAL", 1, 1, a => CalcValue.From(a.Scalar(0).Kind == CellValueKind.Boolean));

        registry.Add("ISEVEN", 1, 1, a => CalcValue.From(Math.Abs(Math.Truncate(a.Number(0)) % 2) < double.Epsilon));
        registry.Add("ISODD", 1, 1, a => CalcValue.From(Math.Abs(Math.Truncate(a.Number(0)) % 2) > double.Epsilon));

        registry.Add("NA", 0, 0, _ => CalcValue.Error(CellError.NotAvailable));

        registry.Add("N", 1, 1, a =>
        {
            var value = a.Scalar(0);
            return value.Kind switch
            {
                CellValueKind.Number => CalcValue.From(value.AsNumber()),
                CellValueKind.Boolean => CalcValue.From(value.AsBoolean() ? 1d : 0d),
                CellValueKind.Error => CalcValue.From(value),
                _ => CalcValue.From(0d)
            };
        });

        registry.Add("TYPE", 1, 1, a =>
        {
            var value = a.Scalar(0);
            return CalcValue.From((double)(value.Kind switch
            {
                CellValueKind.Number or CellValueKind.Blank => 1,
                CellValueKind.Text => 2,
                CellValueKind.Boolean => 4,
                CellValueKind.Error => 16,
                _ => 1
            }));
        });

        registry.Add("ERROR.TYPE", 1, 1, a =>
        {
            var value = a.Scalar(0);
            if (!value.IsError)
                return CalcValue.Error(CellError.NotAvailable);

            return CalcValue.From((double)(value.AsError() switch
            {
                CellError.Null => 1,
                CellError.Div0 => 2,
                CellError.Value => 3,
                CellError.Ref => 4,
                CellError.Name => 5,
                CellError.Num => 6,
                _ => 7
            }));
        });
    }
}
