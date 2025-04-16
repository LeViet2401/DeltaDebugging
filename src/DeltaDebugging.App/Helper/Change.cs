using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaDebugging.App.Helper
{
    public class Change
    {
        public int LineNumber { get; set; }
        public string OriginalLine { get; set; }
        public string NewLine { get; set; }

        public Change(int lineNumber, string originalLine, string newLine)
        {
            LineNumber = lineNumber;
            OriginalLine = originalLine;
            NewLine = newLine;
        }

        public override string ToString() => $"Line {LineNumber}: '{OriginalLine}' -> '{NewLine}'";

        // Override Equals và GetHashCode để sử dụng trong HashSet
        public override bool Equals(object obj)
        {
            if (obj is Change other)
            {
                return LineNumber == other.LineNumber &&
                       OriginalLine == other.OriginalLine &&
                       NewLine == other.NewLine;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(LineNumber, OriginalLine, NewLine);
        }
    }
}
