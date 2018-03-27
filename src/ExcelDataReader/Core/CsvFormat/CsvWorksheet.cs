using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ExcelDataReader.Core.NumberFormat;

namespace ExcelDataReader.Core.CsvFormat
{
    internal class CsvWorksheet : IWorksheet
    {
        public CsvWorksheet(Stream stream, Encoding fallbackEncoding, char[] autodetectSeparators)
        {
            Stream = stream;
            Stream.Seek(0, SeekOrigin.Begin);
            try
            {
                // Try as UTF-8 first, or use BOM if present
                int fieldCount = 0;
                char separator = default(char);
                Encoding encoding = null;
                int bomLength = 0;
                int rowCount = 0;
                CsvAnalyzer.Analyze(Stream, autodetectSeparators, Encoding.UTF8, out fieldCount, out separator, out encoding, out bomLength, out rowCount);
                FieldCount = fieldCount;
                RowCount = rowCount;
                Encoding = encoding;
                Separator = separator;
                BomLength = bomLength;
            }
            catch (DecoderFallbackException)
            {
                // If cannot parse as UTF-8, try fallback encoding
                Stream.Seek(0, SeekOrigin.Begin);

                int fieldCount = 0;
                char separator = default(char);
                Encoding encoding = null;
                int bomLength = 0;
                int rowCount = 0;
                CsvAnalyzer.Analyze(Stream, autodetectSeparators, fallbackEncoding, out fieldCount, out separator, out encoding, out bomLength, out rowCount);
                FieldCount = fieldCount;
                RowCount = rowCount;
                Encoding = encoding;
                Separator = separator;
                BomLength = bomLength;
            }
        }

        public string Name => string.Empty;

        public string CodeName => null;

        public string VisibleState => null;

        public HeaderFooter HeaderFooter => null;

        public CellRange[] MergeCells => null;

        public int FieldCount { get; }

        public int RowCount { get; }

        public Stream Stream { get; }

        public Encoding Encoding { get; }

        public char Separator { get; }

        private int BomLength { get; set; }

        public NumberFormatString GetNumberFormatString(int index)
        {
            return null;
        }

        public IEnumerable<Row> ReadRows()
        {
            var bufferSize = 1024;
            var buffer = new byte[bufferSize];
            var rowIndex = 0;
            var csv = new CsvParser(Separator, Encoding);
            var skipBomBytes = BomLength;

            Stream.Seek(0, SeekOrigin.Begin);
            while (Stream.Position < Stream.Length)
            {
                var bytesRead = Stream.Read(buffer, 0, bufferSize);
                List<List<string>> bufferRows;
                csv.ParseBuffer(buffer, skipBomBytes, bytesRead - skipBomBytes, out bufferRows);

                skipBomBytes = 0; // Only skip bom on first iteration

                foreach (var row in GetReaderRows(rowIndex, bufferRows))
                {
                    yield return row;
                }

                rowIndex += bufferRows.Count;
            }

            List<List<string>> flushRows;
            csv.Flush(out flushRows);
            foreach (var row in GetReaderRows(rowIndex, flushRows))
            {
                yield return row;
            }
        }

        private IEnumerable<Row> GetReaderRows(int rowIndex, List<List<string>> rows)
        {
            foreach (var row in rows)
            {
                var columnIndex = 0;

                var cells = new List<Cell>();
                foreach (var item in row)
                {
                    cells.Add(new Cell()
                    {
                        ColumnIndex = columnIndex,
                        Value = item
                    });

                    columnIndex++;
                }

                yield return new Row()
                {
                    Height = 12.75, // 255 twips
                    Cells = cells,
                    RowIndex = rowIndex
                };

                rowIndex++;
            }
        }
    }
}
