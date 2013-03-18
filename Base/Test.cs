using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base
{
    abstract class Test
    {
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

        public virtual bool Check()
        {
            return true;
        }
    }

    class GMLTest : Test
    {
        bool check = false;

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
            string content = ReadFile("input.gml");
            string json = ReadFile("json.txt");
            string xml = ReadFile("xml.txt");

            try
            {
                GML.Node xmlNode = GML.Translator.XML.Import(xml);
                GML.Node root = GML.Parser.Deserialize(content);
                //Console.WriteLine(root.ToString());

                foreach (GML.Node sub in root["battle"]["events"].Find("container@name='start_phase'")["event"].FindAll("action_dialog@side='delay'"))
                {
                    Console.WriteLine(sub.ToString());
                }

                FileStream file = new FileStream("output.txt", FileMode.Create);
                StreamWriter writer = new StreamWriter(file);
                writer.Write(root.ToString());
                writer.Close();

                check = true;
            }
            catch (GML.ParserException ex)
            {
                Console.WriteLine(ex.ToString());
                check = false;
            }
        }

        public override bool Check()
        {
            return check;
        }
    }

    class TestEntry
    {
        static void Main(string[] args)
        {
            int count = 0;
            Test[] tests = new Test[] { new GMLTest() };
            foreach (Test test in tests)
            {
                Console.WriteLine("Do " + test.Name + " test...");
                Console.WriteLine("--------------------------------------------------");
                test.DoTest();
                Console.WriteLine("--------------------------------------------------");
                if (test.Check())
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
