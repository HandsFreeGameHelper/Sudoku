using System.Collections.Generic;
using System.Drawing;

namespace SudokuDotNetCore.SudokuMain
{
    public class UseableValue
    {
        public bool Replaceable { get; set; } = true;

        public int ResetCount { get; set; } = 0;

        public List<int> Values { get; set; } = new List<int>();

        public Point Point { get; set; } = new Point();

        public List<int> LastUsedItem { get; set; } = new List<int>();

    }
}
