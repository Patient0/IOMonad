using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IOMonad
{
    internal delegate IOAction Operation(string input);

    interface IOAction
    {
        IOAction bind(Operation operation);
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

        public static IOAction Main =
            rt.writeLine("Enter your name").
                bind(dummy => rt.readLine()).
                bind(name => rt.writeLine("Hello " + name + ". It's nice to meet you. Press return to exit")).
                bind(dummy => rt.readLine());
    }
}
