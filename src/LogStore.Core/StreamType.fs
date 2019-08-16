namespace LogStore.Core

open System.IO

// 最初的读取文件流。
type internal OriginStream = OriginStream of FileStream

// 写入文件流。
type internal FlushStream = FlushStream of FileStream

// 内存读取流。
type internal ReaderStream = ReaderStream of UnmanagedMemoryStream

// 内存写入流。
type internal WriterStream = WriterStream of UnmanagedMemoryStream

// 写入内存所用的缓存。
type internal BufferStream = Buffer of MemoryStream