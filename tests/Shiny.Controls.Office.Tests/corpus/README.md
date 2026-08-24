# Corpus

Drop real `.xlsx` files in this folder. `CorpusRoundTripTests` will open every one of them, make a
single-cell edit, save, and assert that every part of the package the editor did not deliberately
touch comes back byte-identical.

Files here are gitignored on purpose — the corpus is whatever documents you personally cannot afford
to have corrupted, not a fixture set to be shared.

The tests skip silently when the folder is empty, so an empty corpus never fails CI.
