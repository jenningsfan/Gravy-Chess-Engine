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
        private int promotionPiece;

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
            Move[] moves = board.Moves();
            Move bestMove = null;

            int depth = 2;

            if (board.Turn == PieceColor.White)
            {
                double maxEval = int.MinValue;

                foreach (Move move in moves)
                {
                    promotionPiece = -1;
                    board.Move(move);

                    double eval = MiniMax(board, depth, int.MinValue, int.MaxValue, false);
                    if (eval > maxEval)
                    {
                        bestMove = move;
                        maxEval = eval;
                    }
                    board.Cancel();
                }
            }
            else
            {
                double minEval = int.MaxValue;

                foreach (Move move in moves)
                {
                    promotionPiece = -1;
                    board.Move(move);

                    double eval = MiniMax(board, depth, int.MinValue, int.MaxValue, false);
                    if (eval < minEval)
                    {
                        bestMove = move;
                        minEval = eval;
                    }
                    board.Cancel();
                }
            }

            board.Move(bestMove);

            return GetMoveString(bestMove);
        }

        private double MiniMax(ChessBoard board, int depth, double alpha, double beta, bool whiteMax)
        {
            Move[] moves = board.Moves();

            if (depth <= 0)
            {
                return EvaluateBoard(board);
            }

            if (board.IsEndGame)
            {
                if (board.EndGame.WonSide == PieceColor.White)
                {
                    if (whiteMax)
                    {
                        return double.MaxValue;
                    }
                    else
                    {
                        return double.MinValue;
                    }
                }
                else if (board.EndGame.WonSide == PieceColor.Black)
                {
                    if (whiteMax)
                    {
                        return double.MinValue;
                    }
                    else
                    {
                        return double.MaxValue;
                    }
                }
                else
                {
                    return 0;
                }
            }

            if (whiteMax)
            {
                double maxEval = int.MinValue;

                foreach (Move move in moves)
                {
                    promotionPiece = -1;
                    board.Move(move);

                    double eval = MiniMax(board, depth - 1, alpha, beta, false);
                    maxEval = Math.Max(maxEval, eval);
                    alpha = Math.Max(alpha, eval);

                    board.Cancel();

                    if (beta <= alpha)
                    {
                        break;
                    } 
                }

                return maxEval;
            }
            else
            {
                double minEval = int.MaxValue;

                foreach (Move move in moves)
                {
                    promotionPiece = -1;
                    board.Move(move);

                    double eval = MiniMax(board, depth - 1, alpha, beta, true);
                    minEval = Math.Min(minEval, eval);
                    beta = Math.Min(beta, eval);

                    board.Cancel();

                    if (beta <= alpha)
                    {
                        break;
                    }  
                }

                return minEval;
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
                    case 'n':
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
                    case 'n':
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
