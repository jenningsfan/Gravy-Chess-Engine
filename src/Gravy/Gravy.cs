using System.Diagnostics;
using Chess;

namespace Gravy;

using TranspositionKey = Tuple<int, int, int>; // Key, depth, score

internal class Gravy
{
    public bool IsWhite { get { return board.Turn == PieceColor.White; } }

    public int nodesSearched;
    private Stopwatch timer;
    private long maxTime;
    private bool outOfTime;

    private ChessBoard board;
    private double[][] pieceValues = new double[][]// P, R, N, B, Q, K
    {
            new double[] { 1, 5.25, 3.5, 3.5, 10, 0 },
            new double[] { -1, -5.25, -3.5, -3.5, -10, 0 },
    };

    private Dictionary<int, TranspositionKey> transpositionTable;

    public Gravy()
    {
        StartNewGame();
    }

    public void StartNewGame()
    {
        ChessBoard board = new ChessBoard();
    }

    public void SetPosition(string fen, string[] moves)
    {
        board = ChessBoard.LoadFromFen(fen);

        foreach (string move in moves)
        {
            DoMove(move);
        }
    }

    public async Task<Tuple<bool, string>> ChooseMove(int depth, long time)
    {
        return await Task.Run(() =>
        {
            nodesSearched = 0;
            maxTime = time;
            outOfTime = false;

            timer = new Stopwatch();
            timer.Start();

            Move bestMove = NegaMax(board, depth, int.MinValue + 1, int.MaxValue - 1, (board.Turn == PieceColor.White) ? 1 : -1).Item1;
            //board.Move(bestMove);

            timer.Stop();

            return Tuple.Create(outOfTime, GetMoveString(bestMove));
        });
    }

    private Tuple<Move, double> NegaMax(ChessBoard board, int depth, double alpha, double beta, int colour)
    {
        Move[] moves = board.Moves();

        if (depth <= 0 || board.IsEndGame)
        {
            return Tuple.Create((Move)null, colour * EvaluateBoard());
        }

        Move bestMove = null;
        double maxEval = int.MinValue;

        foreach (Move move in OrderMoves(moves, colour))
        {
            nodesSearched++;

            if (nodesSearched % 1024 == 0 || outOfTime)
            {
                if (timer.ElapsedMilliseconds > maxTime || outOfTime)
                {
                    outOfTime = true;
                    break;
                }
            }

            board.Move(move);

            double eval = -NegaMax(board, depth - 1, -beta, -alpha, -colour).Item2;
            if (eval > maxEval)
            {
                maxEval = eval;
                bestMove = move;
            }
            alpha = Math.Max(alpha, eval);

            board.Cancel();

            if (alpha >= beta)
            {
                break;
            }
        }

        return Tuple.Create(bestMove, maxEval);
    }

    private List<Move> OrderMoves(Move[] moves, int colour)
    {
        PriorityQueue<Move, double> queue = new PriorityQueue<Move, double>(Comparer<double>.Create((x, y) => y.CompareTo(x)));

        foreach (Move move in moves)
        {
            board.Move(move);
            queue.Enqueue(move, colour * EvaluateBoard());
            board.Cancel();
        }

        List<Move> orderedMoves = new();

        while (queue.Count > 0)
        {
            orderedMoves.Add(queue.Dequeue());
        }
        
        return orderedMoves;
    }

    public double EvaluateBoard()
    {
        double evaluation = 0;

        for (short i = 0; i < 8; i++)
        {
            for (short j = 0; j < 8; j++)
            {
                if (board[i, j] != null)
                {
                    evaluation += pieceValues[board[i, j].Color - 1][board[i, j].Type.Value - 1];
                }
            }
        }

        if (board.IsEndGame)
        {
            if (board.EndGame.WonSide == null) evaluation = 0;
            if (board.EndGame.WonSide == PieceColor.White) evaluation = int.MaxValue;
            if (board.EndGame.WonSide == PieceColor.Black) evaluation = int.MinValue;
        }

        return evaluation;
    }

    public void DoMove(string move)
    {
        PromotionType promotion = PromotionType.Default;

        switch (move.ToLower().Last())
        {
            case 'q':
                promotion = PromotionType.ToQueen;
                break;
            case 'r':
                promotion = PromotionType.ToRook;
                break;
            case 'b':
                promotion = PromotionType.ToBishop;
                break;
            case 'n':
                promotion = PromotionType.ToKnight;
                break;
        }

        board.OnPromotePawn += (sender, e) => e.PromotionResult = promotion;

        board.Move(new Move(move[0..2], move[2..4]));
    }

    private string GetMoveString(Move move)
    {
        if (move is null) return "0000";

        string moveString = move.OriginalPosition.ToString() + move.NewPosition.ToString();

        if (move.Parameter != null)
        {
            char lastChar = move.San.Last();

            if (lastChar == 'Q' || lastChar == 'R' || lastChar == 'B' || lastChar == 'N')
            {
                moveString += char.ToLower(lastChar);
            }
        }

        return moveString;
    }
}