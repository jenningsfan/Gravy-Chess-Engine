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
            Move move = moves[random.Next(moves.Length)];
            board.Move(move);

            string moveString = move.OriginalPosition.ToString() + move.NewPosition.ToString();
            if (move.Parameter != null)
            {
                moveString += move.San.Last();
            }

            return moveString;
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
    }
}
