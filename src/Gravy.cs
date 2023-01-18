using Chess;

namespace Gravy
{
    internal class Gravy
    {
        private ChessBoard board;
        private double[][] pieceValues = new double[][]// P, R, N, B, Q, K
        {
            new double[] { 1, 5.25, 3.5, 3.5, 10, 0 },
            new double[] { -1, -5.25, -3.5, -3.5, -10, 0 },
        };

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

        public string ChooseMove()
        {
            Move bestMove = NegaMax(board, 2, int.MinValue + 1, int.MaxValue - 1, (board.Turn == PieceColor.White) ? 1 : -1).Item1;

            board.Move(bestMove);

            return GetMoveString(bestMove);
        }

        private Tuple<Move, double> NegaMax(ChessBoard board, int depth, double alpha, double beta, int colour)
        {
            Move[] moves = board.Moves();

            if (depth <= 0 || board.IsEndGame)
            {
                return Tuple.Create((Move)null, colour * EvaluateBoard(board));
            }

            Move bestMove = null;
            double maxEval = int.MinValue;

            foreach (Move move in OrderMoves(moves, colour))
            {
                board.Move(move);

                double eval = -NegaMax(board, depth - 1, -beta, -alpha, -colour).Item2;
                if (eval > maxEval)
                {
                    eval = maxEval;
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
                queue.Enqueue(move, colour * EvaluateBoard(board));
                board.Cancel();
            }

            List<Move> orderedMoves = new();

            while (queue.Count > 0)
            {
                orderedMoves.Add(queue.Dequeue());
            }

            return orderedMoves;
        }

        private double EvaluateBoard(ChessBoard board)
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
            switch (move.ToLower().Last())
            {
                case 'q':
                    board.OnPromotePawn += (sender, e) => e.PromotionResult = PromotionType.ToQueen;
                    break;
                case 'r':
                    board.OnPromotePawn += (sender, e) => e.PromotionResult = PromotionType.ToRook;
                    break;
                case 'b':
                    board.OnPromotePawn += (sender, e) => e.PromotionResult = PromotionType.ToBishop;
                    break;
                case 'n':
                    board.OnPromotePawn += (sender, e) => e.PromotionResult = PromotionType.ToKnight;
                    break;
            }
            
            board.Move(new Move(move[0..2], move[2..4]));
        }

        private string GetMoveString(Move move)
        {
            string moveString = move.OriginalPosition.ToString() + move.NewPosition.ToString();

            if (move.Parameter != null)
            {
                char lastChar = move.San.Last();

                if (lastChar == 'Q' || lastChar == 'R' || lastChar == 'B' || lastChar == 'N')
                {
                    Console.WriteLine($"last: {lastChar}");
                    moveString += char.ToLower(lastChar);
                }
            }

            return moveString;
        }
    }
}
