main = (putStrLn "Enter your name") >>=
        (\_ -> getLine) >>=
        (\name -> putStrLn $ "Hello " ++ name ++ ". It's nice to meet you.")
