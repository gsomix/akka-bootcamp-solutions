namespace WinTail

open System
open System.IO
open Akkling

[<AutoOpen>]
module FileUtility =
    open Messages

    type FileObserver(tailActor: IActorRef<FileCommand>, filePath: string) =
        let fileDir = Path.GetDirectoryName filePath
        let fileNameOnly = Path.GetFileName filePath
        let mutable watcher = null : FileSystemWatcher

        member this.Start () =
            watcher <- new FileSystemWatcher(fileDir, fileNameOnly)
            watcher.NotifyFilter <- NotifyFilters.FileName ||| NotifyFilters.LastWrite
            watcher.Changed.Add (fun e -> if e.ChangeType = WatcherChangeTypes.Changed then tailActor <! FileWrite(e.Name) else ())
            watcher.Error.Add (fun e -> tailActor <! FileError(fileNameOnly, (e.GetException ()).Message))
            watcher.EnableRaisingEvents <- true

        interface IDisposable with
            member this.Dispose () = watcher.Dispose ()