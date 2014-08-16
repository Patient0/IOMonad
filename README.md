IOMonad
=======

This is a small program I wrote to clarify in my own mind how the IO monad is implemented in Haskell.

The IO Monad in Haskell is how you can write "purely functional" programs with no side effects which somehow
can still interact with the user.

The way it works is that you have "IO actions", which are actually capable of interacting with the user.
You write a purely functional program which composes together a bunch of IOActions - but your code does not
actually *execute* the IO actions. Rather, the Haskell runtime executes them.

I found it hard to imagine how this works in Haskell itself because, of course, Haskell is pure so
it can't actually execute any imperative code.

However, what you can imagine is how a language like C# will "evaluate" an IOAction that has been
given to it.

### Builtin IO actions

So, we start with a bunch of pre-defined functions for performing input and output, which again are
all "IOActions":

```
    interface Runtime
    {
        // An IO action which reads a line of input from the user.
        IOAction readLine();
        // An IO action which writes the specified line of text to the screen.
        IOAction writeLine(string text);
    }
```

So, for example, "Runtime.writeLine" is a pure function with no side-effects: it does not actually write
a line of text when you call it. It instead returns an IOAction which will write a line of text
to the screen *if* it ends up being evaluated.

### Binding operations to actions

The next part of the puzzle is that we can bind these IO actions to additional
operations, each of which takes the input from the previous IO action and returns the IO action to be evaluated
next. In C#, this could look like this:

```
    internal delegate IOAction Operation(string input);

    interface IOAction
    {
        IOAction bind(Operation operation);
        IOAction wrap(string text);
    }
```

So here, "Operation" is a pure function which takes the result of the previous IO action and then evaluates
to the next IO action that should execute.

### Example

Here is a "purely functional" program which asks a user for their name, and then greets them
using their name:

```
        public static IOAction Main =
            rt.writeLine("Enter your name").
                bind(dummy => rt.readLine()).
                bind(name => rt.writeLine("Hello " + name + ". It's nice to meet you. Press return to exit")).
                bind(dummy => rt.readLine());
```

So basically, you end up writing a purely functional program which builds up an IOAction out of these individual
primitive IOActions. The final IOAction, "Main", is then evaluated by the imperative Haskell runtime to execute the code.

### Looping
All types of interactivity are possible in this framework.

Here is a purely functional program which will run forever until the user
answers the question correctly:

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

### Example dialog:

Here's a transcript from one run of this program:
```
Enter your name
Paul
Hello Paul. It's nice to meet you.
OK time for a little test...
What is 2 + 2?
3
3 sorry, we're not in Orwell's novel 1984. Please try again...
What is 2 + 2?
7
7 sorry, we're not in Orwell's novel 1984. Please try again...
What is 2 + 2?
4
That's the right answer!
```

### Implementation

How might the actual evaluation of IOActions be implemented? Here's one very simple approach.

We arrange things so that, on the "imperative" side of things, all IOActions are also instances of
'RuntimeAction', which has an imperative "perform" method that actually evaluates the IOAction:


```
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
```

RuntimeAction implements IOAction, and provides the "bind" and "wrap"
methods: 
```
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
```

The real "meat" of the implementation is in the implementation of the bind
method, which returns a CombinedAction. CombinedAction evaluates the first IO action,
passes that result to the next Operation, and then evaluates the IO action returned
from that next operation:

```
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
```

All that's left for our *real* main method to is to evaluate the IOAction
of the purely functional part of our program:

```
    static void Main(string[] args)
    {
        ((RuntimeAction)FunctionalProgram.Main).perform();
    }

```

### Conclusion

Instead of explicitly calling "bind", Haskell has a special syntax, called "do notation", which can be used instead
to give a slightly cleaner syntax for chaining together a bunch of "monad" operations.

C# actually also has its own "Monad" syntax (Linq!).

Also, when I wrote this, for simplicity, I just assumed that the input and output of each IO operation has to be a string.

You could "genericize" all of the above code so that each "IO action" can return any type of object, and also
make use of C#'s Linq syntax to give a cleaner way of combining together each of the IO operations.

In fact, after I wrote this, I've discovered
[this other article] (http://themechanicalbride.blogspot.co.uk/2008/11/haskell-for-c-programmers-part-2.html) which
makes greater use of C#'s Linq syntax and allows for a IO actions that can evaluate to any type.

