# fsharp-lucene
Sample application to index documents using lucene.net and fsharp

# Version 0.1
## Extractors
For now, the only extractor defined is with PDF files (using iText). It can index concurrent documents using the --concurrent-extractors command line option.

# Usage
## Create the index
```
dotnet run -- --directory /Data/Documents --index-directory /Data/Index --rebuild --concurrent-extractors 10
```
## Watch for file changes
```
dotnet run -- --directory /Data/Documents --index-directory /Data/Index --watch
```
## Test search
```
dotnet run -- --directory /Data/Documents --index-directory /Data/Index --search 'phrase or word'
```