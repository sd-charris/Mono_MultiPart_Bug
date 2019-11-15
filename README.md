## Steps to Reproduce

1. Perform a Multipart/form-data request with multiple parts where the part boundary is followed by a LF not a CRLF.
A sample body shown below:
`--9051914041544843365972754266\nContent-Disposition: form-data; name=\"text\"\n\ntext default\n--9051914041544843365972754266\nContent-Disposition: form-data; name=\"file1\"; filename=\"a.txt\"\nContent-Type: text/plain\n\nContent of a.txt.\n\n--9051914041544843365972754266\nContent-Disposition: form-data; name=\"file2\"; filename=\"a.html\"\nContent-Type: text/html\n\n<!DOCTYPE html><title>Content of a.html.</title>\n\n--9051914041544843365972754266--`

I have attached a sample .net project with the offending class logic in Mono: System.Web.HttpRequest "HelperClass" HttpMultipart.  Also included in the project is the .net Framework 4.8 ReferenceSource implementation (sparing some unnecessary code).  Additionally there is a modified version of the Mono Class with a proposed fix to the bug.

If this bug is acknowledged as an valid issue will submit the fix in a separate PR.

### Current Behavior

Currently each part boundary must be followed by a CRLF. If there is not a CRLF the entire body will be treated as a single part.

### Expected Behavior

Each part boundary should be followed by either a CRLF or LF per .Net Frameworks implementation.

## On which platforms did you notice this

[x] macOS
[x] Linux
[x] Windows

**Version Used**:

6.4.0.198
