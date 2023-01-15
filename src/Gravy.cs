using Chess;

namespace Gravy
{
    internal class Gravy
    {
        private ChessBoard board;
        private int promotionPiece;
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
            board.OnPromotePawn += DoPromotion;
            //board.OnPromotePawn += (sender, e) => e.PromotionResult = PromotionType.ToKnight;
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
            Move bestMove = MiniMax(board, 2, int.MinValue + 1, int.MaxValue - 1, (board.Turn == PieceColor.White) ? 1 : -1).Item1;

            board.Move(bestMove);

            return GetMoveString(bestMove);
        }

        private Tuple<Move, double> MiniMax(ChessBoard board, int depth, double alpha, double beta, int colour)
        {
            Move[] moves = board.Moves();

            if (depth <= 0 || board.IsEndGame)
            {
                return Tuple.Create((Move)null, colour * EvaluateBoard(board));
            }

            Move bestMove = null;
            double maxEval = int.MinValue;

            foreach (Move move in moves)
            {
                promotionPiece = -1;
                board.Move(move);

                double eval = -MiniMax(board, depth - 1, -beta, -alpha, -colour).Item2;
                if (eval > maxEval)
                {
                    eval = maxEval;
                    bestMove = move;
                }
                alpha = Math.Max(alpha, eval);

                board.Cancel();

                if (alpha >= beta)
                {
                    //break;
                }
            }

            return Tuple.Create(bestMove, maxEval);
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
                    promotionPiece = 1;
                    break;
                case 'r':
                    promotionPiece = 2;
                    break;
                case 'b':
                    promotionPiece = 3;
                    break;
                case 'n':
                    promotionPiece = 4;
                    break;
            }

            board.Move(new Move(move[0..2], move[2..4]));
        }

        private void DoPromotion(object sender, PromotionEventArgs e)
        {
            Console.WriteLine($"promo: {promotionPiece}");
            if (promotionPiece != -1) e.PromotionResult = (PromotionType)promotionPiece;
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
