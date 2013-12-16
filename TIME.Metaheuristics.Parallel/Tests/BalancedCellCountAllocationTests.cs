using System;
using System.Collections.Generic;
using System.Linq;
using TIME.Metaheuristics.Parallel.ExtensionMethods;
using TIME.Metaheuristics.Parallel.WorkAllocation;
using NUnit.Framework;
using TIME.Tools.Metaheuristics.Persistence;
using TIME.Tools.Metaheuristics.Persistence.Gridded;

namespace TIME.Metaheuristics.Parallel.Tests
{
    [TestFixture]
    public class BalancedCellCountAllocationTests
    {
        #region Setup/Teardown

        [SetUp]
        public void Setup()
        {
            gd = new GlobalDefinition();
            allocator = new TestBalancedCellCountAllocator(gd);
        }

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            rand = new Random();
        }


        #endregion

        private GlobalDefinition gd;
        private TestBalancedCellCountAllocator allocator;
        private Random rand;

        private class TestBalancedCellCountAllocator : BalancedCellCountAllocator
        {
            public TestBalancedCellCountAllocator(GlobalDefinition g)
                :
                    base(g)
            {
            }

            // Calls the allocation worker method, bypassing the MPI communications in the 
            // main Allocate() method.

            public WorkPackage this[int index]
            {
                get { return Allocations[index]; }
            }

            public WorkPackage[] Allocations { get; private set; }

            public void Allocate(int numProcesses)
            {
                InitStorage(numProcesses);
                Allocations = PerformAllocation(numProcesses);
            }
        }

        private int AddComplexCatchments()
        {
            gd.RandomCatchments(22, 2, 2);
            gd.RandomCatchments(12, 12, 12);
            gd.RandomCatchments(4, 31, 31);
            return gd.GetFlatCellList().Count;
        }

        [Test]
        public void NotEnoughWork()
        {
            const int processes = 10;
            gd.RandomCatchments(2,3,3);
            var cells = gd.GetFlatCellList();

            // pre-conditions
            Assert.That(cells.Count, Is.EqualTo(6));

            allocator.Allocate(processes);

            // post-conditions
            Assert.That(allocator[0], Is.Null);
            Assert.That(allocator[1].Cells.Length, Is.EqualTo(1));
            Assert.That(allocator[2].Cells.Length, Is.EqualTo(1));
            Assert.That(allocator[3].Cells.Length, Is.EqualTo(1));
            Assert.That(allocator[4].Cells.Length, Is.EqualTo(1));
            Assert.That(allocator[5].Cells.Length, Is.EqualTo(1));
            Assert.That(allocator[6].Cells.Length, Is.EqualTo(1));
            Assert.That(allocator[7].Cells.Length, Is.EqualTo(0));
            Assert.That(allocator[8].Cells.Length, Is.EqualTo(0));
            Assert.That(allocator[9].Cells.Length, Is.EqualTo(0));
        }
      
        [Test]
        public void AllCatchmentsAreAllocated()
        {
            int processes = rand.Next(3, 30);

            gd.RandomCatchments(rand.Next(1, 10), 1, 10);
            gd.RandomCatchments(rand.Next(5, 23), 50, 100);
            gd.RandomCatchments(rand.Next(3, 9), 5, 43);

            allocator.Allocate(processes);

            // for each work package:
            //  for each catchment:
            //      add it to a set
            // assert that every catchment in the global def is a member of the set
            HashSet<CatchmentDefinition> catchments = new HashSet<CatchmentDefinition>();
            foreach (CatchmentDefinition catchment in
                    allocator.Allocations.Where(workPackage => workPackage != null).SelectMany(workPackage => workPackage.Catchments))
            {
                catchments.Add(catchment);
            }

            foreach (var srcCatchment in gd.Catchments)
                Assert.That(catchments.Contains(srcCatchment), Is.True);
        }

        [Test]
        public void AllocationToOneProcessThrowsException()
        {
            gd.RandomCatchments(1, 1, 5);
            Assert.Throws<ArgumentOutOfRangeException>(() => allocator.Allocate(1));
        }

