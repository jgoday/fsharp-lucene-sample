namespace DocumentStore

module FileUtils =
    open System.IO

    let fileExtension (path: string) =
        (new FileInfo(path)).Extension

    let fileName (path: string) =
        let fi = new FileInfo(path)
        fi.Name