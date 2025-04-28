using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SudokuDotNetCore.SudokuMain
{
    public class TaskInfo
    {
        public Sudoku Sudoku { get; set; } = new Sudoku();
        public bool IsUniqueSolution { get; set; }
        public List<List<Panel>> ResolvedPanel { get; set; } = new List<List<Panel>>();
        public List<List<Panel>> RegionPanel { get; set; } = new List<List<Panel>>();
        public List<List<Panel>> ReplacedPanel { get; set; } = new List<List<Panel>>();

        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

        public Stopwatch AsyncTaskWatch { get; set; } = new Stopwatch();

        public bool IsFound { get; set; } = false;

        public int Index { get; set; }
        public int Count { get; set; }
        public int Level { get; set; }
        public double Time { get; set; }
        public double TimePerCount { get => this.Time / this.Count; }
        public double LevelPerCount { get => this.Level / this.Count; }
        public bool Reserve { get; set; }
        public bool? Midtoright { get; set; }
        public bool? Mix { get; set; }
        public bool? Skip { get; set; }
        public string Opreation { get => $"TASK{Index}"; }
    }
}
