using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Linq;
using System.Numerics;
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
    class Point
    {
        public static readonly Point INFINITY = new Point(null, default(BigInteger), default(BigInteger));
        public CurveFp Curve { get; private set; }
        public BigInteger X { get; private set; }
        public BigInteger Y { get; private set; }
        public Point(CurveFp curve, BigInteger x, BigInteger y)
        {
            this.Curve = curve;
            this.X = x;
            this.Y = y;
        }
        public Point Double()
        {
            if (this == INFINITY)
                return INFINITY;
            BigInteger p = this.Curve.p;
            BigInteger a = this.Curve.a;
            BigInteger l = ((3 * this.X * this.X + a) * InverseMod(2 * this.Y, p)) % p;
            BigInteger x3 = (l * l - 2 * this.X) % p;
            BigInteger y3 = (l * (this.X - x3) - this.Y) % p;
            return new Point(this.Curve, x3, y3);
        }
        public override string ToString()
        {
            if (this == INFINITY)
                return "infinity";
            return string.Format("({0},{1})", this.X, this.Y);
        }
        public static Point operator +(Point left, Point right)
        {
            if (right == INFINITY)
                return left;
            if (left == INFINITY)
                return right;
            if (left.X == right.X)
            {
                if ((left.Y + right.Y) % left.Curve.p == 0)
                    return INFINITY;
                else
                    return left.Double();
            }
            var p = left.Curve.p;
            var l = ((right.Y - left.Y) * InverseMod(right.X - left.X, p)) % p;
            var x3 = (l * l - left.X - right.X) % p;
            var y3 = (l * (left.X - x3) - left.Y) % p;
            return new Point(left.Curve, x3, y3);
        }
        public static Point operator *(Point left, BigInteger right)
        {
            var e = right;
            if (e == 0 || left == INFINITY)
                return INFINITY;
            var e3 = 3 * e;
            var negativeLeft = new Point(left.Curve, left.X, -left.Y);
            var i = LeftmostBit(e3) / 2;
            var result = left;
            while (i > 1)
            {
                result = result.Double();
                if ((e3 & i) != 0 && (e & i) == 0)
                    result += left;
                if ((e3 & i) == 0 && (e & i) != 0)
                    result += negativeLeft;
                i /= 2;
            }
            return result;
        }
        private static BigInteger LeftmostBit(BigInteger x)
        {
            BigInteger result = 1;
            while (result <= x)
                result = 2 * result;
            return result / 2;
        }
        private static BigInteger InverseMod(BigInteger a, BigInteger m)
        {
            while (a < 0) a += m;
            if (a < 0 || m <= a)
                a = a % m;
            BigInteger c = a;
            BigInteger d = m;
            BigInteger uc = 1;
            BigInteger vc = 0;
            BigInteger ud = 0;
            BigInteger vd = 1;
            while (c != 0)
            {
                BigInteger r;
                //q, c, d = divmod( d, c ) + ( c, );
                var q = BigInteger.DivRem(d, c, out r);
                d = c;
                c = r;
                //uc, vc, ud, vd = ud - q*uc, vd - q*vc, uc, vc;
                var uct = uc;
                var vct = vc;
                var udt = ud;
                var vdt = vd;
                uc = udt - q * uct;
                vc = vdt - q * vct;
                ud = uct;
                vd = vct;
            }
            if (ud > 0) return ud;
            else return ud + m;
        }
    }
    class CurveFp

    {
        public BigInteger p { get; private set; }
        public BigInteger a { get; private set; }
        public BigInteger b { get; private set; }
        public CurveFp(BigInteger p, BigInteger a, BigInteger b)
        {
            this.p = p;
            this.a = a;
            this.b = b;
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
        private static string privateKey = "123456789";
        public static string publicKey = GetPublicKeyFromPrivateKey(privateKey);
        public string signature;
        static string path = @"C:\xampp\htdocs\HealthKeeper\data.txt";
        static List<string> list;
        static List<int> intlist;
        static int totalcount;
        static int minima;
        static void Main(string[] args)
        {
            AnekaApplication<AnekaThread, ThreadManager> app = null;
            Console.WriteLine("Initialized Master with public key : " + publicKey);
            try
            {
                // Initialize Blockchain
                var myBlockChain = new Blockchain();
                Console.WriteLine("Hash : " + myBlockChain.GetLatestBlock().hash);

                while (true)
                {
                    // Start Aneka
                    Logger.Start();
                    Configuration conf =
                    Configuration.GetConfiguration(@"C:\Aneka\conf.xml");

                    // Analyze data.txt
                    while (Analyze() == false)
                    {
                        System.Threading.Thread.Sleep(500);
                        continue;
                    }
                    Console.WriteLine("Data Parsed");

                    // Start Aneks application
                    app = new AnekaApplication<AnekaThread, ThreadManager>(conf);
                    
                    // Parse data.txt
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

                    // Add data partitions to blockchain
                    myBlockChain.AddBlock(partitions[0]);
                    Console.WriteLine("Hash value : " + myBlockChain.GetLatestBlock().hash);
                    myBlockChain.AddBlock(partitions[1]);
                    Console.WriteLine("Hash value : " + myBlockChain.GetLatestBlock().hash);

                    // Initialize 2 HelloWorld objects for two data halves
                    HelloWorld hw = new HelloWorld(partitions[0]);
                    HelloWorld hw2 = new HelloWorld(partitions[1]);

                    // Declare Aneka thread array
                    AnekaThread[] th = new AnekaThread[2];
                    
                    // Initialize Aneka threads with PrintHello method
                    th[0] = new AnekaThread(hw.PrintHello, app);
                    th[1] = new AnekaThread(hw2.PrintHello, app);

                    // Start threads
                    th[0].Start();
                    th[1].Start();

                    // Wait for first thread to finish
                    th[0].Join();
                    hw = (HelloWorld)th[0].Target;
                    foreach (var val in partitions[0])
                    {
                        Console.WriteLine("Value in partitions[0] : " + val);
                    }

                    Console.WriteLine("Check : " + hw.check);
                    Console.WriteLine("Value : {0} , NodeId:{1},SubmissionTime:{2},Completion Time{3}", hw.result, th[0].NodeId, th[0].SubmissionTime, th[0].CompletionTime);
                    Console.WriteLine("Minimum : {0}", hw.min);
                    Console.WriteLine("Count : {0}", hw.count);

                    // Wait for second thread to finish
                    th[1].Join();
                    hw2 = (HelloWorld)th[1].Target;
                    foreach (var val in partitions[1])
                    {
                        Console.WriteLine("Value in partitions[1] : " + val);
                    }

                    Console.WriteLine("Check : " + hw2.check);
                    Console.WriteLine("Value : {0} , NodeId:{1},SubmissionTime:{2},Completion Time{3}", hw2.result, th[1].NodeId, th[1].SubmissionTime, th[1].CompletionTime);
                    Console.WriteLine("Minimum : {0}", hw2.min);
                    Console.WriteLine("Count : {0}", hw2.count);


                    // Validate blockchain
                    myBlockChain.ValidateChain();
                    Console.WriteLine("Blockchain Validation checked!");

                    // Publish results in result.txt
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
        private static string GetPublicKeyFromPrivateKey(string privateKey)
        {
            var p = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFFC2F", NumberStyles.HexNumber);
            var b = (BigInteger)7;
            var a = BigInteger.Zero;
            var Gx = BigInteger.Parse("79BE667EF9DCBBAC55A06295CE870B07029BFCDB2DCE28D959F2815B16F81798", NumberStyles.HexNumber);
            var Gy = BigInteger.Parse("483ADA7726A3C4655DA4FBFC0E1108A8FD17B448A68554199C47D08FFB10D4B8", NumberStyles.HexNumber);

            CurveFp curve256 = new CurveFp(p, a, b);
            Point generator256 = new Point(curve256, Gx, Gy);

            var secret = BigInteger.Parse(privateKey, NumberStyles.HexNumber);
            var pubkeyPoint = generator256 * secret;
            return pubkeyPoint.X.ToString("X") + pubkeyPoint.Y.ToString("X");
        }
    }
}