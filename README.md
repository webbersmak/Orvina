# Orvina

## Introduction

This is a high performance text search utility written in C#/.NET 6. Speed, ease of use, and robustness were all accounted for in the development of Orvina. The current version of Orvina does not support Regular Expressions and by default, only searches for matching text strings (case-insensitive). 

This repository contains 2 projects: 

1. **Orvina.Console**
    - Console Application
    - Builds the application for searching files for text
    - The output will list the files containing the search text and show the lines of the file containing the search text 
    - Example usage:
    
    ```
    orvina.exe "C:\my files" "search text" .cs,.js
    ```
    5. When the search completes, you may enter the (Id) of the file containing matching text to open it 
    
    ![orvina_finished.png](/orvina_finished.png)

2. **Orvina.Engine**
    - Class Library available as a [nuget package](https://www.nuget.org/packages/Orvina.Engine)
    - The only dependency of Orvina.Console
    - Example usage:
    ```
     using (var search = new Orvina.Engine.SearchEngine())
    {
            search.OnError += Search_OnError;
            search.OnFileFound += Search_OnFileFound;
            search.OnSearchComplete += Search_OnSearchComplete;
            search.OnProgress += Search_OnProgress;

            search.Start(searchPath, includeSubdirectories, searchText, fileExtensions);
            
            //TODO
            //wait for the OnSearchComplete event before Disposing
    }
    ```
    
# Under the hood

Orvina.Engine is multithreaded to build the result list as quickly as possible on your machine. While no amount of multithreading can fix a slow disk (I/O reads), care was taken to allow the disk to be as efficient as possible. This is achieved with a clever queuing and locking model that allows files streaming off of the disk to be promptly scanned.

### Directory Thread

These are the first batch of threads that run and use the latest file enumerators available in .NET 6 that call directly into NTDLL.DLL. They recursively scan the root folder and its subfolders. While scanning the folder tree, it also tracks an shared list of files that have the desire file extension(s). Such as .cs or .js.

### File Tractor

The file tractor leverages Asynchronous File I/O built into the OS. It allows file streaming from the disk to be stored as raw bytes in a memory queue. Any available threads will pull from this queue to scan the file for matching "search text".

### Text Search Thread

Threads of this type will read the in-memory file byte stream. They read from the File Tractor queue and convert the byte stream to UTF8 text. The reads are synchronized such that only 1 thread may read a file at a time. 

### Notfification Thread

The notification thread no exists but it originally streamed output to the Console. The "-progress" flag can be used to view the search action. It was decided that having a dedicated thread for Console output was wasted CPU energy that would be better used in the search effort. 
