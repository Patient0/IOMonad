main = do
        putStrLn "Enter your name"
        name <- getLine
        putStrLn ("Hello " ++ name ++ ". It's nice to meet you.")
