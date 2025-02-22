using SudokuGame.SudokuMain;
using System.Diagnostics;
using System.Drawing;
using static SudokuGame.SudokuMain.Sudoku;

namespace SudokuGame
{
    public static class Utils
    {
        private static object _lock;
        public static int[,] TransformPanel(List<List<Panel>> panel)
        {
            var res = new int[panel.Count, panel.Count];
            for (int i = 0; i < panel.Count; i++)
            {
                for (int j = 0; j < panel[i].Count; j++)
                {
                    res[i, j] = panel[i][j].Value;
                }
            }
            return res;
        }

        public static List<List<Panel>> TransformPanel(int[,] panel)
        {
            var res = new List<List<Panel>>();
            for (int i = 0; i < panel.GetLength(0); i++)
            {
                var row = new List<Panel>();
                for (int j = 0; j < panel.GetLength(1); j++)
                {
                    var p = new Panel()
                    {
                        Value = panel[i,j],
                        Replaceable = panel[i,j] == 0,
                        Point = new Point(i, j)
                    };
                    row.Add(p);
                }
                res.Add(row);
            }
            return res;
        }

        public static List<int[,]> GeneratePanels(Level gameLevel, int count = 1)
        {
            var total = new Stopwatch();
            total.Start();
            var dic2 = new List<string>();
            var now = DateTime.Now.ToString("yyyyMMddHHmmss");
            string filePath = $"Sudokuku_output_{LevelToString(gameLevel)}_{now}.txt";
            var res = new List<int[,]>();
            var gameSetting = new SudokuSetting()
            {
                GameLevel = gameLevel,
            };
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                var i = 0;
                while (i < count)
                {
                    var cancellationTokenSource = new CancellationTokenSource();
                    var sudoku = new Sudoku(gameSetting);

                    var level = 0;
                    var panel = sudoku.Generate(cancellationTokenSource.Token);
                    var repanel = CopyTo(cancellationTokenSource.Token, sudoku.ReplacePanel(cancellationTokenSource.Token, panel));
                    var key = GenerateSudokuKey(repanel);
                    while (dic2.Contains(key) || !JudgeLevel(cancellationTokenSource.Token, repanel, out level, sudoku.SudokuGameSetting.GameLevel))
                    {
                        repanel = CopyTo(cancellationTokenSource.Token, sudoku.ReplacePanel(cancellationTokenSource.Token, panel));
                        key = GenerateSudokuKey(repanel);
                    }
                    dic2.Add(key);

                    var reason = string.Empty;
                    var score = EvaluatePanel.EvaluateSudokuDifficulty(TransformPanel(repanel), out reason);
                    writer.WriteLine("Score " + score + " Reason " + reason);
                    writer.WriteLine("Replaced Panel");
                    writer.WriteLog(repanel);
                    writer.WriteLine("");
                    i++;
                    sudoku.ConsoleLog(panel, 0);
                    sudoku.ConsoleLog(repanel, 1);
                    GC.Collect();
                    res.Add(TransformPanel(repanel));
                    //Console.ReadKey();
                }
                total.Stop();
                Console.SetCursorPosition(0, 0);
                writer.WriteLine("Total Hours: " + total.Elapsed.TotalHours);
                Console.WriteLine(total.Elapsed.TotalHours);
            }
            return res;
        }

