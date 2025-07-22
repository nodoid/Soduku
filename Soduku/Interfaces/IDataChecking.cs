namespace Soduku.Interfaces
{
    public interface IDataChecking
    {
        void SetupVars(int[,] actual, string[,] poss, bool hint);
        bool IsMoveValid(int col, int row, int value);
        bool CheckColumnsAndRows { get; }
        string CalculatePossibleValues(int col, int row);
        bool SolvePuzzle { get; }
        void FindCellWithFewestPossibleValues(ref int col, ref int row);
        void RandomizePossibleValues(ref string str);
        void SolvePuzzleByBruteForce();


    }
}
