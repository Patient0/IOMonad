IOMonad
=======

This is a small program I wrote to clarify in my own mind how the IO monad is implemented in Haskell.

The IO Monad in Haskell is how you can write "purely functional" programs with no side effects which somehow
can still interact with the user.

The way it works is that you have "IO actions", which are actually capable of interacting with the user.
You write a purely functional program which composes together a bunch of IOActions - but your code does not
actually *execute* the IO actions. Rather, the Haskell runtime executes them. It's hard to imagine how
this works in Haskell itself because, of course, Haskell is pure so it can't actually execute any
imperative code.

However, what you can imagine is how a language like C# will "evaluate" an IOAction that has been
given to it.

To purely functional C# code, the IO action might look like this:

```
    internal delegate IOAction Operation(string input);

    interface IOAction
    {
        IOAction bind(Operation operation);
        IOAction wrap(string text);
    }
```

So here, "Operation" is a function which takes the result of the previous IO action and then evaluates
to the next IO action that they should execute.

The way it works is that you can "bind" additional behaviour (I've called it an 'Operation') here
to the results of a previous IO action.

Finally, here are a bunch of pre-defined functions for performing input and output, which again are
all "IOActions":

```
    interface Runtime
    {
        IOAction readLine();
        IOAction writeLine(string text);
        // etc.
    }
```

So, for example, the "readLine" function is a pure function: it does not actually read a line. But it
returns an IOAction which *will* read a line of input from the user, and arrange for that line of
input to be available as an input to the operation that gets "bound" to the next IOAction.

In this framework, here is a "purely functional" program which asks a user for their name, then
says "Hello {name}" back to them:

```
        public static IOAction Main =
            rt.writeLine("Enter your name").
                bind(dummy => rt.readLine()).
                bind(name => rt.writeLine("Hello " + name + ". It's nice to meet you. Press return to exit")).
                bind(dummy => rt.readLine());
```


You write a purely functional program which evaluates to a particular "IO action". This IO action
then 

The way it actually works is that the Haskell runtime, which can actually implement side effects, executes
your "IO" actions.

The key thing that makes it hard to understand, in my opinion, is that you can't actually look at implementation
of the "IO" bind method, (which is what basically makes a Monad a Monad).


The way that this works is that you write a function that has the type "IO". 

WHat happens is that your purely functional program, "main", has to actually be of the type "IO ()".

This then 

How the IOMonad works from an OO perspective