        public static List<List<Panel>> GeneratePanel(Level gameLevel, int count = 1)
        {
            var total = new Stopwatch();
            total.Start();
            var dic2 = new List<string>();
            var now = DateTime.Now.ToString("yyyyMMddHHmmss");
            string filePath = $"Sudokuku_output_{LevelToString(gameLevel)}_{now}.txt";
            var res = new List<List<Panel>>();
            var gameSetting = new SudokuSetting()
            {
                GameLevel = gameLevel,
            };
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                var i = 0;
                while (i < count)
                {
                    var cancellationTokenSource = new CancellationTokenSource();
                    var sudoku = new Sudoku(gameSetting);

                    var level = 0;
                    var panel = sudoku.Generate(cancellationTokenSource.Token);
                    var repanel = CopyTo(cancellationTokenSource.Token, sudoku.ReplacePanel(cancellationTokenSource.Token, panel));
                    var key = GenerateSudokuKey(repanel);
                    while (dic2.Contains(key) || !JudgeLevel(cancellationTokenSource.Token, repanel, out level, sudoku.SudokuGameSetting.GameLevel))
                    {
                        repanel = CopyTo(cancellationTokenSource.Token, sudoku.ReplacePanel(cancellationTokenSource.Token, panel));
                        key = GenerateSudokuKey(repanel);
                    }
                    dic2.Add(key);

                    var reason = string.Empty;
                    var score = EvaluatePanel.EvaluateSudokuDifficulty(TransformPanel(repanel), out reason);
                    writer.WriteLine("Score " + score + " Reason " + reason);
                    writer.WriteLine("Replaced Panel");
                    writer.WriteLog(repanel);
                    writer.WriteLine("");
                    i++;
                    sudoku.ConsoleLog(panel, 0);
                    sudoku.ConsoleLog(repanel, 1);
                    GC.Collect();
                    res = repanel;
                    //Console.ReadKey();
                }
                total.Stop();
                Console.SetCursorPosition(0, 0);
                writer.WriteLine("Total Hours: " + total.Elapsed.TotalHours);
                Console.WriteLine(total.Elapsed.TotalHours);
            }
            return res;
        }

        public static async Task<int[,]> Resolve(List<List<Panel>> panel)
        {
            var res = new int[9, 9];
            var total = new Stopwatch();
            total.Start();
            var asyncTaskWatch = new Stopwatch();
            var dic1 = new Dictionary<string, TaskInfo>();
            var dic2 = new List<string>();
            var now = DateTime.Now.ToString("yyyyMMddHHmmss.fff");
            var gameLevel = Level.Master;
            string filePath = $"Sudokuku_output_Resolver_{now}.txt";
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                #region TaskInfo
                var taskInfos = new List<TaskInfo>();
                var taskInfo1 = new TaskInfo()
                {
                    AsyncTaskWatch = asyncTaskWatch,
                    Reserve = false,
                    Index = 1
                };
                var taskInfo2 = new TaskInfo()
                {
                    AsyncTaskWatch = asyncTaskWatch,
                    Reserve = true,
                    Index = 2
                };
                var taskInfo3 = new TaskInfo()
                {
                    AsyncTaskWatch = asyncTaskWatch,
                    Reserve = false,
                    Midtoright = true,
                    Index = 3
                };
                var taskInfo4 = new TaskInfo()
                {
                    AsyncTaskWatch = asyncTaskWatch,
                    Reserve = false,
                    Midtoright = false,
                    Index = 4
                };
                var taskInfo5 = new TaskInfo()
                {
                    AsyncTaskWatch = asyncTaskWatch,
                    Reserve = false,
                    Mix = true,
                    Index = 5
                };
                var taskInfo6 = new TaskInfo()
                {
                    AsyncTaskWatch = asyncTaskWatch,
                    Reserve = false,
                    Midtoright = true,
                    Mix = true,
                    Index = 6
                };
                var taskInfo7 = new TaskInfo()
                {
                    AsyncTaskWatch = asyncTaskWatch,
                    Reserve = false,
                    Skip = true,
                    Index = 7
                };
                var taskInfo8 = new TaskInfo()
                {
                    AsyncTaskWatch = asyncTaskWatch,
                    Reserve = true,
                    Skip = true,
                    Index = 8
                };
                var taskInfo9 = new TaskInfo()
                {
                    AsyncTaskWatch = asyncTaskWatch,
                    Reserve = false,
                    Midtoright = true,
                    Skip = true,
                    Index = 9
                };
                var taskInfo10 = new TaskInfo()
                {
                    AsyncTaskWatch = asyncTaskWatch,
                    Reserve = false,
                    Midtoright = false,
                    Skip = true,
                    Index = 10
                };
                var taskInfo11 = new TaskInfo()
                {
                    AsyncTaskWatch = asyncTaskWatch,
                    Reserve = false,
                    Mix = true,
                    Skip = true,
                    Index = 11
                };
                var taskInfo12 = new TaskInfo()
                {
                    AsyncTaskWatch = asyncTaskWatch,
                    Reserve = false,
                    Midtoright = true,
                    Mix = true,
                    Skip = true,
                    Index = 12
                };
                taskInfos.Add(taskInfo1);
                taskInfos.Add(taskInfo2);
                taskInfos.Add(taskInfo3);
                //taskInfos.Add(taskInfo4);
                taskInfos.Add(taskInfo5);
                taskInfos.Add(taskInfo6);
                taskInfos.Add(taskInfo7);
                taskInfos.Add(taskInfo8);
                taskInfos.Add(taskInfo9);
                //taskInfos.Add(taskInfo10);
                taskInfos.Add(taskInfo11);
                taskInfos.Add(taskInfo12);
                #endregion

                asyncTaskWatch.Restart();
                var tasks = new List<Task>();
                var cancellationTokenSource = new CancellationTokenSource();
                var sudokuT = new Sudoku(new SudokuSetting()
                {
                    GameLevel = gameLevel,
                });

                var repanel = CopyTo(cancellationTokenSource.Token, panel);
                JudgeLevel(cancellationTokenSource.Token, repanel, out var level, sudokuT.SudokuGameSetting.GameLevel);
                Console.Clear();
                Init(taskInfos, sudokuT, panel, repanel, cancellationTokenSource);
                var tasklist = GetTasks(taskInfos);
                tasks.AddRange(tasklist);
                await Task.WhenAll(tasks);
                var ok = taskInfos.Where(x => x.IsFound).FirstOrDefault();
                if (ok != null)
                {
                    Console.Clear();
                    sudokuT.ConsoleLog(ok.ReplacedPanel, 0);
                    sudokuT.ConsoleLog(ok.ResolvedPanel, 1);
                    ok.Count = 1;
                    ok.Time = asyncTaskWatch.Elapsed.TotalSeconds;
                    ok.Level = level;
                    dic1.Add(ok.Opreation, ok);
                    Console.WriteLine(taskInfos.Where(x => x.IsFound).First().Opreation + " TotalTime:" + asyncTaskWatch.Elapsed.TotalSeconds + "s");
                    writer.WriteLine($"{ok.Opreation}  {level}  {asyncTaskWatch.Elapsed.TotalSeconds + "s"}");
                    var reason = string.Empty;
                    var score = EvaluatePanel.EvaluateSudokuDifficulty(Utils.TransformPanel(ok.ReplacedPanel), out reason);
                    writer.WriteLine("Score " + score + " Reason " + reason);
                    writer.WriteLine("Replaced Panel");
                    writer.WriteLog(ok.ReplacedPanel);
                    writer.WriteLine("");
                    writer.WriteLog(ok.ResolvedPanel);
                    writer.WriteLine("");
                    res = TransformPanel(ok.ResolvedPanel);
                }
                else
                {
                    Console.WriteLine("NotFound TotalTime:" + asyncTaskWatch.Elapsed.TotalSeconds + "s");
                }
                GC.Collect();
                total.Stop();
                var resDic = dic1.OrderByDescending(x => x.Value.Count);
                foreach (var item in resDic)
                {
                    Console.WriteLine($"{item.Key}  {item.Value.Count}  {item.Value.TimePerCount}  {item.Value.LevelPerCount}");
                    writer.WriteLine($"{item.Key}  {item.Value.Count}  {item.Value.TimePerCount}  {item.Value.LevelPerCount}");
                    writer.WriteLine("");
                }
                writer.WriteLine("Total Hours: " + total.Elapsed.TotalHours);
                Console.WriteLine(total.Elapsed.TotalHours);
            }
            return res;
        }

        public static void Init(List<TaskInfo> taskInfos, Sudoku Sudoku, List<List<Panel>> regionPanel, List<List<Panel>> replacedPanel, CancellationTokenSource cancellationTokenSource)
        {
            foreach (var taskInfo in taskInfos)
            {
                taskInfo.Sudoku = Sudoku;
                taskInfo.RegionPanel = CopyTo(cancellationTokenSource.Token, regionPanel);
                taskInfo.ReplacedPanel = CopyTo(cancellationTokenSource.Token, replacedPanel);
                taskInfo.CancellationTokenSource = cancellationTokenSource;
                taskInfo.IsFound = false;
            }
        }

        public static List<Task> GetTasks(List<TaskInfo> taskInfos)
        {
            var res = new List<Task>();
            foreach (var taskInfo in taskInfos)
            {
                res.Add(GetTask(taskInfo));
            }

            return res;
        }

        public static Task GetTask(TaskInfo taskInfo)
        {
            var task = Task.Run(async () =>
            {
                try
                {
                    try
                    {
                        var panel2 = CopyTo(taskInfo.CancellationTokenSource.Token, taskInfo.ReplacedPanel);
                        taskInfo.ResolvedPanel = await taskInfo.Sudoku.ResolveAsync(taskInfo.CancellationTokenSource.Token, panel2, taskInfo);
                        taskInfo.CancellationTokenSource.Cancel();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(Environment.NewLine);
                        Console.WriteLine("Panel generated successfully.");
                        taskInfo.AsyncTaskWatch.Stop();
                        taskInfo.IsFound = true;
                    }
                    catch (OperationCanceledException)
                    {

                    }
                }
                catch (OperationEndException)
                {

                }
            });

            return task;
        }

        public static bool JudgeLevel(CancellationToken cancellationToken, List<List<Panel>> panel, out int sum, Level level)
        {
            var res = 0;
            var tempPanel = Sudoku.TransformToCloumOrRow(cancellationToken, panel);
            for (int i = 0; i < panel.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (int j = 0; j < panel[i].Count; j++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    res += panel[i].Where(x => x.Value == 0).Count();
                    res += tempPanel[j].Where(x => x.Value == 0).Count();
                }
            };
            sum = res;
            EvaluatePanel.EvaluateSudokuDifficulty(TransformPanel(panel), out double score);
            var (min, max) = GetLevelScore(level);
            return sum >= 0 && min <= score && max >= score;
        }

        public static (int, int) GetLevelScore(Level level)
        {
            var (min, max) = (-10000, 10000);
            switch (level)
            {
                case Level.Easy:
                    max = 20;
                    break;
                case Level.Nomal:
                    min = 20;
                    max = 40;
                    break;
                case Level.Hard:
                    min = 40;
                    max = 60;
                    break;
                case Level.Perfessionnal:
                    min = 60;
                    max = 100;
                    break;
                case Level.Master:
                    min = 100;
                    max = 120;
                    break;
                case Level.Legendary:
                    min = 120;
                    break;
            }
            return (min, max);
        }

        public static string LevelToString(this Level level)
        {
            var res = string.Empty;
            switch (level)
            {
                case Level.Easy:
                    res = "Easy(9~19)";
                    break;
                case Level.Nomal:
                    res = "Nomal(19~29)";
                    break;
                case Level.Hard:
                    res = "Hard(29~39)";
                    break;
                case Level.Perfessionnal:
                    res = "Perfessionnal(39~49)";
                    break;
                case Level.Master:
                    res = "Master(52~62)";
                    break;
                case Level.Legendary:
                    res = "Leagen(62~72)";
                    break;
            }
            return res;
        }

        public static Point LastPoint(this Point point, List<List<Panel>> panel)
        {
            var temps = panel.SelectMany(x => x.Where(y => y.Replaceable && (y.Point.X < point.X || (y.Point.X == point.X && y.Point.Y < point.Y)))).LastOrDefault();
            return temps?.Point ?? panel.SelectMany(x => x.Where(y => y.Replaceable)).First().Point;
        }

        public static void WriteLog(this StreamWriter streamWriter, List<List<Panel>> panel)
        {

            Func<int, int, string, string> Judge = (int index, int length, string item) =>
            {
                return index == 0 ? "[ " + item + "," : index == length - 1 ? item + " ]" : item + ",";
            };
            var cloum = string.Empty;
            for (int i = 0; i < panel.Count; i++)
            {
                var row = string.Empty;
                for (int j = 0; j < panel[i].Count; j++)
                {
                    row += Judge(j, panel[i].Count, panel[i][j].Value.ToString());
                }
                //streamWriter.WriteLine(row);
                cloum += row;
            }
            streamWriter.WriteLine(Environment.NewLine);
            streamWriter.WriteLine(cloum);
            streamWriter.WriteLine(Environment.NewLine);
        }

        public static List<List<Panel>> GetPanelFromStr(string panelStr)
        {
            var panel = new List<List<Panel>>();
            if (panelStr == null) return panel;
            var rows = panelStr.Split(']').Where(x => !string.IsNullOrEmpty(x.Trim())).Select(x => x.Trim().Replace("[", string.Empty)).ToList();
            for (int i = 0; i < rows.Count; i++)
            {
                var row = new List<Panel>();
                var items = rows[i].Split(",").Where(x => !string.IsNullOrEmpty(x.Trim())).Select(x => x.Trim()).ToList();
                for (int j = 0; j < items.Count; j++)
                {
                    var val = Convert.ToInt32(items[j]);
                    row.Add(new Panel()
                    {
                        Point = new Point(i, j),
                        Value = val,
                        Replaceable = val == 0,
                    });
                }
                panel.Add(row);
            }
            return panel;
        }
    }
}
