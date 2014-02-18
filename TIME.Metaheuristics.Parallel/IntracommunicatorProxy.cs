using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPI;

namespace TIME.Metaheuristics.Parallel
{
    public interface IIntracommunicatorProxy
    {
        int GetRank(int worldRank);

        int Size { get; }

        Tools.Collections.SerializableDictionary<string, MpiTimeSeries>[] Gather(Tools.Collections.SerializableDictionary<string, MpiTimeSeries> serializableDictionary, int root, int sender);
    }

    internal class SerialIntracommunicatorProxy : IIntracommunicatorProxy
    {
        private SerialGroupProxy catchmentGroup;
        private Dictionary<int, int> groupRank = new Dictionary<int,int>();
        private int[] wr;
        public SerialIntracommunicatorProxy(IGroupProxy catchmentGroup)
        {
            this.catchmentGroup = (SerialGroupProxy)catchmentGroup;
            this.wr = this.catchmentGroup.GetWorldRanks();
            for (int i = 0; i < wr.Length; i++)
            {
                groupRank.Add(wr[i], i);
            }
        }

        public int Size
        {
            get { return catchmentGroup.Size; }
        }

        public Tools.Collections.SerializableDictionary<string, MpiTimeSeries>[] Gather(Tools.Collections.SerializableDictionary<string, MpiTimeSeries> serializableDictionary, int root, int sender)
        {
            throw new NotImplementedException("This is not essential to test the netCDF issue; will need implementation later on for other purposes.");
            if (sender == root)
            {
                
            }
        }

        public int GetRank(int worldRank)
        {
            if (groupRank.ContainsKey(worldRank))
                return groupRank[worldRank];
            return -32766; // to be consistent with what seems to be in logs of the MPI runs
        }
    }

    internal class MpiIntracommunicatorProxy : IIntracommunicatorProxy
    {
        private Group catchmentGroup;
        Intracommunicator catchmentCommunicator;

        public MpiIntracommunicatorProxy(IGroupProxy catchmentGroup)
        {
            this.catchmentGroup = ((MpiGroupProxy)catchmentGroup).group;
            catchmentCommunicator = (Intracommunicator)Communicator.world.Create(this.catchmentGroup);
        }

        public int Size
        {
            get { return catchmentCommunicator.Size; }
        }

        public Tools.Collections.SerializableDictionary<string, MpiTimeSeries>[] Gather(Tools.Collections.SerializableDictionary<string, MpiTimeSeries> serializableDictionary, int root, int sender)
        {
            return catchmentCommunicator.Gather(serializableDictionary, root);
        }

        public int GetRank(int worldRank)
        {
            return catchmentCommunicator.Rank;
        }

        /// <summary>
        /// Is the MPI Intracommunicator pointed to by this proxy a null reference?
        /// </summary>
        public bool IsNull { get { return catchmentCommunicator == null; } }
    }

}
