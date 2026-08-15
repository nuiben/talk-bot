# talk-bot
## Overview
CLI application developed in C# that uses Speech Synthesis to convert text inputs into spoken words, offering a straightforward and efficient way to implement text-to-speech functionality.

Customizable Speech Parameters: Allows users to adjust various speech parameters such as speed, pitch, and volume (if applicable).

![Talk Bot demo](docs/demo.gif)

## Voice settings
"Voice settings" on the main menu picks the voice and sets speed, pitch and
volume. Speed and pitch are dials from -10 to 10 with 0 as normal, and volume
runs from 0 to 100, so the same settings mean roughly the same thing whichever
synthesizer is speaking; each backend converts them to its own units. Preview
reads a sample line so a setting can be heard rather than guessed at, and Reset
puts everything back.

The voice list comes from the synthesizer itself: installed voices on Windows,
`say -v ?` on macOS, the English voices and variants on espeak, and the voice
types on speech-dispatcher. Backends that cannot do a dial say so on the
settings screen - festival takes no options, and the macOS voices carry their
own pitch and volume.

## Reading a page
Text fetched from a URL is printed to the console before it is read, so the
page can be seen as well as heard, and is still on the screen once the speech
has finished or been stopped with a key.

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

## Demo
The GIF above is recorded from the real program with
[vhs](https://github.com/charmbracelet/vhs):
```
dotnet build && vhs docs/demo.tape
```

## System Requirements
Operating System: Windows, macOS, Linux
.NET Framework: .NET 10.0 or later
