using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Base
{
    class GMLTest : TestBase
    {
        public override string Name
        {
            get { return "GMLTest"; }
        }

        static string ReadFile(string filename)
        {
            StreamReader reader = new StreamReader(File.OpenRead(filename));

            return reader.ReadToEnd();
        }

        protected override void DoTask()
        {
            string content = ReadFile("resList.gml");
            string json = ReadFile("json.txt");
            string xml = ReadFile("xml.txt");

            try
            {
                //GML.Node xmlNode = GML.Translator.XML.Import(xml);
                GML.Node root = GML.Parser.Deserialize(content);
                //Console.WriteLine(root.ToString());

                foreach (Base.GML.Node fileNode in root["manifest"].Children)
                {
                    //Console.WriteLine(fileNode.ToString());
                    string url = fileNode.Attributes["MD5"];
                    Console.WriteLine(url);
                }

                isPass = true;
            }
            catch (GML.ParserException ex)
            {
                Console.WriteLine(ex.ToString());
                isPass = false;
            }
        }
    }

    class CodecTest : TestBase
    {
        public override string Name
        {
            get { return "CodecTest"; }
        }

        static string ReadFile(string filename)
        {
            StreamReader reader = new StreamReader(File.OpenRead(filename));

            return reader.ReadToEnd();
        }

        protected override void DoTask()
        {
            string content = ReadFile("input.gml");

            byte[] bytes = Base.Codec.Encryption.RSA.Instance.Encode(content);
            string rsa = Encoding.UTF8.GetString(Base.Codec.Encryption.RSA.Instance.Decode(bytes));
            bytes = Base.Codec.Encryption.RFA.Instance.Encode(rsa);
            string rfa = Encoding.UTF8.GetString(Base.Codec.Encryption.RFA.Instance.Decode(bytes));

            isPass = (content == rsa) && (rfa == rsa);
        }
    }

    class WebTest : TestBase
    {
        public override string Name
        {
            get { return "WebTest"; }
        }

        protected override void DoTask()
        {
            Base.GEvent.WebHandler request = new Base.GEvent.WebHandler();
            request.Timeout = 100;
            Console.WriteLine(request.GET("http://www.baidu.com"));
            isPass = true;
        }
    }

    class LoggerTest : TestBase
    {
        public override string Name
        {
            get { return "LoggerTest"; }
        }

        protected override void DoTask()
        {
            System.Threading.Thread.Sleep(1000);
            Logger.Default.Log("okey doki");
            System.Threading.Thread.Sleep(1000);
            Logger.Default.Log("haha");
            Logger.Default.Log("hehe", Logger.Level.Debug);
            Logger.Default.Log("mama", Logger.Level.Warn);
            try
            {
                Logger.Default.Log("dfdf", Logger.Level.Error);
            }
            catch (BaseException exception)
            {
                Console.WriteLine(exception.ToString());
            }

            isPass = true;
        }
    }

    class ThreadTest : TestBase
    {
        public override string Name
        {
            get { return "ThreadTest"; }
        }

        protected override void DoTask()
        {
            Thread.TaskPool pool = new Thread.TaskPool(10);
            pool.Start();

            for (int i = 0; i < 3; i++)
            {
                int tmp = i;
                pool.Run(new Thread.Task(() => Haha(tmp)));
            }
            pool.Join();

            isPass = true;
        }

        void Haha(int data)
        {
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(data.ToString());
                System.Threading.Thread.Sleep(10);
            }
        }
    }

    class TestEntry
    {
        static void Main(string[] args)
        {
            TestGroup test = new TestGroup();

            test.Add(new ThreadTest())
                .Test();

            Console.WriteLine();
            Console.WriteLine("Total succeed : " + test.Passed.ToString() + "\t failed : " + test.Failed.ToString());
        }
    }
}
