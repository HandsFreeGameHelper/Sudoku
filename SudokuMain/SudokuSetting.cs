namespace SudokuDotNetCore.SudokuMain
{
    public class SudokuSetting
    {
        public int PanelSize = 9; // fixed Value
        public int LineHeight { get => PanelSize + 3; }
        public int PanelCountIndex { get; set; }
        public bool NeedResolve { get; set; } = false; // fixed Value
        public bool NeedReplace { get; set; } = false; // fixed Value
        public Level GameLevel { get; set; } = Level.Easy;
        public int ValidateValue { get => (1 + PanelSize) * PanelSize / 2; }
    }
}
