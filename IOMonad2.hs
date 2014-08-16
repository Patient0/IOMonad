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
