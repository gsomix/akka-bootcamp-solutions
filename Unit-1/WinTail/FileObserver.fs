namespace WinTail

open System
open System.IO

type FileUtility =
    static member Watch (fullFilePath: string, onChange: FileSystemEventArgs -> unit, onError: ErrorEventArgs -> unit) =
        let fileDir = Path.GetDirectoryName fullFilePath
        let fileNameOnly = Path.GetFileName fullFilePath
        let notifyFilter = NotifyFilters.FileName ||| NotifyFilters.LastWrite
        let watcher = 
            new FileSystemWatcher(fileDir, fileNameOnly,
                NotifyFilter = notifyFilter,
                EnableRaisingEvents = true)

        watcher.Changed.Add (fun e -> if e.ChangeType = WatcherChangeTypes.Changed then onChange e else ())
        watcher.Error.Add (onError)

        { new IDisposable with
            member this.Dispose () = watcher.Dispose () }