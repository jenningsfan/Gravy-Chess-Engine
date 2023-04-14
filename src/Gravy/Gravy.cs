using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Chess;

using Cosette.Polyglot;
using Cosette.Polyglot.Book;

namespace Gravy;

internal class Gravy
{
    public bool IsWhite { get { return board.Turn == PieceColor.White; } }

    public int nodesSearched;
    private Stopwatch timer;
    private long maxTime;
    private bool outOfTime;

    private ChessBoard board;
    private PolyglotBook openingBook;
    private Random _random;
    private ulong _hash;

    private bool[] castlingStatus;

    private static double[][] pieceValues = new double[][]// P, R, N, B, Q, K
    {
            new double[] {  100,  500,  320,  330,  900,  20000 },
            new double[] { -100, -500, -320, -330, -900, -20000 },
    };

    private static int[][] pieceHash = new int[][]// P, R, N, B, Q, K
    {         
        new int[] { 0, 6, 2, 4, 8, 10 },
        new int[] { 1, 7, 3, 5, 9, 11 },
    };

    private static int[] pawnTable = new int[]
    {
        0,  0,  0,  0,  0,  0,  0,  0,
        50, 50, 50, 50, 50, 50, 50, 50,
        10, 10, 20, 30, 30, 20, 10, 10,
         5,  5, 10, 25, 25, 10,  5,  5,
         0,  0,  0, 20, 20,  0,  0,  0,
         5, -5,-10,  0,  0,-10, -5,  5,
         5, 10, 10,-20,-20, 10, 10,  5,
         0,  0,  0,  0,  0,  0,  0,  0
    };

    private static int[] knightTable = new int[]
    {
        -50,-40,-30,-30,-30,-30,-40,-50,
        -40,-20,  0,  0,  0,  0,-20,-40,
        -30,  0, 10, 15, 15, 10,  0,-30,
        -30,  5, 15, 20, 20, 15,  5,-30,
        -30,  0, 15, 20, 20, 15,  0,-30,
        -30,  5, 10, 15, 15, 10,  5,-30,
        -40,-20,  0,  5,  5,  0,-20,-40,
        -50,-40,-30,-30,-30,-30,-40,-50,
    };

    private static int[] bishopTable = new int[]
    {
        -20,-10,-10,-10,-10,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5, 10, 10,  5,  0,-10,
        -10,  5,  5, 10, 10,  5,  5,-10,
        -10,  0, 10, 10, 10, 10,  0,-10,
        -10, 10, 10, 10, 10, 10, 10,-10,
        -10,  5,  0,  0,  0,  0,  5,-10,
        -20,-10,-10,-10,-10,-10,-10,-20,
    };

    private static int[] rookTable = new int[]
    {
          0,  0,  0,  0,  0,  0,  0,  0,
          5, 10, 10, 10, 10, 10, 10,  5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
          0,  0,  0,  5,  5,  0,  0,  0
    };

    private static int[] queenTable = new int[]
    {
        -20,-10,-10, -5, -5,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5,  5,  5,  5,  0,-10,
         -5,  0,  5,  5,  5,  5,  0, -5,
          0,  0,  5,  5,  5,  5,  0, -5,
        -10,  5,  5,  5,  5,  5,  0,-10,
        -10,  0,  5,  0,  0,  0,  0,-10,
        -20,-10,-10, -5, -5,-10,-10,-20
    };

    private static int[] kingMGTable = new int[]
    {
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -20,-30,-30,-40,-40,-30,-30,-20,
        -10,-20,-20,-20,-20,-20,-20,-10,
         20, 20,  0,  0,  0,  0, 20, 20,
         20, 30, 10,  0,  0, 10, 30, 20
    };

    private static int[] kingEGTable = new int[]
    {
        -50,-40,-30,-20,-20,-30,-40,-50,
        -30,-20,-10,  0,  0,-10,-20,-30,
        -30,-10, 20, 30, 30, 20,-10,-30,
        -30,-10, 30, 40, 40, 30,-10,-30,
        -30,-10, 30, 40, 40, 30,-10,-30,
        -30,-10, 20, 30, 30, 20,-10,-30,
        -30,-30,  0,  0,  0,  0,-30,-30,
        -50,-30,-30,-30,-30,-30,-30,-50
    };

    private int[][] MGPieceTables;
    private int[][] EGPieceTables;

