namespace DocumentStore

module FileUtils =
    open System.IO

    let fileSize (path: string): int64 =
        (new FileInfo(path)).Length

    let md5 (path: string): string =
        use md5 = System.Security.Cryptography.MD5.Create()
        use stream = File.OpenRead(path)
        md5.ComputeHash(stream)
            |> System.BitConverter.ToString
            |> fun s -> s.Replace("-", "").ToLowerInvariant()

    let fileExtension (path: string): string =
        (new FileInfo(path)).Extension

    let fileName (path: string): string =
        let fi = new FileInfo(path)
        fi.Name