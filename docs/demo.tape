# Records docs/demo.gif with vhs (https://github.com/charmbracelet/vhs):
#
#   dotnet build && vhs docs/demo.tape
#
# The build is done first so the recording opens on the menu rather than on a
# compiler, and QA is skipped because it drives a real browser for a minute.

Output docs/demo.gif

Set Shell bash
Set FontSize 16
Set Width 1100
Set Height 760
Set Padding 20
Set TypingSpeed 60ms

Type "dotnet run --no-build -- --noqa"
Enter
Sleep 5s

# Walk the menu so the highlighted row opens to explain itself, down to the
# voice settings, which is the second row from the bottom.
Down@600ms 4
Sleep 1s
Enter
Sleep 2s

# Pick a voice: page down jumps to the bottom of the list, which is the
# whisper. vhs has no End key, and the menu takes either.
Type "1"
Sleep 500ms
Enter
Sleep 1500ms
PageDown
Sleep 1500ms
Enter
Sleep 2s

# Slow it down and raise the pitch.
Type "2"
Sleep 500ms
Enter
Sleep 1s
Type "-4"
Enter
Sleep 1500ms

Type "3"
Sleep 500ms
Enter
Sleep 1s
Type "6"
Enter
Sleep 1500ms

# Hear it. The right arrow stops the sample if it is still playing and is
# ignored by the menu if it is not, which a digit is not: a digit pressed
# during speech is swallowed as the stop key and the enter after it then
# selects whatever row was highlighted.
Type "5"
Sleep 500ms
Enter
Sleep 8s
Right
Sleep 1s

# Back to the main menu, then read a page out loud.
Escape
Sleep 1500ms
Type "4"
Sleep 500ms
Enter
Sleep 1s
Type "https://github.com/nuiben/talk-bot/blob/main/pengy.md"
Enter

# Firefox takes the best part of a minute to fetch the page, which is nothing
# to watch, so the recording skips it and comes back for the text.
Hide
Sleep 45s
Show
Sleep 8s

# Any key stops the reading. Space would do the stopping too, but the menu
# reads it as select, so it would start the page over.
Right
Sleep 3s

# The mascot's story has no row on the menu any more. Spelling out his name at
# the menu still saves it, and nothing is echoed while it is being typed, so
# the reveal is the line that turns up afterwards.
Type@200ms "pengy"
Hide
Sleep 40s
Show
Sleep 5s

# Escape closes Talk Bot.
Escape
Sleep 3s
