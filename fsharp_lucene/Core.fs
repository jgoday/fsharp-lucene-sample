namespace DocumentStore

module Core =
    type Config = {
        IndexDirectory: string;
        DocumentsDirectory: string;
    }

    let defaultConfig = { IndexDirectory = ""; DocumentsDirectory = "" }
