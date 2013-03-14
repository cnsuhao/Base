using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base
{
    class Test
    {
        static string ReadFile(string filename)
        {
            StreamReader reader = new StreamReader(File.OpenRead(filename));

            return reader.ReadToEnd();
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        static void Main(string[] args)
        {
            string content = ReadFile("input.gml");
            string json = ReadFile("json.txt");
            string xml = ReadFile("xml.txt");

            try
            {
                GML.Node xmlNode = GML.Translator.XML.Import(xml);
                DateTime now = DateTime.Now;
                GML.Node root = GML.Parser.Deserialize(content);
                TimeSpan delta = DateTime.Now - now;
                Console.WriteLine(delta.ToString());
                //Console.WriteLine(root.ToString());

                foreach (GML.Node sub in root["battle"]["events"].Find("contai/name='start_phase'", true))
                {
                    Console.WriteLine(sub.ToString());
                }

                FileStream file = new FileStream("output.txt", FileMode.Create);
                StreamWriter writer = new StreamWriter(file);
                writer.Write(root.ToString());
                writer.Close();
            }
            catch (GML.ParserException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
