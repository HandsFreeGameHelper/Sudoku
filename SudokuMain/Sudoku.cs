using System.Diagnostics;
using System.Drawing;

namespace SudokuGame.SudokuMain
{
    public class Sudoku
    {
        public Sudoku() { }

        public Sudoku(SudokuSetting SudokuSetting)
        {
            SudokuGameSetting = SudokuSetting;
        }

        public SudokuSetting SudokuGameSetting { get; set; } = new SudokuSetting();

        public bool Preprocessed { get; set; }

        private const int TimeoutSeconds = 120;

        private static readonly object _lock = new object();

        public List<List<Panel>> RegionPanel { get; set; } = GenerateDefualtPanel(new SudokuSetting());

        public List<List<Panel>> PreprocessedPanel { get; set; } = GenerateDefualtPanel(new SudokuSetting());

        public static List<List<Panel>> GenerateDefualtPanel(SudokuSetting SudokuGameSetting)
        {
            var panel = new List<List<Panel>>();
            for (int i = 0; i < SudokuGameSetting.PanelSize; i++)
            {
                var rows = new List<Panel>();
                for (int j = 0; j < SudokuGameSetting.PanelSize; j++)
                {
                    rows.Add(new Panel() { Value = 0, Point = new(i, j) });
                }
                panel.Add(rows);
            }
            return panel;
        }