        [Test]
        public void CellCatchmentIdsMatchWorkPackageCatchments()
        {
            const int processes = 5;
            gd.RandomCatchments(3, 1, 4);
            allocator.Allocate(processes);
            CatchmentDefinition[] catchments = new CatchmentDefinition[3];

            for (int process = 1; process < processes; process++)
            {
                WorkPackage package = allocator[process];
                package.Catchments.CopyTo(catchments); // HashSets are hard to work with. Copying to an array makes it much easier to do the test
                foreach (CellDefinition cell in package.Cells)
                    Assert.That(catchments.Any(catchment => catchment.Id == cell.CatchmentId));
            }
        }

        [Test]
        public void ComplexAllocation()
        {
            int numCells = AddComplexCatchments();
            const int processes = 7;
            const int workers = processes - 1; // The allocator never assigns work to process 0
            int cellsPerWorker = numCells / workers;
            int cellRemainder = numCells % workers;

            // preconditions
            Assert.That(cellRemainder, Is.EqualTo(0), "Cell remainder");
            // we don't want to complicate things by checking for how the remainder is distributed across workers.

            allocator.Allocate(processes);

            // post conditions
            Assert.That(allocator.Allocations.Length, Is.EqualTo(processes));
            Assert.That(allocator[0], Is.Null);
            Assert.That(allocator[1].Cells.Length, Is.EqualTo(cellsPerWorker));
            Assert.That(allocator[2].Cells.Length, Is.EqualTo(cellsPerWorker));
            Assert.That(allocator[3].Cells.Length, Is.EqualTo(cellsPerWorker));
            Assert.That(allocator[4].Cells.Length, Is.EqualTo(cellsPerWorker));
            Assert.That(allocator[5].Cells.Length, Is.EqualTo(cellsPerWorker));
            Assert.That(allocator[6].Cells.Length, Is.EqualTo(cellsPerWorker));
        }

        [Test]
        public void GriddedResultCountTest()
        {
            gd.RandomCatchments(8);
            allocator.Allocate(8);
            Assert.That(allocator.GriddedResultCount, Is.EqualTo(gd.GetFlatCellList().Count));
        }

        [Test]
        public void TestRanksByCatchment()
        {
            const int processes = 5;
            gd.RandomCatchments(6, 3, 6);
            allocator.Allocate(processes);

            /* for each process
             *  for each cell in the work package
             *      the current process id must be a member of RanksByCatchment[cell.CatchmentId]
             *
             * Note that Rank 0 is never allocated work 
             * */
            for (int process = 1; process < processes; process++)
            {
                WorkPackage package = allocator[process];
                foreach (CellDefinition cell in package.Cells)
                    Assert.That(allocator.RanksByCatchment[cell.CatchmentId].Contains(process));
            }
        }

        [Test]
        public void TwoEqualCatchments()
        {
            gd.RandomCatchments(2, 30, 30);
            const int processes = 4;
            const int workers = processes - 1; // The allocator never assigns work to process 0
            const int cellsPerWorker = 60 / workers;

            // preconditions
            Assert.That(gd.GetFlatCellList().Count, Is.EqualTo(60));
            Assert.That(gd.Count, Is.EqualTo(2));

            // allocate the work.
            // 3 workers, we have 4 processes.
            allocator.Allocate(processes);

            // postconditions
            Assert.That(allocator.Allocations.Length, Is.EqualTo(processes));
            Assert.That(allocator[0], Is.Null);
            Assert.That(allocator[1].Cells.Length, Is.EqualTo(cellsPerWorker));
            Assert.That(allocator[2].Cells.Length, Is.EqualTo(cellsPerWorker));
            Assert.That(allocator[3].Cells.Length, Is.EqualTo(cellsPerWorker));
        }

        [Test]
        public void UnequalCellCounts()
        {
            const int numCells = 719;
            const int processes = 29;
            const int workers = processes - 1;
            const int cellsPerWorker = numCells / workers;
            const int cellRemainder = numCells % workers;

            gd.RandomCatchments(1, numCells, numCells);

            // pre conditions
            Assert.That(cellRemainder, Is.Not.EqualTo(0));
            Assert.That(gd.GetFlatCellList().Count, Is.EqualTo(numCells));

            allocator.Allocate(processes);

            // The first cellRemainder workers should have 1 extra cell, since this is the way that the remainder is distributed.
            for (int i = 1; i <= cellRemainder; i++)
                Assert.That(allocator[i].Cells.Length, Is.EqualTo(cellsPerWorker + 1));

            for (int i = cellRemainder + 1; i < workers; i++)
                Assert.That(allocator[i].Cells.Length, Is.EqualTo(cellsPerWorker));
        }
    }
}