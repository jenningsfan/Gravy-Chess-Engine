namespace Gravy.GravyChess
{
    internal class Board
    {
        public ulong[] bitboards;
        public Piece[] mailbox;

        public Colour turn;

        public Board()
        {
            bitboards = new ulong[12];
        }

        public void LoadFen(string fen)
        {
            Dictionary<char, Piece> pieceLookup = new Dictionary<char, Piece> {
                { 'P', new(Colour.White, PieceType.Pawn) },
                { 'N', new(Colour.White, PieceType.Knight) },
                { 'B', new(Colour.White, PieceType.Bishop) },
                { 'R', new(Colour.White, PieceType.Rook) },
                { 'Q', new(Colour.White, PieceType.Queen) },
                { 'K', new(Colour.White, PieceType.Knight) },
                { 'p', new(Colour.Black, PieceType.Pawn) },
                { 'n', new(Colour.Black, PieceType.Knight) },
                { 'b', new(Colour.Black, PieceType.Bishop) },
                { 'r', new(Colour.Black, PieceType.Rook) },
                { 'q', new(Colour.Black, PieceType.Queen) },
                { 'k', new(Colour.Black, PieceType.Knight) },
            };

            int squareIndex = 63;

            foreach (string rank in fen.Split(" ")[0].Split("/"))
            {
                foreach (char piece in rank)
                {
                    if (char.IsDigit(piece))
                    {
                        squareIndex -= piece - '0';
                    }
                    else
                    {
                        bitboards[pieceLookup[piece].BitboardIndex()] |= 1ul << squareIndex;
                        mailbox[squareIndex] = pieceLookup[piece];

                        squareIndex--;
                    }
                }
            }
        }

        public void PrintBoard()
        {
            Dictionary<int, char> pieceLookup = new Dictionary<int, char> {
                { 0, 'P' },
                { 1, 'N' },
                { 2, 'B' },
                { 3, 'R' },
                { 4, 'Q' },
                { 5, 'K' },
                { 6, 'p' },
                { 7, 'n' },
                { 8, 'b' },
                { 9, 'r' },
                { 10, 'q' },
                { 11, 'k' },
            };

            for (int i = 63; i >= 0; i--)
            {
                bool foundPiece = false;

                for (int j = 0; j < 12; j++)
                {
                    if ((bitboards[j] & (1ul << i)) == (1ul << i))
                    {
                        Console.Write(pieceLookup[j]);
                        foundPiece = true;
                        break;
                    }
                }

                if (!foundPiece)
                {
                    Console.Write(" ");
                }

                if (i % 8 == 0)
                {
                    Console.WriteLine("");
                }
            }
        }
    }
}
