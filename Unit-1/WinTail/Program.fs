open System
open Akkling
open Akka.Actor

open WinTail

[<EntryPoint>]
let main argv = 
    // initialize an actor system
    use myActorSystem = System.create "my-system" (Configuration.load())
    
    //SupervisionStrategy used by tailCoordinatorActor
    let strategy () = Strategy.OneForOne((function
        | :? ArithmeticException -> Directive.Resume
        | :? NotSupportedException -> Directive.Stop
        | _ -> Directive.Restart), 10, TimeSpan.FromSeconds(30.))

    let consoleWriterActor = 
        spawn myActorSystem "consoleWriterActor" (props (actorOf Actors.consoleWriterActor))

    let tailCoordinatorActor =
        spawn myActorSystem "tailCoordinatorActor" 
            { props (actorOf2 Actors.tailCoordinatorActor) with
                SupervisionStrategy = Some (strategy ())}

    let fileValidatorActor = 
        spawn myActorSystem "fileValidatorActor" 
            (props (actorOf2 (Actors.fileValidatorActor consoleWriterActor tailCoordinatorActor)))

    let consoleReaderActor = 
        spawn myActorSystem "consoleReaderActor" (props (actorOf2 (Actors.consoleReaderActor fileValidatorActor)))

    // tell the consoleReader actor to begin
    consoleReaderActor <! Actors.Start

    myActorSystem.WhenTerminated.Wait ()
    0