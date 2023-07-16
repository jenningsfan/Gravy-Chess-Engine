using Chess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravy;

internal static class Evaluation
{
    private static int[][] pieceValues = new int[][]// P, R, N, B, Q, K
    {
        new int[] {  100,  500,  320,  330,  900,  20000 },
        new int[] { -100, -500, -320, -330, -900, -20000 },
    };

    private static int pawnPhase = 0;
    private static int knightPhase = 1;
    private static int bishopPhase = 1;
    private static int rookPhase = 2;
    private static int queenPhase = 4;
    public static int[] piecePhases = new int[] { pawnPhase, rookPhase, knightPhase, bishopPhase, queenPhase, 0 }; // 0 for king

    public static int totalPhase = pawnPhase * 16 + knightPhase * 4 + bishopPhase * 4 + rookPhase * 4 + queenPhase * 2;

    public static int EvaluateBoard(ChessBoard board, bool[] castlingStatus)
    {
        if (board.IsEndGame) return EvaluateEndGame(board);

        int evaluation = 0;

        evaluation += EvaluateMaterial(board);
        //evaluation += EvaluatePawns();
        evaluation += EvaluateCastling(castlingStatus);
        evaluation += EvaluatePieceTables(board);

        return evaluation;
    }

    private static int EvaluatePieceTables(ChessBoard board)
    {
        int evaluation = 0;

        int MGEval = 0;
        for (short i = 0; i < 8; i++)
        {
            for (short j = 0; j < 8; j++)
            {
                if (board[i, j] != null)
                {
                    MGEval += PieceTables.MGPieceTables[board[i, j].Color - 1][board[i, j].Type.Value - 1][i * 8 + j];
                }
            }
        }

        int EGEval = 0;
        for (short i = 0; i < 8; i++)
        {
            for (short j = 0; j < 8; j++)
            {
                if (board[i, j] != null)
                {
                    EGEval += PieceTables.EGPieceTables[board[i, j].Color - 1][board[i, j].Type.Value - 1][i * 8 + j];
                }
            }
        }

        int phase = GetGamePhase(board);
        evaluation = ((MGEval * (256 - phase)) + (EGEval * phase)) / 256;

        return evaluation;
    }

    private static int GetGamePhase(ChessBoard board)
    {
        // This is taken from the tapered eval algorithim on https://www.chessprogramming.org/index.php?title=Tapered_Eval&oldid=25214
        int phase = totalPhase;

        for (short i = 0; i < 8; i++)
        {
            for (short j = 0; j < 8; j++)
            {
                if (board[i, j] != null)
                {
                    phase -= piecePhases[board[i, j].Type.Value - 1];
                }
            }
        }

        return (phase * 256 + (totalPhase / 2)) / totalPhase;
    }

    private static int EvaluateCastling(bool[] castlingStatus)
    {
        int evaluation = 0;

        if (castlingStatus[0] is true)
        {
            evaluation += 100;
        }
        if (castlingStatus[1] is true)
        {
            evaluation -= 100;
        }

        return evaluation;
    }

    private static int EvaluateMaterial(ChessBoard board)
    {
        int evaluation = 0;

        for (short i = 0; i < 8; i++)
        {
            for (short j = 0; j < 8; j++)
            {
                if (board[i, j] != null)
                {
                    evaluation += pieceValues[board[i, j].Color - 1][board[i, j].Type.Value - 1];
                }
            }
        }

        return evaluation;
    }

    private static int EvaluatePawns(ChessBoard board)
    {
        int evaluation = 0;

        bool[][] pawnFiles = new bool[2][] { new bool[8], new bool[8] };

        for (short i = 0; i < 8; i++)
        {
            for (short j = 0; j < 8; j++)
            {
                Piece piece = board[i, j];

                if (piece is not null && piece.Type == Chess.PieceType.Pawn)
                {
                    pawnFiles[piece.Color.Value - 1][i] = true;
                }
            }
        }

        for (int file = 0; file < 8; file++)
        {
            if (pawnFiles[0][file])
            {
                bool isolated = true;
                // Check if there are pawns on adjacent files
                if (file > 0 && pawnFiles[0][file - 1])
                {
                    isolated = false;
                }
                if (file < 7 && pawnFiles[0][file + 1])
                {
                    isolated = false;
                }

                if (isolated)
                {
                    evaluation -= 75;
                }
            }

            if (pawnFiles[1][file])
            {
                bool isolated = true;
                // Check if there are pawns on adjacent files
                if (file > 0 && pawnFiles[1][file - 1])
                {
                    isolated = false;
                }
                if (file < 7 && pawnFiles[1][file + 1])
                {
                    isolated = false;
                }

                if (isolated)
                {
                    evaluation += 75;
                }
            }
        }

        return evaluation;
    }

    private static int EvaluateEndGame(ChessBoard board)
    {
        if (board.EndGame.WonSide is null) return 0;
        if (board.EndGame.WonSide == PieceColor.White) return int.MaxValue - 1;
        if (board.EndGame.WonSide == PieceColor.Black) return int.MinValue + 1;

        return 0;
    }
}