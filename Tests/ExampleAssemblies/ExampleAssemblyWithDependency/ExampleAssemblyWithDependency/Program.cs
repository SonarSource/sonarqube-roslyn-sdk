using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExampleAssembly;

namespace ExampleAssemblyWithDependency
{
    public class Program
    {

        public Program()
        {
            new SimpleProgram();
            System.Console.WriteLine("foobar");
        }

        static void Main(string[] args)
        {
            new Program();
        }
    }
}
