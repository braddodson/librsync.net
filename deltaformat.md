Delta Format
----------------
A delta represents the change between two versions of a file. Given an initial version of a file and a delta, you can compute the updated version of the file.

The delta format contains a list of "commands". There are two main kinds of commands:

- **Literal Commands** - these represent data that should be output to the new version of the file directly
- **Copy Commands** - these point to a range of data from the original version of the file that should be copied into the output

###Header
The Delta Stream begins with a header containing a magic number for the format. This magic number is 0x72730236.

###Command Format
After the header, the file consists of a series of commands.
Each command starts with a single byte indicating the type of command. Each command can contain a few optional parameters of a certain length determined by the command type.

| Command Type Byte | Type of Command | Parameter1 size | Parameter 2 size |
| ------------- |:-------------|:-----|:---------|
| 0 | End of File ||
| 1-64 | Literal | |
| 65 | Literal | 1 byte
| 66 | Literal | 2 bytes
| 67 | Literal | 4 bytes
| 68 | Literal | 8 bytes
| 69 | Copy | 1 byte | 1 byte
| 70 | Copy | 1 byte | 2 bytes
| 71 | Copy | 1 byte | 4 bytes
| 72 | Copy | 1 byte | 8 bytes
| 73 | Copy | 2 bytes | 1 byte
| 74 | Copy | 2 bytes | 2 bytes
| 75 | Copy | 2 bytes | 4 bytes
| 76 | Copy | 2 bytes | 8 bytes
| 77 | Copy | 4 bytes | 1 byte
| 78 | Copy | 4 bytes | 2 bytes
| 79 | Copy | 4 bytes | 4 bytes
| 80 | Copy | 4 bytes | 8 bytes
| 81 | Copy | 8 bytes | 1 byte
| 82 | Copy | 8 bytes | 2 bytes
| 83 | Copy | 8 bytes | 4 bytes
| 84 | Copy | 8 bytes | 8 bytes
| 85-127 | Reserved


For literal commands 1-64, the length of the literal is specified directly in the command type code (so command code 5 means the next 5 bytes of the delta stream should get copied directly to the output).

For the other literal commands, Parameter1 specifies the number of bytes of literal data to copy to the output. So if the command type is 65 and the next byte of the delta stream is 122, then the subsequent 122 bytes of the delta stream should be copied to the output.

The copy commands each have 2 parameters. The first parameter specifies the start point in the source file. The second parameter specifies the number of bytes to copy beginning at that start point. So if the input contains: "69, 5, 120", that command indicates that the result stream should copy 120 bytes starting at position 5 in the source file into the output stream.

The last command in the delta stream should be command code 0 "End of File".
