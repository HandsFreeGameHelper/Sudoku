using SudokuDotNetCore.SudokuMain;

namespace SudokuDotNetCore
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var gameLevel = Level.Legendary;

            while (true)
            {
                Console.Clear();
                var uniquePuzzleList = Utils.GeneratePanel(gameLevel).Item2;
                Utils.ConsoleLog(uniquePuzzleList, 0, new SudokuSetting());
                Console.WriteLine("InitialValeCount :" + uniquePuzzleList.SelectMany(x=>x.Where(y=>y.Value != 0)).Count());
                var panel = await Utils.Resolve(uniquePuzzleList);
                Utils.ConsoleLog(panel.Item2, 1, new SudokuSetting());
                Console.WriteLine(panel.Item1 ? "唯一解" :"多解");
              

                Console.ReadKey();
            }
        }
    }
}
