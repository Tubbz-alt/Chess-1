﻿namespace Chess.Data.Models
{
    using System;

    public class Position : ICloneable
    {
        public Position()
            : this(-1, -1)
        {
        }

        public Position(int rank, int file)
        {
            this.Rank = rank;
            this.File = file;
        }

        public int File { get; set; }

        public int Rank { get; set; }

        public static bool IsInBoard(int file, int rank)
        {
            return file >= 0 && file <= 7 && rank >= 0 && rank <= 7;
        }

        public override string ToString()
        {
            return "[" + this.Rank + ", " + this.File + "]";
        }

        public override bool Equals(object obj)
        {
            Position other = (Position)obj;
            return this.File == other.File && this.Rank == other.Rank;
        }

        public object Clone()
        {
            return Factory.GetPosition(this.Rank, this.File);
        }
    }
}
