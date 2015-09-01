# librsync.net

librsync.net is a C# implementation of the rolling-checksum algorithm used by the rsync utility.

librsync.net is intended to be compatible with the librsync library (https://github.com/librsync/librsync/). However, it is a completely new implementation of the algorithm.

This implementation only supports the BLAKE2 hash function. A dll is included to compute this function, based on the code at https://github.com/BLAKE2 
(which is released under a CC0 license).


Usage
-----

To use the library in your app, simply Add a Reference to the dll. The library supports three operations:

**Librsync.ComputeSignature** - this takes an input file and computes a "signature" from it.

**Librsync.ComputeDelta** - this takes a signature and a new version of the file and computes a delta representing the difference between the original and new versions

**Librsync.ApplyDelta** - this takes the old version of a file and applies a delta to give the new version


Example of remote sync
----------------------

To upload a file by differential sync:

1. The client requests the signature of the server's version.

2. The client computes a delta between it's version and the server's signature.

3. The client uploads it's delta. The server applies that delta to it's version, resulting in a replica of the client's version.


Explanation of Algorithm
------------------------

The rsync algorithm is based upon a rolling hash function. When computing a file signature, each block of the file is 
hashed by two hash functions - a "weak" hash function, which uses a rolling checksum algorithm, and a "strong" hash function
which is based on the Blake2 cryptographic hash.

In order to compute a delta between the signature file and a new version, the algorithm computes the rolling checksum of each block
of the input file. If that rolling checksum matches one of the blocks of the original file, and the cryptographic hashes of those
blocks also match, then the algorithm outputs a command to copy that block into the result stream. Otherwise, the algorithm outputs
the next byte of the file, and makes another attempt with the input block shifted over by one. In this way, the algorithm is able
to reuse every block of the original file which still exists in the updated version.

Format Specifications
---------------------

[Delta Format](./deltaformat.md)