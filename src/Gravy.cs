using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chess;

namespace Gravy
{
    internal class Gravy
    {
        private ChessBoard board;
        private Random random;
        private int promotionPiece;

        public Gravy()
        {
            random = new Random(4);

            StartNewGame();
        }
        
        public void StartNewGame()
        {
            ChessBoard board = new ChessBoard();
            board.OnPromotePawn += DoPromotion;
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
            Move[] moves = board.Moves();
            Move move = MiniMax(board, 2, board.Turn == PieceColor.White).Item1;

            board.Move(move);

            return GetMoveString(move);
        }

        private Tuple<Move, double> MiniMax(ChessBoard board, int depth, bool whiteMax)
        {
            Move[] moves = board.Moves();

            if (board.IsEndGame || depth <= 0)
            {
                return Tuple.Create((Move)null, EvaluateBoard(board));
            }

            if (whiteMax)
            {
                double maxEval = int.MinValue;
                Move bestMove = null;

                foreach (Move move in moves)
                {
                    board.Move(move);

                    double eval = MiniMax(board, depth - 1, false).Item2;
                    if (eval > maxEval)
                    {
                        maxEval = eval;
                        bestMove = move;
                    }

                    board.Cancel();
                }

                return Tuple.Create(bestMove, maxEval);
            }
            else
            {
                double minEval = int.MaxValue;
                Move bestMove = null;

                foreach (Move move in moves)
                {
                    board.Move(move);

                    double eval = MiniMax(board, depth - 1, true).Item2;
                    if (eval < minEval)
                    {
                        minEval = eval;
                        bestMove = move;
                    }

                    board.Cancel();
                }

                return Tuple.Create(bestMove, minEval);
            }
        }

        private double EvaluateBoard(ChessBoard board)
        {
            double evaluation = 0;

            foreach (Piece piece in board.CapturedWhite)
            {
                switch (piece.Type.AsChar)
                {
                    case 'p':
                        evaluation -= 1;
                        break;
                    case 'k':
                        evaluation -= 3.5;
                        break;
                    case 'b':
                        evaluation -= 3.5;
                        break;
                    case 'r':
                        evaluation -= 5.25;
                        break;
                    case 'q':
                        evaluation -= 10;
                        break;
                }
            }

            foreach (Piece piece in board.CapturedBlack)
            {
                switch (piece.Type.AsChar)
                {
                    case 'p':
                        evaluation += 1;
                        break;
                    case 'k':
                        evaluation += 3.5;
                        break;
                    case 'b':
                        evaluation += 3.5;
                        break;
                    case 'r':
                        evaluation += 5.25;
                        break;
                    case 'q':
                        evaluation += 10;
                        break;
                }
            }

            return evaluation;
        }

        public void DoMove(string move)
        {
            board.Move(new Move(move[0..2], move[2..4]));

            switch (move.Last())
            {
                case 'Q':
                    promotionPiece = 1;
                    break;
                case 'R':
                    promotionPiece = 2;
                    break;
                case 'B':
                    promotionPiece = 3;
                    break;
                case 'K':
                    promotionPiece = 4;
                    break;
            }
        }

        private void DoPromotion(object sender, PromotionEventArgs e)
        {
            e.PromotionResult = (PromotionType)promotionPiece;
        }

        private string GetMoveString(Move move)
        {
            string moveString = move.OriginalPosition.ToString() + move.NewPosition.ToString();
            if (move.Parameter != null)
            {
                moveString += move.San.Last();
            }

            return moveString;
        }
    }
}
