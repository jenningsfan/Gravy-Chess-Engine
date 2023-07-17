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
        Black
    }

    enum PieceType
    {
        Pawn,
        Knight,
        Bishop,
        Rook,
        Queen,
        King,
        Empty,
    }

    struct Piece
    {
        public Colour Colour;
        public PieceType Type;
    }
}
