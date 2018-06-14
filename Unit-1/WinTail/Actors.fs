namespace WinTail

open System
open System.IO
open Akkling
open Newtonsoft.Json

module Actors =
    open Messages
    open FileUtility

    type Command = 
    | Start
    | Continue
    | Exit

    let fileValidatorActor 
        (consoleWriter: IActorRef<InputResult>) 
        (tailCoordinator: IActorRef<TailCommand>)
        (mailbox: Actor<string>) 
        (message: string) =

        let sender: IActorRef<Command> = mailbox.Sender()

        let (|IsFileUri|_|) path = if File.Exists path then Some path else None

        let (|EmptyMessage|Message|) (msg: string) =
            match msg.Length with
            | 0 -> EmptyMessage
            | _ -> Message

        match message with
        | EmptyMessage -> 
            consoleWriter <! InputError("Input was blank. Please try again.\n", ErrorType.Null)
            sender <! Continue
        | IsFileUri _ ->
            consoleWriter <! InputSuccess(sprintf "Starting processing for %s" message)
            tailCoordinator <! StartTail(message, consoleWriter)
        | _ ->
            consoleWriter <! InputError (sprintf "%s is not an existing URI on disk." message, ErrorType.Validation)
            sender <! Continue
        
        ignored ()

    let tailActor (filePath: string) (reporter: IActorRef<InputResult>) (mailbox: Actor<FileCommand>) =
        let fullPath = Path.GetFullPath(filePath)
        let observer = new FileObserver(mailbox.Self, fullPath)
        do observer.Start()

        let fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        let fileStreamReader = new StreamReader(fileStream, Text.Encoding.UTF8)
        let text = fileStreamReader.ReadToEnd ()
        do mailbox.Self <! InitialRead(filePath, text)
        
        let rec loop () = actor {
            let! message = mailbox.Receive ()
            match message with
            | FileWrite _ ->
                let text = fileStreamReader.ReadToEnd ()
                if not (String.IsNullOrEmpty text) then reporter <! InputSuccess text else ()
            | FileError (_, reason) -> reporter <! InputSuccess (sprintf "Tail error: %s" reason)
            | InitialRead (_, text) -> reporter <! InputSuccess text 

            return! loop()
        }

        loop ()

    let tailCoordinatorActor (mailbox: Actor<TailCommand>) (message: TailCommand) =
        match message with
        | StartTail(filePath, reporter) -> 
            spawn mailbox.UntypedContext "tailActor" (props (tailActor filePath reporter)) |> ignore
            ignored ()
        | _ -> ignored ()

    let consoleReaderActor (validation: IActorRef<string>) (mailbox: Actor<Command>) (message: Command) = 
        let printInstructions () =
            Console.WriteLine "Please provide the URI of a log file on disk.\n"

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