using System.Diagnostics;
using Chess;

using Cosette.Polyglot;
using Cosette.Polyglot.Book;

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
    private PolyglotBoard polyglotBoard;
    private PolyglotBook openingBook;
    private Random _random;

    private double[][] pieceValues = new double[][]// P, R, N, B, Q, K
    {
            new double[] { 1, 5.25, 3.5, 3.5, 10, 0 },
            new double[] { -1, -5.25, -3.5, -3.5, -10, 0 },
    };

    private Dictionary<int, TranspositionKey> transpositionTable;

    public Gravy()
    {
        string bookName = "/gm2001.bin";
        //string bookName = "/Cerebellum3Merge.bin";
        string firstPath = Path.GetFullPath("engines/books") + bookName;
        string secondPath = Path.GetFullPath("../../../../../lichess-bot/engines/books") + bookName;

        if (File.Exists(firstPath))
        {
            openingBook = new PolyglotBook(firstPath);
        }
        else if (File.Exists(secondPath))
        {
            openingBook = new PolyglotBook(secondPath);
        }
        else
        {
            throw new FileNotFoundException("The file does not exist in either path.");
        }

        StartNewGame();
    }

    public void StartNewGame()
    {
        board = new ChessBoard();
        polyglotBoard = new PolyglotBoard();

        polyglotBoard.InitDefaultState();

        _random = new Random();
    }

    public void SetPosition(string fen, string[] moves)
    {
        polyglotBoard = new PolyglotBoard();
        polyglotBoard.InitDefaultState();

        board = ChessBoard.LoadFromFen(fen);

        foreach (string move in moves)
        {
            DoMove(move);
        }
    }

    public Tuple<bool, bool, string> ChooseMove(int depth, long time)
    {
        nodesSearched = 0;
        maxTime = time;
        outOfTime = false;

        timer = new Stopwatch();
        timer.Start();

        Move bestMove;
        Move polyglotMove = GetPolyglotMove();

        if (polyglotMove is not null)
        {
            bestMove = polyglotMove;
        }
        else
        {
            bestMove = NegaMax(board, depth, int.MinValue + 1, int.MaxValue - 1, (board.Turn == PieceColor.White) ? 1 : -1).Item1;
        }
       
        //board.Move(bestMove);

        timer.Stop();

        return Tuple.Create(outOfTime, polyglotMove is not null, GetMoveString(bestMove));
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

    private Move GetPolyglotMove()
    {
        //Console.WriteLine($"info poly hash {polyglotBoard.CalculateHash()}");
        //Console.WriteLine($"info poly colour {((int)polyglotBoard._colorToMove)}");

        var availableMoves = openingBook.GetBookEntries(polyglotBoard.CalculateHash());
        if (availableMoves.Count == 0)
        {
            return null;
        }

        availableMoves = availableMoves.OrderBy(p => p.Weight).ToList();
        var weightSum = availableMoves.Sum(p => p.Weight);

        var probabilityArray = new double[availableMoves.Count];
        for (var availableMoveIndex = 0; availableMoveIndex < availableMoves.Count; availableMoveIndex++)
        {
            probabilityArray[availableMoveIndex] = (double)availableMoves[availableMoveIndex].Weight / weightSum;
        }

        var randomValue = _random.NextDouble();
        for (var availableMoveIndex = 0; availableMoveIndex < availableMoves.Count; availableMoveIndex++)
        {
            if (probabilityArray[availableMoveIndex] > randomValue || availableMoveIndex == availableMoves.Count - 1)
            {
                PolyglotBookMove move = availableMoves[availableMoveIndex].Move;
                string moveString = move.ToString();

                Console.WriteLine($"info promo {move.PromotionPiece}");
                return new Move(moveString[0..2], moveString[2..4]);
            }
        }

        return null;
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
            if (board.EndGame.WonSide is null) evaluation = 0;
            if (board.EndGame.WonSide == PieceColor.White) evaluation = int.MaxValue;
            if (board.EndGame.WonSide == PieceColor.Black) evaluation = int.MinValue;
        }

        return evaluation;
    }

    public ulong CalculateHash()
    {
        ulong result = 0;

        for (short file = 0; file < 8; file++)
        {
            for (short rank = 0; rank < 8; rank++)
            {
                if (board[file, rank] is not null)
                {
                    int colour = board[file, rank].Color == PieceColor.White ? 1 : 0;
                    result ^= PolyglotConstants.Keys[64 * (board[file, rank].Type.Value - 1 + colour) + 8 * rank + file];
                }
            }
        }

        board

        if ((_castlingFlags & CastlingFlags.WhiteShort) != 0)
        {
            result ^= PolyglotConstants.Keys[768];
        }
        if ((_castlingFlags & CastlingFlags.WhiteLong) != 0)
        {
            result ^= PolyglotConstants.Keys[769];
        }
        if ((_castlingFlags & CastlingFlags.BlackShort) != 0)
        {
            result ^= PolyglotConstants.Keys[770];
        }
        if ((_castlingFlags & CastlingFlags.BlackLong) != 0)
        {
            result ^= PolyglotConstants.Keys[771];
        }

        if (_enPassantFile != -1)
        {
            result ^= PolyglotConstants.Keys[772 + _enPassantFile];
        }

        if (_colorToMove == ColorType.White)
        {
            result ^= PolyglotConstants.Keys[780];
        }

        return result;
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

        //Console.WriteLine(polyglotBoard._colorToMove);
        polyglotBoard.MakeMove(move);
        board.Move(new Move(move[0..2], move[2..4]));
    }

    private string GetMoveString(Move move)
    {
        if (move is null) return "0000";

        string moveString = move.OriginalPosition.ToString() + move.NewPosition.ToString();

        if (move.Parameter != null)
        {
            char lastChar = move.Parameter.ShortStr.Last();

            if (lastChar == 'Q' || lastChar == 'R' || lastChar == 'B' || lastChar == 'N')
            {
                moveString += char.ToLower(lastChar);
            }
        }

        return moveString;
    }
}