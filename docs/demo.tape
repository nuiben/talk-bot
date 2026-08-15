# Records docs/demo.gif with vhs (https://github.com/charmbracelet/vhs):
#
#   dotnet build && vhs docs/demo.tape
#
# The build is done first so the recording opens on the menu rather than on a
# compiler, and QA is skipped because it drives a real browser for a minute.
#
# A GIF carries no sound, so the only ear the speech reaches is whoever is
# making the recording, and it speaks a whole web page. The machine is muted
# for the duration and put back afterwards.

Output docs/demo.gif

Set Shell bash
Set FontSize 16
Set Width 1100
Set Height 760
Set Padding 20
Set TypingSpeed 60ms

# Muting is setup rather than something to watch, so it is done off camera.
# What the sink was set to before is remembered rather than assumed, because
# unmuting a machine that was muted to begin with is its own small rudeness.
# The trap puts it back even if the recording is stopped part way through,
# which the line at the end cannot do on its own.
Hide
Type "PRIOR=$(pactl get-sink-mute @DEFAULT_SINK@ | awk '{print $2}')"
Enter
Type "trap 'pactl set-sink-mute @DEFAULT_SINK@ ${PRIOR:-0}' EXIT"
Enter
Type "pactl set-sink-mute @DEFAULT_SINK@ 1 && clear"
Enter
Show

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

# Wind it up to the top of the dial and raise the pitch.
Type "2"
Sleep 500ms
Enter
Sleep 1s
Type "10"
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
Sleep 3s
Right
Sleep 1s

# Back to the main menu, then read a page out loud.
Escape
Sleep 1500ms
Type "4"
Sleep 500ms
Enter
Sleep 1s
Type "https://github.com/nuiben/talk-bot/blob/main/docs/pengy.md"
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

# Escape closes Talk Bot. Only once it has is there a shell to type at again:
# anything typed before this point goes to the menu, where a line of shell is
# a stream of keystrokes and the enter after it picks whatever row is
# highlighted.
Escape
Sleep 3s

# The sound comes back the way it was found. The trap would do this anyway
# when the shell closes; this is the same thing said out loud, for the case
# where the shell is killed rather than closed.
Hide
Type "pactl set-sink-mute @DEFAULT_SINK@ ${PRIOR:-0}"
Enter
Sleep 500ms
Show
