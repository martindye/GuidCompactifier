# GuidCompactifier

This is a utility that compacts a GUID using base64 encoding.
This, of course, has been done before... however, when a GUID is Base64 encoded, only the last 2 x bits of the last Base64 "digit" are used. This leaves 4 bits unused.

Those unused bits are tweaked so that each of them represent the parity of 4 x blocks of bytes.

Of course, there's a 50/50 chance that the parity is still correct but the GUID is incorrect.

This method simply increases the chance that you can throw a parity exception rather than allowing a GUID through.

Note that the last character is the one that's adjusted, so if that's incorrect, then the parity may fail for any part.

When there's a parity issue, an exception is thrown, and the routine tries to identify where the corruption has occurred (you need to use a proportionally spaced font).

Note - if this needs to be used in a web app, some base 64 characters should be swapped for web friendly ones.
