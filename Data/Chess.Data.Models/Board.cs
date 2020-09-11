﻿namespace Chess.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Chess.Common;
    using Chess.Common.Enums;
    using Chess.Data.Models.EventArgs;
    using Chess.Data.Models.Pieces;
    using Chess.Data.Models.Pieces.Contracts;
    using Chess.Data.Models.Pieces.Helpers;

    public class Board : ICloneable
    {
        private Queue<string> movesThreefold;
        private Queue<string> movesFivefold;
        private string[] arrayThreefold;
        private string[] arrayFivefold;

        private string[] letters = new string[] { "A", "B", "C", "D", "E", "F", "G", "H" };

        private Dictionary<string, Piece> setup = new Dictionary<string, Piece>()
        {
            { "A1", new Rook(Color.White) },  { "B1", new Knight(Color.White) }, { "C1", new Bishop(Color.White) }, { "D1", new Queen(Color.White) },
            { "E1", new King(Color.White) }, { "F1", new Bishop(Color.White) }, { "G1", new Knight(Color.White) }, { "H1", new Rook(Color.White) },
            { "A2", new Pawn(Color.White) },  { "B2", new Pawn(Color.White) },   { "C2", new Pawn(Color.White) },   { "D2", new Pawn(Color.White) },
            { "E2", new Pawn(Color.White) },  { "F2", new Pawn(Color.White) },   { "G2", new Pawn(Color.White) },   { "H2", new Pawn(Color.White) },
            { "A7", new Pawn(Color.Black) },  { "B7", new Pawn(Color.Black) },   { "C7", new Pawn(Color.Black) },   { "D7", new Pawn(Color.Black) },
            { "E7", new Pawn(Color.Black) },  { "F7", new Pawn(Color.Black) },   { "G7", new Pawn(Color.Black) },   { "H7", new Pawn(Color.Black) },
            { "A8", new Rook(Color.Black) },  { "B8", new Knight(Color.Black) }, { "C8", new Bishop(Color.Black) }, { "D8", new Queen(Color.Black) },
            { "E8", new King(Color.Black) }, { "F8", new Bishop(Color.Black) }, { "G8", new Knight(Color.Black) }, { "H8", new Rook(Color.Black) },
        };

        public Board()
        {
            this.movesThreefold = new Queue<string>();
            this.movesFivefold = new Queue<string>();
            this.arrayThreefold = new string[9];
            this.arrayFivefold = new string[17];

            this.Matrix = Factory.GetMatrix();

            this.Source = Factory.GetSquare();
            this.Target = Factory.GetSquare();
        }

        public event EventHandler OnMessage;

        public event EventHandler OnMoveComplete;

        public event EventHandler OnTakePiece;

        public event EventHandler OnThreefoldDrawAvailable;

        public Square[][] Matrix { get; set; }

        public Square Source { get; set; }

        public Square Target { get; set; }

        public void Initialize()
        {
            var toggle = Color.White;

            for (int row = 0; row < GlobalConstants.BoardRows; row++)
            {
                for (int col = 0; col < GlobalConstants.BoardCols; col++)
                {
                    var name = this.letters[col] + (8 - row);
                    var square = new Square()
                    {
                        Position = Factory.GetPosition(row, col),
                        Piece = this.setup.FirstOrDefault(x => x.Key.Equals(name)).Value,
                        Color = toggle,
                        Name = name,
                    };

                    if (col != 7)
                    {
                        toggle = toggle == Color.White ? Color.Black : Color.White;
                    }

                    this.Matrix[row][col] = square;
                }
            }
        }

        public object Clone()
        {
            var board = Factory.GetBoard();

            for (int row = 0; row <= 7; row++)
            {
                for (int col = 0; col <= 7; col++)
                {
                    board.Matrix[row][col] = this.Matrix[row][col].Clone() as Square;
                }
            }

            return board;
        }

        public bool TryMove(string source, string target, string targetFen, Player movingPlayer, Player opponent, int turn)
        {
            this.Source = this.GetSquare(source);
            this.Target = this.GetSquare(target);

            var oldSource = this.Source.Clone() as Square;
            var oldTarget = this.Target.Clone() as Square;

            if (this.MovePiece(movingPlayer, turn) ||
                this.TakePiece(movingPlayer, turn) ||
                this.EnPassantTake(movingPlayer, turn))
            {
                if (this.Target.Piece is Pawn && this.Target.Piece.IsLastMove)
                {
                    this.Target.Piece = Factory.GetQueen(movingPlayer.Color);
                    var isWhite = movingPlayer.Color == Color.White ? true : false;

                    this.GetPawnPromotionFenString(targetFen, isWhite);
                    this.CalculateAttackedSquares();
                }

                if (movingPlayer.IsCheck)
                {
                    this.OnMessage?.Invoke(movingPlayer, new MessageEventArgs(Notification.CheckSelf));
                    return false;
                }

                string notation = this.GetAlgebraicNotation(oldSource, oldTarget, opponent, turn);
                this.OnMoveComplete?.Invoke(movingPlayer, new NotationEventArgs(notation));

                return true;
            }

            return false;
        }

        public bool IsCheckmate(Player movingPlayer, Player opponent)
        {
            var king = this.GetKingSquare(opponent.Color);

            if (!this.IsKingAbleToMove(king, movingPlayer) &&
                !this.AttackingPieceCanBeTaken(this.Target, movingPlayer) &&
                !this.OtherPieceCanBlockTheCheck(king, this.Target, opponent))
            {
                opponent.IsCheckMate = true;
                return true;
            }

            return false;
        }

        public bool IsStalemate(Player player)
        {
            for (int y = 0; y < GlobalConstants.BoardRows; y++)
            {
                for (int x = 0; x < GlobalConstants.BoardCols; x++)
                {
                    var currentFigure = this.Matrix[y][x].Piece;

                    if (currentFigure != null && currentFigure.Color == player.Color)
                    {
                        currentFigure.IsMoveAvailable(this.Matrix);
                        if (currentFigure.IsMovable)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public bool IsDraw()
        {
            int counterBishopKnightWhite = 0;
            int counterBishopKnightBlack = 0;

            for (int y = 0; y < GlobalConstants.BoardRows; y++)
            {
                for (int x = 0; x < GlobalConstants.BoardCols; x++)
                {
                    var currentFigure = this.Matrix[y][x].Piece;

                    if (!(currentFigure == null || currentFigure is King))
                    {
                        if (currentFigure is Pawn ||
                            currentFigure is Rook ||
                            currentFigure is Queen ||
                            counterBishopKnightWhite > 1 ||
                            counterBishopKnightBlack > 1)
                        {
                            return false;
                        }

                        if (currentFigure.Color == Color.White)
                        {
                            counterBishopKnightWhite++;
                        }
                        else
                        {
                            counterBishopKnightBlack++;
                        }
                    }
                }
            }

            return true;
        }

        public void IsThreefoldRepetionDraw(string fen, Player movingPlayer, Player opponent)
        {
            this.movesThreefold.Enqueue(fen);

            movingPlayer.IsThreefoldDrawAvailable = false;
            this.OnThreefoldDrawAvailable?.Invoke(movingPlayer, new ThreefoldDrawEventArgs(false));

            if (this.movesThreefold.Count == 9)
            {
                this.arrayThreefold = this.movesThreefold.ToArray();

                var isFirstFenSame = string.Compare(fen, this.arrayThreefold[0]) == 0;
                var isFiveFenSame = string.Compare(fen, this.arrayThreefold[4]) == 0;

                if (isFirstFenSame && isFiveFenSame)
                {
                    opponent.IsThreefoldDrawAvailable = true;
                    this.OnThreefoldDrawAvailable?.Invoke(movingPlayer, new ThreefoldDrawEventArgs(true));
                }

                this.movesThreefold.Dequeue();
            }
        }

        public bool IsFivefoldRepetitionDraw(string fen)
        {
            this.movesFivefold.Enqueue(fen);

            if (this.movesFivefold.Count == 17)
            {
                this.arrayFivefold = this.movesFivefold.ToArray();

                var isFirstFenSame = string.Compare(fen, this.arrayFivefold[0]) == 0;
                var isFiveFenSame = string.Compare(fen, this.arrayFivefold[4]) == 0;
                var isNineFenSame = string.Compare(fen, this.arrayFivefold[8]) == 0;
                var isThirteenFenSame = string.Compare(fen, this.arrayFivefold[12]) == 0;

                if (isFirstFenSame && isFiveFenSame && isNineFenSame && isThirteenFenSame)
                {
                    return true;
                }
                else
                {
                    this.movesFivefold.Dequeue();
                }
            }

            return false;
        }

        public bool IsPlayerChecked(Player player)
        {
            var kingSquare = this.GetKingSquare(player.Color);

            if (kingSquare.IsAttacked.Where(x => x.Color != kingSquare.Piece.Color).Any())
            {
                player.IsCheck = true;
                return true;
            }

            return false;
        }

        private bool MovePiece(Player movingPlayer, int turn)
        {
            if (this.Target.Piece == null &&
                movingPlayer.Color == this.Source.Piece.Color &&
                this.Source.Piece.Move(this.Target.Position, this.Matrix, turn))
            {
                if (!this.Try(movingPlayer))
                {
                    movingPlayer.IsCheck = true;
                    return true;
                }

                this.Target.Piece.IsFirstMove = false;
                return true;
            }

            return false;
        }

        private bool TakePiece(Player movingPlayer, int turn)
        {
            if (this.Target.Piece != null &&
                this.Target.Piece.Color != this.Source.Piece.Color &&
                movingPlayer.Color == this.Source.Piece.Color &&
                this.Source.Piece.Take(this.Target.Position, this.Matrix, turn))
            {
                var piece = this.Target.Piece;

                if (!this.Try(movingPlayer))
                {
                    movingPlayer.IsCheck = true;
                    return true;
                }

                this.Target.Piece.IsFirstMove = false;
                movingPlayer.TakeFigure(piece.Name);
                movingPlayer.Points += piece.Points;
                this.OnTakePiece?.Invoke(movingPlayer, new TakePieceEventArgs(piece.Name, movingPlayer.Points));
                return true;
            }

            return false;
        }

        private bool EnPassantTake(Player movingPlayer, int turn)
        {
            if (EnPassant.Position != null)
            {
                var positions = this.GetAllowedPositions(movingPlayer);

                var firstPosition = positions[0];
                var secondPosition = positions[1];

                if (EnPassant.Turn == turn &&
                    EnPassant.Position.Equals(this.Target.Position) &&
                    this.Source.Piece is Pawn &&
                    (this.Source.Position.Equals(firstPosition) ||
                    this.Source.Position.Equals(secondPosition)))
                {
                    var piece = Factory.GetPawn(movingPlayer.Color);
                    int x = this.Target.Position.X > this.Source.Position.X ? 1 : -1;

                    this.EnPassantMovePiece(x);
                    this.CalculateAttackedSquares();

                    if (this.IsPlayerChecked(movingPlayer))
                    {
                        this.EnPassantReversePiece(x);
                        this.CalculateAttackedSquares();

                        movingPlayer.IsCheck = true;
                        return true;
                    }

                    string position = this.GetStringPosition(this.Source.Position.X + x, this.Source.Position.Y);
                    GlobalConstants.EnPassantTake = position;

                    movingPlayer.TakeFigure(piece.Name);
                    movingPlayer.Points += piece.Points;
                    this.OnTakePiece?.Invoke(movingPlayer, new TakePieceEventArgs(piece.Name, movingPlayer.Points));
                    movingPlayer.IsCheck = false;
                    return true;
                }
            }

            return false;
        }

        private bool Try(Player movingPlayer)
        {
            this.PlacePiece(this.Source, this.Target);
            this.RemovePiece(this.Source);
            this.CalculateAttackedSquares();

            if (this.IsPlayerChecked(movingPlayer))
            {
                this.ReversePiece(this.Source, this.Target);
                this.RemovePiece(this.Target);
                this.CalculateAttackedSquares();
                return false;
            }

            movingPlayer.IsCheck = false;
            return true;
        }

        private void CalculateAttackedSquares()
        {
            for (int y = 0; y < GlobalConstants.BoardRows; y++)
            {
                for (int x = 0; x < GlobalConstants.BoardCols; x++)
                {
                    this.Matrix[y][x].IsAttacked.Clear();
                }
            }

            for (int y = 0; y < GlobalConstants.BoardRows; y++)
            {
                for (int x = 0; x < GlobalConstants.BoardCols; x++)
                {
                    if (this.Matrix[y][x].Piece != null)
                    {
                        this.Matrix[y][x].Piece.Attacking(this.Matrix);
                    }
                }
            }
        }

        private string GetAlgebraicNotation(Square source, Square target, Player opponent, int turn)
        {
            var sb = new StringBuilder();

            var playerTurn = Math.Ceiling(turn / 2.0);
            sb.Append(playerTurn + ". ");

            if (GlobalConstants.EnPassantTake != null)
            {
                var file = source.ToString()[0];
                sb.Append(file + "x" + target + "e.p");
            }
            else if (GlobalConstants.CastlingMove)
            {
                if (target.ToString()[0] == 'g')
                {
                    sb.Append("0-0");
                }
                else
                {
                    sb.Append("0-0-0");
                }
            }
            else if (GlobalConstants.PawnPromotionFen != null)
            {
                sb.Append(target + "=Q");
            }
            else if (target.Piece == null)
            {
                if (source.Piece is Pawn)
                {
                    sb.Append(target);
                }
                else
                {
                    sb.Append(source.Piece.Symbol + target.ToString());
                }
            }
            else if (target.Piece.Color != source.Piece.Color)
            {
                if (source.Piece is Pawn)
                {
                    var file = source.ToString()[0];
                    sb.Append(file + "x" + target);
                }
                else
                {
                    sb.Append(source.Piece.Symbol + "x" + target);
                }
            }

            if (opponent.IsCheck)
            {
                if (!opponent.IsCheckMate)
                {
                    sb.Append("+");
                }
                else
                {
                    sb.Append("#");
                }
            }

            return sb.ToString();
        }

        private void GetPawnPromotionFenString(string targetFen, bool isWhite)
        {
            var sb = new StringBuilder();
            string[] rows = targetFen.Split('/');

            var lastRow = isWhite ? 0 : 7;
            var pawn = isWhite ? "P" : "p";
            var queen = isWhite ? "Q" : "q";

            for (int i = 0; i < rows.Length; i++)
            {
                var currentRow = rows[i];

                if (i == lastRow)
                {
                    for (int k = 0; k < currentRow.Length; k++)
                    {
                        var currentSymbol = currentRow[k].ToString();

                        if (string.Compare(currentSymbol, pawn) == 0)
                        {
                            sb.Append(queen);
                            continue;
                        }

                        sb.Append(currentSymbol);
                    }
                }
                else
                {
                    if (isWhite)
                    {
                        sb.Append("/" + currentRow);
                    }
                    else
                    {
                        sb.Append(currentRow + "/");
                    }
                }
            }

            GlobalConstants.PawnPromotionFen = sb.ToString();
        }

        private Square GetSquare(string position)
        {
            int col = char.Parse(position[0].ToString().ToUpper()) - 65;
            int row = Math.Abs(int.Parse(position[1].ToString()) - 8);

            return this.Matrix[row][col];
        }

        private Square GetKingSquare(Color color)
        {
            for (int y = 0; y < GlobalConstants.BoardRows; y++)
            {
                var kingSquare = this.Matrix[y].FirstOrDefault(x => x.Piece is King && x.Piece.Color == color);

                if (kingSquare != null)
                {
                    return kingSquare;
                }
            }

            return null;
        }

        #region EnPassant Internal Methods
        private void EnPassantMovePiece(int x)
        {
            this.PlacePiece(this.Source, this.Target);
            this.RemovePiece(this.Source);
            this.RemovePiece(this.Matrix[this.Source.Position.Y][this.Source.Position.X + x]);
        }

        private void EnPassantReversePiece(int x)
        {
            this.ReversePiece(this.Source, this.Target);
            this.RemovePiece(this.Target);
            var color = this.Matrix[this.Source.Position.Y][this.Source.Position.X + x].Piece.Color == Color.White ? Color.White : Color.Black;
            this.Matrix[this.Source.Position.Y][this.Source.Position.X + x].Piece = Factory.GetPawn(color);
        }

        private List<Position> GetAllowedPositions(Player movingPlayer)
        {
            var positions = new List<Position>();

            var sign = movingPlayer.Color == Color.White ? 1 : -1;

            int row = this.Target.Position.Y + sign;
            int colFirst = this.Target.Position.X + 1;
            int colSecond = this.Target.Position.X - 1;

            var firstAllowedPosition = Factory.GetPosition(row, colFirst);
            var secondAllowedPosition = Factory.GetPosition(row, colSecond);

            positions.Add(firstAllowedPosition);
            positions.Add(secondAllowedPosition);

            return positions;
        }

        private string GetStringPosition(int x, int y)
        {
            char col = (char)(97 + x);
            int row = Math.Abs(y - 8);
            return $"{col}{row}";
        }
        #endregion

        #region IsOpponentCheckmate Internal Methods
        private bool IsKingAbleToMove(Square king, Player movingPlayer)
        {
            int kingY = king.Position.Y;
            int kingX = king.Position.X;

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (y == 0 && x == 0)
                    {
                        continue;
                    }

                    if (Position.IsInBoard(kingY + y, kingX + x))
                    {
                        var checkedSquare = this.Matrix[kingY + y][kingX + x];

                        if (this.NeighbourSquareAvailable(checkedSquare, movingPlayer))
                        {
                            var currentFigure = this.Matrix[kingY][kingX].Piece;
                            var neighbourFigure = this.Matrix[kingY + y][kingX + x].Piece;

                            this.AssignNewValuesAndCalculate(kingY, kingX, y, x, currentFigure);

                            if (!this.Matrix[kingY + y][kingX + x].IsAttacked.Where(k => k.Color == movingPlayer.Color).Any())
                            {
                                this.AssignOldValuesAndCalculate(kingY, kingX, y, x, currentFigure, neighbourFigure);
                                return true;
                            }

                            this.AssignOldValuesAndCalculate(kingY, kingX, y, x, currentFigure, neighbourFigure);
                        }
                    }
                }
            }

            return false;
        }

        private bool AttackingPieceCanBeTaken(Square attackingSquare, Player opponent)
        {
            if (attackingSquare.IsAttacked.Where(x => x.Color == opponent.Color).Any())
            {
                if (attackingSquare.IsAttacked.Count(x => x.Color == opponent.Color) > 1)
                {
                    return true;
                }
                else if (!(attackingSquare.IsAttacked.Where(x => x.Color == opponent.Color).First() is King))
                {
                    return true;
                }
            }

            return false;
        }

        private bool OtherPieceCanBlockTheCheck(Square king, Square attackingSquare, Player opponent)
        {
            if (!(attackingSquare.Piece is Knight) && !(attackingSquare.Piece is Pawn))
            {
                int kingY = king.Position.Y;
                int kingX = king.Position.X;

                int attackingRow = attackingSquare.Position.Y;
                int attackingCol = attackingSquare.Position.X;

                if (attackingRow == kingY)
                {
                    int difference = Math.Abs(attackingCol - kingX) - 1;

                    for (int i = 1; i <= difference; i++)
                    {
                        int sign = attackingCol - kingX < 0 ? i : -i;
                        var signPlayer = opponent.Color == Color.White ? 1 : -1;

                        var currentSquare = this.Matrix[kingY][attackingCol + sign];
                        var neighbourSquare = this.Matrix[kingY][attackingCol + sign + signPlayer];

                        if (currentSquare.IsAttacked.Where(x => x.Color == opponent.Color && !(x is King) && !(x is Pawn)).Any() ||
                            (neighbourSquare.Piece is Pawn && neighbourSquare.Piece.Color == opponent.Color))
                        {
                            return true;
                        }
                    }
                }

                if (attackingCol == kingX)
                {
                    int difference = Math.Abs(attackingRow - kingY) - 1;

                    for (int i = 1; i <= difference; i++)
                    {
                        int sign = attackingRow - kingY < 0 ? i : -i;

                        if (this.Matrix[attackingRow + sign][kingX].IsAttacked.Where(x => x.Color == opponent.Color && !(x is King) && !(x is Pawn)).Any())
                        {
                            return true;
                        }
                    }
                }

                if (attackingRow != kingY && attackingCol != kingX)
                {
                    int difference = Math.Abs(attackingRow - kingY) - 1;

                    for (int i = 1; i <= difference; i++)
                    {
                        int signRow = attackingRow - kingY < 0 ? i : -i;
                        int signCol = attackingCol - kingX < 0 ? i : -i;
                        var signPlayer = opponent.Color == Color.White ? 1 : -1;

                        var currentSquare = this.Matrix[attackingRow + signRow][attackingCol + signCol];
                        var neighbourSquare = this.Matrix[attackingRow + signRow + signPlayer][attackingCol + signCol];

                        if (currentSquare.IsAttacked.Where(x => x.Color == opponent.Color && !(x is King) && !(x is Pawn)).Any() ||
                            (neighbourSquare.Piece is Pawn && neighbourSquare.Piece.Color == opponent.Color))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool NeighbourSquareAvailable(Square square, Player movingPlayer)
        {
            if (square.Piece != null &&
                square.Piece.Color == movingPlayer.Color &&
                !square.IsAttacked.Where(x => x.Color == movingPlayer.Color).Any())
            {
                return true;
            }

            if (square.Piece == null &&
                !square.IsAttacked.Where(x => x.Color == movingPlayer.Color).Any())
            {
                return true;
            }

            return false;
        }

        private void AssignNewValuesAndCalculate(int kingRow, int kingCol, int i, int k, IPiece currentFigure)
        {
            this.Matrix[kingRow][kingCol].Piece = null;
            this.Matrix[kingRow + i][kingCol + k].Piece = currentFigure;
            this.CalculateAttackedSquares();
        }

        private void AssignOldValuesAndCalculate(int kingRow, int kingCol, int i, int k, IPiece currentFigure, IPiece neighbourFigure)
        {
            this.Matrix[kingRow][kingCol].Piece = currentFigure;
            this.Matrix[kingRow + i][kingCol + k].Piece = neighbourFigure;

            this.CalculateAttackedSquares();
        }
        #endregion

        #region Board Base Methods
        private void PlacePiece(Square source, Square target)
        {
            this.Matrix[target.Position.Y][target.Position.X].Piece = this.Matrix[source.Position.Y][source.Position.X].Piece;
        }

        private void RemovePiece(Square square)
        {
            this.Matrix[square.Position.Y][square.Position.X].Piece = null;
        }

        private void ReversePiece(Square source, Square target)
        {
            this.Matrix[source.Position.Y][source.Position.X].Piece = this.Matrix[target.Position.Y][target.Position.X].Piece;
        }
        #endregion
    }
}
