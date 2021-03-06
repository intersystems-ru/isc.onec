﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace isc.onec.tcp.async
{   
	internal sealed class BufferManager {
		// This class creates a single large buffer which can be divided up 
		// and assigned to SocketAsyncEventArgs objects for use with each 
		// socket I/O operation.  
		// This enables buffers to be easily reused and guards against 
		// fragmenting heap memory.
		// 
		// This buffer is a byte array which the Windows TCP buffer can copy its data to.

		// the total number of bytes controlled by the buffer pool
		private readonly int bufferBlockLength;

		// Byte array maintained by the Buffer Manager.
		private byte[] bufferBlock;		 
		private Stack<int> freeIndexPool;
		private int currentIndex;
		private int bufferBytesAllocatedForEachSaea;
		
		internal BufferManager(int bufferBlockLength, int totalBufferBytesInEachSaeaObject)
		{
			this.bufferBlockLength = bufferBlockLength;
			this.bufferBytesAllocatedForEachSaea = totalBufferBytesInEachSaeaObject;
			this.freeIndexPool = new Stack<int>();
		}

		// Allocates buffer space used by the buffer pool
		internal void InitBuffer()
		{
			// Create one large buffer block.
			this.bufferBlock = new byte[this.bufferBlockLength];
		}

		// Divide that one large buffer block out to each SocketAsyncEventArg object.
		// Assign a buffer space from the buffer block to the 
		// specified SocketAsyncEventArgs object.
		//
		// returns true if the buffer was successfully set, else false
		internal bool SetBuffer(SocketAsyncEventArgs args) {
			if (this.freeIndexPool.Count > 0) {
				// This if-statement is only true if you have called the FreeBuffer
				// method previously, which would put an offset for a buffer space 
				// back into this stack.
				args.SetBuffer(this.bufferBlock, this.freeIndexPool.Pop(), this.bufferBytesAllocatedForEachSaea);
			}
			else
			{
				// Inside this else-statement is the code that is used to set the 
				// buffer for each SAEA object when the pool of SAEA objects is built
				// in the Init method.
				if (this.bufferBlockLength - this.bufferBytesAllocatedForEachSaea < this.currentIndex)
				{
					return false;
				}
				args.SetBuffer(this.bufferBlock, this.currentIndex, this.bufferBytesAllocatedForEachSaea);
				this.currentIndex += this.bufferBytesAllocatedForEachSaea;
			}
			return true;
		}
	}
}
