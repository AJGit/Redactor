using System.Globalization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using RedactorApi.Analyzer;
using RedactorApi.Util;

namespace RedactorApi.FileScanners.Analyzers;

public class ExcelAnalyzer(IAnalyzer analyzer, ILogger<ExcelAnalyzer> logger) : ScannerBase(analyzer, logger)
{
    private readonly ILogger<ExcelAnalyzer> _logger = logger;

    internal override string SupportedExtension => ".xlsx";

    internal async override IAsyncEnumerable<PageInfo> ExtractTextAsync(Stream xlsxStream)
    {
        using var _ = LogUtils.Create(_logger, nameof(ExcelAnalyzer), nameof(ExtractTextAsync));
        var sheetCounter = 0;

        using var document = SpreadsheetDocument.Open(xlsxStream, false);
        // Get the SharedStringTable if it exists
        var sharedStringTablePart = document.WorkbookPart?.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
        var sharedStrings = sharedStringTablePart?.SharedStringTable.Elements<SharedStringItem>().ToArray() ?? [];

        if (document.WorkbookPart?.WorksheetParts is not { } worksheetParts)
        {
            yield break;
        }

        foreach (var worksheetPart in worksheetParts)
        {
            var sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();
            if (sheetData == null)
            {
                continue;
            }

            sheetCounter++;
            var sb = StringBuilderCache.Acquire();

            foreach (var row in sheetData.Elements<Row>())
            {
                foreach (var cell in row.Elements<Cell>())
                {
                    var cellValue = GetCellValue(cell, sharedStrings, document.WorkbookPart);
                    if (!string.IsNullOrWhiteSpace(cellValue))
                    {
                        sb.AppendLine(cellValue);
                    }
                }
            }
            yield return await ValueTask.FromResult(new PageInfo(sheetCounter, sb.GetStringAndRelease()));
        }
    }

    private static string GetCellValue(Cell? cell, SharedStringItem[]? sharedStrings, WorkbookPart workbookPart)
    {
        var cellValue = cell?.CellValue?.InnerText;
        if (cell == null || string.IsNullOrWhiteSpace(cellValue))
        {
            return string.Empty;
        }

        cellValue = GetCellValueIfShared(cell, sharedStrings, cellValue);

        cellValue = GetCellValueUsingStyle(cell, workbookPart, cellValue);

        return cellValue;
    }

    private static string GetCellValueUsingStyle(Cell cell, WorkbookPart workbookPart, string cellValue)
    {
        // Get the styles part
        var stylesPart = workbookPart.WorkbookStylesPart;
        var styleIndex = cell.StyleIndex;
        if (styleIndex != null && stylesPart != null)
        {
            var cellFormat = stylesPart.Stylesheet.CellFormats?.ElementAt((int)styleIndex.Value) as CellFormat;
            var numberFormatId = cellFormat?.NumberFormatId?.Value;
            if (numberFormatId != null)
            {
                var isDate = IsDateFormat(numberFormatId.Value, stylesPart);

                if (isDate && double.TryParse(cellValue, out var numericDate))
                {
                    var dateValue = new DateTime(1899, 12, 30).AddDays(numericDate);
                    cellValue = dateValue.ToUniversalTime().ToString(CultureInfo.InvariantCulture); //"o"
                }
            }
        }

        return cellValue;
    }

    private static string GetCellValueIfShared(Cell cell, SharedStringItem[]? sharedStrings, string cellValue)
    {
        // If it's a shared string type, we need to look it up from the shared strings array
        if (cell.DataType?.Value == CellValues.SharedString && sharedStrings != null)
        {
            if (int.TryParse(cellValue, out var index) && index >= 0 && index < sharedStrings.Length)
            {
                cellValue = sharedStrings[index].InnerText;
            }
        }

        return cellValue;
    }

    private static bool IsDateFormat(uint numberFormatId, WorkbookStylesPart stylesPart)
    {
        // Built-in date formats check
        var knownDateFormatIds = new uint[] { 14, 15, 16, 17, 18, 19, 20, 21, 22, 45, 46, 47 };
        return knownDateFormatIds.Contains(numberFormatId) ||
               CustomFormat(numberFormatId, stylesPart);
    }

    private static bool CustomFormat(uint numberFormatId, WorkbookStylesPart stylesPart)
    {
        // Check custom formats in NumberingFormats section if any
        var numberingFormats = stylesPart.Stylesheet.NumberingFormats;
        if (numberingFormats != null)
        {
            var format = numberingFormats.Elements<NumberingFormat>()
                .FirstOrDefault(nf => nf.NumberFormatId?.Value == numberFormatId);
            return format != null && LooksLikeADate(format);
        }

        return false;
    }

    private static bool LooksLikeADate(NumberingFormat format)
    {
        // Check if format code contains typical date/time chars
        var formatCode = format.FormatCode?.Value?.ToLower();
        if (formatCode != null)
        {
            if (formatCode.Contains("m") || 
                formatCode.Contains("d") || 
                formatCode.Contains("y") ||
                formatCode.Contains("h") || 
                formatCode.Contains("s"))
            {
                return true;
            }
        }

        return false;
    }
}
