using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using Aneka;
using Aneka.Entity;
using Aneka.Tasks;
using System.Threading;
namespace AnekaHealthKeeperTask
{
    [Serializable]
    public class MyTask : ITask
    {
        public string result = "None";
        public List<int> input = new List<int>();
        public int count = 0;
        public int min = 100;
        public bool dip = false;
        public int len;
        public string check = "Function not executed";
        public MyTask(List<int> input)
        {
            result = "HelloWorld";
            this.input = input;
            len = this.input.Count();
        }
        public void Execute()
        {
            this.check = "Function Executed!";
            Console.WriteLine("Entered analysis function");
            count = 0;
            foreach (var val in this.input)
            {
                Console.WriteLine("Value in input : " + val);
                if (val <= 88 && !dip)
                {
                    count++;
                }
                else if (val > 88 && dip)
                {
                    dip = false;
                }
                if (min > val)
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
        static AutoResetEvent semaphore = null;
        static AnekaApplication<AnekaTask, TaskManager> app = null;
        static void Main(string[] args)
        {
            AnekaTask gt = null;
            try
            {
                Logger.Start();
                semaphore = new AutoResetEvent(false);
                Configuration conf =
                Configuration.GetConfiguration(@"C:\Aneka\conf.xml");
                conf.SingleSubmission = false;

                while (Analyze() == false)
                {
                    System.Threading.Thread.Sleep(500);
                    continue;
                }
                Console.WriteLine("Data Parsed");
                app = new AnekaApplication<AnekaTask, TaskManager>(conf);
                app = new AnekaApplication<AnekaTask, TaskManager>(conf);
                app.WorkUnitFailed += new EventHandler<WorkUnitEventArgs<AnekaTask>>(app_WorkUnitFailed);
                app.WorkUnitFinished += new EventHandler<WorkUnitEventArgs<AnekaTask>>(app_WorkUnitFinished);
                app.ApplicationFinished += new EventHandler<ApplicationEventArgs>(app_ApplicationFinished);
                
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

                MyTask task = new MyTask(partitions[0]);
                gt = new AnekaTask(task);
                app.ExecuteWorkUnit(gt);
                semaphore.WaitOne();
                MyTask task2 = new MyTask(partitions[1]);
                gt = new AnekaTask(task2);
                app.ExecuteWorkUnit(gt);
                semaphore.WaitOne();

                foreach (var val in partitions[0])
                {
                    Console.WriteLine("Value in partitions[0] : " + val);
                }
                foreach (var val in partitions[1])
                {
                    Console.WriteLine("Value in partitions[1] : " + val);
                }

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
                lines[0] = "Analyse = Done";
                System.IO.File.WriteAllLines(sourceFile, lines);

            }
            catch (Exception e)
            {
                Console.Write(e.StackTrace);
            }
            finally
            {
                app.StopExecution();
                Logger.Stop();
            }
        }
        static void app_ApplicationFinished(object sender, ApplicationEventArgs e)
        {

            semaphore.Set();
        }
        static void app_WorkUnitFinished(object sender, WorkUnitEventArgs<AnekaTask> e)
        {
            Console.WriteLine("Workunit finished:" + ((MyTask)e.WorkUnit.UserTask).result);
            app.StopExecution();
        }
        static void app_WorkUnitFailed(object sender, WorkUnitEventArgs<AnekaTask> e)
        {
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