        public List<List<UseableValue>> GenerateUseableValue(CancellationToken cancellationToken, List<List<Panel>> panel, bool use, List<List<UseableValue>>? lastUseableValues = null, Point? point = null, int? value = null)
        {
            var useableValues = new List<List<UseableValue>>();

            for (int i = 0; i < SudokuGameSetting.PanelSize; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var useable = new List<UseableValue>();
                for (int j = 0; j < SudokuGameSetting.PanelSize; j++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var inerValues = new List<int>();
                    for (int m = 1; m < SudokuGameSetting.PanelSize + 1; m++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        inerValues.Add(m);
                    }
                    useable.Add(new UseableValue { Values = inerValues, Point = new Point(i, j), LastUsedItem = lastUseableValues?[i][j].LastUsedItem ?? new List<int>() });
                }
                useableValues.Add(useable);
            }
            if (panel != null)
            {
                for (int row = 0; row < SudokuGameSetting.PanelSize; row++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var rowUseableValues = useableValues[row];
                    for (int cloum = 0; cloum < panel[row].Count; cloum++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var usedValues = panel[row][cloum];
                        panel[row][cloum].Point = new Point(row, cloum);
                        rowUseableValues.ForEach(x =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (x.Values.Contains(usedValues.Value))
                            {
                                x.Values.Remove(usedValues.Value);
                            }
                        });

                        var cluomUseableValues = TransformToCloumOrRow(cancellationToken, useableValues)[cloum];
                        cluomUseableValues.ForEach(x =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (x.Values.Contains(usedValues.Value))
                            {
                                x.Values.Remove(usedValues.Value);
                            }
                        });

                        rowUseableValues[cloum].Replaceable = RegionPanel[row][cloum].Replaceable;
                        panel[row][cloum].Replaceable = RegionPanel[row][cloum].Replaceable;

                        var range = GetRange(useableValues, row, cloum);

                        foreach (var item in range)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (item.Values.Contains(usedValues.Value))
                            {
                                item.Values.Remove(usedValues.Value);
                            }
                        }
                    }
                }
            }

            if (point != null)
            {
                var val = value ?? 0;
                if (val != 0)
                {
                    var p = new Point(point?.X ?? 0, point?.Y ?? 0);
                    var temp = useableValues.SelectMany(x => x.Where(y => y.Point.X > p.X || (y.Point.X == p.X && y.Point.Y > p.Y))).ToList();
                    if (temp.Any())
                    {
                        foreach (var item in temp)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            item.LastUsedItem.Clear();
                        }
                    }
                    if (!useableValues[p.X][p.Y].LastUsedItem.Contains(val))
                    {
                        useableValues[p.X][p.Y].LastUsedItem.Add(val);
                    }
                    var rowUseableValues = useableValues[p.X];
                    rowUseableValues.ForEach(x =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!x.Values.Contains(val))
                        {
                            x.Values.Add(val);
                        }
                    });

                    var cluomUseableValues = TransformToCloumOrRow(cancellationToken, useableValues)[p.Y];
                    cluomUseableValues.ForEach(x =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!x.Values.Contains(val))
                        {
                            x.Values.Add(val);
                        }
                    });

                    var range = GetRange(useableValues, p.X, p.Y);

                    foreach (var item in range)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!item.Values.Contains(val))
                        {
                            item.Values.Add(val);
                        }
                    }
                }
            }
            return useableValues;
        }

        public List<List<UseableValue>> GenerateUseableValue(CancellationToken cancellationToken, List<List<Panel>>? panel = null, List<List<UseableValue>>? values = null, int resetRow = -1, int resetCloum = -1, int resetCount = 0)
        {

            var useableValues = new List<List<UseableValue>>();

            for (int i = 0; i < SudokuGameSetting.PanelSize; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var useable = new List<UseableValue>();
                for (int j = 0; j < SudokuGameSetting.PanelSize; j++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var inerValues = new List<int>();
                    for (int m = 1; m < SudokuGameSetting.PanelSize + 1; m++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        inerValues.Add(m);
                    }
                    useable.Add(new UseableValue { Values = inerValues, Point = new Point(i, j) });
                }
                useableValues.Add(useable);
            };

            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    for (int j = 0; j < values[i].Count; j++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        useableValues[i][j].Replaceable = values[i][j].Replaceable;
                    }
                }
            }

            if (panel != null)
            {
                for (int row = 0; row < SudokuGameSetting.PanelSize; row++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var rowUseableValues = useableValues[row];
                    for (int cloum = 0; cloum < panel[row].Count; cloum++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        panel[row][cloum].Point = new Point(row, cloum);
                        var usedValues = panel[row][cloum];
                        rowUseableValues.ForEach(x =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (x.Values.Contains(usedValues.Value))
                            {
                                x.Values.Remove(usedValues.Value);
                            }
                        });

                        var cluomUseableValues = TransformToCloumOrRow(cancellationToken, useableValues)[cloum];
                        cluomUseableValues.ForEach(x =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (x.Values.Contains(usedValues.Value))
                            {
                                x.Values.Remove(usedValues.Value);
                            }
                        });
                        if (usedValues.Value != 0 && values == null)
                        {
                            rowUseableValues[cloum].Replaceable = false;
                            panel[row][cloum].Replaceable = false;
                        }
                        if (row <= resetRow && cloum <= resetCloum)
                        {
                            rowUseableValues[cloum].ResetCount = resetCount;
                        }

                        var range = GetRange(useableValues, row, cloum);

                        foreach (var item in range)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (item.Values.Contains(usedValues.Value))
                            {
                                item.Values.Remove(usedValues.Value);
                            }
                        }
                    }
                }
            }
            return useableValues;
        }

        Func<int, int, int> GetStart = (int s, int mod) =>
        {
            switch (s)
            {
                case 0:
                    return 0;
                case 1:
                    if (mod > 0)
                    {
                        return 1;
                    }
                    return 0;
                case 2:
                    if (mod > 0)
                    {
                        return 2;
                    }
                    return 1;
                case 3:
                    if (mod > 0)
                    {
                        return 3;
                    }
                    return 2;
                case 4:
                    return 3;
            }

            return 0;
        };

        public List<T> GetRange<T>(List<List<T>> useableValues, int row, int cloum, bool switcher = false)
        {
            var res = new List<T>();
            var mod = this.SudokuGameSetting.PanelSize == 9 ? 3 : this.SudokuGameSetting.PanelSize == 16 ? 4 : 0;
            if (mod != 0)
            {
                var rowStart = GetStart((row + 1) / mod, (row + 1) % mod) * mod;
                var cloumStart = GetStart((cloum + 1) / mod, (cloum + 1) % mod) * mod;
                for (int i = rowStart; i < rowStart + mod; i++)
                {
                    for (int j = cloumStart; j < cloumStart + mod; j++)
                    {
                        if (switcher && (i != row || j != cloum))
                        {
                            res.Add(useableValues[i][j]);
                        }
                        else if (!switcher)
                        {
                            res.Add(useableValues[i][j]);
                        }
                    }
                }
            }
            return res;
        }

        public void ConsoleLog(List<List<Panel>> panel, int logIndex)
        {
            lock (_lock)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Func<int, int, string, string> Judge = (int index, int length, string item) =>
                {
                    return index == 0 ? "#[ " + item + "," : index == length - 1 ? item + " ]#" : item + ",";
                };
                var cloum = string.Empty;
                var symble = GenerateSymble();
                var line = logIndex * SudokuGameSetting.LineHeight;
                line += 1;
                Console.SetCursorPosition((Console.WindowWidth - symble.Length) / 2, line);
                Console.WriteLine(symble);
                for (int i = 0; i < panel.Count; i++)
                {
                    var row = string.Empty;
                    for (int j = 0; j < panel[i].Count; j++)
                    {
                        row += Judge(j, panel[i].Count, SudokuGameSetting.PanelSize > 9 ? panel[i][j].Value.ToString().PadLeft(2, ' ') : panel[i][j].Value.ToString());
                    }
                    line += 1;
                    Console.SetCursorPosition((Console.WindowWidth - row.Length) / 2, line);
                    Console.WriteLine(row);
                }
                line += 1;
                Console.SetCursorPosition((Console.WindowWidth - symble.Length) / 2, line);
                Console.WriteLine(symble);
                line += 1;
                Console.WriteLine(Environment.NewLine);
            }
        }

        public string GenerateSymble()
        {
            var symble = string.Empty;
            for (int i = 0; i < SudokuGameSetting.PanelSize; i++)
            {
                symble += i < SudokuGameSetting.PanelSize - 1 ? SudokuGameSetting.PanelSize > 9 ? "###" : "##" : SudokuGameSetting.PanelSize > 9 ? "##" : "#";
            }

            return symble + "######";
        }

        public List<List<Panel>> Generate(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var totalWatch = new Stopwatch();
            totalWatch.Start();
            var panel = GeneratePanel(cancellationToken);

            if (SudokuGameSetting.NeedReplace)
            {
                panel = ReplacePanel(cancellationToken, panel);
            }
            totalWatch.Stop();
            return panel;
        }

        public List<List<Panel>> GeneratePanel(CancellationToken cancellationToken)
        {
            var genrateWatch = new Stopwatch();
            Preprocessed = false;
            genrateWatch.Restart();
            var random = new Random();
            var panel = GenerateDefualtPanel(SudokuGameSetting);
            var useableValus = GenerateUseableValue(cancellationToken);
            var usedValues = GenerateUsedValue();
            Resolve(cancellationToken, ref panel, useableValus, random, false);
            genrateWatch.Stop();
            return panel;
        }


        public List<List<Panel>> ReplacePanel(CancellationToken cancellationToken, List<List<Panel>> panel)
        {
            var random = new Random();
            var repalceCount = random.Next((int)SudokuGameSetting.GameLevel, (int)SudokuGameSetting.GameLevel + 10) + 1;
            var repalceLocations = new List<string>();
            var repalcedPanel = TransformToCloumOrRow(cancellationToken, panel, false);

            for (int i = 0; i < repalceCount; i++)
            {
                var row = -1;
                var cloum = -1;
                while (true)
                {
                    var repalceLocation = GetRandomRowAndCloum(SudokuGameSetting, random, ref row, ref cloum);
                    if (!repalceLocations.Contains(repalceLocation))
                    {
                        repalceLocations.Add(repalceLocation);
                        break;
                    }
                }
                if (row != -1 && cloum != -1)
                {
                    repalcedPanel[row][cloum].Value = 0;
                }
            }
            return repalcedPanel;
        }

        public void Resolve(CancellationToken cancellationToken, ref List<List<Panel>> panel, List<List<UseableValue>> useableValus, Random random, bool isResolve)
        {
            var resolveWatch = new Stopwatch();
            resolveWatch.Restart();
            if (Validate(cancellationToken, panel))
            {
                resolveWatch.Stop();
                return;
            }
            var usedValues = GenerateUsedValue();
            var resetCount = 0;
            for (int row = 0; row < panel.Count; row++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var usedValue = new List<int>();
                for (int cloum = 0; cloum < panel[row].Count; cloum++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var backupPanel = CopyTo(cancellationToken, panel);
                    PreProcess(cancellationToken, ref panel, ref useableValus, backupPanel);
                    var panelKey = GenerateSudokuKey(panel);
                    var useableValusKey = GenerateUseableValuesKey(useableValus);
                    var rowUseableValues = useableValus[row];
                    var cluomUseableValues = TransformToCloumOrRow(cancellationToken, useableValus)[cloum];
                    if (panel[row][cloum].Value != 0)
                    {
                        usedValue.Add(panel[row][cloum].Value);
                        continue;
                    }
                    else
                    {
                        while (true)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            usedValue = usedValue.Distinct().ToList();
                            foreach (var item in usedValues)
                            {
                                for (int i = 0; i < item.Count; i++)
                                {
                                    item[i] = item[i].Distinct().ToList();
                                }
                            }
                            if (usedValue.Count() == SudokuGameSetting.PanelSize || rowUseableValues[cloum].Values.Count == 0 || cluomUseableValues[row].Values.Count == 0)
                            {
                                Back(cancellationToken, ref cloum, ref row, ref panel, ref usedValue, ref rowUseableValues, ref cluomUseableValues, ref useableValus, ref usedValues, isResolve, ref resetCount);
                                break;
                            }
                            var val = rowUseableValues[cloum].Values[random.Next(rowUseableValues[cloum].Values.Count)];


                            if (!IsUseable(panel, rowUseableValues, cluomUseableValues, row, cloum, val))
                            {
                                if (isResolve &&
                                rowUseableValues[cloum].ResetCount > 0 &&
                                usedValues[row][cloum].Contains(val))
                                {
                                    if (usedValues[row][cloum].IndexOf(val) < rowUseableValues[cloum].ResetCount &&
                                    rowUseableValues[cloum].Values.Count > rowUseableValues[cloum].ResetCount)
                                    {
                                        continue;
                                    }
                                    else if (resolveWatch.Elapsed.TotalSeconds > TimeoutSeconds)
                                    {
                                        resolveWatch.Restart();
                                        rowUseableValues[cloum].ResetCount = 0;
                                        usedValues = GenerateUsedValue();
                                        Reset(cancellationToken, ref cloum, ref row, ref panel, ref usedValue, ref useableValus, isResolve);
                                        break;
                                    }
                                }
                                if (cloum == panel[row].Count - 1)
                                {
                                    usedValue.Add(val);
                                }
                                continue;
                            }
                            else
                            {
                                panel[row][cloum].Value = val;
                                usedValue.Add(val);
                                usedValues[row][cloum].Add(val);
                                useableValus = GenerateUseableValue(cancellationToken, panel, useableValus);
                                break;
                            }
                        }
                    }

                    if (row == panel.Count - 1 && cloum == panel[0].Count - 1 && !Validate(cancellationToken, panel))
                    {
                        Reset(cancellationToken, ref cloum, ref row, ref panel, ref usedValue, ref useableValus, isResolve);
                    }

                    GC.Collect();
                }
                GC.Collect();
            }
            resolveWatch.Stop();
        }

        public bool Validate(CancellationToken cancellationToken, List<List<Panel>> panel)
        {
            var res = true;
            var tempPanel = TransformToCloumOrRow(cancellationToken, panel);
            for (int i = 0; i < panel.Count; i++)
            {
                if (panel[i].Sum(x => x.Value) != SudokuGameSetting.ValidateValue || tempPanel[i].Sum(x => x.Value) != SudokuGameSetting.ValidateValue)
                {
                    res = false;
                    break;
                }
            }
            return res;
        }

        public static List<UseableValue> CopyTo(CancellationToken cancellationToken, List<UseableValue> useableValus)
        {
            var res = new List<UseableValue>();
            for (int i = 0; i < useableValus.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var values = new UseableValue();
                values = useableValus[i];
                res.Add(values);
            }
            return res;
        }

        public bool IsUseable(List<List<Panel>> panel, List<UseableValue> rowUseableValues, List<UseableValue> cluomUseableValues, int rowIndex, int cloumIndex, int val)
        {
            if ((cloumIndex < rowUseableValues.Count && !rowUseableValues[cloumIndex].Values.Contains(val)) ||
                (rowIndex < cluomUseableValues.Count && !cluomUseableValues[rowIndex].Values.Contains(val)))
            {
                return false;
            }
            var range = GetRange(panel, rowIndex, cloumIndex);
            if (range != null && range.Any(x => x.Value == val))
            {
                return false;
            }
            return true;
        }

        public void PreProcess(CancellationToken cancellationToken, ref List<List<Panel>> panel, ref List<List<UseableValue>> useableValus, List<List<Panel>> backupPanel, bool needCp = false)
        {
            while (useableValus.Any(x => x.Any(y => y.Values.Count == 1 && y.Replaceable == true)))
            {
                for (int i = 0; i < useableValus.Count; i++)
                {
                    for (int j = 0; j < useableValus[i].Count; j++)
                    {
                        if (useableValus[i][j].Values.Count == 1 && useableValus[i][j].Replaceable == true && panel[i][j].Value == 0)
                        {
                            backupPanel[i][j].Value = useableValus[i][j].Values[0];
                            useableValus = GenerateUseableValue(cancellationToken, backupPanel);
                        }
                    }
                }

                if (needCp)
                {
                    PreprocessedPanel = CopyTo(cancellationToken, backupPanel);
                    Preprocessed = true;
                }
                useableValus = GenerateUseableValue(cancellationToken, backupPanel);
            }

            if (Preprocessed == true)
            {
                var repalceable = useableValus.Select(x => x.Where(y => y.Replaceable == true && y.Values.Count > 0));
                if (repalceable.Count() == 0)
                {
                    Preprocessed = false;
                }
                else
                {
                    bool isok = false;
                    foreach (var item in repalceable)
                    {
                        foreach (var item2 in item)
                        {
                            if (panel[item2.Point.X][item2.Point.Y].Value == 0)
                            {
                                isok = true;
                            }
                        }
                    }
                    if (!isok)
                    {
                        Preprocessed = false;
                    }
                    else
                    {
                        panel = CopyTo(cancellationToken, backupPanel);
                        useableValus = GenerateUseableValue(cancellationToken, panel);
                    }
                }

            }
            else
            {
                panel = CopyTo(cancellationToken, backupPanel);
                useableValus = GenerateUseableValue(cancellationToken, panel);
            }
            GC.Collect();
        }

        public static string GetRandomRowAndCloum(SudokuSetting SudokuGameSetting, Random random, ref int row, ref int cloum)
        {
            row = random.Next(SudokuGameSetting.PanelSize);
            cloum = random.Next(SudokuGameSetting.PanelSize);

            return row.ToString() + cloum.ToString();
        }

        public static string GenerateSudokuKey(List<List<Panel>> panel)
        {
            string s = string.Empty;
            foreach (var item in panel)
            {
                foreach (var item1 in item)
                {
                    s += item1.Value;
                }
            }
            return s;
        }

        public static string GenerateUseableValuesKey(List<List<UseableValue>> useableValues)
        {
            string s = string.Empty;
            foreach (var item in useableValues)
            {
                foreach (var item1 in item)
                {
                    foreach (var item2 in item1.Values)
                    {
                        s += item2;
                    }
                }
            }
            return s;
        }


        public void Back(CancellationToken cancellationToken, ref int cloum, ref int row, ref List<List<Panel>> panel, ref List<int> usedValue, ref List<UseableValue> rowUseableValues, ref List<UseableValue> cluomUseableValues, ref List<List<UseableValue>> useableValus, ref List<List<List<int>>> usedValues, bool isResolve, ref int resetCount)
        {
            if (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var tempCloum = TransformToCloumOrRow(cancellationToken, useableValus);
                for (int i = 0; i < cloum; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    for (int j = 0; j < rowUseableValues[i].Values.Count; j++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!rowUseableValues[i].Replaceable)
                        {
                            continue;
                        }
                        var currVal = panel[row][i];
                        var temPanel = TransformToCloumOrRow(cancellationToken, panel);
                        var tempRowUseableValues = CopyTo(cancellationToken, rowUseableValues);
                        var tempCloumUseableValues = CopyTo(cancellationToken, cluomUseableValues);
                        var useableIndexValue = rowUseableValues[i].Values[j];
                        if (!(tempRowUseableValues[i].Values.Contains(currVal.Value) || temPanel[i].Contains(currVal)))
                        {
                            tempRowUseableValues[i].Values.Add(currVal.Value);
                        }
                        if (!(tempRowUseableValues[cloum].Values.Contains(currVal.Value) || temPanel[cloum].Contains(currVal)))
                        {
                            tempRowUseableValues[cloum].Values.Add(currVal.Value);
                        }
                        if (!tempCloumUseableValues[row].Values.Contains(currVal.Value))
                        {
                            tempCloumUseableValues[row].Values.Add(currVal.Value);
                        }
                        if (!tempCloumUseableValues[row].Values.Contains(useableIndexValue))
                        {
                            tempCloumUseableValues[row].Values.Add(useableIndexValue);
                        }
                        if (IsUseable(panel, tempRowUseableValues, tempCloumUseableValues, row, i, useableIndexValue) &&
                            IsUseable(panel, tempRowUseableValues, tempCloumUseableValues, row, cloum, currVal.Value))
                        {
                            usedValue.Add(useableIndexValue);
                            usedValue.Add(currVal.Value);
                            panel[row][i].Value = useableIndexValue;
                            panel[row][cloum] = currVal;
                            if (usedValues[row][i].Contains(currVal.Value))
                            {
                                usedValues[row][i].Remove(currVal.Value);
                            }
                            if (!usedValues[row][i].Contains(useableIndexValue))
                            {
                                usedValues[row][i].Add(useableIndexValue);
                            }
                            if (!usedValues[row][cloum].Contains(currVal.Value))
                            {
                                usedValues[row][cloum].Add(currVal.Value);
                            }
                            useableValus = GenerateUseableValue(cancellationToken, panel, useableValus);
                            return;
                        }
                    }

                }
            }

            useableValus[row][cloum].ResetCount += 1;
            Reset(cancellationToken, ref cloum, ref row, ref panel, ref usedValue, ref useableValus, isResolve);
            resetCount++;
        }

        public void Reset(CancellationToken cancellationToken,
                          ref int cloum,
                          ref int row,
                          ref List<List<Panel>> panel,
                          ref List<int> usedValue,
                          ref List<List<UseableValue>> useableValus,
                          bool isResolve)
        {
            panel = isResolve ? Preprocessed ? CopyTo(cancellationToken, PreprocessedPanel) : CopyTo(cancellationToken, RegionPanel) : GenerateDefualtPanel(SudokuGameSetting);
            useableValus = isResolve ? GenerateUseableValue(cancellationToken, panel, null, row, cloum, useableValus[row][cloum].ResetCount) : GenerateUseableValue(cancellationToken);
            cloum = -1;
            row = 0;
            usedValue = new List<int>();
        }


        public Task<List<List<Panel>>> ResolveAsync(CancellationToken cancellationToken, List<List<Panel>> repalcedPanel, TaskInfo taskInfo)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var totalWatch = new Stopwatch();
                totalWatch.Start();
                repalcedPanel = ResolvePanel(cancellationToken, repalcedPanel, taskInfo);

                totalWatch.Stop();
                return repalcedPanel;
            }, cancellationToken);
        }

        public List<List<Panel>> ResolvePanel(CancellationToken cancellationToken, List<List<Panel>> repalcedPanel, TaskInfo taskInfo)
        {
            RegionPanel = CopyTo(cancellationToken, repalcedPanel);
            var resolveWatch = new Stopwatch();
            resolveWatch.Restart();
            RegionPanel = CopyTo(cancellationToken, repalcedPanel);
            GenerateUseableValue(cancellationToken, RegionPanel);
            repalcedPanel = Resolver(cancellationToken, repalcedPanel, taskInfo);

            resolveWatch.Stop();
            return repalcedPanel;
        }

        public List<List<Panel>> Resolver(CancellationToken cancellationToken, List<List<Panel>> panel, TaskInfo taskInfo)
        {
            var backupPanel = CopyTo(cancellationToken, panel);
            var useableValues = GenerateUseableValue(cancellationToken, panel, true);
            var panels = panel.SelectMany(x => x.Where(y => y.Value == 0 && useableValues[y.Point.X][y.Point.Y].Replaceable)).ToList();
            var resolver = new Stopwatch();
            var random = new Random();
            resolver.Restart();
            for (int i = 0; i < panel.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (int j = 0; j < panel[i].Count; j++)
                {
                    var skipNum = 1;
                    if (taskInfo.Skip ?? false)
                    {
                        skipNum = 2;
                    }
                    if (taskInfo.Mix ?? false)
                    {
                        taskInfo.Reserve = random.Next(0, 2) == 0;
                        if (taskInfo.Midtoright != null)
                        {
                            taskInfo.Midtoright = random.Next(0, 2) == 0;
                        }
                        if (taskInfo.Skip ?? false)
                        {
                            skipNum = random.Next(1, 3);
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    var isFound = false;
                    if (!panel[i][j].Replaceable) continue;
                    var useableValue = useableValues[i][j];

                    if (PreProcess(cancellationToken, ref panel, ref useableValues))
                    {
                        if (panel[i][j].Value != 0) continue;
                        if (useableValue.Values.Count > 0 && useableValue.LastUsedItem.Count < useableValue.Values.Count)
                        {
                            var startIndex = taskInfo.Reserve ? useableValue.Values.Count - 1 : taskInfo.Midtoright != null ? ((useableValue.Values.Count - 1) / 2) : 0;
                            var morethanZore = taskInfo.Reserve | !taskInfo.Midtoright;
                            for (int m = taskInfo.Reserve ? useableValue.Values.Count - 1 : 0; morethanZore ?? false ? m >= 0 : m < useableValue.Values.Count;)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                if (ValidatePanel(cancellationToken, panel, i, j, useableValue, m))
                                {
                                    panel[i][j].Value = useableValue.Values[m];
                                    if (!useableValues[i][j].LastUsedItem.Contains(useableValue.Values[m]))
                                    {
                                        useableValues[i][j].LastUsedItem.Add(useableValue.Values[m]);
                                    }
                                    useableValues = GenerateUseableValue(cancellationToken, panel, true, useableValues);
                                    isFound = true;
                                    break;
                                }
                                if (morethanZore ?? false)
                                {
                                    m -= skipNum;
                                }
                                else
                                {
                                    m += skipNum;
                                }
                            }
                        }
                    }

                    while (!isFound)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var point = panel[i][j].Point.LastPoint(panel);
                        var val = panel[point.X][point.Y].Value;
                        var needRecoveryPanel = panel.SelectMany(x => x.Where(y => y.Replaceable && y.Value != 0 && (y.Point.X > point.X || (y.Point.Y >= point.Y && y.Point.X == point.X))));
                        foreach (var item in needRecoveryPanel)
                        {
                            item.Value = 0;
                        }
                        useableValues = GenerateUseableValue(cancellationToken, panel, true, useableValues, point, val);

                        if (useableValues[point.X][point.Y].Values.Count > 1 && useableValues[point.X][point.Y].LastUsedItem.Count < useableValues[point.X][point.Y].Values.Count)
                        {
                            if (point.X > 0)
                            {
                                if (point.Y > 0)
                                {
                                    i = point.X;
                                    j = point.Y - 1;
                                }
                                else
                                {
                                    i = point.X - 1;
                                    j = this.SudokuGameSetting.PanelSize - 1;
                                }
                            }
                            else
                            {
                                if (point.Y > 0)
                                {
                                    i = point.X;
                                    j = point.Y - 1;
                                }
                                else
                                {
                                    i = point.X;
                                    j = -1;
                                }
                            }
                            break;
                        }

                        i = point.X;
                        j = point.Y;

                        if (i == 0 && j == 0)
                        {
                            while (true)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                lock (_lock)
                                {
                                    Console.SetCursorPosition(0, taskInfo.Index + 1);
                                    Console.Write($"{taskInfo.Opreation} End");
                                    throw new OperationEndException();
                                }
                            }
                        }
                    }
                    lock (_lock)
                    {
                        if ((taskInfo.Index != 1 || taskInfo.Index != 5 || taskInfo.Index != 11) && resolver.Elapsed.TotalSeconds > 180)
                        {
                            Console.SetCursorPosition(0, taskInfo.Index + 1);
                            Console.Write($"{taskInfo.Opreation} End");
                            throw new OperationEndException();
                        }
                    }
                }
            }
            resolver.Stop();
            return panel;
        }

        public List<List<List<int>>> GenerateUsedValue()
        {
            var usedValue = new List<List<int>>();
            var usedValues = new List<List<List<int>>>();
            for (int i = 0; i < SudokuGameSetting.PanelSize; i++)
            {
                usedValue.Add(new List<int>());
            }
            for (int i = 0; i < SudokuGameSetting.PanelSize; i++)
            {
                usedValues.Add(usedValue);
            }
            return usedValues;
        }

        public bool ValidatePanel(CancellationToken cancellationToken, List<List<Panel>> panel, int row, int cloum, UseableValue useableValue, int index)
        {
            var temPanel = TransformToCloumOrRow(cancellationToken, panel);
            var range = GetRange(panel, row, cloum);
            var usedRowList = panel[row].Select(x => x.Value);
            var usedCloumList = temPanel[cloum].Select(x => x.Value);
            var value = useableValue.Values[index];

            if (value != 0 &&
               (usedRowList.Contains(value) ||
                usedCloumList.Contains(value) ||
                useableValue.LastUsedItem.Contains(value)
               ) ||
               range.Any(x => x.Value == value))
            {
                return false;
            }

            var backupPanel = CopyTo(cancellationToken, panel);
            backupPanel[row][cloum].Value = value;
            var tempUseable = GenerateUseableValue(cancellationToken, backupPanel, true);
            var backupPanels = backupPanel.SelectMany(x => x.Where(y => y.Replaceable && y.Value == 0 && (y.Point.X > row || y.Point.X == row && y.Point.Y > cloum))).ToList();
            if (backupPanels.Any(x => tempUseable[x.Point.X][x.Point.Y].Values.Count < 1))
            {
                return false;
            }
            return true;
        }

        public bool PreProcess(CancellationToken cancellationToken, ref List<List<Panel>> panel, ref List<List<UseableValue>> useableValus)
        {
            var temp = panel;
            while (useableValus.Any(x => x.Any(y => y.Values.Count == 1 && y.Replaceable == true && temp[y.Point.X][y.Point.Y].Value == 0)))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var useableValue = useableValus.SelectMany(x => x.Where(y => y.Values.Count == 1 && y.Replaceable == true && temp[y.Point.X][y.Point.Y].Value == 0)).First();

                if (ValidatePanel(cancellationToken, panel, useableValue.Point.X, useableValue.Point.Y, useableValue, 0))
                {
                    var val = useableValue.Values[0];
                    panel[useableValue.Point.X][useableValue.Point.Y].Value = val;
                    temp = panel;
                    if (!useableValue.LastUsedItem.Contains(val))
                    {
                        useableValue.LastUsedItem.Add(val);
                    }
                    useableValus = GenerateUseableValue(cancellationToken, panel, true, useableValus);
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public static List<List<UseableValue>> TransformToCloumOrRow(CancellationToken cancellationToken, List<List<UseableValue>> useableValus, bool isCloum = true)
        {
            var res = new List<List<UseableValue>>();
            for (int i = 0; i < useableValus.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var values = new List<UseableValue>();
                for (int j = 0; j < useableValus[i].Count; j++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (isCloum)
                    {
                        values.Add(useableValus[j][i]);
                    }
                    else
                    {
                        values.Add(useableValus[i][j]);
                    }
                }
                res.Add(values);
            }
            return res;
        }

        public static List<List<Panel>> TransformToCloumOrRow(CancellationToken cancellationToken, List<List<Panel>> useableValus, bool isCloum = true)
        {
            var res = new List<List<Panel>>();
            for (int i = 0; i < useableValus.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var values = new List<Panel>();
                for (int j = 0; j < useableValus[i].Count; j++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (isCloum)
                    {
                        values.Add(new Panel()
                        {
                            Value = useableValus[j][i].Value,
                            Point = useableValus[j][i].Point,
                        });
                    }
                    else
                    {
                        values.Add(new Panel()
                        {
                            Value = useableValus[i][j].Value,
                            Point = useableValus[i][j].Point,
                        });
                    }

                }
                res.Add(values);
            }
            return res;
        }

        public static List<List<Panel>> CopyTo(CancellationToken cancellationToken, List<List<Panel>> useableValus)
        {
            var res = new List<List<Panel>>();
            for (int i = 0; i < useableValus.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var values = new List<Panel>();
                for (int j = 0; j < useableValus[i].Count; j++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    values.Add(new Panel()
                    {
                        Value = useableValus[i][j].Value,
                        Point = useableValus[i][j].Point
                    });
                }
                res.Add(values);
            }
            return res;
        }
    }
}
