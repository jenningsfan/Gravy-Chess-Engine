using Chess;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravy.GravyChess
{
    internal enum CastlingType
    {
        Short,
        Long
    }

    internal struct Move
    {
        public int StartSquare;
        public int TargetSquare;
        public Piece Piece;

        public bool IsCapture;
        public Piece? CapturedPiece;

        public bool IsCastling;
        public CastlingType CastleType;

        public bool IsEnPassant;
        public int EnPassantSquare;

        public bool IsPromotion;
        public Piece? PromotionPiece;

        public Move(int startSquare, int targetSquare, Piece piece, bool isCapture = false, Piece? capturedPiece = null, bool isCastling = false, CastlingType castleType = 0, bool isEnPassant = false, int enPassantSquare = -1, bool isPromotion = false, Piece? promotionPiece = null)
        {
            StartSquare = startSquare;
            TargetSquare = targetSquare;
            Piece = piece;

            IsCapture = isCapture;
            CapturedPiece = capturedPiece;

            IsCastling = isCastling;
            CastleType = castleType;

            IsEnPassant = isEnPassant;
            EnPassantSquare = enPassantSquare;

            IsPromotion = isPromotion;
            PromotionPiece = promotionPiece;
        }

        public Move(string move, Board board)
        {
            StartSquare = Board.ConvertNotationSquare(move[0..2]);
            TargetSquare = Board.ConvertNotationSquare(move[2..4]);

            Piece = new Piece(board.FindPieceType(StartSquare));

            IsCapture = board.FindPieceType(TargetSquare) != -1;
            CapturedPiece = new Piece(board.FindPieceType(TargetSquare));

            IsCastling = Piece.Type == PieceType.King && (StartSquare - TargetSquare == 2 || StartSquare - TargetSquare == -2);
            
            if (IsCastling)
            {
                if (TargetSquare > StartSquare)
                {
                    CastleType = CastlingType.Long;
                }
                else
                {
                    CastleType = CastlingType.Short;
                }
            }
            else { CastleType = 0; }
            
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

            if (move.Length == 5)
            {
                Dictionary<char, PieceType> pieceLookup = new()
                {
                    { 'p', PieceType.Pawn },
                    { 'n', PieceType.Knight },
                    { 'b', PieceType.Bishop },
                    { 'r', PieceType.Rook },
                    { 'q', PieceType.Queen },
                };

                IsPromotion = true;
                PromotionPiece = new Piece(Piece.Colour, pieceLookup[move[4]]);
            }
            else
            {
                IsPromotion = false;
                PromotionPiece = null;
            }
        }
    }
}
