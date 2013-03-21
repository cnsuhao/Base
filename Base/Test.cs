using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Base
{
    abstract class Test
    {
        protected bool isPass = true;

        public virtual string Name
        {
            get { return "Test"; }
        }

        protected virtual void Prepare()
        {
        }

        protected abstract void DoTask();

        public void DoTest()
        {
            Prepare();
            DoTask();
        }

        public bool IsPass
        {
            get{return isPass;}
        }
    }

    class GMLTest : Test
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

                foreach (Base.GML.Node fileNode in root.Children)
                {
                    Console.WriteLine(fileNode.ToString());
                    //string url = fileNode.Attributes["url"];
                    //Console.WriteLine(url);
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

    class CodecTest : Test
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

    class TestEntry
    {
        static void Main(string[] args)
        {
            int count = 0;
            Test[] tests = new Test[] { new GMLTest() /*new CodecTest()*/ };
            foreach (Test test in tests)
            {
                Console.WriteLine("Do " + test.Name + " test...");
                Console.WriteLine("--------------------------------------------------");
                test.DoTest();
                Console.WriteLine("--------------------------------------------------");
                if (test.IsPass)
                {
                    count++;
                    Console.WriteLine("Test succeed!");
                }
                else
                    Console.WriteLine("Test failed");
            }
            Console.WriteLine();
            Console.WriteLine("Total succeed : " + count.ToString() + "\t failed : " + (tests.Length - count).ToString());
        }
    }
}
