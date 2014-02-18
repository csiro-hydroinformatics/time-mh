using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPI;

namespace TIME.Metaheuristics.Parallel
{
    internal interface IGroupProxy
    {
        int Size { get; }
    }

    internal class SerialGroupProxy : IGroupProxy
    {
        private int[] ranks;
        public int[] GetWorldRanks() { return (int[])ranks.Clone(); }

        public SerialGroupProxy(int[] ranks)
        {
            this.ranks = ranks;
        }

        public int Size
        {
            get { return this.ranks.Length; }
        }
    }

    internal class MpiGroupProxy : IGroupProxy
    {
        private int[] ranks;
        internal Group group;

        public MpiGroupProxy(int[] ranks)
        {
            this.ranks = ranks;
            this.group = Communicator.world.Group.IncludeOnly(ranks);
        }

        //public int Rank
        //{
        //    get { return this.group.Rank; }
        //}

        public int Size
        {
            get { return this.group.Size; }
        }
    }
}
