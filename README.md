# Orvina

## Introduction

This is a high performance text search application written in C#. Speed, ease of use, and robustness were all accounted for during the development of Orvina. This repository contains 2 projects: 

1. **Orvina.Console**
    - Console Application
    - Builds the application for searching files for text
    - The output will list the files containing the search text and show the lines of the file containing the search text 
    - Example usage:
    
    ```
    orvina.exe "C:\my files" "search text" .cs,js
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

Orvina.Engine is multithreaded to build the result list as quickly as possible on your machine. While no amount of multithreading can fix a slow disk (I/O reads), care was taken to allow the disk to be as efficient as possible. This is achieved with clever use of Monitor.Wait & Monitor.Pulse.

### Directory Thread 

This is the first thread to run and there is only 1 thread of this type. It recursively scans the root folder and its subfolders. While scanning the folder tree, it also tracks a shared list of files that have the desire file extension(s). Such as .cs or .js.

### Search Threads

On a modern machine with many CPU cores, there can be many of these threads. Threads of this type will monitor the shared list, select a file, and attempt a full read of that file. The reads are synchronized such that only 1 thread may read a file at a time. While that sounds like "anti performance", it allows the disk, which is almost always the bottleneck, to focus one job. Once the file contents are in memory, the disk is released, and the Search Thread can run full speed looking for the search string.

In the event that the file is too large to be read into memory, the Search Thread will fall back to a line by line read of the file.

### Notfification Thread

There is 1 thread of this type. The Directory and Search threads load events into a event queue. The events include File Found, Error, Search Complete. The Notification Thread consumes this queue and raises events up to the hosting application. This approach frees the Search Threads from blocking while the hosting application processes the events.
