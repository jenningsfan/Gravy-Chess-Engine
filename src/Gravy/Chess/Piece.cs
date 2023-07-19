using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravy.GravyChess
{
    enum Colour
    {
        White,
        Black,
    }

    enum PieceType
    {
        Pawn,
        Knight,
        Bishop,
        Rook,
        Queen,
        King,
    }

    struct Piece
    {
        public Colour Colour;
        public PieceType Type;
        public int BitboardIndex;

        public Piece(Colour colour, PieceType type)
        {
            Colour = colour;
            Type = type;

            BitboardIndex = (int)Type + 6 * (int)Colour;
        }

        public Piece(int bitboardIndex)
        {
            BitboardIndex = bitboardIndex;

            Colour = BitboardIndex > 5 ? Colour.Black : Colour.White;
            Type = (PieceType)(BitboardIndex - 6 * (int)Colour);
        }
    }
}
