namespace DocumentStore

module FileWalker =
    open System
    open System.IO

    let private watcher = new FileSystemWatcher()

    let rec private traverseAllFiles (dir: string) fn =
        Directory.EnumerateFiles(dir)
            |> Seq.iter fn
        Directory.EnumerateDirectories(dir)
            |> Seq.iter (fun d -> traverseAllFiles d fn)

    let close() =
        watcher.Dispose()

    let traverse (rootPath: string) fn =
        traverseAllFiles rootPath fn

    let watchForNewFiles (rootPath: string) fn rmFn =
        watcher.Path <- rootPath
        watcher.IncludeSubdirectories <- true
        watcher.EnableRaisingEvents <- true

        watcher.Created.Add (fun e -> fn e.FullPath)
        watcher.Deleted.Add (fun e -> rmFn e.FullPath)

        printfn "FileWalker.watchForNewFiles: start watching %A" rootPath