    private static int pawnPhase = 0;
    private static int knightPhase = 1;
    private static int bishopPhase = 1;
    private static int rookPhase = 2;
    private static int queenPhase = 4;
    private static int[] piecePhases = new int[] {pawnPhase, rookPhase, knightPhase, bishopPhase, queenPhase, 0}; // 0 for king

    private static int totalPhase = pawnPhase * 16 + knightPhase * 4 + bishopPhase * 4 + rookPhase * 4 + queenPhase * 2;

    private LimitedSizeDictionary<ulong, double> _transpositionTable;
    private int _transpositionSize = 20000;

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

        InitPieceTables();

        StartNewGame();
    }

    public void StartNewGame()
    {
        board = new ChessBoard();
        InitialHash();

        _random = new Random();
    }

    public void SetPosition(string fen, string[] moves)
    {
        board = ChessBoard.LoadFromFen(fen);

        foreach (string move in moves)
        {
            DoMove(move);
        }

        InitialHash();
    }

    private void InitPieceTables()
    {
        int[][] pieceTablesTemp = new int[7][]
        {
            pawnTable,
            rookTable,
            knightTable,
            bishopTable,
            queenTable,
            kingMGTable,
            kingEGTable,
        };

        MGPieceTables = new int[12][];
        EGPieceTables = new int[12][];

        for (int i = 0; i < 6; i++)
        {
            MGPieceTables[i] = pieceTablesTemp[i];
            MGPieceTables[i + 6] = FlipPieceTable(pieceTablesTemp[i]);
        }

        for (int i = 0; i < 5; i++)
        {
            EGPieceTables[i] = pieceTablesTemp[i];
            EGPieceTables[i + 5] = FlipPieceTable(pieceTablesTemp[i]);
        }

        EGPieceTables[10] = kingEGTable;
        EGPieceTables[11] = FlipPieceTable(kingEGTable);
    }

    private int GetGamePhase()
    {
        // This is taken from the tapered eval algorithim on https://www.chessprogramming.org/index.php?title=Tapered_Eval&oldid=25214
        int phase = totalPhase;

        for(short i = 0; i < 8; i++)
        {
            for (short j = 0; j < 8; j++)
            {
                if (board[i, j] != null)
                {
                    phase -= piecePhases[board[i, j].Type.Value - 1];
                }
            }
        }

        return (phase * 256 + (totalPhase / 2)) / totalPhase;
    }

    private int[] FlipPieceTable(int[] pieceTable)
    {
        int[] result = new int[64];

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                result[i * 8 + j] = -pieceTable[i * 8 + j];
            }
        }

        return result.Reverse().ToArray();
    }

    public Tuple<bool, bool, bool, string> ChooseMove(int depth, long time)
    {
        nodesSearched = 0;
        maxTime = time;
        outOfTime = false;

        _transpositionTable = new(_transpositionSize);

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
            Tuple<Move, double> result = NegaScout(board, depth, int.MinValue + 1, int.MaxValue - 1, (board.Turn == PieceColor.White) ? 1 : -1);
            
            bestMove = result.Item1;
            //Console.WriteLine($"\n{bestMove}: {result.Item2}");
        }
       
        //board.Move(bestMove);

        timer.Stop();

        bool isMate = bestMove is null ? false : bestMove.IsMate;

        return Tuple.Create(outOfTime, polyglotMove is not null, isMate, GetMoveString(bestMove));
    }

    private Tuple<Move, double> NegaScout(ChessBoard board, int depth, double alpha, double beta, int colour)
    {
        if (depth <= 0 || board.IsEndGame)
        {
            return Tuple.Create((Move)null, colour * EvaluateBoard());
        }

        List<Move> moves = OrderMoves(board.Moves(), colour);
        Move bestMove = null;
        double maxEval = int.MinValue + 1;

        for (int i = 0; i < moves.Count; i++)
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

            Move move = moves[i];

            UpdateHash(move);
            board.Move(move);

            ulong oldHash = _hash;

            double transpositonResult = CheckTranspositon();
            //double transpositonResult = -1;
            double eval;

            if (transpositonResult == -1)
            {
                if (i == 0)
                {
                    eval = -NegaScout(board, depth - 1, -beta, -alpha, -colour).Item2;
                }
                else
                {
                    eval = -NegaScout(board, depth - 1, -alpha - 1, -alpha, -colour).Item2;

                    if (alpha < eval && eval < beta)
                    {
                        eval = -NegaScout(board, depth - 1, -beta, -eval, -colour).Item2;
                    }
                }

                _transpositionTable[_hash] = eval;
            }
            else
            {
                eval = transpositonResult;
            }
            
            if (eval >= maxEval) // This not being >= seems to have improved chess ability??? TODO: investigate
            {
                maxEval = eval;
                bestMove = move;
            }
            alpha = Math.Max(alpha, eval);
            
            board.Cancel();
            _hash = oldHash;

            if (alpha >= beta)
            {
                break;
            }
        }

        //Console.WriteLine($"{bestMove}: {maxEval}");

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
        //Console.WriteLine($"info poly hash {_hash}");
        //Console.WriteLine($"info poly colour {((int)polyglotBoard._colorToMove)}");

        var availableMoves = openingBook.GetBookEntries(_hash);
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

    private void UpdateHash(Move move)
    {
        int colour = board[move.OriginalPosition].Color == PieceColor.White ? 1 : 0;

        _hash ^= PolyglotConstants.Keys[64 * (pieceHash[colour][board[move.OriginalPosition].Type.Value - 1]) + 8 * move.OriginalPosition.Y + move.OriginalPosition.X];

        if (move.Parameter is MovePromotion)
        {
            int piece = 5;  // Default to queen

            switch (((MovePromotion)move.Parameter).PromotionType)
            {
                case PromotionType.ToRook:
                    piece = 2;
                    break;
                case PromotionType.ToKnight:
                    piece = 3;
                    break;
                case PromotionType.ToBishop:
                    piece = 4;
                    break;
                case PromotionType.ToQueen:
                    piece = 5;
                    break;
            }

            _hash ^= PolyglotConstants.Keys[64 * (piece - 1 + colour) + 8 * move.NewPosition.Y + move.NewPosition.X];
        }
        else
        {
            _hash ^= PolyglotConstants.Keys[64 * (pieceHash[colour][board[move.OriginalPosition].Type.Value - 1]) + 8 * move.NewPosition.Y + move.NewPosition.X];
        }

        if (move.Parameter is MoveCastle)
        {
            CastleType castle = ((MoveCastle)move.Parameter).CastleType;

            if (move.Piece.Color == PieceColor.White)
            {
                castlingStatus[0] = true;

                if (castle == CastleType.King)
                {
                    _hash ^= PolyglotConstants.Keys[768];
                }
                else if (castle == CastleType.Queen)
                {
                    _hash ^= PolyglotConstants.Keys[769];
                }
            }

            if (move.Piece.Color == PieceColor.Black)
            {
                castlingStatus[1] = true;

                if (castle == CastleType.King)
                {
                    _hash ^= PolyglotConstants.Keys[770];
                }
                else if (castle == CastleType.Queen)
                {
                    _hash ^= PolyglotConstants.Keys[771];
                }
            }
        }

        if (move.Parameter is MoveEnPassant)
        {
            MoveEnPassant moveEnPassant = (MoveEnPassant)move.Parameter;

            _hash ^= PolyglotConstants.Keys[772 + moveEnPassant.CapturedPawnPosition.X];
        }

        if (move.CapturedPiece is not null)
        {
            _hash ^= PolyglotConstants.Keys[64 * (pieceHash[colour][move.CapturedPiece.Type.Value - 1]) + 8 * move.NewPosition.Y + move.NewPosition.X];
        }

        if (move.Piece.Color == PieceColor.Black)
        {
            _hash ^= PolyglotConstants.Keys[780];
        }
    }

    private void InitialHash()
    {
        _hash = 0;

        for (short file = 0; file < 8; file++)
        {
            for (short rank = 0; rank < 8; rank++)
            {
                if (board[file, rank] is not null)
                {
                    int colour = board[file, rank].Color == PieceColor.White ? 1 : 0;

                    _hash ^= PolyglotConstants.Keys[64 * pieceHash[colour][board[file, rank].Type.Value - 1] + 8 * rank + file];
                }
            }
        }
        
        for (int i = 768; i <= 771; i++)
        {
            _hash ^= PolyglotConstants.Keys[i];
        }      

        if (board.Turn == PieceColor.White)
        {
            _hash ^= PolyglotConstants.Keys[780];
        }

        castlingStatus = new bool[2] { false, false };
    }

    private double CheckTranspositon()
    {
        if (_transpositionTable.ContainsKey(_hash))
        {
            return _transpositionTable[_hash];
        }
        else
        {
            return -1;
        }
    }

    public void PrintBoard()
    {
        for(short i = 7; i >= 0; i--)
        {
            for (short j = 0; j < 8; j++)
            {
                if (board[j, i] != null)
                {
                    Console.Write(board[j, i].ToFenChar());
                }
                else
                {
                    Console.Write(" ");
                }
            }
            Console.WriteLine("");
        }
    }


    public double EvaluateBoard()
    {
        double evaluation = 0;

        evaluation += EvaluateMaterial(); //Console.WriteLine(evaluation);
        evaluation += EvaluatePawns(); //Console.WriteLine(evaluation);
        evaluation += EvaluateCastling(); //Console.WriteLine(evaluation);
        //evaluation += EvaluatePieceTables(); Console.WriteLine(evaluation);

        if (board.IsEndGame) evaluation += EvaluateEndGame();

        return evaluation;
    }

    private double EvaluatePieceTables()
    {
        double evaluation = 0;

        double MGEval = 0;
        for (short i = 0; i < 8; i++)
        {
            for (short j = 0; j < 8; j++)
            {
                if (board[i, j] != null)
                {
                    MGEval += MGPieceTables[(board[i, j].Color - 1) * 6 + board[i, j].Type.Value - 1][i * 8 + j];
                }
            }
        }

        double EGEval = 0;
        for (short i = 0; i < 8; i++)
        {
            for (short j = 0; j < 8; j++)
            {
                if (board[i, j] != null)
                {
                    EGEval += EGPieceTables[(board[i, j].Color - 1) * 6 + board[i, j].Type.Value - 1][i * 8 + j];
                }
            }
        }

        int phase = GetGamePhase();
        evaluation = ((MGEval * (256 - phase)) + (EGEval * phase)) / 256;

        return evaluation;
    }

    private double EvaluateCastling()
    {
        double evaluation = 0;

        if (castlingStatus[0] is true)
        {
            evaluation += 100;
        }
        if (castlingStatus[1] is true)
        {
            evaluation -= 100;
        }

        return evaluation;
    }

    private double EvaluateMaterial()
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

        return evaluation;
    }

    private double EvaluatePawns()
    {
        double evaluation = 0;

        bool[][] pawnFiles = new bool[2][] { new bool[8], new bool[8] }; 

        for (short i = 0; i < 8; i++)
        {
            for (short j = 0; j < 8; j++)
            {
                Piece piece = board[i, j];

                if (piece is not null && piece.Type == Chess.PieceType.Pawn)
                {
                    pawnFiles[piece.Color.Value - 1][i] = true;
                }
            }
        }

        for (int file = 0; file < 8; file++)
        {
            if (pawnFiles[0][file])
            {
                bool isolated = true;
                // Check if there are pawns on adjacent files
                if (file > 0 && pawnFiles[0][file - 1])
                {
                    isolated = false;
                }
                if (file < 7 && pawnFiles[0][file + 1])
                {
                    isolated = false;
                }

                if (isolated)
                {
                    evaluation -= 75;
                }
            }

            if (pawnFiles[1][file])
            {
                bool isolated = true;
                // Check if there are pawns on adjacent files
                if (file > 0 && pawnFiles[1][file - 1])
                {
                    isolated = false;
                }
                if (file < 7 && pawnFiles[1][file + 1])
                {
                    isolated = false;
                }

                if (isolated)
                {
                    evaluation += 75;
                }
            }
        }

        return evaluation;
    }

    private double EvaluateEndGame()
    {
        if (board.EndGame.WonSide is null) return 0;
        if (board.EndGame.WonSide == PieceColor.White) return int.MaxValue - 1;
        if (board.EndGame.WonSide == PieceColor.Black) return int.MinValue + 1;

        return -1;
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
            char lastChar = move.Parameter.ShortStr.Last();

            if (lastChar == 'Q' || lastChar == 'R' || lastChar == 'B' || lastChar == 'N')
            {
                moveString += char.ToLower(lastChar);
            }
        }

        return moveString;
    }
}