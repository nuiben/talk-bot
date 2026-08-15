# talk-bot
```
 _______________________________
< Fish sticks first. Then talk. >
 -------------------------------
       \   .--.
        \ |o_o |
          |:_/ |
         //   \ \
        (|     | )
       /'\_   _/`\
       \___)=(___/
```
Pengy, unofficial mascot of Talk Bot. He lives in the freezer aisle of a
supermarket, which is not where penguins are supposed to live, and nobody has
worked up the nerve to tell him. His side of it is in [pengy.md](pengy.md).
His story used to have a row on the menu; it no longer does, but the bot will
still fetch and save it for anyone who thinks to ask him by name.

## Overview
CLI application developed in C# that uses Speech Synthesis to convert text inputs into spoken words, offering a straightforward and efficient way to implement text-to-speech functionality.

Customizable Speech Parameters: Allows users to adjust various speech parameters such as speed, pitch, and volume (if applicable).

![Talk Bot demo](docs/demo.gif)

## Voice settings
`Voice settings` on the main menu picks the voice and sets speed, pitch and
volume. Speed and pitch are dials from -10 to 10 with 0 as normal, and volume
runs from 0 to 100, so the same settings mean roughly the same thing whichever
synthesizer is speaking; each backend converts them to its own units. Preview
reads a sample line so a setting can be heard rather than guessed at, and Reset
puts everything back.

Menus longer than about six rows are walked a window at a time, with a count of
how many rows are above and below it. A menu redraws in place by winding the
cursor back over what it wrote, which only works while all of it is still on
the screen, so a list as long as the voice one used to scroll its own top away
and leave a trail of half-erased menus behind it.

The voice list comes from the synthesizer itself: installed voices on Windows,
`say -v ?` on macOS, the English voices and variants on espeak, and the voice
types on speech-dispatcher. Backends that cannot do a dial say so on the
settings screen - festival takes no options, and the macOS voices carry their
own pitch and volume.

## Kokoro voices
`Engine` on the voice settings screen chooses between the synthesizer this
machine already has and [Kokoro](https://github.com/hexgrad/kokoro), an
82M-parameter neural model that runs here in the process through
[KokoroSharp](https://github.com/Lyrcaxis/KokoroSharp) rather than being shelled
out to. It sounds a great deal better than espeak and takes a good deal longer
to start.

The 157 voices ship with the package, so the list can be walked before anything
has been downloaded. The model cannot: it is about 320MB and is fetched on the
first phrase Kokoro is asked to say, not when the engine is picked, so choosing
it to see what is there costs nothing. It is kept in the user's own data folder
(`~/.local/share/talk-bot` and the equivalent elsewhere) rather than next to the
executable, so it survives a clean build and is fetched once per machine instead
of once per folder the bot is run from. A download that is interrupted leaves
nothing behind to be mistaken for a whole model.

157 voices is far more than a menu can hold, so they are grouped by language and
gender and the group is asked for first. English comes first and Mandarin last,
which is where the v1.1-zh release's hundred numbered voices live.

Kokoro takes a voice, a speed and a volume. It has no pitch of its own - the
voices carry their own - so the pitch dial does nothing there, which the
settings screen says. Speed is a multiplier rather than words a minute, and the
same -10 to 10 dial is scaled onto it between half and double pace.

On Linux the audio goes through OpenAL, which is not part of the package:
`sudo pacman -S openal`, or your distribution's equivalent. Without it Kokoro
says so and reads nothing rather than taking the menu down with it.

## Reading a page
Text fetched from a URL is printed to the console before it is read, so the
page can be seen as well as heard, and is still on the screen once the speech
has finished or been stopped with a key.

## Running
```
dotnet run                    # runs QA, then opens the menu
dotnet run -- --noqa          # skips QA and goes straight to the menu
dotnet run -- --test          # runs QA only; exit code 0 passed, 1 failed
dotnet run -- --test --quick  # the same without the page tests, in a second
```
QA is in two halves. The input tests run first and need nothing installed: they
check what the program does with what it is typed, so they finish in about a
second and are the ones worth running on every change. The page tests then
drive a real Firefox against real pages, which is where the minute and the
network go, and `--quick` leaves them out.

QA reports failures and carries on to the menu rather than holding the program
shut, since a page check says nothing about whether the menu works.

## Handling what gets typed
The input tests in `tests/` cover the answers that are easy to give by
accident, and each one was written against a case the program used to get
wrong:

- a phrase beginning with a dash, which every synthesizer here read as options
  and spoke as nothing at all - the phrase now goes behind `--`
- nothing typed at the phrase prompt, which used to be saved as a phrase of
  silence, and at the end of a piped input, which arrives as no line at all
- a stray keystroke at the URL prompt: `7` cost a minute of Firefox failing to
  reach `https://7`, and is now turned back before a browser starts
- `javascript:`, `file:`, `data:` and `about:` addresses, which a browser will
  open but a reader should not
- `localhost:8080`, whose colon was read as a scheme rather than a port
- numbers past either end of a dial, decimals, words and lines of punctuation,
  none of which may move a setting or end the program
- a menu answer that is not on the menu, which asks again rather than guessing

## Demo
The GIF above is recorded from the real program with
[vhs](https://github.com/charmbracelet/vhs):
```
dotnet build && vhs docs/demo.tape
```

## System Requirements
Operating System: Windows, macOS, Linux
.NET Framework: .NET 10.0 or later
