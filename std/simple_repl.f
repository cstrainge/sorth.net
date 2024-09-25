
( Keep track of if we're shutting down. )
true variable! repl_running


( Exit the repl. )
: quit  description: "Exit the interpreter."
        signature: " -- "
    false repl_running !
;


( Alternate ways to exit the interpreter. )
: q  description: "Exit the interpreter."
     signature: " -- "
    quit
;


: exit description: "Exit the interpreter."
       signature: " -- "
    quit
;


( Define a user prompt for the REPL. )
: prompt description: "Prints the user input prompt in the REPL."
    ">> " .
;


( Implementation of the language's REPL. )
: repl description: "Sorth's REPL: read, evaluate, and print loop."
       signature: " -- "

    sorth.version
    "*
       Strange Forth REPL.
       Version: {}

       Enter quit, q, or exit to quit the REPL.
       Enter .w to show defined words.
       Enter show_word <word_name> to list detailed information about a word.

    *"
    string.format .

    begin
        repl_running @
    while
        try
            ( Always make sure we get the newest version of the prompt.  That way the user can )
            ( change it at runtime. )
            cr "prompt" execute

            ( Get the text from the user and execute it.  We are just using a really simple )
            ( implementation of readline for now. )
            term.readline
            code.execute_source

            ( If we got here, everything is fine. )
            "ok" .cr
        catch
            ( Something in the user code failed, display the error and try again. )
            cr .cr
        endcatch
    repeat
;
