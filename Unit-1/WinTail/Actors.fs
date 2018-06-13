namespace WinTail

open System
open Akkling
open System.Net.Sockets

module Actors =
    open Messages

    type Command = 
    | Start
    | Continue
    | Exit

    let validationActor (consoleWriter: IActorRef<InputResult>) (mailbox: Actor<string>) (message: string) =
        let sender: IActorRef<Command> = mailbox.Sender()

        let (|EmptyMessage|MessageLengthIsEven|MessageLengthIsOdd|) (msg: string) =
            match msg.Length, msg.Length % 2 with
            | 0, _ -> EmptyMessage
            | _, 0 -> MessageLengthIsEven
            | _, _ -> MessageLengthIsOdd

        let validate (msg: string) = 
            match msg with
            | EmptyMessage -> 
                InputError ("No input received.", ErrorType.Null)
            | MessageLengthIsEven -> 
                InputSuccess ("Thank you! The message was valid.")
            | MessageLengthIsOdd ->
                InputError ("The message is invalid (odd number of characters)!", ErrorType.Validation)

        consoleWriter <! validate message
        sender <! Continue
        
        ignored ()

    let consoleReaderActor (validation: IActorRef<string>) (mailbox: Actor<Command>) (message: Command) = 
        let printInstructions () =
            Console.WriteLine "Write whatever you want into the console!"
            Console.WriteLine "Some entries will pass validation, and some won't...\n\n"
            Console.WriteLine "Type 'exit' to quit this application at any time.\n"

        let processInput () = 
            let line = Console.ReadLine ()
            match line.ToLower () with
            | "exit" -> mailbox.Self <! Exit
            | _ -> validation <! line

        match message with
        | Start -> 
            printInstructions ()
            mailbox.Self <! Continue
        | Continue -> processInput ()
        | Exit -> mailbox.System.Terminate () |> ignore

        ignored ()

    let consoleWriterActor (message: InputResult) =  
        let printInColor color (msg: string) =
            Console.ForegroundColor <- color
            Console.WriteLine msg
            Console.ResetColor ()

        match message with
        | InputError (reason, _) -> printInColor ConsoleColor.Red reason
        | InputSuccess reason -> printInColor ConsoleColor.Green reason

        ignored ()