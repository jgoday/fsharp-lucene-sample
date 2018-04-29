namespace DocumentStore

open Argu

module Core =
    type Config = {
        IndexDirectory: string;
        DocumentsDirectory: string;
    }

    type ProgramArguments =
        | Directory of path: string
        | Index_Directory of path: string
        | Rebuild
        | Watch
        | Search of term: string
    with
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Directory _ -> "documents directory."
                | Index_Directory _ -> "directory of lucene's index."
                | Rebuild -> "rebuilds all the index."
                | Watch -> "watch for new documents."
                | Search _ -> "search documents by content."

    let defaultConfig = { IndexDirectory = ""; DocumentsDirectory = "" }
