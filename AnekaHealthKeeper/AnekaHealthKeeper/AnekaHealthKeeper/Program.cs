using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Linq;
using Aneka;
using Aneka.Threading;
using Aneka.Entity;
using System.Threading;
namespace AnekaHealthKeeper
{

    public class Block
    {
        public List<int> data = new List<int>();
        public string hash;
        public string prevHash;
        public int index;
        public int salt;
        public string timestamp;

        public Block(int index, List<int> data, string previousHash = "")
        {
            this.index = index;
            this.salt = 0;
            this.prevHash = previousHash;
            this.data = data;
            this.timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            this.hash = this.CalculateHash();
        }
        public string CalculateHash()
        {
            string calculatedhash = SHA256_hash(this.salt + this.prevHash + this.timestamp + this.index + string.Join(";", data.Select(x => x.ToString()).ToArray()));
            return calculatedhash;
        }
        public void Mine(int difficulty)
        {
            while (this.hash.Substring(0, difficulty) != new String('0', difficulty))
            {
                this.salt++;
                this.hash = this.CalculateHash();
            }
        }
        // Create a hash string from stirng
        static string SHA256_hash(string value)
        {
            var sb = new StringBuilder();
            using (SHA256 hash = SHA256Managed.Create())
            {
                var enc = Encoding.UTF8;
                var result = hash.ComputeHash(enc.GetBytes(value));
                foreach (var b in result)
                    sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
    public class Blockchain
    {
        List<Block> chain;
        public int i = 1;
        private int difficulty = 2;
        public Blockchain()
        {
            this.chain = new List<Block>();
            this.chain.Add(CreateGenesisBlock());
        }
        Block CreateGenesisBlock()
        {
            return new Block(0, new List<int>(), "");
        }
        public Block GetLatestBlock()
        {
            return this.chain.Last();
        }
        public void AddBlock(List<int> data)
        {
            Console.WriteLine("Adding Block at index : " + i);
            Block newBlock = new Block(i, data, this.GetLatestBlock().hash);
            newBlock.Mine(difficulty);
            this.chain.Add(newBlock);
            i += 1;
        }
        public void ValidateChain()
        {
            for (var i = 1; i < this.chain.Count; i++)
            {
                var currentBlock = this.chain[i];
                var previousBlock = this.chain[i - 1];

                Console.WriteLine("Data at index " + currentBlock.index + " : " + string.Join(";", currentBlock.data.Select(x => x.ToString()).ToArray()));

                // Check if the current block hash is consistent with the hash calculated
                if (currentBlock.hash != currentBlock.CalculateHash())
                {
                    throw new Exception("Chain is not valid! Current hash is incorrect!");
                }
                // Check if the Previous hash match the hash of previous block
                if (!currentBlock.prevHash.Equals(previousBlock.hash))
                {
                    throw new Exception("Chain is not valid! PreviousHash isn't pointing to the previous block's hash!");
                }
                // Check if hash string has initial zeroes
                if (currentBlock.hash.Substring(0, difficulty) != new String('0', difficulty))
                {
                    throw new Exception("Chain is not valid! Hash does not show proof-of-work!");
                }
            }
        }
    }
    [Serializable]
    public class HelloWorld
    {
        public string result = "None";
        public List<int> input = new List<int>();
        public int count = 0;
        public int min = 100;
        public bool dip = false;
        public int len;
        public string check = "Function not executed";
        public HelloWorld(List<int> input)
        {
            result = "HelloWorld";
            this.input = input;
            len = this.input.Count();
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
        public void PrintHello()
        {


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
                var myBlockChain = new Blockchain();
                Console.WriteLine("Hash : " + myBlockChain.GetLatestBlock().hash);
                while (true)
                {
                    Logger.Start();
                    Configuration conf =
                    Configuration.GetConfiguration(@"C:\Aneka\conf.xml");

                    while (Analyze() == false)
                    {
                        System.Threading.Thread.Sleep(500);
                        continue;
                    }
                    Console.WriteLine("Data Parsed");
                    app = new AnekaApplication<AnekaThread, ThreadManager>(conf);


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
                    HelloWorld hw = new HelloWorld(partitions[0]);
                    HelloWorld hw2 = new HelloWorld(partitions[1]);
                    AnekaThread[] th = new AnekaThread[2];


                    th[0] = new AnekaThread(hw.PrintHello, app);
                    th[1] = new AnekaThread(hw2.PrintHello, app);
                    th[0].Start();
                    th[1].Start();

                    th[0].Join();
                    hw = (HelloWorld)th[0].Target;
                    foreach (var val in partitions[0])
                    {
                        Console.WriteLine("Value in partitions[0] : " + val);
                    }

                    myBlockChain.AddBlock(partitions[0]);
                    Console.WriteLine("Hash value : " + myBlockChain.GetLatestBlock().hash);
                    myBlockChain.AddBlock(partitions[1]);
                    Console.WriteLine("Hash value : " + myBlockChain.GetLatestBlock().hash);

                    Console.WriteLine("Check : " + hw.check);
                    Console.WriteLine("Value : {0} , NodeId:{1},SubmissionTime:{2},Completion Time{3}", hw.result, th[0].NodeId, th[0].SubmissionTime, th[0].CompletionTime);
                    Console.WriteLine("Minimum : {0}", hw.min);
                    Console.WriteLine("Count : {0}", hw.count);

                    th[1].Join();
                    hw2 = (HelloWorld)th[1].Target;
                    foreach (var val in partitions[1])
                    {
                        Console.WriteLine("Value in partitions[1] : " + val);
                    }

                    Console.WriteLine("Value : {0} , NodeId:{1},SubmissionTime:{2},Completion Time{3}", hw2.result, th[1].NodeId, th[1].SubmissionTime, th[1].CompletionTime);
                    Console.WriteLine("Minimum : {0}", hw2.min);
                    Console.WriteLine("Count : {0}", hw2.count);

                    myBlockChain.ValidateChain();
                    Console.WriteLine("Blockchain Validation checked!");

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
                    lines[0] = "Analyze = Done";
                    System.IO.File.WriteAllLines(sourceFile, lines);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine(e.StackTrace);
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
                else if (result[0] == "Analyze = Done")
                {
                    Console.WriteLine("DEBUG: Analysis Done");
                    return false;
                }
                list = (result[1]).Split(',').ToList<string>();
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