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

        public Gravy()
        {
            
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

        public void ChooseMove()
        {
            Move[] moves = board.Moves();
            board.Move(moves[Random.Shared.Next(moves.Length)]);
        }

        public void DoMove(string move)
        {
            board.Move(new Move(move[0..1], move[2..3]));
        }
        
    }
}
