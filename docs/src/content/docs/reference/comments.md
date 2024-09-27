---
title: Comments
---

As in many C-like languages, `//` starts a comment until the end of the line, whereas `/*` and `*/` delimit a block comment.

If a block comment is placed directly before a definition, field, or method declaration, it is considered a documentation comment. The contents of such a comment are parsed and included in the generated documentation.

```bebop
// This is a line comment.

/* This is a block comment. */
struct Point {
    /* the x coordinate */
    uint32 x;
    /* the y coordinate */
    uint32 y;
}
```