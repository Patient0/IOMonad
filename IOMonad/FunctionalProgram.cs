using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IOMonad
{
    internal delegate IOAction Operation(string input);

    interface IOAction
    {
        // Bind another operation on to this IOAction
        IOAction bind(Operation operation);
        // Create a plain IO operation that evaluates to 'text'
        IOAction wrap(string text);
    }

    interface Runtime
    {
        IOAction readLine();
        IOAction writeLine(string text);
    }

    class FunctionalProgram
    {
        public static readonly Runtime rt = RuntimeImpl.Instance;

        private static IOAction checkInput(string input)
        {
            int num;
            if(int.TryParse(input, out num) && num == 4)
                return rt.writeLine("That's the right answer!");
            else
                return rt.writeLine(string.Format("{0} sorry, we're not in the 1984. Please try again...", input)).bind(ask);
        }

        private static IOAction ask(string input)
        {
            return rt.writeLine("What is 2 + 2?").
                bind(unused => rt.readLine()).
                bind(checkInput);
        }

        public static IOAction Main =
            rt.writeLine("Enter your name").
                bind(unused => rt.readLine()).
                bind(name => rt.writeLine("Hello " + name + ". It's nice to meet you.")).
                bind(unused => rt.writeLine("OK time for a little test...")).
                bind(ask);
    }
}
