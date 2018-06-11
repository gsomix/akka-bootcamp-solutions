namespace WinTail

open System
open Akkling

module Actors =
    type Command = 
    | Start
    | Continue
    | Message of string
    | Exit

    let (|Message|Exit|) (str:string) =
        match str.ToLower() with
        | "exit" -> Exit
        | _ -> Message(str)


    let consoleReaderActor (consoleWriter: IActorRef<string>) (mailbox: Actor<Command>) message = 
        let line = Console.ReadLine ()
        match line with
        | Exit -> stop ()
        | Message(input) -> 
            // send input to the console writer to process and print
            consoleWriter <! input

            // continue reading messages from the console
            mailbox.Self <! Continue

            ignored ()

    let consoleWriterActor (message: string) = 
        let (|Even|Odd|) n = if n % 2 = 0 then Even else Odd
    
        let printInColor color message =
            Console.ForegroundColor <- color
            Console.WriteLine (message.ToString ())
            Console.ResetColor ()

        match message.ToString().Length with
        | 0    -> printInColor ConsoleColor.DarkYellow "Please provide an input.\n"
        | Even -> printInColor ConsoleColor.Red "Your string had an even # of characters.\n"
        | Odd  -> printInColor ConsoleColor.Green "Your string had an odd # of characters.\n"

        ignored ()