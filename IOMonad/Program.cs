using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IOMonad
{
    abstract class RuntimeAction : IOAction
    {
        public IOAction bind(Operation operation)
        {
            return new CombinedAction(this, operation);
        }

        public IOAction wrap(string text)
        {
            return new Wrapped(text);
        }

        public abstract string perform();
    }

    class ReadLine : RuntimeAction
    {
        public override string perform()
        {
            return Console.ReadLine();
        }
    }

    class WriteLine : RuntimeAction
    {
        readonly string text;
        public WriteLine(string text)
        {
            this.text = text;
        }
        public override string perform()
        {
            Console.WriteLine(text);
            return "";
        }
    }

    class Wrapped : RuntimeAction
    {
        readonly string text;
        public Wrapped(string text)
        {
            this.text = text;
        }
        public override string perform()
        {
            return text;
        }
    }

    class CombinedAction : RuntimeAction
    {
        readonly RuntimeAction first;
        readonly Operation next;
        public CombinedAction(RuntimeAction first, Operation next)
        {
            this.first = first;
            this.next = next;
        }

        public override string perform()
        {
            var result = first.perform();
            var nextAction = next(result);
            return ((RuntimeAction)nextAction).perform();
        }
    }

    class RuntimeImpl : Runtime
    {
        private readonly IOAction readLn = new ReadLine();

        public IOAction getLine()
        {
            return readLn;
        }

        public IOAction putStrLn(string text)
        {
            return new WriteLine(text);
        }

        public static Runtime Instance = new RuntimeImpl();
    }

    class Program
    {
        static void Main(string[] args)
        {
            ((RuntimeAction)FunctionalProgram.Main).perform();
        }
    }
}
