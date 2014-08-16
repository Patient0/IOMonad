IOMonad
=======
Haskell is a *purely* functional language. This means that functions in Haskell, without exception,
are not allowed to do anything other than return values based on their inputs. A Haskell function
cannot:

  * print to the screen
  * read input typed in by a user
  * do anything other than evaluate something based on the function's inputs

How on earth then do Haskell programs actually do anything "in the real world"? How do you write a program
in Haskell which asks questions and gives different answers based on what someone has typed in?

Haskell's answer to this problem is an abstraction called the *IO Monad*.

This IO Monad concept allows you to write code that is still *purely functional* but which still actually
does stuff, in such a way that the parts which depend on "the outside world" are clearly separated
from the parts that don't.

The way it works is that you have "IO actions", which are actually capable of interacting with the outside world.

You write a purely functional program which combines together a bunch of IOActions - but your code does not
actually *execute* the IO actions. Rather, the Haskell runtime executes them.

I found it hard to imagine how this works in Haskell itself because, of course, Haskell is pure so
it can't actually execute any imperative code. So in pure Haskell, there's a piece of the puzzle which is
implicit.

However, what is easier to reason about is how you might structure a C# program into "functional" and "imperative"
parts such that the functional part is *purely* functional, yet still has full control over everything that happens.

### Builtin IO actions

So, we start with a bunch of pre-defined functions for performing input and output, which again are
all "IOActions":

```C#
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
    }
```

So here, an "Operation" is a pure function which takes the result of the previous IO action and then evaluates
to the next IO action that should execute.

### Example

Here is a "purely functional" program in this framework which asks a user for their name, and then greets them
using their name:

```C#
        public static readonly Runtime rt = RuntimeImpl.Instance;

        public static IOAction Main =
            rt.putStrLn("Enter your name").
                bind(dummy => rt.getLine()).
                bind(name => rt.putStrLn("Hello " + name + ". It's nice to meet you.")).
                bind(dummy => rt.getLine());
```

So basically, you end up writing a purely functional program which builds up an IOAction out of these individual
primitive IOActions. The resultant "Main" IOAction is then going to be evaluated by our imperative "runtime".

### Implementation of the imperative part

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

```
RuntimeAction implements IOAction, providing the actual implementation of the "bind"
method: 

```C#
    abstract class RuntimeAction : IOAction
    {
        public IOAction bind(Operation operation)
        {
            return new CombinedAction(this, operation);
        }

        public abstract string perform();
    }
```

The real "meat" of it all is the CombinedAction class. It evaluates the first IO action,
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

All that's left for our *real* main method to do is to finally evaluate the IOAction
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

Here is what the Haskell version of our first example would look like:

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
        \name -> putStrLn ("Hello " ++ name ++ ". It's nice to meet you.")
```

'>>=' is the name used in Haskell for what we called 'bind'. The reason that they did not just use 'bind' is because
in Haskell, any 'non-alphamnumeric' symbols, while being ordinary functions, are interpreted by the parser
as 'infix' functions, just like '+', '-' and '*'. So just as, to sum two numbers you write:

```
x + y
```

in Haskell, to "bind" an IO action 'x' to an operation 'y', you write:

```
x >>= y
```

If Haskell had actually used the word 'bind' for this binding function, the infix style wouldn't apply
and we would have had much worse syntax. Something like:

```Haskell
main = bind (putStrLn "Enter your name")
            (\_ -> (bind getLine
                         (\name -> putStrLn ("Hello " ++ name ++ ". It's nice to meet you."))))
```

### A scoping/precedence gotcha

I want to bring your attention to something quite important that I think
has been glossed over in all of the Haskell introductions that I have read so far. At least,
I've had a lot of trouble understanding how Haskell monads work until I got this point!

Consider the following Haskell program:

```Haskell
main = do
        putStrLn "Enter your first name"
        first <- getLine
        putStrLn "Enter your last name"
        last <- getLine
        putStrLn ("Hello " ++ first ++ " " ++ last ++ ".")
```

We now know that this is syntactic sugar for:

```Haskell
main = putStrLn "Enter your first name" >>=
       \_ -> getLine >>=
       \first -> putStrLn "Enter your last name" >>=
       \_ -> getLine >>=
       \last -> putStrLn ("Hello " ++ first ++ " " ++ last ++ ".")
```

If you compiled the above program you'd find that everything works perfectly.

Naively follow this logic in our C# framework, you might try something like this:

```C#
        public static IOAction main =
            rt.writeLine("Enter first name").
                bind(dummy => rt.readLine()).
                bind(first => rt.writeLine("Enter last name")).
                bind(dummy => rt.readLine()).
                bind(last => rt.writeLine("Hello " + first + " " + last + "."));
```

Except that you'll find that it does not compile. It will complain that "first" is not in scope.

It comes down to the fact that "->" used to construct an anonymous function in Haskell has lower precedence
than the ">>=" used in the body. So in effect, each of the subsequent "bind" calls ends up grouped
*to the right* rather than to the left.

The correct translation of the Haskell into our C# version is:

```C#
        public static IOAction main =
            rt.writeLine("Enter first name").
            bind(dummy1 => rt.readLine().
            bind(first => rt.writeLine("Enter last name").
            bind(dummy2 => rt.readLine().
            bind(last => rt.writeLine("Hello " + first + " " + last + ".")))));
```

See what I've done? I moved the parentheses so that each function is nested inside its parent. Here is the
same code indented to reflect that:
```C#
        public static IOAction main =
            rt.writeLine("Enter first name").
                bind(dummy1 => rt.readLine().
                    bind(first => rt.writeLine("Enter last name").
                        bind(dummy2 => rt.readLine().
                            bind(last => rt.writeLine("Hello " + first + " " + last + ".")))));
```

Now, each new operation knows all of the contextual information from the preceding operations.
'first' is still in scope when we evaluate '"Hello " + first + " " + last + "."'.

Nearly all Haskell "monadic" code that I have come across implicitly makes use of this in one way or
another.

### Final thoughts
C# actually also has its own version of Haskell's "do" notation - it's called Linq.

As well as using Linq, I could have genericized the IO action type so that each operation could return
any type, not just a string.

To keep things simple, I didn't use "Linq", and I restricted things to just strings.

However, here is an excellent article that explains these concepts using Linq and which makes use of proper generics:
[haskell-for-c-programmers-part-2] (http://themechanicalbride.blogspot.co.uk/2008/11/haskell-for-c-programmers-part-2.html).

