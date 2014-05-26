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

        void Scatter<T>(T[] workPackages);

        T Scatter<T>(int rank);

        void Broadcast<T>(ref T message, int rank);
    }

    internal class SerialWorldIntracommunicatorProxy : IIntracommunicatorProxy
    {
        public int GetRank(int worldRank)
        {
            throw new NotImplementedException();
        }

        public int Size
        {
            get { throw new NotImplementedException(); }
        }

        public Tools.Collections.SerializableDictionary<string, MpiTimeSeries>[] Gather(Tools.Collections.SerializableDictionary<string, MpiTimeSeries> serializableDictionary, int root, int sender)
        {
            throw new NotImplementedException();
        }


        public void Scatter<T>(T[] workPackages)
        {
            throw new NotImplementedException();
        }

        public T Scatter<T>(int rank)
        {
            throw new NotImplementedException();
        }

        public void Broadcast<T>(ref T message, int rank)
        {
            throw new NotImplementedException();
        }
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
            //if (sender == root)
            //{
            //}
        }

        public int GetRank(int worldRank)
        {
            if (groupRank.ContainsKey(worldRank))
                return groupRank[worldRank];
            return -32766; // to be consistent with what seems to be in logs of the MPI runs
        }


        public void Scatter<T>(T[] workPackages)
        {
            throw new NotImplementedException();
        }

        public T Scatter<T>(int rank)
        {
            throw new NotImplementedException();
        }

        public void Broadcast<T>(ref T message, int rank)
        {
            throw new NotImplementedException();
        }
    }

    internal abstract class BaseMpiIntracommunicatorProxy
    {
        protected Intracommunicator communicator;
        protected BaseMpiIntracommunicatorProxy(Intracommunicator communicator)
        {
            this.communicator = communicator;
        }
        protected BaseMpiIntracommunicatorProxy()
        {
        }
        public int GetRank(int worldRank)
        {
            return communicator.Rank;
        }

        public int Size
        {
            get { return communicator.Size; }
        }

        public Tools.Collections.SerializableDictionary<string, MpiTimeSeries>[] Gather(Tools.Collections.SerializableDictionary<string, MpiTimeSeries> serializableDictionary, int root, int sender)
        {
            return communicator.Gather(serializableDictionary, root);
        }

        public void Scatter<T>(T[] workPackages)
        {
            communicator.Scatter(workPackages);
        }

        public T Scatter<T>(int rank)
        {
            return communicator.Scatter<T>(rank);
        }

        public void Broadcast<T>(ref T message, int rank)
        {
            communicator.Broadcast(ref message, rank);
        }

        /// <summary>
        /// Is the MPI Intracommunicator pointed to by this proxy a null reference?
        /// </summary>
        public bool IsNull { get { return communicator == null; } }

    }

    internal class MpiWorldIntracommunicatorProxy : BaseMpiIntracommunicatorProxy, IIntracommunicatorProxy
    {
        public MpiWorldIntracommunicatorProxy() : base(Communicator.world) {}
    }

    internal class MpiIntracommunicatorProxy : BaseMpiIntracommunicatorProxy, IIntracommunicatorProxy
    {
        private Group catchmentGroup;
        public MpiIntracommunicatorProxy(IGroupProxy catchmentGroup)
        {
            this.catchmentGroup = ((MpiGroupProxy)catchmentGroup).group;
            communicator = (Intracommunicator)Communicator.world.Create(this.catchmentGroup);
        }
    }
}
