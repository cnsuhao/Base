using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Base
{
    public abstract class TestBase
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
            get { return isPass; }
        }
    }
    public class TestGroup
    {
        List<TestBase> tests = new List<TestBase>();
        int passed = 0;

        public int Count { get { return tests.Count; } }
        public int Passed { get { return passed; } }
        public int Failed { get { return Count - Passed; } }
        public float PassRatio { get { return (float)passed / tests.Count; } }

        public TestGroup() { }
        public TestGroup(TestBase[] tests)
        {
            Add(tests);
        }

        public TestGroup Add(TestBase[] tests)
        {
            foreach (TestBase test in tests)
                Add(test);
            return this;
        }
        public TestGroup Add(TestBase test)
        {
            tests.Add(test);
            return this;
        }

        public TestBase[] Test()
        {
            List<TestBase> notPass = new List<TestBase>();

            int count = 0;
            foreach (TestBase test in tests)
            {
                Logger.Default.Info("Do Test [{0}]...", test.Name);
                test.DoTest();
                if (test.IsPass)
                {
                    count++;
                    Logger.Default.Info("Successful!");
                }
                else
                {
                    notPass.Add(test);
                    Logger.Default.Warning("Woops!");
                }
            }
            passed = count;
            Logger.Default.Info("--------------------------------------------------");
            Logger.Default.Info("Test Complete, {0}/{1}", Passed, Count);
            if (Passed == Count)
                Logger.Default.Info("All Passed, Congratulations!");

            return notPass.ToArray();
        }
    }
}
