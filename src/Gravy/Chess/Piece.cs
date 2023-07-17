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

        public Piece(Colour colour, PieceType type)
        {
            Colour = colour;
            Type = type;
        }

        public int BitboardIndex()
        {
            return (int)Type + 6 * (int)Colour;
        }
    }
}
