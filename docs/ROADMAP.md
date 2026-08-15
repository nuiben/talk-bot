# Roadmap
What Talk Bot might grow into, and the order that makes each step usable on its
own. Nothing here is committed to; it is a map of what is worth building and
what has to exist first.

## Where it stands
Two things in the program as it is decide what can be built next, and both are
worth stating before the list.

Phrases live in a `List<Phrase>` in memory (`PhraseLibrary.cs`) and go away when
the program does, IDs starting again from 1 on the next run. Saving a page as a
phrase, which is what `Add a web page` now does, is worth much less than it
should be while the list cannot outlive the session.

`ISpeechEngine.Speak` returns when the phrase has been read (`SpeechEngine.cs`).
That is the right shape for a phrase somebody typed, and the wrong one for
anything that arrives a piece at a time or has to be interruptible partway
through - which is both of the larger features below.

## Near: the two pieces everything else stands on

### Phrases that survive the program
Phrases and their IDs written to a JSON file and read back at startup.
Newtonsoft.Json is already referenced for Kokoro's voice data, so nothing new is
taken on. Small on its own, and it is what makes a saved page, a saved chat
reply or a dictated note worth saving at all.

### Speech that streams and can be cut off
`SpeakAsync` alongside `Speak`, with a long phrase split on sentence boundaries
so it starts speaking at the first sentence rather than after the last, and any
keypress calling the `Stop` that every backend already implements.

Kokoro synthesizes in chunks already and `KokoroAudioPlayer` has `Play` and
`Stop`, so a good part of this exists and is not yet joined up. Everything below
reads better with it, and the chat feature is unusable without it: a reply that
only speaks once it is complete is a wait in silence followed by a wall of
sound.

## Next: the larger features

### Interactive chat
A conversation rather than a list - the bot answers and speaks the answer. The
Claude API streams deltas, which chunk on sentence boundaries and go straight to
`SpeakAsync`, so the reply begins out loud while the rest is still arriving.
Needs a key kept in config and the streaming piece above; reuses the whole voice
settings screen unchanged, and each reply can be kept as a phrase like any
other.

This is the feature that changes what the program is, which is the argument for
it over the smaller ones - and the argument for doing the two enabling pieces
first rather than around it.

### Speaking to it
Push-to-talk capture feeding Whisper, or the recognizer the system already has,
as another way of answering `Add a phrase` and of taking a turn in a chat. Self
contained and testable against recorded audio, in the way the page tests run
against real pages.

Always-listening and wake words are a different and much larger problem -
silence detection, false triggers, a microphone held open for a session that
lasts all day - and are deliberately not on this map.

## Smaller things, worth their own rows

- **Save speech to a file.** Kokoro produces samples that currently only ever
  reach the player; writing them to a WAV instead makes the bot useful for
  making audio rather than only hearing it.
- **Highlight the word being spoken.** `ConsoleView.Wrap` already lays the text
  out; marking the current word as it is read is cheap on a backend that
  reports boundaries, as SAPI does, and is the kind of thing a terminal does
  well.
- **Playlists.** Queue several phrases, skip and pause between them. Nearly
  free once speech is asynchronous, and the natural home for a page that has
  been split up.
- **A voice per phrase.** A phrase remembers the voice it was saved with, so
  Pengy's story always reads in the same one. A small change to the model and a
  large one to the program's character.
- **Pauses and emphasis.** SSML on the backends that take it. Only the system
  engine does, and there is precedent for a row that says a dial is not on this
  engine - the pitch row does exactly that for Kokoro.
- **Split a long page up.** A fetched page becomes one phrase of several
  thousand characters. Breaking it on its headings into several would make the
  list something that can be navigated rather than one entry that swallows the
  page.

## Order
Persistence, then streaming speech, then chat. Each of the three leaves the
program better than it found it on its own, and by the time chat is started the
plumbing under it is already built and already tested.
