using SudokuGame.SudokuMain;

namespace SudokuGame
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var gameLevel = Level.Master;
            var panels =  Utils.GeneratePanels(gameLevel, 10);
            //var panel = Utils.GetPanelFromStr(
            //    "[ 0,0,1,0,0,5,0,0,4 ]" +
            //    "[ 0,0,0,0,0,9,0,0,6 ]" +
            //    "[ 4,0,6,3,0,0,0,0,8 ]" +
            //    "[ 0,0,0,0,0,0,0,0,0 ]" +
            //    "[ 0,0,7,0,8,0,0,0,1 ]" +
            //    "[ 0,0,4,0,0,3,5,0,0 ]" +
            //    "[ 0,0,3,0,0,7,2,0,0 ]" +
            //    "[ 0,7,0,9,0,0,1,0,0 ]" +
            //    "[ 9,6,0,0,0,0,0,0,0 ]"
            //);
            foreach (var panel in panels)
            {
                await Utils.Resolve(Utils.TransformPanel(panel));
            }
            Console.ReadKey();
        }
    }
}
