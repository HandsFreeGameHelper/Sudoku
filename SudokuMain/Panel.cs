using System.Drawing;

namespace SudokuGame.SudokuMain
{

    public class Panel
    {
        public int Value { get; set; }
        public Point Point { get; set; }
        public bool Replaceable { get; set; } = true;
    }
}
