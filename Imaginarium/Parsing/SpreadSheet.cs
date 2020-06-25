#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SpreadSheet.cs" company="Ian Horswill">
// Copyright (C) 2019, 2020 Ian Horswill
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in the
// Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
#endregion

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Imaginarium.Parsing
{
    public class Spreadsheet
    {
        public readonly string Path;
        public readonly object[][] Data;
        private readonly int idColumnIndex;

        public Spreadsheet(string path, string idColumnName)
        {
            Data = Read(path, ',');
            idColumnIndex = ColumnIndex(idColumnName);
            Path = path;
        }

        private int ColumnIndex(string columnName)
        {
            return System.Array.IndexOf(Data[0], columnName);
        }

        public string[] Header => Data[0].Cast<string>().ToArray();

        public object[] this[object key]
        {
            get => Data.First(row => row[idColumnIndex].Equals(key));
        }

        public object this[object key, string column]
        {
            get => this[key][ColumnIndex(column)];
            set => this[key][ColumnIndex(column)] = value;
        }

        /// <summary>
        /// Return the value of specified column in the row identified by key.
        /// If there is no row matching key, then return null.
        /// </summary>
        public object LookupOrNull(object key, string column)
        {
            var row = Data.FirstOrDefault(r => r[idColumnIndex].Equals(key));
            return row?[ColumnIndex(column)];
        }

        public bool ContainsKey(object key) => Data.FirstOrDefault(r => r[idColumnIndex].Equals(key)) != null;

        public static object[][] Read(string path, char delimiter)
        {
            using (TextReader r = File.OpenText(path))
            {
                StringBuilder b = new StringBuilder();
                List<object[]> allRows = new List<object[]>();
                List<object> currentRow = new List<object>();

                int peek = r.Peek();
                while (peek >= 0)
                {
                    if (peek == delimiter)
                    {
                        r.Read(); // Skip over delimiter
                        currentRow.Add(ReadItem(r, delimiter, b));
                    }
                    else if (peek == '\r' || peek == '\n')
                    {
                        // end of line - swallow cr and/or lf
                        r.Read();
                        if (peek == '\r')
                        {
                            // Swallow LF if CRLF
                            peek = r.Peek();
                            if (peek == '\n')
                                r.Read();
                        }

                        allRows.Add(currentRow.ToArray());
                        currentRow.Clear();
                    }
                    else
                        currentRow.Add(ReadItem(r, delimiter, b));

                    peek = r.Peek();
                }

                if (currentRow.Count > 0)
                    allRows.Add(currentRow.ToArray());
                // End of file
                return allRows.ToArray();
            }
        }

        static string ReadItem(TextReader input, char delimiter, StringBuilder b)
        {
            bool quoted = false;
            b.Clear();
            int peek = (char) input.Peek();
            if (peek == delimiter)
                return "";
            if (peek == '\"')
            {
                quoted = true;
                input.Read();
            }

            getNextChar:
            peek = input.Peek();
            if (peek < 0)
                goto done;
            if (quoted && peek == '\"')
            {
                input.Read(); // Swallow quote
                if ((char) input.Peek() == '\"')
                {
                    // It was an escaped quote
                    input.Read();
                    b.Append('\"');
                    goto getNextChar;
                }
                else
                {
                    // It was the end of the item
// ReSharper disable RedundantJumpStatement
                    goto done;
// ReSharper restore RedundantJumpStatement
                }
            }
            else if (!quoted && (peek == delimiter || peek == '\r' || peek == '\n'))
// ReSharper disable RedundantJumpStatement
                goto done;
// ReSharper restore RedundantJumpStatement
            else
            {
                b.Append((char) peek);
                input.Read();
                goto getNextChar;
            }

            //System.Diagnostics.Debug.Assert(false, "Line should not be reachable.");
            done:
            return b.ToString();
        }

        public static object[][] ConvertAllNumbers(object[][] spreadsheet)
        {
            for (int i = 0; i < spreadsheet.Length; i++)
            {
                object[] row = spreadsheet[i];
                for (int j = 0; j < row.Length; j++)
                {
                    string s = row[j] as string;
                    double parsed;
                    if (s != null && double.TryParse(s, out parsed))
                        row[j] = parsed;
                }
            }

            return spreadsheet;
        }

        public static object[][] TrimWhitespace(object[][] spreadsheet)
        {
            for (int i = 0; i < spreadsheet.Length; i++)
            {
                object[] row = spreadsheet[i];
                for (int j = 0; j < row.Length; j++)
                {
                    string s = row[j] as string;
                    if (s != null)
                        row[j] = s.Trim();
                }
            }

            return spreadsheet;
        }

        public static void Write(IList rows, string path, char delimiter)
        {
            List<IList> data = new List<IList>(rows.Count);
            foreach (IList l in rows)
                data.Add(l);

            StringBuilder b = new StringBuilder();
            File.WriteAllLines(path, data.Select((l, i) => Format(l, delimiter, b)));
        }

        static string Format(IList items, char delimiter, StringBuilder b)
        {
            b.Clear();
            bool firstOne = true;
            foreach (var item in items)
            {
                if (!firstOne)
                    b.Append(delimiter);
                else
                    firstOne = false;
                if (item is string)
                {
                    b.Append('\"');
                    b.Append(((string) item).Replace("\"", "\"\""));
                    b.Append('\"');
                }
                else
                    b.Append(item);
            }

            return b.ToString();
        }

        public void Save()
        {
            Write(Data, Path, ',');
        }
    }
}