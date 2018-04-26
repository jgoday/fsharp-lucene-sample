namespace DocumentStore

module FileUtils =
    open System.IO

    let fileName (path: string) =
        let fi = new FileInfo(path)
        fi.Name