open System
open Akkling
open WinTail

[<EntryPoint>]
let main argv = 
    // initialize an actor system
    use myActorSystem = System.create "my-system" (Configuration.load())
    
    let consoleWriterActor = 
        spawn myActorSystem "consoleWriterActor" (props (actorOf Actors.consoleWriterActor))

    let validationActor = 
        spawn myActorSystem "validationActor" (props (actorOf2 (Actors.validationActor consoleWriterActor)))

    let consoleReaderActor = 
        spawn myActorSystem "consoleReaderActor" (props (actorOf2 (Actors.consoleReaderActor validationActor)))

    // tell the consoleReader actor to begin
    consoleReaderActor <! Actors.Start

    myActorSystem.WhenTerminated.Wait ()
    0