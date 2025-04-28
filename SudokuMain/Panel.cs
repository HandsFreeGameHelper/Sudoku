using System.Drawing;

namespace SudokuDotNetCore.SudokuMain
{

    public class Panel
    {
        public int Value { get; set; }
        public Point PanelPoint { get; set; }
        public bool Replaceable { get; set; } = true;
    }
}
