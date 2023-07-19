using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravy.GravyChess
{
    internal struct Move
    {
        public int StartSquare;
        public int TargetSquare;
        public Piece Piece;

        public bool IsCapture;
        public Piece CapturedPiece;

        public bool IsCastling;
        public bool IsEnPassant;
        public int EnPassantSquare;

        public Move(int startSquare, int targetSquare, Piece piece, bool isCapture = false, Piece? capturedPiece = null, bool isCastling = false, bool isEnPassant = false, int enPassantSquare = -1)
        {
            StartSquare = startSquare;
            TargetSquare = targetSquare;
            Piece = piece;

            IsCapture = isCapture;
            CapturedPiece = (Piece)capturedPiece;

            IsCastling = isCastling;
            IsEnPassant = isEnPassant;
            EnPassantSquare = enPassantSquare;
        }

        public Move(string move, Board board)
        {
            StartSquare = Board.ConvertNotationSquare(move[0..2]);
            TargetSquare = Board.ConvertNotationSquare(move[2..4]);

            Piece = new Piece(board.FindPieceType(StartSquare));

            IsCapture = board.FindPieceType(TargetSquare) != -1;
            CapturedPiece = new Piece(board.FindPieceType(TargetSquare));

            IsCastling = Piece.Type == PieceType.King && (StartSquare - TargetSquare == 2 || StartSquare - TargetSquare == -2);
            IsEnPassant = Piece.Type == PieceType.Pawn && !IsCapture && (StartSquare - TargetSquare == 7 || StartSquare - TargetSquare == -7 || StartSquare - TargetSquare == 9 || StartSquare - TargetSquare == -9);

            if (IsEnPassant)
            {
                if (StartSquare - TargetSquare > 0) // Capturing below
                {
                    EnPassantSquare = TargetSquare + 8; // Square above
                }
                else // Capturing above
                {
                    EnPassantSquare = TargetSquare - 8; // Square below
                }

                CapturedPiece = new Piece(board.FindPieceType(EnPassantSquare));
            }
            else { EnPassantSquare = -1; }
        }
    }
}
