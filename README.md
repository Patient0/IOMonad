IOMonad
=======

This is a small program I wrote to clarify in my own mind how the IO monad is implemented in Haskell.

Haskell is a *purely* functional language. This means that functions in Haskell, without exception,
are not allowed to do anything other than return values based on their inputs. A haskell function
cannot:
  * print to the screen
  * read input from the screen
  * do anything that depends on something other than the input that was passed in

How on earth then do Haskell programs actually do anything "in the real world"? How do you write a program
in Haskell which asks questions and gives different answers based on what someone has typed in?

Haskell's answer to this problem is an abstraction called the *IO Monad*.

The IO Monad in Haskell is how you can write "purely functional" programs with no side effects which somehow
can still interact with the outside world.

The way it works is that you have "IO actions", which are actually capable of interacting with the outside world.

You write a purely functional program which composes together a bunch of IOActions - but your code does not
actually *execute* the IO actions. Rather, the Haskell runtime executes them.

I found it hard to imagine how this works in Haskell itself because, of course, Haskell is pure so
it can't actually execute any imperative code.

However, what you can imagine is how a language like C# will "evaluate" an IOAction that has been
given to it.

So this article describes how you might build such a framework in C#.

### Builtin IO actions

So, we start with a bunch of pre-defined functions for performing input and output, which again are
all "IOActions":

```
    interface Runtime
    {
        // An IO action which reads a line of input from the user.
        IOAction getLine();
        // An IO action which writes the specified line of text to the screen.
        IOAction putStrLn(string text);
    }
```

So, for example, "Runtime.putStrLn" is a pure function with no side-effects: it does not actually write
a line of text when you call it. Rather, when it is evaluated outside of the functional program, it will
write a line of text to the screen, *if* it ends up being evaluated at all.

### Binding operations to actions

The next part of the puzzle is that we can bind these IO actions to additional
operations, each of which takes the input from the previous IO action and returns the IO action to be evaluated
next. In C#, this could look like this:

```C#
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

```C#
        public static IOAction Main =
            rt.putStrLn("Enter your name").
                bind(dummy => rt.getLine()).
                bind(name => rt.putStrLn("Hello " + name + ". It's nice to meet you.")).
                bind(dummy => rt.getLine());
```

So basically, you end up writing a purely functional program which builds up an IOAction out of these individual
primitive IOActions. The final IOAction, "Main", is then evaluated by the imperative Haskell runtime to execute the code.

### Haskell equivalent

Here is what the Haskell version of this program looks like:

```Haskell
main = do
        putStrLn "Enter your name"
        name <- getLine
        putStrLn ("Hello " ++ name ++ ". It's nice to meet you.")
```

The way to understand the above code is that this "do" notation is just syntactic sugar
for the following:

```Haskell
main =  putStrLn "Enter your name" >>=
        \_ -> getLine >>=
        \name -> putStrLn $ "Hello " ++ name ++ ". It's nice to meet you."
```

where '>>=' is the equivalent of the 'bind' function in our example, and \x -> expression is the Haskell syntax
for creating an anonymous function taking a parameter "x".

### Result
```
Enter your name
Paul
Hello Paul. It's nice to meet you.
```

### Implementation

How might the actual evaluation of IOActions be implemented? Here's one very simple approach.

We arrange things so that, on the "imperative" side of things, all IOActions are also instances of
'RuntimeAction', which has an imperative "perform" method that actually evaluates the IOAction:


```C#
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
```C#
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

The real "meat" of it all is in the implementation of the bind
method, which returns a CombinedAction. CombinedAction evaluates the first IO action,
passes that result to the next Operation, and then evaluates the IO action returned
from that next operation:

```C#
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

All that's left for our *real* main method to is to finally evaluate the IOAction
of the purely functional part of our program, in "the real world":

```C#
    static void Main(string[] args)
    {
        ((RuntimeAction)FunctionalProgram.Main).perform();
    }

```

### Looping
All types of interactivity are possible in this framework.

Here is a purely functional program which will run forever until the user
answers the question correctly:

```C#
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
```

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

### Haskell equivalent

Here's the Haskell equivalent of the above program:

```Haskell
checkInput input = case (reads input) of
                    [(4, _)] -> (putStrLn "That's the right answer!")
                    _ -> (putStrLn (input ++ " sorry, we're not in Orwell's novel 1984. Please try again...")) >>= ask

ask input = (putStrLn "What is 2 + 2?") >>=
            (\_ -> getLine) >>=
            checkInput

main = (putStrLn "Enter your name") >>=
       (\_ -> getLine) >>=
       (\name -> putStrLn ("Hello " ++ name ++ ". It's nice to meet you.")) >>=
       (\_ -> putStrLn "Ok time for a little test...") >>=
       ask
```

### Final thoughts

Instead of explicitly calling "bind", Haskell has a special syntax, called "do notation", which can be used instead
to give a slightly cleaner syntax for chaining together a bunch of "monad" operations. For example, instead of:

```Haskell
main = putStrLn "Enter your name" >>=
        \_ -> getLine >>=
        \name -> putStrLn $ "Hello " ++ name ++ ". It's nice to meet you."
```

we could have written

```Haskell
main = do
        putStrLn "Enter your name"
        name <- getLine
        putStrLn ("Hello " ++ name ++ ". It's nice to meet you.")
```

which is expanded by the Haskell compiler into the "bind/>>=" version.

Well, C# actually also has its own "Monad" syntax (Linq!).

Also, when I wrote this, for simplicity, I just assumed that the input and output of each IO operation has to be a string.

You could "genericize" all of the above code so that each "IO action" can return any type of object, and also
make use of C#'s Linq syntax to give a cleaner way of combining together each of the IO operations.

In fact, after I wrote this, I've discovered
[this other article] (http://themechanicalbride.blogspot.co.uk/2008/11/haskell-for-c-programmers-part-2.html) which
makes greater use of C#'s Linq syntax and allows for a IO actions that can evaluate to any type.

