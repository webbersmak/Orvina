# Orvina - A High Performance Text Search Utility

> ## "No matter how the sands arrange, her path prevails."
> 
> <img src="/orvina.jpg" width=256>


## Introduction

Orvina is a high performance text search utility written in C#/.NET 6. Speed, ease of use, and robustness were all accounted for in the development of Orvina.
Text search allows for basic wildcards like " \* " (0 or more characters) and " \? " (any character). Case sensitivity can be enabled with "-cases" parameter.

This repository contains 3 projects: 

1. **Orvina.Console**
    - Console Application - [Download Exe](https://github.com/webbersmak/Orvina/releases)
    - Targets .NET 6 (Portable Build)
    - The output will list the files containing the search text and show the lines of the file containing the search text 
    - use "orvina.exe -help" to see all options
    - Example usage:
    
    ```
    orvina.exe "C:\my files" "search text" .cs,.js
    ```
    - When the search completes, you may enter the (Id) of the file containing matching text to open it 
    
    ![orvina_finished.png](/orvina_finished.png)

    - Wildcards "\?" and "\*" are supported. So "p?n" will match "pen" and "pin". And "b\*d" will match "bind" and "bound".

2. **Orvina.UI**
    - WinForms UI for Orvina.Engine
    - Targets .NET 6 (win64 binary)
    - Supports Wildcards
    - Example usage:
    
    ![ui.png](/ui.png)

3. **Orvina.Engine**
    - Class Library available as a [nuget package](https://www.nuget.org/packages/Orvina.Engine)
    - The only dependency of Orvina.Console and Orvina.UI
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

### Search Mechanism

Orvina converts your search string to bytes and searches for the matching byte string in your files. For search queries that use "*" wildcards, a small state machine is used to determine if the search text exists in a line of text.  
