using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Aneka;
using Aneka.Threading;
using Aneka.Entity;
using System.Threading;
namespace AnekaHealthKeeper
{
    [Serializable]
    public class HelloWorld
    {
        public string result = "None";
        public List<int> input;
        public int count = 0;
        public int min = 100;
        public bool dip = false;
        public HelloWorld()
        {
            result = "HelloWorld";
        }
        public void PrintHello()
        {
            count = 0;
            foreach(var val in input)
            {
                if(val <= 88 && !dip)
                {
                    count++;
                }
                else if(val > 88 && dip)
                {
                    dip = false;
                }
                if(min > val)
                {
                    min = val;
                }
            }
        }
    }
    class Program
    {
        static string path = @"C:\xampp\htdocs\HealthKeeper\data.txt";
        static List<string> list;
        static List<int> intlist;
        static int totalcount;
        static int minima;
        static void Main(string[] args)
        {
            AnekaApplication<AnekaThread, ThreadManager> app = null;
            try
            {
                Logger.Start();
                Configuration conf =
                Configuration.GetConfiguration(@"C:\Aneka\conf.xml");

                while(Analyze() == false)
                {
                    System.Threading.Thread.Sleep(500);
                    continue;
                }
                Console.WriteLine("Data Parsed");
                app = new AnekaApplication<AnekaThread, ThreadManager>(conf);
                HelloWorld hw = new HelloWorld();
                HelloWorld hw2 = new HelloWorld();
                AnekaThread[] th = new AnekaThread[2];

                List<int>[] partitions = new List<int>[2];
                int maxSize = (int)Math.Ceiling(list.Count / (double)2);
                int k = 0;
                for (int i = 0; i < 2; i++)
                {
                    partitions[i] = new List<int>();
                    for (int j = k; j < k + maxSize; j++)
                    {
                        if (j >= list.Count)
                            break;
                        partitions[i].Add(intlist[j]);
                    }
                    k += maxSize;
                }

                hw.input = partitions[0];
                hw2.input = partitions[1];

                th[0] = new AnekaThread(hw.PrintHello, app);
                th[0].Start();

                th[1] = new AnekaThread(hw2.PrintHello, app);
                th[1].Start();

                th[0].Join();
                hw = (HelloWorld)th[0].Target;
                Console.WriteLine("Value : {0} , NodeId:{1},SubmissionTime:{2},Completion Time{3}", hw.result,th[0].NodeId,th[0].SubmissionTime,th[0].CompletionTime);
                Console.WriteLine("Minimum : {0}", hw.min);
                Console.WriteLine("Count : {0}", hw.count);

                th[1].Join();
                hw = (HelloWorld)th[1].Target;
                Console.WriteLine("Value : {0} , NodeId:{1},SubmissionTime:{2},Completion Time{3}", hw2.result, th[0].NodeId, th[0].SubmissionTime, th[0].CompletionTime);
                Console.WriteLine("Minimum : {0}", hw2.min);
                Console.WriteLine("Count : {0}", hw2.count);

                totalcount = hw.count + hw2.count;
                if (hw.min < hw2.min)
                    minima = hw.min;
                else
                    minima = hw2.min;

                Console.WriteLine("Result : " + totalcount + ", " + minima);

                int line_to_edit = 1; // Warning: 1-based indexing!
                string sourceFile = path;
                string destinationFile = @"C:\xampp\htdocs\HealthKeeper\result.txt";

                // Read the appropriate line from the file.
                string lineToWrite = null;
                using (StreamReader reader = new StreamReader(sourceFile))
                {
                    for (int i = 1; i <= line_to_edit; ++i)
                        lineToWrite = reader.ReadLine();
                }

                if (lineToWrite == null)
                    throw new InvalidDataException("Line does not exist in " + sourceFile);

                // Read the old file.
                string[] lines = File.ReadAllLines(sourceFile);

                lines[0] = totalcount + "," + minima;
                System.IO.File.WriteAllLines(destinationFile, lines);
                File.Delete(path);
                File.Create(path);

            }
            finally
            {
                app.StopExecution();
                Logger.Stop();
            }
        }

        static bool Analyze()
        {
            try
            {
                Console.WriteLine("Checking data for pending Analysis");
                string readText = File.ReadAllText(path);
                var result = readText.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (result[0] == "Analyze = false")
                {
                    Console.WriteLine("DEBUG: " + result[0]);
                    return false;
                }
                list = (result[1]).Split(',').ToList<string>();
                list.Reverse();
                intlist = list.ConvertAll(s => Int32.Parse(s));
                return true;
            }
            catch
            {
                Console.WriteLine("Exception!");
                return false;
            }

        }
    }
}