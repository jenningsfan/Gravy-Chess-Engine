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
    public int nodesPruned;
    public int transpositionHits;

    private Stopwatch timer;
    private long maxTime;
    private bool outOfTime;

    public ChessBoard board;
    private PolyglotBook openingBook;
    private Random _random;
    private ulong _hash;

    private static int[][] pieceHash = new int[][]// P, R, N, B, Q, K
    {
        new int[] { 0, 6, 2, 4, 8, 10 },
        new int[] { 1, 7, 3, 5, 9, 11 },
    };

    private Move? bestMove;

    public bool[] castlingStatus;

    private LimitedSizeDictionary<ulong, int> _transpositionTable;
    private int _transpositionSize = 30000;

    public Gravy()
    {
        string bookName = "gm2001.bin";
        //string bookName = "/Cerebellum3Merge.bin";
        string firstPath = Path.Join(Path.GetFullPath("engines/books"), bookName);
        string secondPath = Path.Join(Path.GetFullPath("../../../../../lichess-bot/engines/books"), bookName);

        //Console.WriteLine(firstPath);
        //Console.WriteLine(secondPath);

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

        bestMove = null;

        StartNewGame();
    }

    public void StartNewGame()
    {
        board = new ChessBoard();
        board.AutoEndgameRules = AutoEndgameRules.All;

        InitialHash();

        _random = new Random();
        //_transpositionTable = new(_transpositionSize);
        //_transpositionTable = new(_transpositionSize);
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

    public Tuple<bool, bool, bool, string> ChooseMove(int depth, long time)
    {
        nodesSearched = 0;
        nodesPruned = 0;
        transpositionHits = 0;

        maxTime = time;
        outOfTime = false;

        _transpositionTable = new(_transpositionSize);

        timer = new Stopwatch();
        timer.Start();

        Move polyglotMove = GetPolyglotMove();

        if (polyglotMove is not null)
        {
            bestMove = polyglotMove;
        }
        else
        {
            Search(board, depth, int.MinValue + 1, int.MaxValue - 1, (board.Turn == PieceColor.White) ? 1 : -1);
            //Console.WriteLine($"\n{bestMove}: {result.Item2}");
        }
       
        //board.Move(bestMove);

        timer.Stop();

        bool isMate = bestMove is null ? false : bestMove.IsMate;

        return Tuple.Create(outOfTime, polyglotMove is not null, isMate, GetMoveString(bestMove));
    }

    private int Search(ChessBoard board, int depth, int alpha, int beta, int colour)
    {
        if (depth == 0)
        {
            return colour * Evaluation.EvaluateBoard(board, castlingStatus);
            //return Tuple.Create((Move)null, QuiescenceSearch(colour, alpha, beta));
        }

        if (board.IsEndGame)
        {
            return colour * Evaluation.EvaluateBoard(board, castlingStatus);
        }

        Move[] moves = OrderMoves(board.Moves());

        bestMove = null;
        int maxEval = int.MinValue + 1;

        for (int i = 0; i < moves.Length; i++)
        {
            nodesSearched++;

            if (timer.ElapsedMilliseconds > maxTime || outOfTime)
            {
                outOfTime = true;
                break;
            }

            Move move = moves[i];

            UpdateHash(move);
            board.Move(move);

            ulong oldHash = _hash;

            int transpositonResult = CheckTranspositon();
            //int transpositonResult = -1;
            int eval;

            if (transpositonResult == -1)
            {
                if (i == 0)
                {
                    eval = -Search(board, depth - 1, -beta, -alpha, -colour);
                }
                else
                {
                    eval = -Search(board, depth - 1, -alpha - 1, -alpha, -colour);

                    if (alpha < eval && eval < beta)
                    {
                        eval = -Search(board, depth - 1, -beta, -eval, -colour);
                    }
                }

                _transpositionTable[_hash] = eval;
            }
            else
            {
                transpositionHits++;
                eval = transpositonResult;
            }
            
            if (eval >= maxEval)
            {
                maxEval = eval;
                bestMove = move;
            }
            alpha = Math.Max(alpha, eval);
            
            board.Cancel();
            _hash = oldHash;

            if (alpha >= beta)
            {
                nodesPruned++;
                break;
            }
        }

        //Console.WriteLine($"{bestMove}: {maxEval}");

        return maxEval;
    }

    // Search all captures recursively
    // This stops stpuid blunders
    private int QuiescenceSearch(int colour, int alpha, int beta)
    {
        int evaluation = Evaluation.EvaluateBoard(board, castlingStatus);

        if (evaluation >= beta)
        {
            return beta;
        }
        if (evaluation > alpha)
        {
            alpha = evaluation;
        }

        Move[] moves = OrderMoves(board.Moves(), true);

        foreach (Move move in moves)
        {
            board.Move(move);
            evaluation = -QuiescenceSearch(-colour, -beta, -alpha);
            board.Cancel();

            if (evaluation >= beta)
            {
                return beta;
            }
            if (evaluation > alpha)
            {
                alpha = evaluation;
            }
        }

        return alpha;
    }

    private Move[] OrderMoves(Move[] moves, bool onlyCaptures = false)
    {
        int movesLength = moves.Length;
        int nonQuietLength = 0;
        int quietLength = 0;

        Move[] nonQuiet = new Move[movesLength];
        Move[] quiet = new Move[movesLength];

        for (int i = 0; i < movesLength; i++)
        {
            Move move = moves[i];

            if (move.CapturedPiece != null || move.IsCheck)
            {
                nonQuiet[nonQuietLength] = move;
                nonQuietLength++;
            }
            else
            {
                quiet[quietLength] = move;
                quietLength++;
            }
        }

        if (onlyCaptures == true)
        {
            return nonQuiet[..nonQuietLength];
        }

        Move[] orderedMoves = new Move[movesLength];
        Array.Copy(nonQuiet, orderedMoves, nonQuietLength);
        Array.Copy(quiet, 0, orderedMoves, nonQuietLength, quietLength);

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

    private int CheckTranspositon()
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

    public void DoMove(string move)
    {
        if (move == "") { return; }

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