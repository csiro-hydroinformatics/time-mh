using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSIRO.Metaheuristics.Source.UseCases.SourceCalibrationSimpleAWBM;

namespace CSIRO.Metaheuristics.Source.UseCases
{
    class MainUseCase
    {
        static void Main( string[] args )
        {
            Console.WriteLine("Start");
            SourceCalibrationSimpleAWBM.Executor executor = new Executor();
            
            executor.Execute( );

            Console.WriteLine();
            Console.WriteLine("End");
            Console.ReadLine();
        }

    }
}
