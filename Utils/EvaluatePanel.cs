﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SudokuDotNetCore
{
    public static class EvaluatePanel
    {
        #region EvaluatePanel
        
        #region 公开接口
        public static string EvaluateSudokuDifficulty(int[,] board, out string reason)
        {
            var reasons = new List<string>();
            int emptyCells = board.Cast<int>().Count(v => v == 0);

            double baseScore = CalculateBaseScore(emptyCells, reasons);
            double techniqueScore = CalculateTechniqueScore(board, reasons);
            double totalScore = baseScore + techniqueScore;

            reason = FormatReasons(reasons);
            return ClassifyDifficulty(totalScore, emptyCells);
        }
        public static string EvaluateSudokuDifficulty(int[,] board, out double score)
        {
            var reasons = new List<string>();
            int emptyCells = board.Cast<int>().Count(v => v == 0);

            double baseScore = CalculateBaseScore(emptyCells, reasons);
            double techniqueScore = CalculateTechniqueScore(board, reasons);
            double totalScore = baseScore + techniqueScore;

            score = totalScore;
            return ClassifyDifficulty(totalScore, emptyCells);
        }
        #endregion

        #region 核心评分逻辑
        private static double CalculateBaseScore(int emptyCells, List<string> reasons)
        {
            double normalized = emptyCells / 10.0;
            double score = Math.Round(Math.Pow(normalized, 1.2) * 4);
            reasons.Add($"空白格系数 +{score}"); // 记录空白格得分
            return score;
        }

        private static double CalculateTechniqueScore(int[,] board, List<string> reasons)
        {
            double score = 0;
            score += EvaluateTechnique(board, "显性唯一数", -1.5, CountNakedSingles, reasons);
            score += EvaluateTechnique(board, "隐性唯一数", 0.5, CountHiddenSingles, reasons);
            score += EvaluateTechnique(board, "裸对", 0.8, CountNakedPairs, reasons);
            score += EvaluateTechnique(board, "三连数", 1.2, CountNakedTriples, reasons);
            score += EvaluateTechnique(board, "指向对", 1.4, CountPointingPairs, reasons);
            score += EvaluateTechnique(board, "行列排除", 1.6, CountBoxLineReductions, reasons);
            score += EvaluateTechnique(board, "X-Wing", 1.7, b => HasXWing(b) ? 1 : 0, reasons);
            score += EvaluateTechnique(board, "剑鱼", 1.8, b => HasSwordfish(b) ? 1 : 0, reasons);
            score += EvaluateTechnique(board, "Y-Wing", 1.9, CountYWings, reasons);
            score += EvaluateTechnique(board, "XY-Wing", 2.3, CountXYWings, reasons);
            score += EvaluateTechnique(board, "水母", 2.6, b => HasJellyfish(b) ? 1 : 0, reasons);
            score += EvaluateTechnique(board, "唯一矩形", 3.5, CountUniqueRectangles, reasons);

            if (RequiresBacktracking(board))
            {
                score += 80; // 降低回溯得分
                reasons.Add($"需回溯 +80");
            }

            return score;
        }

        private static string ClassifyDifficulty(double score, int emptyCells)
        {
            var res = string.Empty;
            if (score < 20)
            {
                res = "简单";
            }
            else if (score < 40)
            {
                res = "中等";
            }
            else if (score < 60)
            {
                res = "困难";
            }
            else if (score < 100)
            {
                res = "专业";
            }
            else if (score < 120)
            {
                res = "专家";
            }
            else 
            {
                res = "挑战";
            }
            return res;
        }

        private static string FormatReasons(List<string> reasons)
        {
            // 按技巧名称分组，并计算总得分
            var groupedReasons = reasons
                .GroupBy(r => r.Split('+')[0].Trim()) // 按技巧名称分组
                .Select(g => $"{g.Key} (共{g.Sum(r => double.Parse(r.Split('+')[1].Trim()))}分)"); // 计算总得分

            return string.Join("\n", groupedReasons);
        }
        #endregion

        #region 技巧检测核心方法
        private static double EvaluateTechnique(int[,] board, string name, double weight,
            Func<int[,], int> detector, List<string> reasons)
        {
            int count = detector(board);
            if (count > 0)
            {
                double totalScore = weight * count;
                reasons.Add($"{name} +{totalScore}"); // 记录得分
                return totalScore;
            }
            return 0;
        }

        private static int CountNakedSingles(int[,] board)
        {
            int count = 0;
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    if (GetCandidates(board, r, c).Count == 1) count++;
            return count;
        }

        private static int CountHiddenSingles(int[,] board)
        {
            int count = 0;
            for (int num = 1; num <= 9; num++)
            {
                for (int r = 0; r < 9; r++)
                {
                    var cols = new List<int>();
                    for (int c = 0; c < 9; c++)
                        if (IsCandidate(board, r, c, num)) cols.Add(c);
                    if (cols.Count == 1) count++;
                }

                for (int c = 0; c < 9; c++)
                {
                    var rows = new List<int>();
                    for (int r = 0; r < 9; r++)
                        if (IsCandidate(board, r, c, num)) rows.Add(r);
                    if (rows.Count == 1) count++;
                }
            }
            return count;
        }

        private static int CountNakedPairs(int[,] board)
        {
            int count = 0;
            foreach (var unit in GetAllUnits(board))
            {
                var pairs = unit.Where(c => GetCandidates(board, c.Item1, c.Item2).Count == 2).ToList();
                for (int i = 0; i < pairs.Count; i++)
                    for (int j = i + 1; j < pairs.Count; j++)
                        if (GetCandidates(board, pairs[i].Item1, pairs[i].Item2)
                            .SetEquals(GetCandidates(board, pairs[j].Item1, pairs[j].Item2)))
                            count++;
            }
            return count;
        }

        private static bool HasXWing(int[,] board)
        {
            for (int num = 1; num <= 9; num++)
            {
                var rowDict = new Dictionary<int, List<int>>();
                for (int r = 0; r < 9; r++)
                {
                    var cols = new List<int>();
                    for (int c = 0; c < 9; c++)
                        if (IsCandidate(board, r, c, num)) cols.Add(c);
                    if (cols.Count == 2) rowDict[r] = cols;
                }
                if (FindXWingPattern(rowDict)) return true;

                var colDict = new Dictionary<int, List<int>>();
                for (int c = 0; c < 9; c++)
                {
                    var rows = new List<int>();
                    for (int r = 0; r < 9; r++)
                        if (IsCandidate(board, r, c, num)) rows.Add(r);
                    if (rows.Count == 2) colDict[c] = rows;
                }
                if (FindXWingPattern(colDict)) return true;
            }
            return false;
        }

        private static bool FindXWingPattern<T>(Dictionary<T, List<int>> positions)
        {
            var keys = positions.Keys.ToList();
            for (int i = 0; i < keys.Count; i++)
                for (int j = i + 1; j < keys.Count; j++)
                    if (positions[keys[i]].SequenceEqual(positions[keys[j]]))
                        return true;
            return false;
        }
        #endregion

        #region 辅助方法
        private static HashSet<int> GetCandidates(int[,] board, int row, int col)
        {
            if (board[row, col] != 0) return new HashSet<int>();

            var candidates = new HashSet<int>(Enumerable.Range(1, 9));
            for (int i = 0; i < 9; i++)
            {
                candidates.Remove(board[row, i]);
                candidates.Remove(board[i, col]);
            }

            int sr = 3 * (row / 3), sc = 3 * (col / 3);
            for (int r = sr; r < sr + 3; r++)
                for (int c = sc; c < sc + 3; c++)
                    candidates.Remove(board[r, c]);

            return candidates;
        }

        private static bool IsCandidate(int[,] board, int row, int col, int num) =>
            GetCandidates(board, row, col).Contains(num);

        private static IEnumerable<List<Tuple<int, int>>> GetAllUnits(int[,] board)
        {
            for (int i = 0; i < 9; i++)
            {
                yield return GetCellsInRow(i);
                yield return GetCellsInColumn(i);
                yield return GetCellsInBox(i);
            }

            List<Tuple<int, int>> GetCellsInRow(int r) =>
                Enumerable.Range(0, 9).Select(c => Tuple.Create(r, c)).ToList();

            List<Tuple<int, int>> GetCellsInColumn(int c) =>
                Enumerable.Range(0, 9).Select(r => Tuple.Create(r, c)).ToList();

            List<Tuple<int, int>> GetCellsInBox(int b)
            {
                int sr = 3 * (b / 3), sc = 3 * (b % 3);
                return Enumerable.Range(0, 3)
                    .SelectMany(x => Enumerable.Range(0, 3)
                    .Select(y => Tuple.Create(sr + x, sc + y))).ToList();
            }
        }

        private static bool RequiresBacktracking(int[,] board)
        {
            int[,] clone = (int[,])board.Clone();
            return !SolveWithLogic(clone);
        }

        private static bool IsSolved(int[,] board) =>
            board.Cast<int>().All(n => n != 0);
        #endregion

        #region 完整技巧检测实现
        private static int CountNakedTriples(int[,] board)
        {
            int count = 0;
            foreach (var unit in GetAllUnits(board))
            {
                var cells = unit.Where(c => board[c.Item1, c.Item2] == 0).ToList();
                var triples = new List<HashSet<int>>();

                // 收集候选数数量<=3的单元格
                foreach (var cell in cells)
                {
                    var candidates = GetCandidates(board, cell.Item1, cell.Item2);
                    if (candidates.Count >= 2 && candidates.Count <= 3)
                        triples.Add(candidates);
                }

                // 检查所有三元组合
                for (int i = 0; i < triples.Count - 2; i++)
                {
                    for (int j = i + 1; j < triples.Count - 1; j++)
                    {
                        for (int k = j + 1; k < triples.Count; k++)
                        {
                            var union = new HashSet<int>(triples[i]);
                            union.UnionWith(triples[j]);
                            union.UnionWith(triples[k]);

                            if (union.Count == 3)
                            {
                                // 验证是否影响其他单元格
                                foreach (var cell in cells)
                                {
                                    var cellCandidates = GetCandidates(board, cell.Item1, cell.Item2);
                                    if (!cellCandidates.SetEquals(triples[i]) &&
                                        !cellCandidates.SetEquals(triples[j]) &&
                                        !cellCandidates.SetEquals(triples[k]) &&
                                        cellCandidates.Overlaps(union))
                                    {
                                        count++;
                                        goto NextUnit; // 避免重复计数
                                    }
                                }
                            }
                        }
                    }
                NextUnit:;
                }
            }
            return count;
        }

        private static int CountPointingPairs(int[,] board)
        {
            int count = 0;
            for (int num = 1; num <= 9; num++)
            {
                for (int box = 0; box < 9; box++)
                {
                    int startRow = (box / 3) * 3;
                    int startCol = (box % 3) * 3;
                    var positions = new List<Tuple<int, int>>();

                    // 收集候选数位置
                    for (int r = startRow; r < startRow + 3; r++)
                        for (int c = startCol; c < startCol + 3; c++)
                            if (IsCandidate(board, r, c, num))
                                positions.Add(Tuple.Create(r, c));

                    if (positions.Count < 2) continue;

                    // 检查行对齐
                    bool sameRow = positions.All(p => p.Item1 == positions[0].Item1);
                    if (sameRow)
                    {
                        int row = positions[0].Item1;
                        int boxCol = startCol / 3;
                        for (int c = 0; c < 9; c++)
                        {
                            if (c >= startCol && c < startCol + 3) continue;
                            if (IsCandidate(board, row, c, num)) count++;
                        }
                    }

                    // 检查列对齐
                    bool sameCol = positions.All(p => p.Item2 == positions[0].Item2);
                    if (sameCol)
                    {
                        int col = positions[0].Item2;
                        int boxRow = startRow / 3;
                        for (int r = 0; r < 9; r++)
                        {
                            if (r >= startRow && r < startRow + 3) continue;
                            if (IsCandidate(board, r, col, num)) count++;
                        }
                    }
                }
            }
            return count;
        }

        private static int CountBoxLineReductions(int[,] board)
        {
            int count = 0;
            for (int num = 1; num <= 9; num++)
            {
                // 行检查
                for (int row = 0; row < 9; row++)
                {
                    var cols = new HashSet<int>();
                    for (int col = 0; col < 9; col++)
                        if (IsCandidate(board, row, col, num))
                            cols.Add(col / 3);

                    if (cols.Count == 1)
                    {
                        int boxCol = cols.First();
                        int startCol = boxCol * 3;
                        for (int c = 0; c < 9; c++)
                        {
                            if (c >= startCol && c < startCol + 3) continue;
                            if (IsCandidate(board, row, c, num)) count++;
                        }
                    }
                }

                // 列检查
                for (int col = 0; col < 9; col++)
                {
                    var rows = new HashSet<int>();
                    for (int row = 0; row < 9; row++)
                        if (IsCandidate(board, row, col, num))
                            rows.Add(row / 3);

                    if (rows.Count == 1)
                    {
                        int boxRow = rows.First();
                        int startRow = boxRow * 3;
                        for (int r = 0; r < 9; r++)
                        {
                            if (r >= startRow && r < startRow + 3) continue;
                            if (IsCandidate(board, r, col, num)) count++;
                        }
                    }
                }
            }
            return count;
        }

        private static bool HasSwordfish(int[,] board)
        {
            for (int num = 1; num <= 9; num++)
            {
                // 行模式检测
                var rowDict = new Dictionary<int, List<int>>();
                for (int row = 0; row < 9; row++)
                {
                    var cols = new List<int>();
                    for (int col = 0; col < 9; col++)
                        if (IsCandidate(board, row, col, num)) cols.Add(col);

                    if (cols.Count == 2 || cols.Count == 3) rowDict[row] = cols;
                }

                if (FindSwordfishPattern(rowDict)) return true;

                // 列模式检测
                var colDict = new Dictionary<int, List<int>>();
                for (int col = 0; col < 9; col++)
                {
                    var rows = new List<int>();
                    for (int row = 0; row < 9; row++)
                        if (IsCandidate(board, row, col, num)) rows.Add(row);

                    if (rows.Count == 2 || rows.Count  == 3) colDict[col] = rows;
                }

                if (FindSwordfishPattern(colDict)) return true;
            }
            return false;
        }

        private static bool FindSwordfishPattern(Dictionary<int, List<int>> positions)
        {
            var keys = positions.Keys.Cast<int>().OrderBy(k => k).ToList();
            for (int i = 0; i < keys.Count - 2; i++)
            {
                for (int j = i + 1; j < keys.Count - 1; j++)
                {
                    for (int k = j + 1; k < keys.Count; k++)
                    {
                        var union = positions[keys[i]]
                            .Union(positions[keys[j]])
                            .Union(positions[keys[k]])
                            .Distinct()
                            .OrderBy(x => x)
                            .ToList();

                        if (union.Count == 3)
                        {
                            // 验证列分布（行模式）或行分布（列模式）
                            bool valid = true;
                            foreach (var pos in union)
                            {
                                int count = 0;
                                if (positions[keys[i]].Contains(pos)) count++;
                                if (positions[keys[j]].Contains(pos)) count++;
                                if (positions[keys[k]].Contains(pos)) count++;

                                if (count > 1) valid = false;
                            }
                            if (valid) return true;
                        }
                    }
                }
            }
            return false;
        }

        private static int CountYWings(int[,] board)
        {
            int count = 0;
            for (int pivotRow = 0; pivotRow < 9; pivotRow++)
            {
                for (int pivotCol = 0; pivotCol < 9; pivotCol++)
                {
                    var pivotCandidates = GetCandidates(board, pivotRow, pivotCol);
                    if (pivotCandidates.Count != 2) continue;

                    var wings = GetVisibleCells(pivotRow, pivotCol)
                        .Where(c => GetCandidates(board, c.Item1, c.Item2).Count == 2)
                        .ToList();

                    foreach (var wing1 in wings)
                    {
                        var wing1Candidates = GetCandidates(board, wing1.Item1, wing1.Item2);
                        if (pivotCandidates.Intersect(wing1Candidates).Count() != 1) continue;

                        foreach (var wing2 in wings)
                        {
                            if (wing1.Equals(wing2)) continue;
                            var wing2Candidates = GetCandidates(board, wing2.Item1, wing2.Item2);

                            if (pivotCandidates.Intersect(wing2Candidates).Count() == 1 &&
                                wing1Candidates.Intersect(wing2Candidates).Count() == 1)
                            {
                                count++;
                            }
                        }
                    }
                }

            }
            return count / 2; // 消除重复计数
        }


        private static int CountXYWings(int[,] board)
        {
            int count = 0;
            var pivotCells = GetAllCells().Where(c =>
                GetCandidates(board, c.Item1, c.Item2).Count == 2).ToList();

            foreach (var pivot in pivotCells)
            {
                var (pR, pC) = (pivot.Item1, pivot.Item2);
                var pCandidates = GetCandidates(board, pR, pC);

                var wings = GetVisibleCells(pR, pC)
                    .Where(c => GetCandidates(board, c.Item1, c.Item2).Count == 2)
                    .ToList();

                foreach (var wing1 in wings)
                {
                    var w1Candidates = GetCandidates(board, wing1.Item1, wing1.Item2);
                    if (w1Candidates.Intersect(pCandidates).Count() != 1) continue;

                    foreach (var wing2 in wings)
                    {
                        if (wing1.Equals(wing2)) continue;
                        var w2Candidates = GetCandidates(board, wing2.Item1, wing2.Item2);

                        if (w2Candidates.Intersect(pCandidates).Count() == 1 &&
                            w1Candidates.Intersect(w2Candidates).Count() == 1)
                        {
                            count++;
                        }
                    }
                }
            }
            return count / 2;
        }

        private static bool HasJellyfish(int[,] board)
        {
            for (int num = 1; num <= 9; num++)
            {
                // 行模式检测
                var rowDict = new Dictionary<int, List<int>>();
                for (int row = 0; row < 9; row++)
                {
                    var cols = new List<int>();
                    for (int col = 0; col < 9; col++)
                        if (IsCandidate(board, row, col, num)) cols.Add(col);

                    if (cols.Count == 2 || cols.Count == 3 || cols.Count == 4) rowDict[row] = cols;
                }

                if (FindJellyfishPattern(rowDict)) return true;

                // 列模式检测
                var colDict = new Dictionary<int, List<int>>();
                for (int col = 0; col < 9; col++)
                {
                    var rows = new List<int>();
                    for (int row = 0; row < 9; row++)
                        if (IsCandidate(board, row, col, num)) rows.Add(row);

                    if (rows.Count == 2 || rows.Count == 3 || rows.Count == 4) colDict[col] = rows;
                }

                if (FindJellyfishPattern(colDict)) return true;
            }
            return false;
        }

        private static bool FindJellyfishPattern(Dictionary<int, List<int>> positions)
        {
            var keys = positions.Keys.Cast<int>().OrderBy(k => k).ToList();
            for (int i = 0; i < keys.Count - 3; i++)
            {
                for (int j = i + 1; j < keys.Count - 2; j++)
                {
                    for (int k = j + 1; k < keys.Count - 1; k++)
                    {
                        for (int l = k + 1; l < keys.Count; l++)
                        {
                            var union = positions[keys[i]]
                                .Union(positions[keys[j]])
                                .Union(positions[keys[k]])
                                .Union(positions[keys[l]])
                                .Distinct()
                                .ToList();

                            if (union.Count == 4)
                            {
                                // 验证每个候选列/行只出现两次
                                bool valid = true;
                                foreach (var pos in union)
                                {
                                    int count = 0;
                                    if (positions[keys[i]].Contains(pos)) count++;
                                    if (positions[keys[j]].Contains(pos)) count++;
                                    if (positions[keys[k]].Contains(pos)) count++;
                                    if (positions[keys[l]].Contains(pos)) count++;

                                    if (count > 1) valid = false;
                                }
                                if (valid) return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private static int CountUniqueRectangles(int[,] board)
        {
            int count = 0;
            for (int box = 0; box < 9; box++)
            {
                int sr = (box / 3) * 3, sc = (box % 3) * 3;
                var corners = new[] {
            Tuple.Create(sr, sc), Tuple.Create(sr, sc + 2),
            Tuple.Create(sr + 2, sc), Tuple.Create(sr + 2, sc + 2)
        };

                if (corners.Any(c => board[c.Item1, c.Item2] != 0)) continue;

                var candidates = corners.Select(c => GetCandidates(board, c.Item1, c.Item2)).ToList();
                if (candidates.All(c => c.Count == 2) &&
                    candidates.Select(c => c.First()).Distinct().Count() == 1 &&
                    candidates.Select(c => c.Last()).Distinct().Count() == 1)
                {
                    count++;
                }
            }
            return count;
        }

        private static IEnumerable<Tuple<int, int>> GetAllCells()
        {
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    yield return Tuple.Create(r, c);
        }

        private static List<Tuple<int, int>> GetVisibleCells(int row, int col)
        {
            var cells = new List<Tuple<int, int>>();

            // 添加同行单元格（排除自身）
            for (int c = 0; c < 9; c++)
            {
                if (c != col) cells.Add(Tuple.Create(row, c));
            }

            // 添加同列单元格（排除自身）
            for (int r = 0; r < 9; r++)
            {
                if (r != row) cells.Add(Tuple.Create(r, col));
            }

            // 添加同宫单元格（排除自身）
            int startRow = 3 * (row / 3);
            int startCol = 3 * (col / 3);
            for (int r = startRow; r < startRow + 3; r++)
            {
                for (int c = startCol; c < startCol + 3; c++)
                {
                    if (r != row || c != col)
                        cells.Add(Tuple.Create(r, c));
                }
            }

            // 去重处理
            return cells.Distinct().ToList();
        }
        #endregion

        #region 唯一解验证
        public static bool IsPuzzleUnique(int[,] originalBoard, CancellationToken cancellationToken)
        {
            // 验证初始棋盘是否有效
            if (!IsValidBoard(originalBoard))
                return false;

            int[,] board = (int[,])originalBoard.Clone();
            int solutionCount = 0;
            
            // 使用MRV启发式搜索
            var emptyCells = GetAllEmptyCells(board);
            if (emptyCells.Count == 0)
                return true;

            // 按候选数数量排序
            emptyCells.Sort((a, b) => 
                GetCandidates(board, a.Item1, a.Item2).Count.CompareTo(
                GetCandidates(board, b.Item1, b.Item2).Count));

            return CheckUniqueness(board, emptyCells, 0, ref solutionCount, cancellationToken);
        }

        private static bool CheckUniqueness(int[,] board, List<Tuple<int, int>> emptyCells, 
            int index, ref int solutionCount, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (index >= emptyCells.Count)
            {
                solutionCount++;
                return solutionCount < 2; // 找到两个解时停止
            }

            var (row, col) = emptyCells[index];
            var candidates = GetCandidates(board, row, col).ToList();

            foreach (var num in candidates)
            {
                // 尝试填充当前数字
                board[row, col] = num;

                // 检查是否违反数独规则
                if (!IsValidBoard(board))
                {
                    board[row, col] = 0;
                    continue;
                }

                // 递归检查
                if (!CheckUniqueness(board, emptyCells, index + 1, ref solutionCount, cancellationToken))
                    return false;

                // 回溯
                board[row, col] = 0;

                if (solutionCount >= 2)
                    return false;
            }

            return true;
        }

        private static List<Tuple<int, int>> GetAllEmptyCells(int[,] board)
        {
            var cells = new List<Tuple<int, int>>();
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    if (board[r, c] == 0)
                        cells.Add(Tuple.Create(r, c));
            return cells;
        }

        private static (int row, int col) FindFirstEmptyCell(int[,] board)
        {
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    if (board[r, c] == 0)
                        return (r, c);
            return (-1, -1);
        }

        private static bool IsValidBoard(int[,] board)
        {
            // 检查行
            for (int r = 0; r < 9; r++)
            {
                var numbers = new HashSet<int>();
                for (int c = 0; c < 9; c++)
                {
                    if (board[r, c] != 0 && !numbers.Add(board[r, c]))
                        return false;
                }
            }

            // 检查列
            for (int c = 0; c < 9; c++)
            {
                var numbers = new HashSet<int>();
                for (int r = 0; r < 9; r++)
                {
                    if (board[r, c] != 0 && !numbers.Add(board[r, c]))
                        return false;
                }
            }

            // 检查宫
            for (int box = 0; box < 9; box++)
            {
                int startRow = (box / 3) * 3;
                int startCol = (box % 3) * 3;
                var numbers = new HashSet<int>();
                for (int r = startRow; r < startRow + 3; r++)
                {
                    for (int c = startCol; c < startCol + 3; c++)
                    {
                        if (board[r, c] != 0 && !numbers.Add(board[r, c]))
                            return false;
                    }
                }
            }

            return true;
        }
        #endregion
        #region 核心方法修复
        private static bool ApplyNakedSingles(int[,] board)
        {
            bool changed = false;
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    if (board[r, c] == 0)
                    {
                        var candidates = GetCandidates(board, r, c);
                        if (candidates.Count == 1)
                        {
                            board[r, c] = candidates.First(); // 修改棋盘
                            changed = true;
                        }
                    }
                }
            }
            return changed;
        }

        private static bool ApplyHiddenSingles(int[,] board)
        {
            bool changed = false;
            for (int num = 1; num <= 9; num++)
            {
                // 检查行中的隐性唯一数
                for (int row = 0; row < 9; row++)
                {
                    var possibleCols = new List<int>();
                    for (int col = 0; col < 9; col++)
                    {
                        if (board[row, col] == 0 && IsCandidate(board, row, col, num))
                        {
                            possibleCols.Add(col);
                        }
                    }
                    if (possibleCols.Count == 1)
                    {
                        board[row, possibleCols[0]] = num; // 修改棋盘
                        changed = true;
                    }
                }

                // 检查列中的隐性唯一数
                for (int col = 0; col < 9; col++)
                {
                    var possibleRows = new List<int>();
                    for (int row = 0; row < 9; row++)
                    {
                        if (board[row, col] == 0 && IsCandidate(board, row, col, num))
                        {
                            possibleRows.Add(row);
                        }
                    }
                    if (possibleRows.Count == 1)
                    {
                        board[possibleRows[0], col] = num; // 修改棋盘
                        changed = true;
                    }
                }

                // 检查宫中的隐性唯一数
                for (int box = 0; box < 9; box++)
                {
                    int startRow = (box / 3) * 3;
                    int startCol = (box % 3) * 3;
                    var possiblePositions = new List<Tuple<int, int>>();
                    for (int r = startRow; r < startRow + 3; r++)
                    {
                        for (int c = startCol; c < startCol + 3; c++)
                        {
                            if (board[r, c] == 0 && IsCandidate(board, r, c, num))
                            {
                                possiblePositions.Add(Tuple.Create(r, c));
                            }
                        }
                    }
                    if (possiblePositions.Count == 1)
                    {
                        var pos = possiblePositions[0];
                        board[pos.Item1, pos.Item2] = num; // 修改棋盘
                        changed = true;
                    }
                }
            }
            return changed;
        }

        private static bool SolveWithLogic(int[,] board)
        {
            bool progress;
            do
            {
                progress = ApplyNakedSingles(board) || ApplyHiddenSingles(board);
            } while (progress);

            return IsSolved(board);
        }
        #endregion

        #endregion
    }
}
