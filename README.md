# talk-bot
## Overview
CLI application developed in C# that uses Speech Synthesis to convert text inputs into spoken words, offering a straightforward and efficient way to implement text-to-speech functionality.

Customizable Speech Parameters: Allows users to adjust various speech parameters such as speed, pitch, and volume (if applicable).

## Running
```
dotnet run              # runs QA, then opens the menu
dotnet run -- --noqa    # skips QA and goes straight to the menu
dotnet run -- --test    # runs QA only; exit code 0 passed, 1 failed
```
QA drives a real Firefox against real pages, so it takes about a minute and
needs the network. It reports failures and carries on to the menu rather than
holding the program shut, since a page check says nothing about whether the
menu works.

## System Requirements
Operating System: Windows, macOS, Linux
.NET Framework: .NET 10.0 or later
