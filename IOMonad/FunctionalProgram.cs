using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IOMonad
{
    // An operation is a pure function which takes the result of
    // the previous IO action and returns the IO action that
    // should be evaluated next.
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
        // An IO action which reads a line of input from the user.
        IOAction getLine();
        // An IO action which writes the specified line of text to the screen.
        IOAction putStrLn(string text);
    }

    class FunctionalProgram
    {
        public static readonly Runtime rt = RuntimeImpl.Instance;

        private static IOAction checkInput(string input)
        {
            int num;
            if(int.TryParse(input, out num) && num == 4)
                return rt.putStrLn("That's the right answer!");
            else
                return rt.putStrLn(string.Format("{0} sorry, we're not in Orwell's novel 1984. Please try again...", input)).bind(ask);
        }

        private static IOAction ask(string input)
        {
            return rt.putStrLn("What is 2 + 2?").
                bind(unused => rt.getLine()).
                bind(checkInput);
        }

        public static IOAction Main =
            rt.putStrLn("Enter your name").
                bind(unused => rt.getLine()).
                bind(name => rt.putStrLn("Hello " + name + ". It's nice to meet you.")).
                bind(unused => rt.putStrLn("OK time for a little test...")).
                bind(ask);
    }
}
