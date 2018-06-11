namespace WinTail

open System
open Akkling
open System.Security.Cryptography

module Actors =
    open Messages

    type Command = 
    | Start
    | Continue
    | Message of InputResult
    | Exit

    // Active pattern matching to determine the charateristics of the message (empty, even length, or odd length)
    let (|EmptyMessage|MessageLengthIsEven|MessageLengthIsOdd|) (msg:string) =
        match msg.Length, msg.Length % 2 with
        | 0, _ -> EmptyMessage
        | _, 0 -> MessageLengthIsEven
        | _, _ -> MessageLengthIsOdd

    // Print instructions to the console
    let doPrintInstructions () =
        Console.WriteLine "Write whatever you want into the console!"
        Console.WriteLine "Some entries will pass validation, and some won't...\n\n"
        Console.WriteLine "Type 'exit' to quit this application at any time.\n"

    let consoleReaderActor (consoleWriter: IActorRef<InputResult>) (mailbox: Actor<Command>) message = 
        let continued () =
            mailbox.Self <! Continue |> ignored

        let getAndValidateInput () = 
            let (|Message|Exit|) (str:string) =
                match str.ToLower () with
                | "exit" -> Exit
                | _ -> Message(str)

            let msg input =
                match input with
                | EmptyMessage -> 
                    Message (InputError ("No input received.", ErrorType.Null))
                | MessageLengthIsEven -> 
                    Message (InputSuccess "Thank you! The message was valid.")
                | MessageLengthIsOdd ->
                    Message (InputError ("The message is invalid (odd number of characters)!", ErrorType.Validation))

            let line = Console.ReadLine ()
            let cmd = 
                match line with
                | Exit -> Exit
                | Message(input) -> msg input

            mailbox.Self <! cmd

        match message with
        | Start -> 
            doPrintInstructions () |> continued
        | Message msg ->
            consoleWriter <! msg |> continued
        | Exit -> 
            mailbox.System.Terminate() |> ignored
        | _ -> 
            getAndValidateInput () |> ignored

    let consoleWriterActor (message: InputResult) =  
        let printInColor color message =
            Console.ForegroundColor <- color
            Console.WriteLine (message.ToString ())
            Console.ResetColor ()

        match message with
        | InputError (reason, _) -> printInColor ConsoleColor.Red reason
        | InputSuccess reason -> printInColor ConsoleColor.Green reason

        ignored ()